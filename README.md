# Tours MCP - Tour Operador (.NET 10, DDD)

Solucion con arquitectura DDD para operar vacaciones como tour operador.
Incluye:

- API de actividades con datos completos y filtros.
- API de itinerarios personalizados por destino, fechas, preferencias y presupuesto.
- Evaluacion de clima por actividad con Open-Meteo y sugerencias alternativas.
- MCP con dos transportes: `http` y `stdio`.
- Persistencia en PostgreSQL con EF Core y migraciones.
- Siembra automatica de 120 actividades con combinaciones variadas.

## Arquitectura

- `Tours.Domain`: entidades, enums e interfaces compartidas.
- `Tours.Application`: casos de uso y servicios de negocio.
- `Tours.Infrastructure.Postgres`: EF Core, repositorios, migraciones y seeding.
- `Tours.Infrastructure.Weather`: integracion Open-Meteo.
- `Tours.Api`: API REST con controladores y `ActionResult`.
- `Tours.Mcp`: servidor MCP (`http` y `stdio`).

## Requisitos

- .NET SDK 10
- PostgreSQL (ejemplo por defecto: `localhost:5432`, db `tours`, user `postgres`, pass `postgres`)

## Configuracion

Connection string en:

- `Tours.Api/appsettings.json`
- `Tours.Mcp/appsettings.json`

Clave: `ConnectionStrings:Postgres`

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

Ya incluye migracion inicial en `Tours.Infrastructure.Postgres/Migrations`.

Comando para nuevas migraciones:

```powershell
cd d:\temporal\Tours

dotnet ef migrations add NombreMigracion --project .\Tours.Infrastructure.Postgres\Tours.Infrastructure.Postgres.csproj --startup-project .\Tours.Api\Tours.Api.csproj --output-dir Migrations
```

## Ejecutar MCP por HTTP

```powershell
cd d:\temporal\Tours

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

## Logica climatica (Open-Meteo)

- Evaluacion diaria por actividad: probabilidad de lluvia y codigo meteorologico.
- Si riesgo alto, marca la actividad y sugiere alternativa indoor.
- Para rangos de 14 a 16 dias:
  - promedio ultimos 14 dias
  - historico del mismo rango para los 3 anios anteriores
  - combinacion con forecast para score hibrido
