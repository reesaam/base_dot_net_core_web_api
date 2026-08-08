# BaseWebApi — Modular .NET 10 WebAPI Starter

Production-oriented Clean Architecture starter with **REST + gRPC** (code-first, auto-generated `.proto`), shared DTOs, JWT, EF Core, Redis, Kafka, MinIO, Elasticsearch, Serilog, Docker, and GitLab CI/CD.

## Solution layout

```
BaseWebApi.sln
BaseWebApi.Api/             REST controllers, gRPC hosts, middleware, Docker
BaseWebApi.Application/     DTOs ([ProtoContract]), services, validators, [Service] contracts
BaseWebApi.Domain/          Entities, aggregates, events, enums
BaseWebApi.Infrastructure/  EF, Redis, Kafka, MinIO, Elastic, JWT, proto generation
BaseWebApi.Shared/          Results, exceptions, extensions, JWT/file helpers
scripts/Rename-Solution.ps1 Safe full-solution rename
```

## Quick start

```bash
dotnet restore BaseWebApi.sln
dotnet build BaseWebApi.sln
dotnet run --project BaseWebApi.Api/BaseWebApi.Api.csproj --launch-profile https
```

Development uses **local fallbacks by default** (`Infrastructure:UseLocalFallbacks=true`), so the API starts without SQL Server, Redis, Kafka, MinIO, or Elasticsearch (in-memory DB/cache + no-op integrations). You should see a startup warning confirming that mode.

When real services are ready, set `"UseLocalFallbacks": false` or use Staging/Production / Docker Compose.

- REST / Scalar: `https://localhost:5001/scalar`
- OpenAPI JSON: `https://localhost:5001/openapi/v1.json`
- Health: `https://localhost:5001/health`
- Dev JWT: `POST /api/auth/token` `{ "email": "dev@example.com", "roles": ["User"] }`

## Rename the solution (safe)

Manual renames often break `.csproj` `ProjectReference` paths, namespaces, Docker/CI paths, and leave stale `bin/`/`obj/` artifacts. Use the script instead of renaming folders by hand.

### Complete command

From the repo root (PowerShell):

```powershell
# Preview (no changes)
.\scripts\Rename-Solution.ps1 -NewName MyCrm -WhatIf

# Apply rename: BaseWebApi -> MyCrm (auto-detects current name from *.sln)
.\scripts\Rename-Solution.ps1 -NewName MyCrm

# Explicit old/new names
.\scripts\Rename-Solution.ps1 -OldName BaseWebApi -NewName AcmeErp
```

One-liner (close the IDE first, then run):

```powershell
Set-ExecutionPolicy -Scope Process Bypass; .\scripts\Rename-Solution.ps1 -NewName MyCrm
```

### What the script updates

1. Deletes `bin/`, `obj/`, `.vs/` (avoids locked/stale assemblies)
2. Replaces `OldName` / lowercase tokens in source, `.csproj`, Docker, CI, README, protos, appsettings
3. Renames `OldName.*.csproj` → `NewName.*.csproj`
4. Renames project folders `OldName.*` → `NewName.*`
5. Renames `OldName.sln` → `NewName.sln`
6. Recreates solution project entries with `dotnet sln add`
7. Runs `dotnet restore` + `dotnet build -c Release` to verify

After rename:

```powershell
dotnet run --project MyCrm.Api/MyCrm.Api.csproj --launch-profile https
docker compose -f MyCrm.Api/docker-compose.yml up --build
```

### Rules

- Use a single root identifier only: `MyCrm` (not `MyCrm.Application`)
- Allowed characters: letters, digits, underscore (`^[A-Za-z_][A-Za-z0-9_]*$`)
- Close Visual Studio / Rider / Cursor solution tabs before renaming to avoid file locks
- Do not rename only one project folder — keep `Shared`, `Domain`, `Application`, `Infrastructure`, `Api` in sync

## gRPC (code-first + auto proto)

1. Annotate DTOs with `[ProtoContract]` / `[ProtoMember]`.
2. Annotate service interfaces with `[Service]` (`ProtoBuf.Grpc.Configuration`).
3. Implement in `BaseWebApi.Api/Grpc/Services`.
4. On Development startup (or `GENERATE_PROTOS=true`), `ProtoSchemaGenerator` writes `.proto` files to `BaseWebApi.Api/Grpc/Generated/` via `protobuf-net.Grpc.Reflection`.
5. `protobuf-net.BuildTools` is referenced for build-time protobuf analyzers/generators.

gRPC-Web is enabled for browser clients.

## Docker

```bash
docker compose -f BaseWebApi.Api/docker-compose.yml up --build
```

Ports: REST `6644`, gRPC `5001`, SQL `1433`, Redis `6379`, MinIO `9000/9001`, Elastic `9200`, Kafka `9092`.

## Environments

| Environment   | Config                         | Launch profile |
|---------------|--------------------------------|----------------|
| Development   | `appsettings.Development.json` | `https` / `http` |
| Staging       | `appsettings.Staging.json`     | `Staging` |
| Production    | `appsettings.Production.json`  | `Production` |

Override secrets with environment variables (`SqlServer__ConnectionString`, `Jwt__SigningKey`, etc.).

## Extending

- Clone `Item` entity / `ItemsController` / `IItemGrpcService` for new modules.
- Register services in `AddApplication()` / `AddInfrastructure()`.
- Add EF configurations under `BaseWebApi.Infrastructure/Persistence/Configurations`.
- Oracle access is stubbed in `OracleRepository` (Dapper placeholder).

## CI/CD

See `.gitlab-ci.yml` — stages: `build` → `test` → `publish` → `deploy` (`deploy_dev`, `deploy_stage`, `deploy_prod`).
