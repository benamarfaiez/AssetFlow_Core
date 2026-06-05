# AssetFlow Core

AssetFlow Core est une application d'exemple conçue avec les principes de la Clean Architecture, du Domain-Driven Design (DDD) et du pattern CQRS. Elle gère des assets (matériels), des équipes techniques et des tickets de maintenance, avec un moteur d'assignation automatique basé sur le pattern Strategy.

## Architecture & Principes
- Clean Architecture : séparation claire des couches `WebApi`, `Application`, `Domain` et `Infrastructure`.
- DDD : les entités du domaine (`Asset`, `Team`, `MaintenanceTicket`) encapsulent la logique métier et les invariants.
- CQRS / Vertical Slice : chaque cas d'usage implémente un Handler unique (Create, Update, Get, ...).
- Pattern Strategy : moteur d'assignation de tickets qui sélectionne une stratégie basée sur le type d'actif et la criticité du ticket.

## ✨ Fonctionnalités principales

- **Gestion des Actifs (Assets) :** Enregistrement, suivi d'inventaire unique par numéro de série, et déclassement des équipements obsolètes.
- **Cycle de vie des Tickets :** Création de tickets de maintenance assignés à des équipements spécifiques avec gestion fine des niveaux de criticité (`Low`, `Medium`, `High`).
- **Affectation Intelligente :** Attribution automatique ou manuelle des demandes d'intervention à des équipes techniques dédiées.
- **Résolution collaborative :** Processus de clôture des incidents avec traçabilité et rapports de résolution détaillés.
- **Haute Performance :** Couche de mise en cache avancée éliminant la fragmentation mémoire, validée par des tests rigoureux de benchmarking.
- **Extensibilité :** Architecture modulaire facilitant l'ajout de nouvelles fonctionnalités, types d'actifs ou stratégies d'assignation sans impact sur les composants existants.

## Cas d'usage (UseCases)
- Assets
  - `RegisterAsset` : enregistrer un nouvel asset
  - `GetAllAssets` : lister les assets
  - `DecommissionAsset` : mettre un asset au rebut
- Tickets
  - `CreateMaintenanceTicket` : ouvrir un ticket
  - `AssignTicketToTechnician` : assigner un ticket à un technicien
  - `CloseTicket` : clôturer un ticket
  - `GetTicket` : récupérer un ticket
  - `RequestTicketTransfer` : demander un transfert
- Teams
  - `CreateTeam` : créer une équipe d'astreinte
  - `GetTeam` : récupérer une équipe
  - `UpdateTeam` : mettre à jour une équipe

## Moteur d'assignation automatique (Strategy)
Le moteur `TicketAssignmentEngine` résout dynamiquement la meilleure `IAssignmentStrategy` pour un couple `(AssetType, TicketCriticality)`.

Stratégies implémentées :
- `LaptopHighCriticalityStrategy` : match si `AssetType == Laptop` et `TicketCriticality == High`
- `LaptopStandardStrategy` : match si `AssetType == Laptop` et `TicketCriticality != High`
- `NetworkAssignmentStrategy` : match si `AssetType == NetworkDevice`
- `ServerAssignmentStrategy` : match si `AssetType == Server`

Algorithme résumé :
1. DI injecte `IEnumerable<IAssignmentStrategy>` dans `TicketAssignmentEngine`.
2. `ResolveTeamIdAsync` sélectionne la première stratégie dont `IsMatch(assetType, criticality)` retourne `true`.
3. La stratégie appelle `TeamRepository.GetByAssetTypeAndCriticalityAsync(assetType, criticality)` pour récupérer l'équipe en base et renvoyer son `Name`.
4. Si aucune équipe n'est trouvée, `AssignmentStrategyBase.GetTeamNameAsync` lève une `DomainException` — d'où la nécessité de pré-seeder les équipes.
5. Si aucune stratégie ne matche, fallback explicite vers `LaptopStandardStrategy`.

## Pré-requis pour l'assignation automatique
Avant de créer des assets et tickets, assurez-vous d'avoir au minimum ces 4 équipes en base :
- `LaptopHighCriticality` (AssetType = `Laptop`, TicketCriticality = `High`)
- `LaptopStandard` (AssetType = `Laptop`, TicketCriticality != `High`)
- `NetworkAssignment` (AssetType = `NetworkDevice`)
- `ServerAssignment` (AssetType = `Server`)

Sans ces équipes, l'assignation automatique lèvera une `DomainException` et l'opération échouera.

## Flux fonctionnel (du seed à l'assignation)
1. Seed / Create Teams (4 équipes minimum).
2. `RegisterAsset` : persiste un nouvel asset.
3. `CreateMaintenanceTicket` :
   - Récupère l'asset et valide les invariants.
   - Convertit la criticité en énumération.
   - Appelle `TicketAssignmentEngine.ResolveTeamIdAsync(asset.Type, criticality)`.
   - La stratégie appropriée est choisie et renvoie le `team.Name`.
   - Le ticket est créé (assigné à `assignedTeamId`) et l'asset est marqué `Down`.
   - Persistance via `UnitOfWork.SaveChangesAsync()`.
4. `AssignTicketToTechnician` : mutation du ticket, `asset.MarkInMaintenance()` si nécessaire, puis `SaveChangesAsync()`.

## Ajouter une nouvelle stratégie
1. Implémentez `IAssignmentStrategy` (ou héritez de `AssignmentStrategyBase`).
2. Enregistrez la stratégie dans DI (injection scoped dans `Program.cs`).
3. Seed/ajoutez l'équipe correspondante en base via migration/seed ou API `CreateTeam`.

## Points opérationnels et recommandations
- L'ordre d'enregistrement des stratégies (DI) peut influer sur la priorité si plusieurs stratégies sont compatibles ; `TicketAssignmentEngine` prend la première match.
- `AssignmentStrategyBase` centralise la résolution d'équipe et lève une `DomainException` si la recherche échoue.
- Utiliser les DTOs exposés par l'API pour les réponses (ex : `TeamResponseDto` ne contient pas toutes les propriétés internes).

## Diagramme (flux)
```mermaid
flowchart LR
"CreateTicket" --> "Engine (TicketAssignmentEngine)"
"Engine (TicketAssignmentEngine)" --> "Strategy (LaptopHighCriticality, LaptopStandard, NetworkAssignment, ServerAssignment)"
"Strategy (LaptopHighCriticality, LaptopStandard, NetworkAssignment, ServerAssignment)" --> "Team"
```

## Tests
- Unit tests et Integration tests fournis pour la plupart des UseCases (handlers, repositories, controllers).

## Exécution
- .NET 8 requis
- Lancer les tests via Visual Studio Test Explorer ou `dotnet test`.

