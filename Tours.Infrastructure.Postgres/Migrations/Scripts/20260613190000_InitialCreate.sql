create table if not exists activities (
    "Id" uuid primary key,
    "Name" character varying(200) not null,
    "Description" character varying(1000) not null,
    "Price" numeric(10, 2) not null,
    "DurationMinutes" integer not null,
    "Location" character varying(200) not null,
    "Destination" character varying(120) not null,
    "IsAvailable" boolean not null,
    "Reviews" jsonb not null,
    "Photos" jsonb not null,
    "Category" integer not null,
    minimum_age integer not null,
    difficulty integer not null,
    "IncludesTransport" boolean not null,
    "IncludesGuide" boolean not null,
    "ProviderName" character varying(200) not null,
    "IsIndoorAlternative" boolean not null
);

create table if not exists itineraries (
    "Id" uuid primary key,
    "Destination" character varying(120) not null,
    "StartDate" date not null,
    "EndDate" date not null,
    "NumberOfPeople" integer not null,
    "Ages" jsonb not null,
    "Preferences" jsonb not null,
    "Budget" numeric(12, 2) not null,
    "TotalPrice" numeric(12, 2) not null,
    "Items" jsonb not null
);
