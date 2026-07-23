# Azure deployment plan

## Objective

Deploy MvcMovie as a secure, observable ASP.NET Core web application on Azure,
using Azure SQL Database rather than the local SQLite file. This is a plan only:
it does not yet change the application, provision Azure resources, or move data.

## Target architecture

```text
Browser
  |
  v
Azure App Service (ASP.NET Core app, HTTPS, managed identity)
  |                                  |
  |                                  +--> Application Insights / Log Analytics
  v
Azure SQL Database <---- private endpoint / VNet integration
  ^
  |
Azure Key Vault (transitional secret fallback only)
```

Use a single Azure region for all resources. Start with one production
environment and add a separate staging environment before introducing
production releases.

## Azure resources

Use a consistent `<app>-<environment>-<region>` naming convention and deploy
the following resources:

| Resource | Purpose | Suggested initial configuration |
| --- | --- | --- |
| Resource group | Lifecycle boundary for this environment | One per environment |
| App Service plan | Compute for the web app | Linux plan with an always-on capable production SKU |
| App Service | Hosts MvcMovie | HTTPS only, system-assigned managed identity, health check |
| Azure SQL logical server and database | Managed relational database | Separate server/database per environment; zone-redundant option where required |
| Key Vault | Holds a connection string only if a password-based fallback is needed | RBAC enabled, soft delete and purge protection |
| Application Insights and Log Analytics workspace | Application telemetry, logs, alerts | Connected to App Service |
| Virtual network and private endpoints | Private App Service-to-SQL traffic | Required for the hardened production design |

## Phased implementation

### 1. Make the application Azure SQL ready

1. Keep the local SQLite workflow, but treat it as development-only.
2. Replace the production connection-string name with a single documented
   configuration convention, or retain the current
   `ProductionMvcMovieContext` key and configure that exact key in Azure.
3. Add a production health endpoint that verifies the application is running
   without exposing database details.
4. Add structured logging and Application Insights telemetry.
5. Decide whether starter movie data should exist in production. `SeedData`
   exists in the project but is not currently invoked, so it will not populate
   any database unless explicitly wired into a controlled deployment step.

**Acceptance check:** the app runs locally with a SQL Server-compatible
connection string and the production configuration contains no checked-in
secrets.

### 2. Create SQL Server-specific migrations

The checked-in EF Core migrations were generated using the SQLite provider.
They contain SQLite types such as `TEXT` and `INTEGER` and a SQLite
autoincrement annotation, so they are not a safe migration path for Azure SQL.

1. Preserve the existing SQLite migrations for local development.
2. Create a separate SQL Server migration assembly (for example,
   `MvcMovie.SqlServerMigrations`) configured with `UseSqlServer(...,
   sql => sql.MigrationsAssembly(...))`.
3. Generate an initial SQL Server migration from the current `Movie` model.
4. Apply that migration to a disposable Azure SQL development database and
   validate create, edit, search, and delete flows.
5. Adopt a migration bundle or a dedicated CI/CD migration job. Do not call
   `Database.Migrate()` automatically during App Service startup; schema changes
   should be observable, serialized, and independently recoverable.
6. If the existing `MvcMovie.db` has data worth keeping, export and transform
   it, load it into a test Azure SQL database first, validate row counts and
   data types, then repeat the runbook in production.

**Acceptance check:** a clean Azure SQL database can be created entirely from
the SQL Server migrations, and the app passes its smoke tests against it.

### 3. Provision the Azure foundation

Create the resources using infrastructure as code (Bicep or Terraform), with
parameters for environment, region, App Service SKU, SQL performance tier, and
tags. Store the templates in this repository and deploy the same templates to
development, staging, and production.

For the first proof of concept, allow Azure services or the App Service outbound
addresses through the Azure SQL firewall temporarily. Before production, move
to VNet integration for App Service and an Azure SQL private endpoint; disable
public network access to the database after confirming connectivity.

Enable HTTPS-only access, a custom domain and managed certificate when the DNS
name is available, diagnostic settings, daily database backups, and resource
locks appropriate to production.

**Acceptance check:** the App Service can reach the empty Azure SQL database,
while an arbitrary public client cannot.

### 4. Configure passwordless database access

1. Enable the system-assigned managed identity on App Service.
2. Configure a Microsoft Entra administrator for the Azure SQL logical server.
3. Create a contained database user for the App Service managed identity and
   grant only the roles it needs (typically `db_datareader` and `db_datawriter`; grant
   schema-change permissions only to the separate deployment identity).
4. Set `ConnectionStrings__ProductionMvcMovieContext` as an App Service
   application setting using a Microsoft Entra authentication connection string,
   such as:

   ```text
   Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;
   ```

5. If a password must be used temporarily, keep it in Key Vault and reference
   that secret from App Service configuration. Remove the password-based path
   after managed identity access is proven.

**Acceptance check:** the deployed app reads and writes data using its managed
identity, and no SQL password is present in source code, deployment logs, or
App Service settings.

### 5. Build the release pipeline

The pipeline should run on pull requests and on approved environment releases:

1. Restore dependencies, compile, and run automated tests.
2. Publish the application artifact with `dotnet publish -c Release`.
3. Build and validate the SQL Server migration bundle.
4. Deploy infrastructure changes first.
5. Run the migration bundle with the limited migration identity.
6. Deploy the app to an App Service staging slot when the selected SKU supports
   slots; run smoke tests, then swap into production.
7. Verify the health endpoint, error rate, request latency, and Azure SQL
   connection telemetry. Roll back the app slot if those checks fail.

Use GitHub Actions or Azure DevOps with workload identity federation rather
than long-lived Azure credentials. Protect production with approvals and keep
deployment configuration outside the application artifact.

## Operations and security checklist

- Enable Application Insights request, dependency, exception, and availability
  monitoring.
- Alert on failed requests, elevated response times, unavailable health checks,
  Azure SQL CPU/DTU or vCore pressure, and failed database connections.
- Set data-retention and backup/restore requirements before launch; test a
  restore into a non-production database.
- Apply least-privilege Azure RBAC, Microsoft Entra access reviews, and resource
  locks for production resources.
- Turn on Azure SQL auditing and vulnerability assessment as required by the
  organization.
- Enable App Service authentication only if the movie-management UI must be
  restricted; otherwise decide explicitly whether the application is public.
- Keep development, staging, and production databases isolated. Never deploy a
  production connection string to a non-production environment.

## Delivery sequence and decisions needed

| Order | Deliverable | Owner decision needed |
| --- | --- | --- |
| 1 | SQL Server migration project and local validation | Keep or discard existing SQLite data |
| 2 | Bicep/Terraform for development Azure resources | Azure region, naming, tags, budget owner |
| 3 | Managed identity and Azure SQL access | Microsoft Entra administrator and access model |
| 4 | CI/CD pipeline and staging deployment | GitHub Actions or Azure DevOps; release approvers |
| 5 | Production hardening and go-live | Domain, authentication requirement, retention and recovery objectives |

## Definition of done

The deployment is ready when a reproducible pipeline provisions the environment,
applies SQL Server migrations, deploys the app, and verifies the service through
HTTPS. The App Service connects privately to Azure SQL using a managed identity,
telemetry and alerts are active, a database restore has been tested, and no
secrets are committed to the repository.

## References

- [Azure App Service managed identities](https://learn.microsoft.com/azure/app-service/overview-managed-identity)
- [Connect App Service to Azure SQL using managed identity](https://learn.microsoft.com/azure/app-service/tutorial-connect-msi-azure-database)
- [App Service VNet integration](https://learn.microsoft.com/azure/app-service/overview-vnet-integration)
- [Applying EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)
