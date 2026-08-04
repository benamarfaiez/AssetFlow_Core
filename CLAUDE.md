# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Contexte

API .NET 8 de gestion de parc informatique (assets, équipes, tickets de maintenance) en Clean Architecture / DDD / CQRS, avec assistance IA (RAG) asynchrone. Le code, les commentaires, les messages d'exception, les logs et les messages de commit sont **en français** — conserver cette convention.

## Commandes

Le fichier solution est au format **`.slnx`** : il faut un SDK ≥ 9.0.200 pour le résoudre, même si tous les projets ciblent `net8.0` (SDK installés localement : 9.0.315 et 10.0.302).

```powershell
# Build (Debug par défaut ; la CI compile en Release)
dotnet build AssetFlowCore.slnx

# Vérification de format — GATE bloquant de la CI (job "build")
dotnet format AssetFlowCore.slnx --verify-no-changes --severity warn
dotnet format AssetFlowCore.slnx --severity warn          # applique les corrections

# Tests (3 projets distincts, jamais lancés via la solution en CI)
dotnet test AssetFlowCore.UnitTests/AssetFlowCore.UnitTests.csproj                 # ~176 tests
dotnet test AssetFlowCore.IntegrationTests/AssetFlowCore.IntegrationTests.csproj
dotnet test AssetFlowCore.ArchitectureTests/AssetFlowCore.ArchitectureTests.csproj

# Un seul test / une seule classe
dotnet test AssetFlowCore.UnitTests/AssetFlowCore.UnitTests.csproj --filter "FullyQualifiedName~CreateMaintenanceTicketHandlerTests"
dotnet test AssetFlowCore.UnitTests/AssetFlowCore.UnitTests.csproj --filter "FullyQualifiedName~AssetTests.MarkAsDown_WhenDecommissioned_ShouldThrowDomainException"

# Benchmarks — BenchmarkDotNet REFUSE de tourner en Debug
dotnet run --project AssetFlowCore.Benchmarks -c Release
dotnet run --project AssetFlowCore.Benchmarks -c Release -- --filter "*CachedRepository*"

# Exécution locale : passer par l'AppHost Aspire (il démarre SQL Server en conteneur)
dotnet run --project AssetFlowCore.Aspire/AssetFlowCore.Aspire.AppHost
# Prérequis : Docker + le secret utilisateur du mot de passe SQL, déclaré côté AppHost
dotnet user-secrets set "Parameters:sqlserver-password" "<motdepasse>" --project AssetFlowCore.Aspire/AssetFlowCore.Aspire.AppHost

# Migrations EF Core (assembly de migrations = AssetFlowCore.Infrastructure)
dotnet ef migrations add <Nom> --project AssetFlowCore.Infrastructure --startup-project AssetFlowCore.WebApi
dotnet ef database update --project AssetFlowCore.Infrastructure --startup-project AssetFlowCore.WebApi
```

`Program.cs` **n'applique aucune migration au démarrage** : la base doit être migrée manuellement (ou recréée) avant de faire tourner l'API. La migration `SeedReferenceTeams` amorce les 9 équipes de référence (3 types d'actifs × 3 criticités) sans lesquelles toute création de ticket échoue — `dotnet ef database update` est donc indispensable sur une base neuve.

## Structure des couches et règles imposées par les tests

`Domain` ← `Application` ← `Infrastructure` / `WebApi`. Ces règles ne sont pas seulement conventionnelles : `AssetFlowCore.ArchitectureTests` (ArchUnitNET) les vérifie et **casse la CI** en cas d'écart :

- `Domain` ne dépend de rien (ni Application, ni Infrastructure, ni WebApi) ; `Application` ne dépend que de `Domain` ; `Infrastructure` ignore `WebApi`.
- Les propriétés des entités de `Domain.Entities` n'ont **ni setter public ni setter protected** (mutation uniquement via méthodes métier).
- Toute classe nommée `*Handler` réside dans `AssetFlowCore.Application.*`.
- Les propriétés des types `*Command` / `*Query` sont immuables.
- Les interfaces de `Domain` et `Application` commencent par `I`.
- `WebApi` ne dépend d'aucun type `*Repository` (les controllers passent obligatoirement par MediatR).

## Chemin d'une requête

`Controller (ISender)` → `Command`/`Query` → `ValidationBehavior` (FluentValidation) → `Handler` → entités du domaine + repositories → `IUnitOfWork.SaveChangesAsync()` → notification SignalR → DTO.

- Les controllers n'injectent que `ISender` ; chaque endpoint construit son `Command`/`Query` depuis un `Requests/*Request`, et reçoit un `CancellationToken` propagé jusqu'aux dépôts — toute nouvelle méthode asynchrone de dépôt doit accepter et propager ce jeton. Exceptions assumées : la notification SignalR et la mise en file de l'analyse IA, postérieures à la persistance, ne sont pas annulables.
- Les handlers sont enregistrés **deux fois** dans [DependencyInjection.cs](AssetFlowCore.Application/DependencyInjection.cs) : par le scan MediatR (chemin réel de l'API) et explicitement en `AddScoped` (les benchmarks les résolvent directement depuis le conteneur). Ajouter un handler implique de mettre à jour l'enregistrement explicite et, si besoin, [BenchmarkBase.cs](AssetFlowCore.Benchmarks/BenchmarkBase.cs).
- Le mapping DTO est **manuel** ([MappingExtensions.cs](AssetFlowCore.Application/DTOs/MappingExtensions.cs)) — choix assumé de performance, ne pas introduire AutoMapper.
- [ExceptionHandlingMiddleware.cs](AssetFlowCore.WebApi/Middlewares/ExceptionHandlingMiddleware.cs) traduit les exceptions en `ProblemDetails` : `ValidationException` / `ArgumentException` / `DomainException` → 400, `DbUpdateConcurrencyException` → 409 (via `MaintenanceTicket.RowVersion`), reste → 500. Les handlers lèvent donc des exceptions plutôt que de retourner des résultats d'erreur.

## Moteur d'assignation (Strategy) — pièges

[TicketAssignmentEngine.cs](AssetFlowCore.Application/Services/TicketAssignmentEngine.cs) prend la **première** `IAssignmentStrategy` dont `IsMatch(assetType, criticality)` répond `true` : l'ordre d'enregistrement dans [DependencyInjection.cs](AssetFlowCore.Application/DependencyInjection.cs#L29-L32) fait office de priorité, avec repli explicite sur `LaptopStandardStrategy`.

`Team.AssetType` et `Team.TicketCriticality` sont des **`string`**, pas les enums du domaine ; la résolution compare `assetType.ToString()` / `criticality.ToString()` en base. Conséquence : sans équipe correspondant au couple `(AssetType, TicketCriticality)` en base, `AssignmentStrategyBase.GetTeamNameAsync` lève une `DomainException` et la création de ticket échoue. Toute nouvelle stratégie exige donc aussi le seed des équipes correspondantes — les 9 combinaisons actuelles sont amorcées par la migration `SeedReferenceTeams`, dont les noms doivent rester **uniques** (index `IX_t_teams_name`).

## Cache : décorateurs autour des repositories

`IAssetRepository` et `ITeamRepository` sont résolus vers `CachedAssetRepository` / `CachedTeamRepository`, qui décorent les implémentations EF concrètes (enregistrées en tant que types concrets) avec `IMemoryCache` (expiration absolue 5 min). En ajoutant une méthode d'écriture sur un repository, il faut **invalider les clés correspondantes dans le décorateur** (clés partagées dans [CacheKeys.cs](AssetFlowCore.Infrastructure/Cache/CacheKeys.cs)), sinon les lectures servent des données périmées.

`UnitOfWork` **reçoit ses dépôts du conteneur** : les écritures passant par lui traversent donc les décorateurs. Les mutations d'entités suivies qui n'appellent aucune méthode de dépôt (`asset.Decommission()`, `asset.MarkAsDown()`) sont détectées via le `ChangeTracker` dans `SaveChangesAsync`, qui invalide alors les listes concernées.

Le cache d'actifs porte **uniquement sur la liste** : `CachedAssetRepository.GetByIdAsync` délègue sans mise en cache, car tous ses appelants mutent l'actif retourné — servir une instance détachée du `DbContext` courant ferait échouer silencieusement la persistance. Même précaution à conserver pour toute nouvelle lecture destinée à une mutation.

`TeamRepository.UpdateAsync`/`RemoveAsync` branchent sur `ProviderName` : `ExecuteUpdate`/`ExecuteDelete` sur un provider relationnel, repli `Attach` + `EntityState.Modified` pour InMemory. Les tests exercent donc un chemin de code différent de la production — valider les changements de ces méthodes aussi en intégration/benchmark.

## Assistance IA (RAG)

Flux : `CreateMaintenanceTicketHandler` met le `ticketId` dans `AIAssistanceQueue` (Channel en singleton) → `AIAssistanceWorker` (`BackgroundService`) → embedding de la description → recherche cosinus DuckDB (`LocalVectorStore`, `topK: 3`, seuil `0.7`) → génération de la note Markdown via Semantic Kernel → `ticket.SetAssistanceNote(...)` ou `ticket.FailAiProcessing()` (les deux sortent de l'état `IsAiProcessing`, positionné à `true` par le constructeur du ticket).

- **Bascule de provider** : `AiSettings:UseAzure` (bool) dans [Infrastructure/DependencyInjection.cs](AssetFlowCore.Infrastructure/DependencyInjection.cs#L64-L72) choisit Azure OpenAI (`AzureOpenAi:*`, échec au démarrage si `Endpoint`/`ApiKey` manquants) ou Ollama local via l'API compatible OpenAI (`Ollama:*`, endpoint `{BaseUrl}/v1/`). `IOllamaConnectivityService` **n'est enregistré qu'en mode Ollama** ; le worker le résout via `GetService` et saute le contrôle de disponibilité quand il est absent.
- `LocalVectorStore` (DuckDB, fichier `{VectorStore:DataPath}/tickets.duckdb`) est `IAsyncDisposable` : le worker crée un `CreateAsyncScope()` par ticket. **Aucun code de production n'appelle `UpsertVectorAsync`** — la table `rag_vectors` n'est alimentée que par les tests, donc la recherche de similarité ne renvoie rien en pratique et la note est générée sans contexte historique.

## Configuration : noms de clés incohérents (source d'erreurs)

- L'API obtient son `DbContext` d'Aspire via `builder.AddSqlServerDbContext<AssetFlowDbContext>("assetflow-db")` → la clé attendue hors Aspire est `ConnectionStrings:assetflow-db`.
- `DatabaseOptions` (section `ConnectionStrings`, propriété `ConnectionString`) lit `ConnectionStrings:ConnectionString` et ne sert qu'à `SqlServerDbContextFactory`, lui-même **non consommé** par les handlers.
- [docker-compose.yml](docker-compose.yml) fournit `ConnectionStrings__DefaultConnection` (+ `SQL_SA_PASSWORD` dans l'environnement), qui ne correspond à aucun des deux : l'exécution via compose nécessite d'aligner ce nom de clé.
- `Cors:AllowedOrigins` est déréférencé avec `!` dans `Program.cs` : sans cette section, la résolution de la policy CORS échoue (uniquement en Development, seul environnement où `UseCors` est branché).

## Tests

xUnit + FluentAssertions + Moq. Les tests unitaires mockent les repositories ; les tests d'intégration utilisent EF InMemory :

- [IntegrationTestBase.cs](AssetFlowCore.IntegrationTests/IntegrationTestBase.cs) : `DbContext` InMemory par test (nom de base = `Guid`).
- [CustomWebApplicationFactory.cs](AssetFlowCore.IntegrationTests/WebApi/CustomWebApplicationFactory.cs) : retire **tous** les descripteurs liés à `AssetFlowDbContext` injectés par Aspire avant de réenregistrer InMemory avec un `internalServiceProvider` dédié. Toucher à l'enregistrement du `DbContext` dans `Program.cs` peut casser ce nettoyage.
- Les benchmarks substituent `NoOpNotificationService` à SignalR et seedent 9 équipes (3 types d'assets × 3 criticités) dans `BenchmarkBase.SeedReferenceData()`.

## CI/CD

[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) : `build` (Release + `dotnet format --verify-no-changes`) → `test-architecture` → `test-unit` / `test-integration` (couverture OpenCover) + `benchmark` → `quality` (SonarCloud, `sonar.qualitygate.wait=true`) → `deploy` (image Docker vers GHCR, uniquement sur push `main`). Les tests d'intégration tournent avec `ASPNETCORE_ENVIRONMENT=Testing`, les benchmarks avec `DOTNET_ENVIRONMENT=Production`.
