# AssetFlow Core — Spécification technique (backend et frontend)

**Objet** — Choix technologiques, structure des projets, conventions, schéma de données, configuration, tests, intégration continue et déploiement. La vue structurelle et les décisions d'architecture sont dans [ARCHITECTURE.md](ARCHITECTURE.md) ; le contrat HTTP dans [API-Specification.md](API-Specification.md).

> **Provenance.** La partie backend est **relevée dans le code** le 2026-08-04 (fichiers projet, configurations EF, `Program.cs`, Dockerfile, workflow CI) : elle décrit l'état réel. La partie frontend est une **cible 🎯** — aucun workspace Angular n'existe encore — et comporte des **décisions ouvertes ❓**.

---

## 1. Backend — Socle technique ✅

### 1.1 Versions

| Élément | Version |
|---|---|
| Cible de compilation | `net8.0` (tous les projets) |
| Fichier solution | `AssetFlowCore.slnx` — **format `.slnx`, nécessite un SDK ≥ 9.0.200** malgré la cible net8.0 |
| SDK installés sur le poste de référence | 9.0.315 et 10.0.302 |
| Entity Framework Core | 9.0.16 (SqlServer, Relational, Design, Tools) + InMemory 8.0.27 pour les tests |
| MediatR | 14.2.0 |
| FluentValidation | 12.1.1 (`FluentValidation.DependencyInjectionExtensions`) |
| Microsoft.SemanticKernel | 1.77.0 · connecteur AzureOpenAI 1.78.0 |
| Microsoft.Extensions.AI | 10.7.0 · `Microsoft.Extensions.AI.OpenAI` 10.7.0 |
| DuckDB.NET.Data | 1.5.3 |
| SignalR | `Microsoft.AspNetCore.SignalR` 1.2.11 |
| Swashbuckle.AspNetCore | 6.6.2 |
| .NET Aspire | 13.4.x (`Aspire.Microsoft.EntityFrameworkCore.SqlServer` 13.4.5, `Aspire.Hosting.Testing` 13.4.6) |
| Tests | xUnit 2.9.3 · FluentAssertions 8.10.0 · Moq 4.20.72 · `Microsoft.NET.Test.Sdk` 18.6.0 · coverlet |
| Architecture | `TngTech.ArchUnitNET` 0.13.3 (+ `.xUnit`) |
| Benchmarks | BenchmarkDotNet 0.15.8 |
| Base de données | SQL Server 2022 (conteneur en développement et en composition Docker) |

> ⚠️ `AssetFlowCore.Benchmarks/Benchmarks.md` documente BenchmarkDotNet 0.14.0 : la valeur du fichier projet (0.15.8) fait foi.

### 1.2 Projets de la solution

| Projet | Type | Rôle | Dépendances de projet |
|---|---|---|---|
| `AssetFlowCore.Domain` | bibliothèque | entités, value objects, enums, exceptions, interfaces de dépôt | **aucune** |
| `AssetFlowCore.Application` | bibliothèque | cas d'usage (commandes/requêtes + handlers), DTOs, validateurs, stratégies de routage, interfaces techniques | Domain |
| `AssetFlowCore.Infrastructure` | bibliothèque | EF Core, dépôts, décorateurs de cache, SignalR, module RAG, configuration | Application, Domain |
| `AssetFlowCore.WebApi` | web | controllers, requêtes HTTP, middleware d'exception, composition de l'application | Application, Infrastructure, ServiceDefaults |
| `AssetFlowCore.Aspire.AppHost` | Aspire | orchestration locale (SQL Server conteneurisé + API) | WebApi |
| `AssetFlowCore.Aspire.ServiceDefaults` | bibliothèque | OpenTelemetry, sondes de santé, découverte de services, résilience HTTP | — |
| `AssetFlowCore.UnitTests` | tests | 176 tests unitaires (domaine, handlers, cache, RAG) | Application, Domain, Infrastructure |
| `AssetFlowCore.IntegrationTests` | tests | dépôts sur EF InMemory, controllers via `WebApplicationFactory` | + WebApi |
| `AssetFlowCore.ArchitectureTests` | tests | règles de dépendances et de conception (ArchUnitNET), configuration de l'AppHost | tous |
| `AssetFlowCore.Benchmarks` | console | mesures BenchmarkDotNet par couche | Application, Domain, Infrastructure |

### 1.3 Conventions de code

- `ImplicitUsings` et `Nullable` activés partout ; `PreserveCompilationContext` activé.
- **Constructeurs primaires** pour l'injection de dépendances (`public class X(IY y)`), sans champs de recopie sauf nécessité.
- `record` pour les commandes, requêtes, DTOs et corps de requête HTTP (immuabilité vérifiée par les tests d'architecture).
- Entités du domaine : propriétés en lecture seule publique (`private set`), mutation par méthodes métier, constructeur privé pour EF.
- **Mapping DTO manuel** via méthodes d'extension (`MappingExtensions`) — choix de performance assumé, aucun mapper par réflexion.
- Erreurs métier par exception (`DomainException`), traduites en réponse HTTP par un middleware unique.
- **Commentaires, messages d'exception et journaux en français.**
- Format imposé par `dotnet format --severity warn`, **bloquant en intégration continue**. Aucun `.editorconfig` n'est présent : les règles par défaut du SDK s'appliquent.

### 1.4 Schéma de base de données

Trois tables, nommage en `snake_case`, préfixe `t_`.

**`t_assets`**

| Colonne | Type | Contrainte |
|---|---|---|
| `id` | `uniqueidentifier` | clé primaire |
| `name` | `nvarchar(100)` | requis |
| `serial_num` | `nvarchar(50)` | requis, **index unique** (type possédé `SerialNumber`) |
| `type` | `nvarchar(50)` | énumération convertie en chaîne |
| `status` | `nvarchar(30)` | énumération convertie en chaîne |
| `created_at` | `datetime2` | |

**`t_teams`**

| Colonne | Type | Contrainte |
|---|---|---|
| `id` | `uniqueidentifier` | clé primaire, **jamais générée par la base** |
| `name` | `nvarchar(100)` | requis, index unique `IX_t_teams_name` |
| `description` | `nvarchar(500)` | facultatif |
| `is_active` | `bit` | valeur par défaut `1` |
| `created_at` | `datetime2` | |
| `asset_type` | `nvarchar(100)` | requis — **texte, pas une énumération** |
| `ticket_criticality` | `nvarchar(100)` | requis — **texte, pas une énumération** |
| index | | `IX_t_teams_is_active` |

**`t_maintenance_tickets`**

| Colonne | Type | Contrainte |
|---|---|---|
| `id` | `uniqueidentifier` | clé primaire |
| `title` | `nvarchar(150)` | requis |
| `description` | `nvarchar(max)` | requis |
| `criticality` | `nvarchar(20)` | énumération convertie en chaîne |
| `status` | `nvarchar(30)` | énumération convertie en chaîne |
| `assigned_team_id` | `uniqueidentifier` | requis, clé étrangère → `t_teams`, **`ON DELETE RESTRICT`** |
| `asset_id` | `uniqueidentifier` | requis, clé étrangère → `t_assets`, **`ON DELETE RESTRICT`** |
| `resolution_comment` | `nvarchar(max)` | facultatif |
| `created_at` | `datetime2` | |
| `is_ai_processing` | `bit` | requis, valeur par défaut `0` (le constructeur d'entité positionne `1` à la création) |
| `assistance_note` | `nvarchar(max)` | facultatif |
| `row_version` | `rowversion` | **jeton de concurrence optimiste** |
| index | | `IX_t_maintenance_tickets_asset_id_status`, `IX_t_tickets_assigned_team_id` |

**Migrations** (assembly `AssetFlowCore.Infrastructure`) : `InitialCreate`, `AddTeamsTable`, `AddAiFieldsToMaintenanceTicket`.

> ⚠️ **Aucune migration n'est appliquée au démarrage** : la base doit être migrée explicitement. Aucun jeu de données de référence n'est amorcé, alors que le routage automatique en dépend.

Base vectorielle : fichier **DuckDB** local (`{VectorStore:DataPath}/tickets.duckdb`), table `rag_vectors (id, embedding FLOAT[], metadata JSON, created_at)`, similarité cosinus calculée en SQL. Hors périmètre des migrations EF.

### 1.5 Configuration

| Clé | Consommateur | Remarque |
|---|---|---|
| `ConnectionStrings:assetflow-db` | `AddSqlServerDbContext` (Aspire) | **c'est la clé réellement utilisée par l'API** |
| `ConnectionStrings:ConnectionString` | `DatabaseOptions` → `SqlServerDbContextFactory` | fabrique **non consommée** par les cas d'usage |
| `Cors:AllowedOrigins` | politique CORS | déréférencé sans garde : section obligatoire ; politique appliquée **en développement uniquement** |
| `AiSettings:UseAzure` | choix du fournisseur d'IA | `true` → Azure OpenAI, sinon Ollama local |
| `AzureOpenAi:Endpoint` · `ApiKey` · `ChatDeploymentName` · `EmbeddingDeploymentName` | Azure OpenAI | endpoint et clé obligatoires, sinon **échec au démarrage** ; valeurs par défaut `gpt-4o` et `text-embedding-3-small` |
| `Ollama:BaseUrl` · `ChatModel` · `EmbeddingModel` | Ollama local | défauts `http://localhost:11434`, `phi4`, `nomic-embed-text` |
| `VectorStore:DataPath` | base DuckDB | défaut `./vectordb` |
| `Parameters:sqlserver-password` | AppHost Aspire | **User Secret du projet AppHost** |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | ServiceDefaults | active l'export OTLP s'il est renseigné |

> ⚠️ `docker-compose.yml` fournit `ConnectionStrings__DefaultConnection`, qui ne correspond à **aucune** des deux clés lues par l'application : l'exécution par composition Docker exige d'aligner ce nom.

### 1.6 Tests

| Suite | Contenu | Exécution |
|---|---|---|
| Unitaires | domaine, handlers, validateurs, décorateurs de cache, composition de l'unité de travail, propagation du jeton d'annulation, module RAG (Moq) | `dotnet test AssetFlowCore.UnitTests/...` — 195 tests, tous verts |
| Intégration | dépôts sur **EF InMemory**, controllers via `CustomWebApplicationFactory` (les enregistrements de `DbContext` injectés par Aspire sont retirés puis remplacés) | `dotnet test AssetFlowCore.IntegrationTests/...` |
| Architecture | dépendances entre couches, absence de setter public sur les entités, immuabilité des commandes, emplacement des handlers, préfixe des interfaces, isolation des dépôts, configuration de l'AppHost | `dotnet test AssetFlowCore.ArchitectureTests/...` |

Un test unique : `dotnet test <projet> --filter "FullyQualifiedName~MaClasse.MaMethode"`.

⚠️ Les dépôts `TeamRepository.UpdateAsync`/`RemoveAsync` empruntent un **chemin différent selon le fournisseur** (`ExecuteUpdate`/`ExecuteDelete` en relationnel, `Attach` + `EntityState.Modified` en InMemory) : les tests n'exercent pas le code de production.

### 1.7 Performance

Suite BenchmarkDotNet par couche (domaine, validateurs, cas d'usage, infrastructure), exécutée en intégration continue en configuration `Release` obligatoire. Résultats de référence documentés dans `AssetFlowCore.Benchmarks/Benchmarks.md` — notamment le gain du cache sur la lecture d'inventaire (62× à 1 153× selon le volume).

### 1.8 Observabilité

OpenTelemetry via `ServiceDefaults` : journaux (message formaté + portées), métriques (ASP.NET Core, HttpClient, runtime), traces (ASP.NET Core avec **filtrage des sondes de santé**, HttpClient). Export OTLP activé si `OTEL_EXPORTER_OTLP_ENDPOINT` est défini ; sinon les données restent locales (tableau de bord Aspire en développement).

Sondes : `/health` (toutes les vérifications) et `/alive` (vérifications marquées `live`) — ⚠️ **mappées uniquement en environnement Development**, alors que le `HEALTHCHECK` du Dockerfile et de `docker-compose.yml` les interroge en `Production`.

### 1.9 Intégration continue

`.github/workflows/ci-cd.yml`, déclenché sur `push` et `pull_request` vers `main` et `dev` :

1. **build** — compilation `Release` puis `dotnet format --verify-no-changes --severity warn` (**bloquant**).
2. **test-architecture** — tests ArchUnitNET + couverture OpenCover.
3. **test-unit** et **test-integration** — en parallèle, couverture OpenCover (`ASPNETCORE_ENVIRONMENT=Testing` pour l'intégration).
4. **benchmark** — BenchmarkDotNet en `Release`, exports JSON/HTML/Markdown, artefacts conservés 30 jours.
5. **quality** — SonarCloud avec les trois rapports de couverture, `sonar.qualitygate.wait=true` (**bloquant**).
6. **deploy** — uniquement sur `push` vers `main` : image Docker publiée sur GHCR (`ghcr.io/<owner>/assetflow-api`), cache de build GitHub Actions.

> ⚠️ Le workflow installe le SDK **8.0** alors que la solution est au format `.slnx`, qui exige un SDK ≥ 9.0.200. La commande fonctionne aujourd'hui parce que les exécuteurs GitHub embarquent des SDK plus récents et que `dotnet` retient le plus élevé — dépendance implicite à surveiller (un `global.json` la rendrait explicite).

### 1.10 Déploiement

- **Dockerfile** multi-étapes : restauration ciblée des `.csproj` (cache), publication `linux-musl-x64` non auto-contenue, image finale `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`, **utilisateur non root**, port `8080`, `HEALTHCHECK` sur `/health`, `icu-libs` installé (globalisation active).
- **docker-compose** : services `api` et `sqlserver` (SQL Server 2022 Developer), volume persistant, sonde `sqlcmd`, démarrage de l'API conditionné à la santé de la base, réseau dédié. Mot de passe via `SQL_SA_PASSWORD`.
- **Développement** : orchestration Aspire (`AppHost`) qui démarre SQL Server en conteneur, injecte la chaîne de connexion et expose le tableau de bord.

### 1.11 Commandes

```powershell
dotnet build AssetFlowCore.slnx
dotnet format AssetFlowCore.slnx --verify-no-changes --severity warn   # gate CI
dotnet test AssetFlowCore.UnitTests/AssetFlowCore.UnitTests.csproj
dotnet run --project AssetFlowCore.Benchmarks -c Release -- --filter "*CachedRepository*"
dotnet run --project AssetFlowCore.Aspire/AssetFlowCore.Aspire.AppHost
dotnet user-secrets set "Parameters:sqlserver-password" "<motdepasse>" --project AssetFlowCore.Aspire/AssetFlowCore.Aspire.AppHost
dotnet ef migrations add <Nom> --project AssetFlowCore.Infrastructure --startup-project AssetFlowCore.WebApi
dotnet ef database update --project AssetFlowCore.Infrastructure --startup-project AssetFlowCore.WebApi
```

---

## 2. Frontend — Socle technique cible 🎯

### 2.1 Versions relevées sur le poste de référence (2026-08-04)

| Élément | Version |
|---|---|
| Node.js | 26.5.1 |
| npm | 11.17.0 |
| Angular (dernière stable) | **22.1.0** |
| Angular CLI | 22.1.2 |
| `ng` dans le `PATH` | **non** → utiliser `npx ng ...` ou les scripts npm |
| `@ngrx/signals` | stable 21.1.1 (peer `@angular/core ^21.0.0`) · préversion `22.0.0-beta.1` |
| Client temps réel | `@microsoft/signalr` (à installer) |

> ⚠️ **NgRx SignalStore n'a pas de version stable compatible Angular 22.** L'état par défaut repose donc sur les **Signals natifs**. Recourir à SignalStore impose d'accepter une préversion, décision à acter explicitement.

### 2.2 Structure cible

```
AssetFlowCore.WebUI/                    ❓ nom à valider
├── angular.json · package.json · tsconfig*.json
├── proxy.conf.json                     proxy de développement vers l'API
└── src/
    ├── main.ts                         bootstrapApplication(App, appConfig)
    ├── environments/environment.ts · environment.development.ts
    └── app/
        ├── app.ts · app.html · app.config.ts · app.routes.ts
        ├── core/                        singletons : http/, guards/, api/, realtime/
        ├── shared/                      ui/, pipes/, directives/, models/
        └── features/                    assets/, tickets/, teams/ (routes + pages + état)
```

Règles de dépendances : `features/` → `shared/` + `core/` ; `shared/` sans dépendance métier ni réseau ; `core/` sans dépendance à `features/` ; **aucun import croisé entre deux features**.

### 2.3 Conventions imposées

- **Composants standalone exclusivement**, aucun `NgModule`.
- **`inject()`** pour toute dépendance, y compris services, guards et interceptors ; guards et interceptors **fonctionnels**.
- **Control Flow natif** : `@if`, `@for` (toujours avec `track`), `@switch`, `@defer`.
- **API de composant à base de signaux** : `input()`, `input.required()`, `output()`, `model()`, `viewChild()`.
- **`ChangeDetectionStrategy.OnPush`** partout ; mode *zoneless* à évaluer.
- État : `signal()`, `computed()`, `linkedSignal()` ; `effect()` réservé aux effets de bord réels (jamais pour dériver de l'état).
- Données : `httpResource()` / `resource()` / `rxResource()` ; à défaut, abonnement fermé par `takeUntilDestroyed()`.
- **Formulaires typés** : `NonNullableFormBuilder`, `FormControl<T>`, aucun `any`.
- Appels HTTP centralisés dans `core/api/` ; aucun `HttpClient` dans un composant.
- `provideHttpClient(withFetch(), withInterceptors([...]))`.
- Nommage : fichiers en `kebab-case`, classes en `PascalCase`, commentaires et libellés **en français**.

### 2.4 Typage du contrat d'API

| C# | TypeScript |
|---|---|
| `Guid` | `string` |
| `DateTime` | `string` (ISO 8601) — **jamais `Date`** dans le type de transport |
| `int`/`long`/`decimal` | `number` (signaler le risque de précision au-delà de 2^53) |
| `T?` | `T \| null` (propriété présente) |
| `IEnumerable<T>` | `T[]` |
| `enum` | **union de littéraux `PascalCase`** |

Les **noms de propriétés** sont en `camelCase`, les **valeurs d'enums** restent en `PascalCase` (converteur JSON sans politique de nommage), et les clés du dictionnaire `errors` de `ProblemDetails` sont en `PascalCase`. La génération est outillée par le skill `/sync-api-dtos`.

### 2.5 Tests

- `TestBed.configureTestingModule({ imports: [MonComposant] })` en mode standalone.
- `provideHttpClient()` **puis** `provideHttpClientTesting()`.
- Entrées signaux pilotées par `fixture.componentRef.setInput(...)`.
- Couverture attendue : validation de formulaire, dérivations, gestion d'erreur, guards, interceptors.
- ❓ **Runner à décider** : Vitest (voie moderne du CLI) ou Jasmine/Karma (voie historique).

### 2.6 Décisions ouvertes ❓

| Sujet | Options | Enjeu |
|---|---|---|
| Nom du dossier du workspace | `AssetFlowCore.WebUI` (nomenclature du dépôt) ou `frontend/` | cohérence, scripts CI |
| Framework de style | Tailwind · Angular Material · DaisyUI+Tailwind · SCSS maison | vitesse de production, contrôle du rendu, accessibilité fournie |
| Rendu serveur (SSR) | avec ou sans | complexité de déploiement contre temps d'affichage initial |
| Gestion d'état | Signals natifs (défaut) ou SignalStore en préversion | dépendance non stable |
| Runner de tests | Vitest ou Karma | outillage, vitesse |
| Internationalisation | français seul ou multilingue | les valeurs d'API sont en anglais et doivent être traduites dans tous les cas |
| Déploiement du frontend | conteneur dédié · servi par l'API · hébergement statique + reverse proxy | contrainte CORS (voir §3) |
| Intégration à la CI | ajout de jobs `build`/`test` frontend au workflow existant | portail qualité, artefacts |

---

## 3. Intégration backend ↔ frontend

| Sujet | Décision technique |
|---|---|
| URL de base | `environment.apiBaseUrl` ; jamais d'URL en dur |
| Développement | **`proxy.conf.json`** du serveur Angular vers `https://localhost:7138` — évite CORS et le certificat de développement |
| Production | **même origine derrière un reverse proxy** : hors Development, l'API n'applique **aucune** politique CORS, un appel navigateur d'une autre origine échoue |
| Types | générés depuis le C# (`/sync-api-dtos`), regroupés dans `shared/models/` |
| Erreurs | intercepteur unique traduisant `ProblemDetails` ; le dictionnaire `errors` est reporté sur les champs de formulaire ; le `detail` d'une 500 n'est **jamais** affiché |
| Codes de statut | `PUT /api/teams/{id}` répond **201** ; **aucun endpoint ne renvoie 404** (introuvable → 400) : ne pas coder de branche sur 404 |
| Temps réel | `@microsoft/signalr` sur `/ticketHub` ; `JoinTeamGroup(nomEquipe)` puis écoute de `ReceiveNewTicket` |
| Authentification | ⛔ inexistante côté API : l'interceptor de jeton peut être préparé, il n'a rien à porter |
| Fraîcheur des données | `GET /api/assets` est servi d'un cache serveur de 5 minutes **invalidé par les écritures** : un rechargement après création ou mise au rebut reflète immédiatement l'état réel |

---

## 4. Environnements

| Environnement | Base de données | Swagger | Sondes de santé | CORS | Particularités |
|---|---|---|---|---|---|
| **Development** (Aspire) | SQL Server conteneurisé, chaîne injectée par Aspire | ✅ `/swagger` | ✅ `/health`, `/alive` | ✅ `Cors:AllowedOrigins` (`*` par défaut) | tableau de bord Aspire, redirection HTTPS, mot de passe en User Secret |
| **Testing** (tests d'intégration) | EF **InMemory**, base isolée par test | — | — | — | enregistrements `DbContext` d'Aspire retirés et remplacés |
| **Production** (conteneur) | SQL Server externe ou composé | ⛔ | ⛔ **absentes** alors que le conteneur les interroge | ⛔ aucune politique | image Alpine non root, port 8080 |

---

## 5. Dette technique identifiée

Constats vérifiés, classés par gravité. Le détail comportemental est dans [API-Specification.md](API-Specification.md#9-écarts-et-limitations-connus).

| Gravité | Constat | Effet |
|---|---|---|
| **Critique** | aucune authentification ni autorisation | API totalement ouverte |
| **Majeur** | file d'analyse IA en mémoire | demandes perdues au redémarrage |
| **Majeur** | base vectorielle jamais alimentée en production | assistance IA sans corpus, valeur nulle |
| **Majeur** | absence d'endpoints de liste (incidents, équipes) | interface utilisateur non réalisable |
| **Moyen** | migrations non appliquées au démarrage | une base neuve reste inexploitable tant que `dotnet ef database update` n'a pas été lancé |
| **Moyen** | clés de configuration divergentes (`assetflow-db`, `ConnectionString`, `DefaultConnection`) | exécution par composition Docker inopérante sans ajustement |
| **Moyen** | décorateurs d'équipe réécrivant le cache **avant** `SaveChangesAsync` | un échec de persistance laisse une valeur non persistée en cache |
| **Moyen** | dépôts d'équipe à double chemin selon le fournisseur | production non couverte par les tests |
| **Mineur** | `TicketStatus.Resolved` jamais atteint · `IDbContextFactory` et `DatabaseOptions` non consommés · `Team.IsActive` non pilotable | code mort ou inachevé |
| **Mineur** | dérive documentaire (`README.md`, `Benchmarks.md`) | information obsolète |
| **Mineur** | dépendance implicite au SDK de l'exécuteur CI pour `.slnx` | build fragile |

### 5.1 Dette résorbée par le Lot 1 (2026-08-05)

| Constat d'origine | Correction |
|---|---|
| cache d'inventaire non invalidé par les écritures | `UnitOfWork` résout ses dépôts par le conteneur ; les listes sont invalidées à la persistance, y compris pour les mutations d'entités suivies. Les lectures d'actif **par identifiant** ne sont plus mises en cache : elles alimentent des cas d'usage d'écriture |
| sondes de santé absentes hors Development | `/health` et `/alive` exposées dans tous les environnements |
| transitions d'état d'incident levant `InvalidOperationException` | `DomainException`, donc 400 |
| doublon de nom d'équipe non contrôlé fonctionnellement | contrôle applicatif à la création et au renommage |
| `CancellationToken` absent des controllers et partiel dans les dépôts | propagé du controller au dépôt sur l'ensemble des cas d'usage |
| `detail` d'une erreur 500 = message d'exception brut | message générique + `traceId`, exception journalisée |
| messages de validation de criticité copiés depuis le type d'actif | messages propres à la criticité ; la criticité d'un incident est désormais validée en liste fermée (`IsEnumName`) |
| aucune équipe de référence sur une base neuve | migration `SeedReferenceTeams` (9 combinaisons) |
