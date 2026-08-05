# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Contexte

API .NET 8 de gestion de parc informatique (assets, équipes, tickets de maintenance) en Clean Architecture / DDD / CQRS, avec assistance IA (RAG) asynchrone. Le code, les commentaires, les messages d'exception, les logs et les messages de commit sont **en français** — conserver cette convention.

Depuis le 2026-08-05, le dépôt contient aussi un **frontend Angular 22** dans `AssetFlowCore.WebUI/` : son socle est en place (types du contrat, services d'API, intercepteurs, client SignalR), **aucun écran produit** ne l'est encore. Voir la section « Frontend Angular » plus bas.

**Évolutions de contrat décidées mais non implémentées** (Lot 0 clos le 2026-08-05 ; détail dans [doc/IMPLEMENTATION-PLAN.md](doc/IMPLEMENTATION-PLAN.md) §3 et §5.1) : passage des routes sous `/api/v1/...`, suppression de `TicketStatus.Resolved`, historisation du motif de transfert dans une entité dédiée (au lieu de la concaténation à `Description` par `MaintenanceTicket.TransferToTeam`), endpoints d'activation/désactivation d'équipe, remise en service d'un actif au rebut. Ces cinq changements sont à livrer **avant** les premiers écrans : ne pas construire de code d'écran sur la forme actuelle du contrat.

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

Sans Docker, l'API peut tout de même être lancée seule, avec une base injoignable — suffisant pour exercer le proxy du frontend, les 400 de validation et les sondes, mais **aucun endpoint de données** ne répondra 200 :

```powershell
${env:ConnectionStrings__assetflow-db} = "Server=(localdb)\MSSQLLocalDB;Database=AssetFlowCore_Dev;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project AssetFlowCore.WebApi --launch-profile https   # https://localhost:7138
```

### Frontend Angular (`AssetFlowCore.WebUI/`)

```powershell
cd AssetFlowCore.WebUI
npm install
npm start                        # ng serve sur http://localhost:4200, proxy vers https://localhost:7138
npm run build                    # ng build (configuration production par défaut)
npm run test:ci                  # ng test --watch=false (Vitest, 47 tests)
npm run format:verify            # Prettier — pendant de `dotnet format`, futur gate de CI
npm run verifier:dependances     # règles core/ shared/ features/ — pendant des ArchitectureTests
npm run verifier:contrastes      # contrastes WCAG des jetons, dans les deux thèmes
```

`ng` n'est **pas** dans le `PATH` : passer par les scripts npm ou `npx ng ...`.

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
- [ExceptionHandlingMiddleware.cs](AssetFlowCore.WebApi/Middlewares/ExceptionHandlingMiddleware.cs) traduit les exceptions en `ProblemDetails` : `ValidationException` / `ArgumentException` / `DomainException` → 400, `NotFoundException` → 404, `DbUpdateConcurrencyException` → 409 (via `MaintenanceTicket.RowVersion`), reste → 500. Les handlers lèvent donc des exceptions plutôt que de retourner des résultats d'erreur.
- `NotFoundException` **dérive de** `DomainException` : son cas doit rester **avant** celui de `DomainException` dans le `switch` du middleware, sinon les 404 redeviennent des 400. Règle d'emploi : la ressource désignée par l'URI est absente → `NotFoundException` (404) ; une référence invalide portée par le corps → `DomainException` (400).

## Moteur d'assignation (Strategy) — pièges

[TicketAssignmentEngine.cs](AssetFlowCore.Application/Services/TicketAssignmentEngine.cs) prend la **première** `IAssignmentStrategy` dont `IsMatch(assetType, criticality)` répond `true` : l'ordre d'enregistrement dans [DependencyInjection.cs](AssetFlowCore.Application/DependencyInjection.cs#L29-L32) fait office de priorité, avec repli explicite sur `LaptopStandardStrategy`.

`Team.AssetType` et `Team.TicketCriticality` sont des **`string`**, pas les enums du domaine ; la résolution compare `assetType.ToString()` / `criticality.ToString()` en base. Conséquence : sans équipe correspondant au couple `(AssetType, TicketCriticality)` en base, `AssignmentStrategyBase.GetTeamNameAsync` lève une `DomainException` et la création de ticket échoue. Toute nouvelle stratégie exige donc aussi le seed des équipes correspondantes — les 9 combinaisons actuelles sont amorcées par la migration `SeedReferenceTeams`, dont les noms doivent rester **uniques** (index `IX_t_teams_name`).

## Cache : décorateurs autour des repositories

`IAssetRepository` et `ITeamRepository` sont résolus vers `CachedAssetRepository` / `CachedTeamRepository`, qui décorent les implémentations EF concrètes (enregistrées en tant que types concrets) avec `IMemoryCache` (expiration absolue 5 min). En ajoutant une méthode d'écriture sur un repository, il faut **invalider les clés correspondantes dans le décorateur** (clés partagées dans [CacheKeys.cs](AssetFlowCore.Infrastructure/Cache/CacheKeys.cs)), sinon les lectures servent des données périmées.

`UnitOfWork` **reçoit ses dépôts du conteneur** : les écritures passant par lui traversent donc les décorateurs. Les mutations d'entités suivies qui n'appellent aucune méthode de dépôt (`asset.Decommission()`, `asset.MarkAsDown()`) sont détectées via le `ChangeTracker` dans `SaveChangesAsync`, qui invalide alors les listes concernées.

Le cache d'actifs porte **uniquement sur la liste** : `CachedAssetRepository.GetByIdAsync` délègue sans mise en cache, car tous ses appelants mutent l'actif retourné — servir une instance détachée du `DbContext` courant ferait échouer silencieusement la persistance. Même précaution à conserver pour toute nouvelle lecture destinée à une mutation.

Deux listes d'équipes sont mises en cache sous des clés distinctes (`Teams_List_Active` et `Teams_List_All`) : toute écriture doit périmer **les deux**, ce que centralise `CachedTeamRepository.InvalidateLists()`. `GET /api/tickets` et `GET /api/assets/{id}` ne sont pas mis en cache.

`Criticality` et `Status` sont persistés en texte (`HasConversion<string>()`) : tout tri SQL sur ces colonnes serait alphabétique (« High » < « Low » < « Medium »). `MaintenanceTicketRepository.ApplySort` projette donc chaque valeur sur son rang métier — à reproduire pour toute nouvelle colonne d'énumération triable.

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

## Frontend Angular : structure et pièges

`AssetFlowCore.WebUI/src/app` se découpe en `core/` (api, http, auth, realtime) · `shared/` (models, puis ui au Lot 4) · `features/`. Les règles de dépendances sont **vérifiées par `npm run verifier:dependances`** (pendant frontend des `ArchitectureTests`) : `shared/` n'importe ni `core/` ni `features/`, `core/` n'importe pas `features/`, et deux features ne s'importent jamais l'une l'autre. Chaque zone porte un `README.md` détaillant ses règles.

- **Mode zoneless** : `zone.js` est absent des dépendances. Un état modifié hors d'un signal **ne déclenche aucun rendu**. Dans les tests, `TestBed.tick()` laisse partir la requête d'une ressource ; `await fixture.whenStable()` attend le rendu — mais bloquerait si une requête HTTP était encore en vol.
- **Nommage** : guide de style 2025 du CLI → les composants sont sans suffixe (`app.ts` → `App`), les autres artefacts portent leur rôle (`*.service.ts`, `*.routes.ts`, `*.interceptor.ts`, `*.model.ts`). Les gabarits de `/scaffold-feature` proposent `*.component.ts` : la convention du workspace prime.
- **Contrat d'API** : les types de `shared/models/` sont dérivés du C# et portent leurs sources en en-tête. Ne pas les éditer à la main — toute évolution backend passe par `/sync-api-dtos`. Rappel du piège de casse : les **noms de propriétés** sont en `camelCase`, mais les **valeurs d'énumérations** et les **clés du dictionnaire `errors`** restent en `PascalCase`.
- **Erreurs** : `errorInterceptor` est le seul point qui interprète `ProblemDetails`. Il lève une `ApiError` porteuse d'une nature (`validation`, `business`, `notFound`, `conflict`, `server`, `network`) et d'un `fieldErrors` **converti en `camelCase`** pour correspondre aux contrôles d'un formulaire. Les écrans ne voient jamais de `HttpErrorResponse`. Le `detail` d'une 5xx n'est jamais affiché, seul le `traceId` l'est.
- **Temps réel** : `TicketHubService` ne se connecte pas au démarrage ; l'appelant décide. Les groupes rejoints sont mémorisés et **restaurés après reconnexion**, le serveur ne les conservant pas. Le hub ne diffuse qu'à l'ouverture d'un incident (Lot 6 pour le reste).
- **Jeton** : `authTokenInterceptor` et `AuthTokenService` sont en place mais **sans source** — l'API n'a aucune authentification. Le Lot 7 n'aura qu'à alimenter le service.
- **Design system (Lot 4)** : Tailwind 4, jetons dans `src/styles.css`. Les composants n'ont **aucune feuille de styles** — que des utilitaires — et aucune couleur, taille ou durée n'y est écrite en dur. Les jetons sont déclarés une seule fois via `light-dark()` : ne **jamais** ajouter un bloc de thème sombre parallèle, et repasser `npm run verifier:contrastes` après toute retouche de couleur. La feuille racine est en `.css` et non `.scss` : Tailwind 4 ne passe pas par un préprocesseur.
- **Champs de formulaire** : approche A — le composant reçoit le `FormControl` en entrée, donc pas d'usage avec `formControlName`. `FormControl` n'étant pas réactif au sens des signaux, les champs passent par `suivreEtatControle()` (`shared/forms/`) : sans lui, un `markAsTouched()` de soumission n'afficherait aucun message en mode zoneless. Charge de la feature : déplacer le focus sur le premier champ invalide à la soumission.
- **API des composants partagés** : documentée dans [AssetFlowCore.WebUI/src/app/shared/README.md](AssetFlowCore.WebUI/src/app/shared/README.md). Les badges du domaine encapsulent traduction **et** tonalité — ne pas refaire ces correspondances dans un écran.
- `features/diagnostic/` et `features/design-system/` ne sont pas des écrans produits : ce sont les preuves d'exécution du socle et la page de revue du design system, à retirer quand les écrans du Lot 5 prendront leur place.
- **Limites de l'environnement de test** : `window.localStorage` n'existe pas (origine opaque) et jsdom ne calcule aucune géométrie — le CDK considère alors tout élément comme non focusable. Les deux se contournent dans les tests concernés (`theme.service.spec.ts`, `modal.spec.ts`), jamais dans le code de production.

## Tests

xUnit + FluentAssertions + Moq. Les tests unitaires mockent les repositories ; les tests d'intégration utilisent EF InMemory :

- [IntegrationTestBase.cs](AssetFlowCore.IntegrationTests/IntegrationTestBase.cs) : `DbContext` InMemory par test (nom de base = `Guid`).
- [CustomWebApplicationFactory.cs](AssetFlowCore.IntegrationTests/WebApi/CustomWebApplicationFactory.cs) : retire **tous** les descripteurs liés à `AssetFlowDbContext` injectés par Aspire avant de réenregistrer InMemory avec un `internalServiceProvider` dédié. Toucher à l'enregistrement du `DbContext` dans `Program.cs` peut casser ce nettoyage.
- Les benchmarks substituent `NoOpNotificationService` à SignalR et seedent 9 équipes (3 types d'assets × 3 criticités) dans `BenchmarkBase.SeedReferenceData()`.

Côté frontend : Vitest sur jsdom, primitives globales (`vitest/globals`), `vi` importé explicitement quand un espion est nécessaire. `provideHttpClientTesting()` suit toujours `provideHttpClient()`.

## CI/CD

[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) : `build` (Release + `dotnet format --verify-no-changes`) → `test-architecture` → `test-unit` / `test-integration` (couverture OpenCover) + `benchmark` → `quality` (SonarCloud, `sonar.qualitygate.wait=true`) → `deploy` (image Docker vers GHCR, uniquement sur push `main`). Les tests d'intégration tournent avec `ASPNETCORE_ENVIRONMENT=Testing`, les benchmarks avec `DOTNET_ENVIRONMENT=Production`.
