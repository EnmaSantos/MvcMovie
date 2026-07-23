# MvcMovie

MvcMovie is an ASP.NET Core MVC sample application for creating, browsing,
editing, searching, and deleting movie records. It also includes a live movie
discovery experience powered by TMDB, with optional OMDb detail enrichment.

## Technology

- .NET 10 / ASP.NET Core MVC
- Entity Framework Core 10
- SQLite for local development
- Microsoft SQL Server / Azure SQL Database for deployed environments
- TMDB and OMDb server-side API integrations

## Run locally

Prerequisites: the .NET 10 SDK.

```bash
dotnet restore
dotnet run
```

The development profile uses SQLite and stores data in `MvcMovie.db`. The app
will be available at the HTTPS or HTTP address printed after it starts (the
default profiles use `https://localhost:7030` and `http://localhost:5245`).
When the local catalog is empty, development mode adds the sample shelf entries
from `Models/SeedData.cs`.

## Movie API setup

Copy `.env.example` to `.env` and add your API credentials:

```bash
cp .env.example .env
```

`TMDB_API_READ_KEY` is preferred; `TMDB_API_KEY` is supported as a fallback.
`OMDB_API_KEY` is optional and adds credits, awards, and box-office details to
film profiles. The application loads these values on the server. `.env` is
ignored by Git and API credentials are never intentionally rendered into HTML
or browser-side JavaScript.

## Configuration

`Program.cs` intentionally uses a different database provider by environment:

| Environment | Provider | Connection-string configuration key |
| --- | --- | --- |
| `Development` | SQLite | `ConnectionStrings:MvcMovieContext` |
| Any other environment | SQL Server | `ConnectionStrings:ProductionMvcMovieContext` |

For a local test against SQL Server or for an Azure App Service setting, the
second key can be expressed as this environment variable:

```text
ConnectionStrings__ProductionMvcMovieContext=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;
```

Do not put real connection strings, passwords, or other secrets in
`appsettings.json` or source control. In Azure, use a managed identity with
Azure SQL authentication; see the [Azure deployment plan](docs/azure-deployment-plan.md).

## Database migrations

The existing migrations target SQLite. They are suitable for the local database
only and must **not** be applied directly to Azure SQL Database: they include
SQLite-specific column types and annotations. Before deploying, create and
validate a SQL Server migration set as described in the deployment plan.

## Project layout

- `Controllers/` — MVC request handlers
- `Models/` — movie entities, validation, and view models
- `Views/` — Razor UI
- `Data/MvcMovieContext.cs` — Entity Framework Core context
- `Migrations/` — existing SQLite migrations
- `docs/azure-deployment-plan.md` — proposed Azure implementation plan

## Azure deployment

The intended production architecture is Azure App Service hosting this web app,
Azure SQL Database for persistence, and Application Insights for observability.
Azure Key Vault is included only as a temporary fallback if a password-based
database connection is unavoidable. The phased implementation plan, including
identity, networking, migrations, and release checks, is in
[docs/azure-deployment-plan.md](docs/azure-deployment-plan.md).
