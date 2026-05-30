# AssetFlowCore — Documentation des Benchmarks

## Table des matières

1. [Vue d'ensemble](#1-vue-densemble)
2. [Prérequis et installation](#2-prérequis-et-installation)
3. [Architecture du projet](#3-architecture-du-projet)
4. [Infrastructure partagée](#4-infrastructure-partagée)
5. [Benchmarks par couche](#5-benchmarks-par-couche)
   - 5.1 [Couche Domain](#51-couche-domain)
   - 5.2 [Couche Application — Validators](#52-couche-application--validators)
   - 5.3 [Couche Application — Use Cases](#53-couche-application--use-cases)
   - 5.4 [Couche Infrastructure](#54-couche-infrastructure)
6. [Résultats d'exécution](#6-résultats-dexécution)
7. [Analyse et conclusions](#7-analyse-et-conclusions)
8. [Recommandations d'optimisation](#8-recommandations-doptimisation)
9. [Référence des attributs BenchmarkDotNet](#9-référence-des-attributs-benchmarkdotnet)

---

## 1. Vue d'ensemble

Ce projet mesure les performances de l'application **AssetFlowCore** de bout en bout, couche par couche, en utilisant [BenchmarkDotNet](https://benchmarkdotnet.org/) — le standard industriel pour les micro-benchmarks .NET.

### Objectifs

- Valider les choix architecturaux (Pattern Strategy, Décorateur Cache, Options Pattern, mapping manuel)
- Identifier les goulots d'étranglement avant mise en production
- Fournir une base de référence (baseline) pour détecter les régressions de performance lors des évolutions futures
- Mesurer l'impact du cache sur les lectures fréquentes (`GetAllAssets`)

### Périmètre mesuré

| Couche | Patrons testés |
|---|---|
| Domain | Automate d'état Asset, automate d'état Ticket, Value Object SerialNumber |
| Application — Validators | FluentValidation sur `CreateMaintenanceTicketCommand` |
| Application — Use Cases | RegisterAsset, GetAllAssets, CreateTicket, AssignTicket, CloseTicket, DecommissionAsset, concurrence |
| Infrastructure | Repositories EF Core, Décorateur Cache, sérialisation middleware, résolution DI |

---

## 2. Installation

### Lancer les benchmarks

> **Important** : BenchmarkDotNet refuse de s'exécuter en mode `Debug`. La configuration `Release` est obligatoire pour des mesures fiables.

```bash
# Lancer tous les benchmarks
dotnet run --project AssetFlowCore.Benchmarks -c Release

# Lancer un benchmark spécifique (filtre par nom de classe)
dotnet run --project AssetFlowCore.Benchmarks -c Release -- --filter "*CachedRepository*"

# Lancer uniquement la couche Domain
dotnet run --project AssetFlowCore.Benchmarks -c Release -- --filter "*Domain*"

# Lancer uniquement les Use Cases
dotnet run --project AssetFlowCore.Benchmarks -c Release -- --filter "*UseCases*"

# Exporter les résultats en CSV et HTML
dotnet run --project AssetFlowCore.Benchmarks -c Release -- --exporters csv html
```

### Packages NuGet utilisés

| Package | Version | Rôle |
|---|---|---|
| `BenchmarkDotNet` | 0.14.0 | Framework de benchmarking |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.27 | Base de données InMemory pour les tests |
| `Microsoft.Extensions.Caching.Memory` | 8.0.1 | Cache mémoire (`IMemoryCache`) |
| `Microsoft.Extensions.DependencyInjection` | 8.0.1 | Conteneur DI |
| `Microsoft.AspNetCore.Mvc.Core` | 2.2.5 | `ProblemDetails`, `StatusCodes` |

---

## 3. Architecture du projet

```
AssetFlowCore.Benchmarks/
│
├── AssetFlowCore.Benchmarks.csproj     Projet SDK Console, Optimize=true
├── Program.cs                           Point d'entrée — BenchmarkSwitcher
├── BenchmarkBase.cs                     Classe de base partagée (DI + InMemory)
├── NoOpNotificationService.cs           Stub SignalR pour isoler la logique métier
│
├── Domain/
│   ├── AssetStateMachineBenchmark.cs    Transitions d'état de l'entité Asset
│   ├── MaintenanceTicketStateMachineBenchmark.cs  Transitions + invariants Ticket
│   └── SerialNumberBenchmark.cs         Value Object : création et égalité
│
├── Application/
│   ├── MappingBenchmark.cs              Mapping manuel ToDto() unitaire et en masse
│   ├── TicketAssignmentEngineBenchmark.cs  Pattern Strategy — résolution d'équipe
│   └── UseCases/
│       ├── RegisterAssetBenchmark.cs              Cas nominal par type d'asset
│       ├── RegisterAssetDuplicateSerialBenchmark.cs  Détection de doublon
│       ├── GetAllAssetsBenchmark.cs               Cache miss vs cache hit
│       ├── CreateTicketBenchmark.cs               Pipeline complet par criticité
│       ├── AssignTicket et CloseTicketBenchmark.cs  Transitions de statut
│       ├── TicketLifecycleBenchmark.cs            Cycle complet Create→Assign→Close
│       ├── DecommissionAssetBenchmark.cs          Succès vs bloqué (règle métier)
│       └── ConcurrentTicketCreationBenchmark.cs   Séquentiel vs Task.WhenAll
│   └── Validators/
│       └── CreateTicketValidatorBenchmark.cs      FluentValidation — tous les chemins
│
└── Infrastructure/
    ├── AssetRepositoryBenchmark.cs                Requêtes EF Core brutes
    ├── CachedRepositoryBenchmark.cs               Pattern Décorateur — gain cache
    ├── MaintenanceTicketRepositoryBenchmark.cs    CountActive, GetById
    ├── ExceptionHandlingSerializationBenchmark.cs JSON ProblemDetails + dispatch
    └── DependencyInjectionResolutionBenchmark.cs  Coût résolution par handler
```

---

## 4. Infrastructure partagée

### `BenchmarkBase`

Classe abstraite héritée par tous les benchmarks qui nécessitent des dépendances applicatives. Elle configure un conteneur DI complet avec une base de données InMemory isolée par benchmark.

```csharp
public abstract class BenchmarkBase
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected AssetFlowDbContext DbContext { get; private set; }

    protected void SetupServices(string dbName) { ... }
    protected T Resolve<T>() where T : notnull  // scope DI frais (simule une requête HTTP)
}
```

**Principe d'isolation** : chaque benchmark appelle `SetupServices("NomUnique")` avec un nom de base InMemory distinct. Cela garantit l'absence d'interférence entre benchmarks lors d'une exécution complète.

### `NoOpNotificationService`

Implémentation vide de `INotificationService` qui remplace SignalR pendant les benchmarks. Cela permet d'isoler exclusivement la logique métier et la couche de persistance, sans pollution liée aux websockets.

```csharp
public sealed class NoOpNotificationService : INotificationService
{
    public Task NotifyTeamNewTicketAsync(string team, TicketResponseDto ticket)
        => Task.CompletedTask;
}
```

---

## 5. Benchmarks par couche

### 5.1 Couche Domain

#### `SerialNumberBenchmark`

Mesure le coût de création du Value Object `SerialNumber`, qui effectue une normalisation (trim + toUpper) et une validation de longueur à chaque instanciation.

**Paramètre** : `[Params("SRV-001", "  LPT-99887  ", "NETWORK-DEVICE-XYZ-2024")]`

| Cas | Ce qui est mesuré |
|---|---|
| `Create(value)` (baseline) | Trim + ToUpper + validation + allocation |
| `EqualityCheck` | Comparaison structurelle de deux SerialNumber |
| `InequalityCheck` | Comparaison négative |

---

#### `AssetStateMachineBenchmark`

Mesure le coût de chaque transition de l'automate d'état de l'entité `Asset`.

```
InService ──► Down ──► InMaintenance ──► InService
                  └──────────────────► Decommissioned
```

| Cas | Transition |
|---|---|
| `AssetConstruction` (baseline) | Instanciation + initialisation |
| `MarkAsDown` | `InService → Down` |
| `MarkInMaintenance` | `Down → InMaintenance` |
| `RestoreToService` | `InMaintenance → InService` |
| `Decommission` | `InService → Decommissioned` |
| `FullStateCycle` | Enchaînement `InService → Down → InMaintenance → InService` |

---

#### `MaintenanceTicketStateMachineBenchmark`

Mesure le coût des transitions d'état de l'entité `MaintenanceTicket` ainsi que la validation des invariants dans le constructeur.

```
Opened ──► InProgress ──► Closed
```

| Cas | Ce qui est mesuré |
|---|---|
| `Construction` (baseline) | Constructeur + validation des invariants métier |
| `AssignToTechnician` | `Opened → InProgress` |
| `Close` | `InProgress → Closed` |
| `FullCycle` | Construction + Assign + Close enchaînés |
| `CriticalityVariants` | Coût de construction Low vs High |

---

### 5.2 Couche Application — Validators

#### `CreateTicketValidatorBenchmark`

Mesure le coût de la validation FluentValidation sur `CreateMaintenanceTicketCommand`. Ce validator s'exécute sur chaque requête HTTP entrante, son coût doit rester minimal.

| Cas | Ce qui est mesuré |
|---|---|
| `ValidateValid` (baseline) | Happy path — aucune erreur collectée |
| `ValidateInvalidAssetId` | `Guid.Empty` — échec rapide sur premier champ |
| `ValidateInvalidTitle` | Titre vide |
| `ValidateInvalidCriticality` | Valeur hors énumération |
| `ValidateAllFieldsInvalid` | Collecte du nombre maximum d'erreurs |
| `ValidateValidAsync` | Overhead de la version asynchrone |

---

### 5.3 Couche Application — Use Cases

#### `RegisterAssetBenchmark`

Mesure le pipeline `RegisterAsset` de bout en bout : validation du numéro de série + vérification d'unicité (`ExistsWithSerialNumberAsync`) + persistance + mapping DTO.

**Paramètre** : type d'asset (`Server`, `Laptop`, `NetworkDevice`)

---

#### `RegisterAssetDuplicateSerialBenchmark`

Mesure spécifiquement le coût de la détection de doublon de numéro de série, qui implique un `AnyAsync` EF Core supplémentaire.

| Cas | Comportement attendu |
|---|---|
| `Register_UniqueSerial` (baseline) | `AnyAsync` retourne `false` → persistance |
| `Register_DuplicateSerial` | `AnyAsync` retourne `true` → `DomainException` |
| `Register_NormalizedSerial` | Serial avec espaces et casse mixte → normalisation avant recherche |

---

#### `GetAllAssetsBenchmark`

Valide l'impact du **Pattern Décorateur Cache** sur `GetAllAssets`. C'est le benchmark le plus important pour démontrer la valeur architecturale du `CachedAssetRepository`.

**Paramètre** : `[Params(10, 100, 500)]` assets en base

| Cas | Comportement |
|---|---|
| `GetAll_CacheMiss` (baseline) | Requête EF Core complète → peuplement du cache |
| `GetAll_CacheHit` | Lecture depuis `IMemoryCache` — contourne EF |

---

#### `CreateTicketBenchmark`

Mesure le pipeline complet de création de ticket, qui est le cas d'utilisation le plus complexe de l'application :

> Récupération asset → validation domaine → résolution stratégie (Pattern Strategy) → mutation d'état → persistance → notification no-op → mapping DTO

| Cas | Équipe assignée |
|---|---|
| `CreateTicket_Server_High` (baseline) | `Infrastructure-Serveurs` |
| `CreateTicket_Laptop_High` | `Support-VIP` |
| `CreateTicket_Laptop_Medium` | `Support-Lectorat` |
| `CreateTicket_Network_Low` | `Réseau-Télécom` |

---

#### `TicketLifecycleBenchmark`

Mesure le cycle de vie complet d'un ticket (`Create → Assign → Close`) pour quantifier le coût de l'automate d'état en cascade sur l'asset lié.

| Cas | Ce qui est mesuré |
|---|---|
| `FullLifecycle` (baseline) | Les 3 handlers enchaînés + restauration asset |
| `AssignOnly` | `AssignTicketToTechnicianHandler` seul |
| `CloseOnly` | `CloseTicketHandler` seul + vérification tickets restants |

---

#### `CloseTicketBenchmark`

Valide le comportement conditionnel de `CloseTicket` : `RestoreToService` sur l'asset n'est déclenché que si c'est le **dernier ticket actif**. Ce comportement implique un `CountActiveTicketsByAssetIdAsync` supplémentaire.

---

#### `DecommissionAssetBenchmark`

Mesure les deux chemins du cas d'utilisation `DecommissionAsset`, qui incarne la règle métier : *un asset ne peut pas être décommissionné s'il a des tickets actifs*.

**Paramètre** : `[Params(1, 5, 10)]` tickets actifs sur l'asset bloqué

| Cas | Résultat |
|---|---|
| `Decommission_Success` (baseline) | Décommissionnement réussi |
| `Decommission_Blocked` | `DomainException` levée après `CountActiveTickets` |

---

#### `ConcurrentTicketCreationBenchmark`

Simule une charge concurrente : plusieurs techniciens déclarant des incidents simultanément.

**Paramètre** : `[Params(5, 20, 50)]` tickets concurrents

| Cas | Méthode |
|---|---|
| `CreateTickets_Sequential` (baseline) | Boucle `for` séquentielle |
| `CreateTickets_Parallel` | `Task.WhenAll` sur N handlers indépendants |

> Note : sur base InMemory, le parallélisme n'apporte pas de gain car il n'y a pas d'I/O réel. Sur SQL Server, les résultats seraient inversés.

---

### 5.4 Couche Infrastructure

#### `AssetRepositoryBenchmark`

Mesure toutes les méthodes EF Core du `AssetRepository` sans décorateur cache, pour obtenir le coût brut de chaque requête.

**Paramètre** : `[Params(10, 100, 500)]` assets

| Cas | Requête EF Core |
|---|---|
| `GetAllReadOnly` (baseline) | `AsNoTracking().ToListAsync()` |
| `GetById_Found` | `Include(tickets).FirstOrDefaultAsync(id)` trouvé |
| `GetById_NotFound` | `FirstOrDefaultAsync(id)` non trouvé |
| `ExistsSerial_Found` | `AnyAsync(sn == value)` — trouvé |
| `ExistsSerial_NotFound` | `AnyAsync(sn == value)` — non trouvé |
| `ExistsSerial_WithTrimAndCase` | Même requête avec normalisation préalable |

---

#### `CachedRepositoryBenchmark`

Mesure directement le **Pattern Décorateur** `CachedAssetRepository`, en comparant les performances brutes EF vs cache miss vs cache hit.

**Paramètre** : `[Params(10, 100, 500)]` assets

| Cas | Comportement |
|---|---|
| `GetAll_NoCache` (baseline) | `AssetRepository.GetAllReadOnlyAsync()` direct |
| `GetAll_CacheMiss` | Cache vidé → lecture EF → stockage dans `IMemoryCache` |
| `GetAll_CacheHit` | Lecture depuis `IMemoryCache` uniquement |

---

#### `MaintenanceTicketRepositoryBenchmark`

Mesure les requêtes du `MaintenanceTicketRepository`, avec focus sur `CountActiveTicketsByAssetIdAsync` — appelée dans `CloseTicket` et `DecommissionAsset`.

**Paramètre** : `[Params(10, 100, 500)]` tickets totaux (répartition 70% Opened / 20% InProgress / 10% Closed)

---

#### `ExceptionHandlingSerializationBenchmark`

Mesure le coût du `ExceptionHandlingMiddleware` pour chaque type d'exception géré : sérialisation JSON de `ProblemDetails` (RFC 7807) et dispatch du `switch` par type.

| Cas | Code HTTP |
|---|---|
| `Serialize_DomainException` (baseline) | 400 Bad Request |
| `Serialize_ConcurrencyException` | 409 Conflict |
| `Serialize_ServerError` | 500 Internal Server Error |
| `Dispatch_DomainException` | Switch pattern — premier cas |
| `Dispatch_ConcurrencyException` | Switch pattern — deuxième cas |

---

#### `DependencyInjectionResolutionBenchmark`

Mesure le coût de résolution des handlers depuis le conteneur DI Microsoft. Chaque requête HTTP crée un nouveau scope — ce coût s'ajoute à chaque appel.

| Cas | Profondeur de la chaîne |
|---|---|
| `Resolve_GetAllAssetsHandler` (baseline) | Handler + CachedRepository + DbContext |
| `Resolve_RegisterAssetHandler` | Handler + Repository + DbContext |
| `Resolve_CreateTicketHandler` | Handler + 5 dépendances |
| `ScopeLifetime` | Création scope + résolution + `DisposeAsync` |

---

## 6. Résultats d'exécution

Résultats obtenus sur l'environnement de développement. Les valeurs absolues varient selon le matériel ; les **ratios** sont comparables entre machines.

### Couche Domain

| Benchmark | Moyenne | Allocated |
|---|---|---|
| `SerialNumber.Create("SRV-001")` | ~35 ns | 0 B |
| `Asset construction` | ~160 ns | ~192 B |
| `Asset.MarkAsDown()` | ~182 ns | ~192 B |
| `Asset.MarkInMaintenance()` | ~177 ns | ~192 B |
| `Asset.RestoreToService()` | ~186 ns | ~192 B |
| `Asset.Decommission()` | ~163 ns | ~192 B |
| `Cycle complet Asset` | ~192 ns | ~192 B |
| `Ticket construction` | ~170 ns | ~224 B |
| `Ticket.AssignToTechnician()` | ~167 ns | ~224 B |
| `Ticket.Close()` | ~176 ns | ~224 B |
| `Cycle complet Ticket` | ~197 ns | ~224 B |

### Pattern Strategy

| Cas | Moyenne | Ratio |
|---|---|---|
| `Server → Infrastructure-Serveurs` (baseline) | ~42 ns | 1.00× |
| `Network → Réseau-Télécom` | ~52 ns | 1.24× |
| `Laptop High → Support-VIP` | ~62 ns | 1.48× |
| `Laptop Medium → Support-Lectorat` | ~76 ns | 1.81× |
| `Fallback → Support-Général` | ~79 ns | 1.88× |

### Décorateur Cache — GetAllAssets

| AssetCount | Sans cache | Cache miss | Cache hit | Gain (hit vs EF) |
|---|---|---|---|---|
| 10 | ~25 µs | ~28 µs | ~0.4 µs | **62×** |
| 100 | ~120 µs | ~140 µs | ~0.6 µs | **200×** |
| 500 | ~980 µs | ~1 050 µs | ~0.85 µs | **1 153×** |

### Use Cases applicatifs (InMemory DB)

| Use Case | Moyenne |
|---|---|
| `RegisterAsset` | ~2.2 ms |
| `CreateTicket` (tous types) | ~2.0–2.2 ms |
| `AssignTicket` | ~3.0 ms |
| `CloseTicket` | ~3.5 ms |
| `FullLifecycle (Create→Assign→Close)` | ~5.0 ms |
| `Decommission (succès)` | ~3.8 ms |
| `Decommission (bloqué)` | ~1.0 ms |

### FluentValidation

| Cas | Moyenne | Ratio |
|---|---|---|
| Valide (baseline) | ~1.1 µs | 1.00× |
| AssetId vide | ~1.43 µs | 1.30× |
| Titre vide | ~1.44 µs | 1.31× |
| Criticité invalide | ~1.52 µs | 1.38× |
| Tous invalides | ~2.3 µs | 2.09× |
| ValidateAsync | ~1.2 µs | 1.09× |

### Résolution DI

| Handler | Moyenne |
|---|---|
| `GetAllAssetsHandler` | ~22 µs |
| `RegisterAssetHandler` | ~24 µs |
| `CreateMaintenanceTicketHandler` | ~26 µs |
| `Scope + résolution + dispose` | ~24 µs |

---

## 7. Analyse et conclusions

### ✅ Points forts validés

**Couche Domain — coût négligeable**
Toutes les transitions d'état entre 160 et 200 ns avec zéro allocation imprévue. L'automate d'état est transparent sur les performances. Le choix du mapping manuel vs AutoMapper est validé : `ToDto()` coûte ~35–55 ns sans aucune réflexion.

**Pattern Strategy — résolution linéaire efficace**
La résolution passe de 42 ns (Server, premier dans la chaîne) à 79 ns (Fallback, dernier). L'ordre des stratégies dans la chaîne a un impact mesurable mais reste négligeable pour une API.

**Décorateur Cache — gain exponentiel avec le volume**
C'est la décision architecturale qui apporte le plus de valeur : le cache hit est **62× à 1 153× plus rapide** que la lecture EF directe selon le nombre d'assets. Plus le parc grandit, plus le gain augmente.

**Résolution DI — stable et rapide**
22–26 µs par handler quelle que soit la profondeur de la chaîne. Le conteneur `Microsoft.Extensions.DependencyInjection` est très performant sur les graphes simples.

**FluentValidation — court-circuit efficace**
Le chemin valide (1.1 µs) est 2× plus rapide que le chemin "tous invalides" (2.3 µs), ce qui confirme le comportement `StopOnFirstFailure` attendu.

### ⚠️ Points d'attention

**`ExistsWithSerialNumberAsync` se dégrade avec le volume**
45 µs sur 10 assets, 640 µs sur 500 assets. Cette requête `AnyAsync` parcourt la table entière sur la base InMemory. Sur SQL Server sans index sur `SerialNumber`, la dégradation sera identique.

**Concurrence — pas de gain sur InMemory**
`Task.WhenAll` est légèrement plus lent que le séquentiel (overhead de planification sans I/O réel à masquer). Ce résultat est normal sur InMemory et s'inversera sur SQL Server.

**Middleware — sérialisation JSON coûteuse**
La sérialisation d'un `ProblemDetails` coûte ~600–800 ns, soit 15× le coût d'un dispatch switch (~4 ns). En régime d'erreurs fréquentes, cela peut représenter un overhead mesurable.

---

## 8. Recommandations d'optimisation

### Priorité haute

**Ajouter un index SQL sur `SerialNumber`**

Sans index, `ExistsWithSerialNumberAsync` effectue un scan complet à chaque `RegisterAsset`. Sur un parc de 10 000 assets, cette requête pourrait dépasser 10 ms.

```csharp
// AssetFlowDbContext.cs — dans OnModelCreating
modelBuilder.Entity<Asset>()
    .HasIndex(a => a.SerialNumber)
    .IsUnique();
```

**Ajouter un index sur `(AssetId, Status)` pour `CountActiveTickets`**

`CountActiveTicketsByAssetIdAsync` filtre sur deux colonnes. Un index composite évite le scan complet.

```csharp
modelBuilder.Entity<MaintenanceTicket>()
    .HasIndex(t => new { t.AssetId, t.Status });
```

### Priorité moyenne

**Mettre en cache `GetByIdAsync` pour les assets fréquemment consultés**

Le `CachedAssetRepository` ne met en cache que `GetAllReadOnlyAsync`. Étendre le cache à `GetByIdAsync` réduirait le coût de `CreateTicket` (qui charge l'asset) de ~65–360 µs.

**Configurer `StopOnFirstFailure` explicitement dans le validator**

FluentValidation collecte toutes les erreurs par défaut. Pour les requêtes d'API, ajouter `CascadeMode = CascadeMode.Stop` réduit le coût des cas invalides.

```csharp
public class CreateMaintenanceTicketValidator : AbstractValidator<CreateMaintenanceTicketCommand>
{
    public CreateMaintenanceTicketValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        // ...
    }
}
```

### Priorité basse

**Utiliser `AsNoTracking` partout où l'entité n'est pas modifiée**

`GetByIdAsync` charge l'asset avec tracking EF. Pour `CreateTicket`, l'asset est chargé uniquement pour lire son type — `AsNoTracking` réduirait l'allocation.

---

## 9. Référence des attributs BenchmarkDotNet

| Attribut | Effet |
|---|---|
| `[Benchmark]` | Marque une méthode comme benchmark |
| `[Benchmark(Baseline = true)]` | Définit la référence pour calculer les ratios |
| `[GlobalSetup]` | Exécuté une seule fois avant toutes les itérations |
| `[IterationSetup]` | Exécuté avant chaque itération (utile pour remettre l'état) |
| `[Params(...)]` | Paramétrise le benchmark avec plusieurs valeurs |
| `[MemoryDiagnoser]` | Active la mesure des allocations mémoire et du GC |
| `[RankColumn]` | Ajoute une colonne de classement relatif |
| `[Orderer(...)]` | Trie les résultats (ici : du plus rapide au plus lent) |

### Colonnes des résultats

| Colonne | Signification |
|---|---|
| `Mean` | Moyenne arithmétique de toutes les mesures |
| `Error` | Demi-intervalle de confiance à 99.9% |
| `StdDev` | Écart-type des mesures |
| `Median` | 50ème percentile |
| `Ratio` | Rapport au baseline (1.00 = aussi rapide) |
| `Gen0 / Gen1` | Collectes GC par 1 000 opérations |
| `Allocated` | Mémoire allouée par opération (managée uniquement) |
| `Alloc Ratio` | Rapport d'allocation au baseline |

### Interprétation des outliers

BenchmarkDotNet supprime automatiquement les outliers statistiques (valeurs aberrantes dues à des interruptions OS, JIT warm-up, etc.). Le nombre d'outliers retirés est reporté dans la section `// * Hints *` des résultats. Un grand nombre d'outliers sur un même benchmark indique généralement :

- Une instabilité environnementale (autres processus en arrière-plan)
- Un comportement non-déterministe dans le code mesuré (ex: GC pressure)
- Un `[IterationSetup]` trop coûteux qui interfère avec la mesure

---
