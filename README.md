# AssetFlowCore

Ensemble d'outils et API de gestion d'actifs (Assets) et de tickets de maintenance industriels.


## 📋 Description

**AssetFlowCore** est une API REST hautement performante dédiée à la gestion du cycle de vie des actifs technologiques et industriels (ordinateurs, serveurs, infrastructures) et au suivi collaboratif des tickets de maintenance associés. 

Conçue selon les principes de la **Clean Architecture** et du **Domain-Driven Design (DDD)** en C#, cette solution offre une traçabilité logicielle absolue et une résilience robuste pour les équipes de support IT et les gestionnaires de parcs d'équipements.

---

## ✨ Fonctionnalités principales

- **Gestion des Actifs (Assets) :** Enregistrement, suivi d'inventaire unique par numéro de série, et déclassement des équipements obsolètes.
- **Cycle de vie des Tickets :** Création de tickets de maintenance assignés à des équipements spécifiques avec gestion fine des niveaux de criticité (`Low`, `Medium`, `High`).
- **Affectation Intelligente :** Attribution automatique ou manuelle des demandes d'intervention à des équipes techniques dédiées.
- **Résolution collaborative :** Processus de clôture des incidents avec traçabilité et rapports de résolution détaillés.
- **Haute Performance :** Couche de mise en cache avancée éliminant la fragmentation mémoire, validée par des tests rigoureux de benchmarking.

---

## 🛠️ Prérequis techniques

Avant de commencer, assurez-vous de disposer des outils suivants sur votre poste de travail :

- **SDK .NET 8.0** (version LTS ou supérieure)
- **IDE :** Visual Studio 2026.
- **Base de données :** SQL Server.
- **Docker :** Pour la conteneurisation et le déploiement.
