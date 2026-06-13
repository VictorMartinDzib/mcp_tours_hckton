# Tours MCP - Tour Operador (.NET 10, DDD)

Solucion con arquitectura DDD para operar vacaciones como tour operador.
Incluye:

- API de actividades con datos completos y filtros.
- API de itinerarios personalizados por destino, fechas, preferencias y presupuesto.
- Evaluacion de clima por actividad con Open-Meteo y sugerencias alternativas.
- MCP con dos transportes: `http` y `stdio`.
- Persistencia en PostgreSQL con Dapper y migraciones SQL.
- Siembra automatica de 120 actividades con combinaciones variadas.

## Arquitectura

- `Tours.Domain`: entidades, enums e interfaces compartidas.
- `Tours.Application`: casos de uso y servicios de negocio.
- `Tours.Infrastructure.Postgres`: Dapper, repositorios, migraciones y seeding.
- `Tours.Infrastructure.Weather`: integracion Open-Meteo.
- `Tours.Api`: API REST con controladores y `ActionResult`.
- `Tours.Mcp`: servidor MCP (`http` y `stdio`).

## Requisitos

- .NET SDK 10
- PostgreSQL (ejemplo por defecto: `localhost:5432`, db `tours`, user `postgres`, pass `postgres`)

## Configuracion

Connection string en:

- `Tours.Api/appsettings.json`

Clave: `ConnectionStrings:Postgres`

Configuracion MCP en:

- `Tours.Mcp/appsettings.json`

Claves:

- `Mcp:Transport` (`http` o `stdio`)
- `Mcp:ApiBaseUrl` (URL base de `Tours.Api`)

Para inicializar PostgreSQL con Docker:

```powershell
cd d:\temporal\Tours\Tours.Api

docker compose up -d postgres
```

## Ejecutar API

```powershell
cd d:\temporal\Tours

dotnet build Tours.slnx

dotnet run --project .\Tours.Api\Tours.Api.csproj
```

Endpoints principales:

- `GET /api/activities`
- `GET /api/activities/{id}`
- `POST /api/itineraries/generate`
- `GET /api/itineraries/{id}`
- `PUT /api/itineraries/{id}/replace-activity`
- `POST /api/itineraries/{id}/add-activity`

### Ejemplo generar itinerario

```json
{
  "destination": "Cancun",
  "startDate": "2026-08-10",
  "endDate": "2026-08-15",
  "numberOfPeople": 2,
  "ages": [34, 31],
  "preferences": ["Adventure", "Gastronomic"],
  "budget": 1200
}
```

## Migraciones

Las migraciones SQL viven en `Tours.Infrastructure.Postgres/Migrations/Scripts` y se aplican automaticamente al iniciar API/MCP.

Para una nueva migracion:

1. Crea un archivo con prefijo de version ordenable, por ejemplo: `20260614000100_AddIndexes.sql`.
2. Agrega SQL idempotente (`create table if not exists`, `alter table ... add column if not exists`, etc.).
3. Reinicia API o MCP para aplicar scripts pendientes.

## Ejecutar MCP por HTTP

```powershell
cd d:\temporal\Tours

dotnet run --project .\Tours.Api\Tours.Api.csproj

dotnet run --project .\Tours.Mcp\Tours.Mcp.csproj
```

Endpoint MCP HTTP:

- `POST /mcp`

Metodos soportados:

- `tools/list`
- `tools/call`:
  - `tour_operator.generate_itinerary`
  - `tour_operator.replace_activity`
  - `tour_operator.add_activity`

## Ejecutar MCP por STDIO

```powershell
cd d:\temporal\Tours

dotnet run --project .\Tours.Api\Tours.Api.csproj

$env:MCP_TRANSPORT = "stdio"
dotnet run --project .\Tours.Mcp\Tours.Mcp.csproj
```

En `tour_operator.generate_itinerary`, si faltan campos, responde `needs_input` solicitando:

- destino
- fecha inicio y fin
- numero de personas
- edades
- preferencias
- presupuesto

## Probar MCP con MCP Inspector

Requisito: Node.js 18+ (para usar `npx`).

### Opcion 1: Inspeccionar MCP por STDIO

```powershell
cd d:\temporal\Tours

$env:MCP_TRANSPORT = "stdio"
dotnet build .\Tours.Mcp\Tours.Mcp.csproj
npx @modelcontextprotocol/inspector dotnet run --no-build --no-launch-profile --project .\Tours.Mcp\Tours.Mcp.csproj
```

Esto abre el Inspector en el navegador y levanta el servidor MCP como proceso hijo por `stdio`.

### Opcion 2: Inspeccionar MCP por HTTP

1. Inicia el servidor MCP:

```powershell
cd d:\temporal\Tours

$env:MCP_TRANSPORT = "http"
dotnet run --project .\Tours.Mcp\Tours.Mcp.csproj
```

2. En otra terminal, abre MCP Inspector:

```powershell
npx @modelcontextprotocol/inspector
```

3. En la UI de Inspector, conecta con:

- Transport: `Streamable HTTP`
- URL: `http://localhost:5111/mcp`

Una vez conectado, puedes usar `tools/list` y luego `tools/call` para probar:

- `tour_operator.generate_itinerary`
- `tour_operator.replace_activity`
- `tour_operator.add_activity`

## Logica climatica (Open-Meteo)

- Evaluacion diaria por actividad: probabilidad de lluvia y codigo meteorologico.
- Si riesgo alto, marca la actividad y sugiere alternativa indoor.
- Para rangos de 14 a 16 dias:
  - promedio ultimos 14 dias
  - historico del mismo rango para los 3 anios anteriores
  - combinacion con forecast para score hibrido
