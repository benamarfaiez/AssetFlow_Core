# AssetFlow Core — Architecture (backend et frontend)

**Objet** — Vue structurelle du système : découpage en couches, règles de dépendances, patrons employés, flux de bout en bout, architecture frontend cible, intégration des deux, décisions d'architecture et points de fragilité.

Documents liés : [PRODUCT-REQUIREMENTS.md](PRODUCT-REQUIREMENTS.md) · [PRODUCT-SPECIFICATIONS.md](PRODUCT-SPECIFICATIONS.md) · [TECHNICAL-SPECIFICATION.md](TECHNICAL-SPECIFICATION.md) · [API-Specification.md](API-Specification.md)

> **Provenance.** La partie backend est **relevée dans le code** le 2026-08-04. La partie frontend est une **cible 🎯** : aucun workspace Angular n'existe encore.

---

## 1. Vue d'ensemble

```mermaid
flowchart TB
    subgraph client["Poste utilisateur"]
        SPA["Application Angular 22 🎯<br/>(standalone, Signals)"]
    end

    subgraph api["AssetFlowCore.WebApi (.NET 8)"]
        REST["API REST<br/>/api/assets · /api/tickets · /api/teams"]
        HUB["Hub SignalR<br/>/ticketHub"]
        WORKER["Worker d'assistance IA<br/>(BackgroundService)"]
    end

    subgraph donnees["Persistance"]
        SQL[("SQL Server<br/>t_assets · t_teams · t_maintenance_tickets")]
        DUCK[("DuckDB local<br/>rag_vectors")]
    end

    LLM["Fournisseur de modèle<br/>Azure OpenAI ou Ollama"]
    OTEL["Collecteur OTLP<br/>(tableau de bord Aspire en dev)"]

    SPA -- "HTTP/JSON" --> REST
    SPA -- "WebSocket" --> HUB
    REST --> SQL
    REST -- "met en file" --> WORKER
    WORKER --> SQL
    WORKER --> DUCK
    WORKER --> LLM
    REST -. "traces, métriques, journaux" .-> OTEL
    WORKER -. .-> OTEL
```

**Caractéristiques structurantes**

- Un **seul processus** héberge l'API REST, le hub temps réel et le worker d'analyse IA : pas de service séparé, pas de courtier de messages.
- La file d'analyse IA est un **canal en mémoire** : elle ne franchit pas la frontière du processus et ne survit pas à un redémarrage.
- La base vectorielle est un **fichier local** : elle n'est pas partagée entre instances, ce qui interdit une mise à l'échelle horizontale de l'analyse IA en l'état.
- L'orchestration locale est assurée par **.NET Aspire**, qui démarre SQL Server en conteneur et injecte la chaîne de connexion.

## 2. Backend — Clean Architecture

### 2.1 Couches et sens des dépendances

```mermaid
flowchart RL
    WEB["WebApi<br/>controllers · middleware · requêtes HTTP"]
    INFRA["Infrastructure<br/>EF Core · dépôts · cache · SignalR · RAG"]
    APP["Application<br/>cas d'usage · DTOs · validateurs · stratégies"]
    DOM["Domain<br/>entités · value objects · enums · interfaces de dépôt"]

    WEB --> APP
    WEB --> INFRA
    INFRA --> APP
    INFRA --> DOM
    APP --> DOM
```

Le **domaine ne dépend de rien**. L'application ne connaît ni EF Core ni ASP.NET : elle exprime ses besoins par des interfaces (`IAssetRepository`, `IUnitOfWork`, `INotificationService`, `IAIAssistanceQueue`…) que l'infrastructure implémente. L'inversion de dépendance est donc réelle, pas décorative.

### 2.2 Règles exécutables

Ces règles ne sont pas des conventions écrites : elles sont **vérifiées par `AssetFlowCore.ArchitectureTests` (ArchUnitNET)** et bloquent l'intégration continue.

| Règle | Portée |
|---|---|
| `Domain` ne dépend ni de `Application`, ni de `Infrastructure`, ni de `WebApi` | dépendances |
| `Application` ne dépend ni de `Infrastructure` ni de `WebApi` | dépendances |
| `Infrastructure` ne dépend pas de `WebApi` | dépendances |
| Les propriétés des entités du domaine n'ont **ni setter public ni setter protected** | encapsulation DDD |
| Toute classe `*Handler` réside dans `AssetFlowCore.Application.*` | CQRS |
| Les propriétés des `*Command` et `*Query` sont immuables | CQRS |
| Les dépôts d'infrastructure ne dépendent que d'interfaces de `Domain` ou `Application` | couplage |
| `WebApi` ne dépend d'aucun type `*Repository` | couplage |
| Les interfaces de `Domain` et `Application` sont préfixées `I` | nommage |

Un test vérifie en outre que l'AppHost Aspire déclare bien une ressource SQL Server et un projet exécutable pour l'API.

### 2.3 Flux d'une requête

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as ExceptionHandlingMiddleware
    participant CT as Controller (ISender)
    participant VB as ValidationBehavior
    participant H as Handler
    participant R as Dépôt (+ décorateur cache)
    participant U as UnitOfWork
    participant N as NotificationService

    C->>MW: requête HTTP
    MW->>CT: passe la main
    CT->>VB: Send(Command)
    VB->>VB: validateurs FluentValidation
    alt validation en échec
        VB--xMW: ValidationException
        MW-->>C: 400 ProblemDetails + errors
    else valide
        VB->>H: Handle(Command)
        H->>R: lecture des agrégats
        H->>H: invariants du domaine (méthodes d'entité)
        H->>U: SaveChangesAsync (unique)
        H->>N: notification temps réel
        H-->>CT: DTO
        CT-->>C: 200 / 201 / 204
    end
```

Points structurants :

- Les controllers n'injectent que `ISender` : ils traduisent une requête HTTP en commande et ne contiennent aucune logique.
- La validation de surface est un **comportement de pipeline** MediatR, pas du code de handler.
- **L'exception est le canal d'erreur** : les handlers lèvent, un middleware unique traduit en `ProblemDetails`.
- **Une seule validation transactionnelle par cas d'usage** (`Unit of Work`), donc atomicité.

### 2.4 Patrons employés

| Patron | Emplacement | Rôle |
|---|---|---|
| **CQRS / tranches verticales** | `Application/UseCases/<Domaine>/<CasDUsage>/` | un dossier = une commande ou requête + son handler + son validateur |
| **Médiateur** | MediatR, `ISender` dans les controllers | découple le point d'entrée du cas d'usage |
| **Comportement de pipeline** | `ValidationBehavior` | validation transverse avant tout handler |
| **Stratégie** | `IAssignmentStrategy` + `TicketAssignmentEngine` | routage des incidents extensible sans modification du code existant |
| **Décorateur** | `CachedAssetRepository`, `CachedTeamRepository` | cache mémoire transparent autour des dépôts EF |
| **Unit of Work** | `UnitOfWork` | validation transactionnelle unique, accès groupé aux dépôts |
| **Dépôt** | `Domain/Repositories` + implémentations EF | isole la persistance du domaine |
| **Value Object** | `SerialNumber` | normalisation et invariants du numéro de série |
| **Options** | `DatabaseOptions` | configuration typée |
| **File + worker** | `AIAssistanceQueue` (canal) + `AIAssistanceWorker` | sortie du traitement IA du chemin de requête |
| **Abstraction de notification** | `INotificationService` | SignalR en production, implémentation neutre en benchmark |

### 2.5 Moteur de routage (Stratégie)

```mermaid
flowchart LR
    H["CreateMaintenanceTicketHandler"] --> E["TicketAssignmentEngine"]
    E -->|"IEnumerable&lt;IAssignmentStrategy&gt;"| L["Stratégies injectées<br/>Server · Network · LaptopHigh · LaptopStandard"]
    L -->|"première IsMatch(type, criticité)"| S["Stratégie retenue"]
    L -.->|"aucune correspondance"| F["Repli : LaptopStandardStrategy"]
    S --> T["TeamRepository<br/>GetByAssetTypeAndCriticalityAsync"]
    T --> R[("t_teams")]
```

Deux propriétés à connaître :

- **L'ordre d'enregistrement dans le conteneur d'injection fait office de priorité** : le moteur retient la *première* stratégie correspondante. Réordonner les enregistrements change le comportement fonctionnel.
- Le couple (type d'actif, criticité) est stocké **en texte** sur l'équipe et comparé au nom de la valeur d'énumération. L'extensibilité est donc totale (ajouter une stratégie + des données), mais le typage fort s'arrête à la frontière de la base.

### 2.6 Pipeline d'assistance IA

```mermaid
sequenceDiagram
    participant H as CreateTicketHandler
    participant Q as AIAssistanceQueue (canal mémoire)
    participant W as AIAssistanceWorker
    participant O as Sonde de connectivité (Ollama)
    participant V as LocalVectorStore (DuckDB)
    participant L as Modèle de langage
    participant DB as SQL Server

    H->>Q: met en file l'identifiant de l'incident
    W->>Q: attend un élément
    W->>O: le démon est-il joignable ? (mode Ollama seulement)
    alt indisponible
        W->>Q: réinjecte, pause de 5 s
    else disponible
        W->>DB: relit l'incident
        W->>L: vectorise la description
        W->>V: recherche par similarité cosinus (topK 3, seuil 0,7)
        W->>L: génère la note d'assistance (Markdown)
        W->>DB: enregistre la note, termine le traitement
    end
```

Chaque incident est traité dans une **portée d'injection dédiée** (`CreateAsyncScope`), ce qui libère proprement la connexion DuckDB (`IAsyncDisposable`). La bascule Azure OpenAI / Ollama se fait par configuration ; la sonde de connectivité n'est enregistrée qu'en mode Ollama et le worker la résout de façon facultative.

⚠️ **Aucun code de production n'alimente `rag_vectors`** : la recherche de similarité ne retourne jamais rien et la note est produite sans contexte historique. Le mécanisme est complet, le corpus est vide.

### 2.7 Persistance

- Un `DbContext` par requête, fourni par Aspire ; configurations `IEntityTypeConfiguration` découvertes par assembly.
- Énumérations converties en chaînes, `SerialNumber` en type possédé, index uniques sur le numéro de série et le nom d'équipe, clés étrangères en `RESTRICT`.
- **Concurrence optimiste** sur les incidents (`row_version`) ; conflit traduit en 409.
- Lectures d'inventaire en `AsNoTracking` derrière un cache mémoire de 5 minutes ; lectures destinées à une mutation **suivies** par le change tracker (`GetByIdAsync` tracké, `GetByIdWithTrackingAsync` pour l'accès explicite).
- Les mises à jour et suppressions d'équipes utilisent `ExecuteUpdate`/`ExecuteDelete` en relationnel, avec un chemin de repli pour le fournisseur InMemory des tests.

## 3. Frontend — Architecture cible 🎯

### 3.1 Découpage orienté fonctionnalités

```mermaid
flowchart TB
    subgraph app["src/app"]
        ROUTES["app.routes.ts<br/>routes racine, 100 % lazy"]
        CONFIG["app.config.ts<br/>providers racine"]

        subgraph features["features/"]
            FA["assets/"]
            FT["tickets/"]
            FE["teams/"]
        end

        subgraph shared["shared/"]
            UI["ui/ · pipes/ · directives/"]
            MODELS["models/<br/>types du contrat d'API"]
        end

        subgraph core["core/"]
            API["api/<br/>clients HTTP typés"]
            HTTP["http/<br/>interceptors fonctionnels"]
            GUARDS["guards/"]
            RT["realtime/<br/>client SignalR"]
        end
    end

    ROUTES -->|loadChildren| features
    features --> shared
    features --> core
    core --> MODELS
```

**Règles de dépendances** (miroir frontend des règles backend) : `features/` → `shared/` + `core/` ; `shared/` sans dépendance métier ni réseau ; `core/` sans dépendance à `features/` ; **aucun import croisé entre deux features** — ce qui est partagé remonte dans `shared/`.

### 3.2 Flux de données dans une feature

```mermaid
flowchart LR
    V["Composant de page<br/>(OnPush, signaux)"] -->|"appelle une méthode"| S["Service d'état de feature<br/>signal · computed"]
    S -->|"Observable"| A["core/api<br/>service HttpClient typé"]
    A -->|"HTTP"| API["API .NET"]
    A -.->|"erreur"| I["core/http<br/>interceptor ProblemDetails"]
    S -->|"signaux en lecture seule"| V
    V -->|"entrées / sorties"| C["shared/ui<br/>composants de présentation"]
```

- L'état vit dans un **service de feature** exposant des signaux en lecture seule ; le composant ne fait que lire et déclencher.
- Les composants de `shared/` sont **purement présentationnels** : ils reçoivent des données et émettent des intentions, sans connaître ni HTTP ni routeur.
- Les erreurs sont normalisées une seule fois, dans un interceptor.

### 3.3 Propriété des zones de code

Le dépôt définit des agents spécialisés dont les périmètres délimitent l'architecture (`.claude/agents/`) :

| Zone | Responsable |
|---|---|
| workspace, configuration globale, routing racine | `angular-architect` |
| `features/` : écrans, logique, navigation | `angular-feature-dev` |
| `shared/` : design system, styles, thèmes, accessibilité | `ui-ux-designer` |
| `shared/models/`, `core/api/`, `core/http/`, `core/realtime/` | `dotnet-api-bridge` |
| revue de code | `angular-code-reviewer` · `dotnet-code-reviewer` |

## 4. Intégration backend ↔ frontend

### 4.1 Ouverture d'un incident, de bout en bout

```mermaid
sequenceDiagram
    participant U as Utilisateur
    participant SPA as Application Angular
    participant API as API REST
    participant ENG as Moteur de routage
    participant DB as SQL Server
    participant HUB as Hub SignalR
    participant Q as File IA

    U->>SPA: saisit titre, description, criticité
    SPA->>SPA: validation locale (formulaire typé)
    SPA->>API: POST /api/tickets
    API->>DB: relit l'actif, vérifie les invariants
    API->>ENG: résout l'équipe (type, criticité)
    ENG->>DB: recherche l'équipe active
    API->>DB: crée l'incident, met l'actif en panne (1 seul SaveChanges)
    API->>HUB: ReceiveNewTicket au groupe de l'équipe
    API->>Q: met l'incident en file pour analyse IA
    API-->>SPA: 201 + TicketResponseDto
    SPA->>SPA: met à jour l'état depuis la réponse (pas de rechargement)
    HUB-->>SPA: notification (si abonné au groupe)
```

### 4.2 Contraintes de couplage

| Sujet | Contrainte architecturale |
|---|---|
| **CORS** | politique active **en Development uniquement** → en production, même origine derrière un reverse proxy ; en développement, proxy du serveur Angular |
| **Contrat de types** | dérivé du C# ; propriétés `camelCase`, **valeurs d'enums `PascalCase`** ; aucune génération automatisée en place hors skill `/sync-api-dtos` |
| **Absence de versioning d'API** | toute évolution de contrat casse immédiatement le client : la synchronisation des types est une opération de maintenance récurrente, pas ponctuelle |
| **Codes de statut** | `PUT /api/teams/{id}` → 201 ; **aucun 404** (introuvable → 400) : le client ne peut pas s'appuyer sur le code seul pour distinguer les cas |
| **Cohérence de lecture** | cache serveur de 5 minutes non invalidé par les écritures : le client doit traiter la réponse d'écriture comme source de vérité |
| **Temps réel** | le groupe d'abonnement est un **nom d'équipe** ; sans notion d'utilisateur, l'appartenance à une équipe n'est pas déterminable côté client |
| **Sécurité** | aucune authentification : le frontend ne peut porter aucun contexte utilisateur ni protéger une route de façon significative |

## 5. Déploiement

```mermaid
flowchart TB
    subgraph dev["Développement"]
        AH["Aspire AppHost"] -->|démarre| SQLC[("Conteneur SQL Server")]
        AH -->|lance + injecte la chaîne| APIDEV["WebApi (localhost:5046 / 7138)"]
        AH --> DASH["Tableau de bord Aspire"]
        NG["Serveur Angular 🎯<br/>proxy.conf.json"] --> APIDEV
    end

    subgraph prod["Production (conteneurs)"]
        GH["GitHub Actions"] -->|"push sur main"| GHCR[("GHCR<br/>assetflow-api")]
        GHCR --> APIPROD["Conteneur API<br/>Alpine, non root, port 8080"]
        APIPROD --> SQLP[("SQL Server")]
        RP["Reverse proxy 🎯"] --> APIPROD
        RP --> STATIC["Frontend statique 🎯"]
    end
```

Le pipeline enchaîne compilation et vérification de format, tests d'architecture, tests unitaires et d'intégration, benchmarks, portail qualité SonarCloud, puis publication de l'image. Détail dans [TECHNICAL-SPECIFICATION.md](TECHNICAL-SPECIFICATION.md#19-intégration-continue).

## 6. Décisions d'architecture

| # | Décision | Contexte | Conséquences |
|---|---|---|---|
| AD-01 | **Clean Architecture en quatre couches** | isoler le métier des choix techniques | dépendances contrôlées et **vérifiées automatiquement** ; coût : plus de fichiers, indirections par interfaces |
| AD-02 | **CQRS en tranches verticales avec MediatR** | un cas d'usage = un dossier autonome | ajout de fonctionnalité sans toucher l'existant ; les handlers sont enregistrés **deux fois** (scan MediatR + `AddScoped` explicite pour les benchmarks), ce qui doit être maintenu de pair |
| AD-03 | **Validation par comportement de pipeline** | éviter la validation dispersée dans les handlers | contrôle systématique et homogène ; la validation métier profonde reste dans les entités |
| AD-04 | **L'exception comme canal d'erreur** | simplicité des handlers, traduction centralisée | code de cas d'usage lisible ; toute exception non prévue par le middleware devient un 500 — c'est ce qui arrive aux transitions d'état d'incident |
| AD-05 | **Routage par stratégies + données de référence** | ouvrir l'extension sans modifier le code | nouvelle règle = nouvelle classe + nouvelle équipe ; dépendance forte à un référentiel complet, et priorité déterminée par l'ordre d'injection |
| AD-06 | **Cache par décorateur** plutôt que dans les dépôts | garder les dépôts ignorants du cache | gain mesuré considérable en lecture ; **l'invalidation devient une responsabilité manuelle**, non tenue par les écritures passant par `IUnitOfWork` |
| AD-07 | **Mapping DTO manuel** | éviter la réflexion d'un mapper | coût CPU quasi nul, contrat explicite ; maintenance à la main à chaque évolution de DTO |
| AD-08 | **Concurrence optimiste sur les incidents** | plusieurs techniciens sur le même incident | conflit détecté et traduit en 409 ; le client doit savoir recharger |
| AD-09 | **Orchestration Aspire en développement** | démarrer la base et l'API d'un seul geste | expérience de développement fluide, télémétrie intégrée ; la clé de chaîne de connexion devient `assetflow-db`, ce qui complique l'exécution hors Aspire |
| AD-10 | **Base vectorielle DuckDB locale** | pas de service vectoriel externe à exploiter | aucune dépendance d'infrastructure ; fichier **local**, donc pas de partage entre instances ni de mise à l'échelle horizontale |
| AD-11 | **File d'analyse IA en mémoire** | sortir l'IA du chemin de requête sans courtier | réponse HTTP non pénalisée ; **demandes perdues au redémarrage**, aucune reprise |
| AD-12 | **Frontend Angular standalone + Signals** 🎯 | aligner sur les pratiques Angular 22 | pas de `NgModule`, réactivité fine, moins de RxJS ; NgRx SignalStore indisponible en stable pour Angular 22 |
| AD-13 | **Absence de versioning d'API** | contexte de projet unique | simplicité immédiate ; toute évolution de contrat est cassante pour le client |
| AD-14 | **Aucune authentification** (état de fait, non décidé) | portée initiale d'exemple | API ouverte : incompatible avec une mise en service, et bloquant pour tout écran contextualisé |

## 7. Points de fragilité architecturale

Classés par cause racine, avec la conséquence observable et la piste de correction.

| Cause racine | Conséquence | Piste |
|---|---|---|
| **Deux chemins vers la même abstraction** : `IUnitOfWork` instancie les dépôts EF directement, tandis que l'injection fournit les décorateurs de cache | les écritures n'invalident pas le cache → lectures périmées 5 minutes, garde-fou métier contournable | faire résoudre les dépôts de `UnitOfWork` par le conteneur, ou supprimer l'un des deux chemins d'accès |
| **Exceptions métier non homogènes** : `Asset` lève `DomainException`, `MaintenanceTicket` lève `InvalidOperationException` | erreurs métier normales remontées en 500 | unifier sur `DomainException` ou étendre le middleware |
| **Invariants d'unicité délégués à la base** : nom d'équipe unique en index seulement | violation d'unicité en 500 au lieu de 400/409 | contrôle applicatif préalable + traduction de `DbUpdateException` |
| **Ressources introuvables traitées comme des violations métier** | aucun 404 n'est jamais renvoyé, malgré les attributs déclarés | exception dédiée `NotFoundException` mappée en 404 |
| **État de traitement asynchrone non exposé** : `is_ai_processing` et `assistance_note` restent en base | la fonctionnalité IA est invisible et sans valeur perçue | enrichir le contrat de sortie, et notifier la fin de traitement |
| **Corpus vectoriel jamais alimenté** | recherche de similarité systématiquement vide | indexer les incidents à la clôture |
| **Portée du processus unique** : API, hub et worker ensemble ; file et base vectorielle locales | mise à l'échelle horizontale impossible sans perte de fonctionnalité | file et vecteurs externalisés si la charge le justifie |
| **Sondes de santé conditionnées à l'environnement** | conteneur systématiquement `unhealthy` en production | exposer les sondes hors Development, en maîtrisant leur exposition |
| **Configuration à trois noms de clé pour une même chaîne de connexion** | exécution par composition Docker inopérante sans ajustement | une clé unique, documentée |
| **Absence de jeton d'annulation aux points d'entrée** | traitement serveur poursuivi après abandon du client | propager `CancellationToken` du controller au dépôt |
| **Tests empruntant un chemin distinct de la production** (dépôts d'équipe, fournisseur InMemory) | faux sentiment de couverture | tests d'intégration sur un vrai SQL Server (conteneur éphémère) |

Aucune de ces fragilités n'est bloquante pour le fonctionnement nominal en développement ; les trois premières et la huitième sont à traiter avant toute mise en service, en même temps que l'authentification (AD-14).
