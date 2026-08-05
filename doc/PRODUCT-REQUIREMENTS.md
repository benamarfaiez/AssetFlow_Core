# AssetFlow Core — Exigences produit (PRD)

**Objet** — Ce document énonce le *pourquoi* et le *quoi* du produit : besoins, utilisateurs, exigences fonctionnelles et non fonctionnelles. Il ne décrit ni les écrans (voir [PRODUCT-SPECIFICATIONS.md](PRODUCT-SPECIFICATIONS.md)), ni la technique (voir [TECHNICAL-SPECIFICATION.md](TECHNICAL-SPECIFICATION.md) et [ARCHITECTURE.md](ARCHITECTURE.md)).

> ⚠️ **Provenance et limites de ce document.** Le produit existe déjà sous forme d'une API .NET 8 fonctionnelle. Les exigences ci-dessous ont été **reconstruites par lecture du code source** le 2026-08-04 : elles décrivent fidèlement ce que le logiciel fait. Les sept questions produit du §8 ont été **arbitrées le 2026-08-05** (Lot 0) ; il ne subsiste plus de ❓. Les éléments marqués 🎯 restent des cibles à construire, désormais adossées à une décision.

**Légende des statuts**

| Symbole | Signification |
|---|---|
| ✅ | implémenté et vérifié dans le code |
| 🟡 | partiellement implémenté |
| ⛔ | non implémenté, alors que nécessaire |
| 🎯 | cible proposée, à construire |
| ❓ | décision produit attendue — **plus aucune depuis le 2026-08-05**, voir §8 |

---

## 1. Vision et problème adressé

Les services informatiques gèrent un parc matériel hétérogène (serveurs, postes de travail, équipements réseau) dont les pannes doivent être tracées, routées vers la bonne équipe et résolues sans perte d'information.

**AssetFlow Core** répond à trois problèmes :

1. **Traçabilité du parc** — savoir quels équipements existent, dans quel état, avec une identification unique et fiable (numéro de série).
2. **Routage fiable des incidents** — supprimer l'arbitrage humain de l'affectation : un incident est automatiquement dirigé vers l'équipe d'astreinte compétente selon le type de matériel et la criticité déclarée.
3. **Cohérence des états** — garantir qu'un équipement et ses incidents ne peuvent pas se contredire (pas d'incident sur un équipement mis au rebut, pas de rebut d'un équipement en panne, remise en service automatique quand le dernier incident est clos).

Un quatrième axe est déjà amorcé : **l'assistance au diagnostic par IA**, qui produit une note de résolution à partir de la description de l'incident et d'incidents similaires passés.

## 2. Périmètre

### Dans le périmètre

- Inventaire des actifs matériels et cycle de vie associé.
- Cycle de vie des incidents de maintenance, de l'ouverture à la clôture.
- Référentiel des équipes d'astreinte et règles de routage.
- Notification temps réel de l'équipe destinataire d'un nouvel incident.
- Génération d'une note d'assistance au diagnostic par un modèle de langage.
- 🎯 Interface web (Angular) pour les techniciens et gestionnaires de parc.

### Hors périmètre (confirmé le 2026-08-05)

- **Affectation individuelle** des incidents : « prendre en charge » reste un geste d'équipe (décision produit n°2). Des **utilisateurs existeront** au Lot 7, apportés par l'authentification, et l'auteur d'une prise en charge ou d'une clôture sera enregistré — mais aucun incident ne sera attribué à une personne, ni aucune file personnelle constituée.
- Contrats, garanties, coûts, amortissement, fournisseurs.
- Emplacements physiques, sites, salles.
- Planification d'interventions, calendriers d'astreinte, SLA horaires.
- Pièces détachées, stocks.
- Import/export en masse, connecteurs vers un outil d'inventaire externe.
- Rapports et tableaux de bord analytiques.

## 3. Utilisateurs et rôles

| Rôle | Besoins principaux | Statut |
|---|---|---|
| **Technicien de maintenance** | déclarer un incident, le prendre en charge, le clôturer avec un compte rendu, consulter l'aide au diagnostic | ✅ couvert fonctionnellement |
| **Gestionnaire de parc** | enregistrer un équipement, consulter l'inventaire et les états, mettre au rebut | ✅ couvert fonctionnellement |
| **Responsable d'équipe d'astreinte** | être notifié des incidents affectés à son équipe, transférer un incident mal routé | 🟡 notification ✅, transfert ✅, mais aucune vue de la charge de l'équipe |
| **Administrateur du référentiel** | créer et maintenir les équipes et les règles de routage | ✅ couvert fonctionnellement |

> ⛔ **Aucun de ces rôles n'existe techniquement.** L'API n'a ni authentification ni autorisation : toute opération, y compris la suppression d'une équipe, est accessible anonymement. Les rôles ci-dessus sont des *personas* servant à structurer les besoins, pas des habilitations en vigueur. Voir [ENF-01](#51-sécurité).

## 4. Exigences fonctionnelles

Référence de vérification pour chaque ligne : [API-Specification.md](API-Specification.md).

### 4.1 Gestion des actifs

| Id | Exigence | Statut |
|---|---|---|
| EF-01 | Enregistrer un actif avec un libellé, un numéro de série et un type (`Server`, `Laptop`, `NetworkDevice`) | ✅ |
| EF-02 | Garantir l'unicité du numéro de série dans tout le parc, indépendamment de la casse et des espaces de bord | ✅ |
| EF-03 | Imposer un numéro de série de 5 à 50 caractères | ✅ |
| EF-04 | Consulter l'inventaire complet avec l'état de chaque actif | ✅ |
| EF-05 | Mettre un actif au rebut, opération refusée s'il porte des incidents en cours | ✅ |
| EF-06 | Consulter le détail d'un actif et son historique d'incidents | ⛔ aucun endpoint unitaire ni historique |
| EF-07 | Rechercher, filtrer et paginer l'inventaire | ⛔ non implémenté |
| EF-08 | Modifier le libellé d'un actif | ⛔ non implémenté |
| EF-09 | Remettre en service un actif mis au rebut | 🎯 **retenue** (décision produit n°1, 2026-08-05) : réversible, réservée à un rôle d'administrateur, motif obligatoire et opération tracée. Aujourd'hui encore un état terminal |

### 4.2 Cycle de vie des incidents

| Id | Exigence | Statut |
|---|---|---|
| EF-10 | Ouvrir un incident sur un actif, avec titre, description et criticité (`Low`, `Medium`, `High`) | ✅ |
| EF-11 | Refuser l'ouverture d'un incident sur un actif mis au rebut | ✅ |
| EF-12 | Router automatiquement l'incident vers l'équipe d'astreinte compétente selon (type d'actif, criticité) | ✅ |
| EF-13 | Basculer automatiquement l'actif en panne à l'ouverture d'un incident | ✅ |
| EF-14 | Prendre en charge un incident ouvert, ce qui place l'actif en maintenance | ✅ |
| EF-15 | Clôturer un incident en cours avec un compte rendu de résolution obligatoire | ✅ |
| EF-16 | Remettre l'actif en service à la clôture du **dernier** incident actif le concernant | ✅ |
| EF-17 | Transférer un incident vers une autre équipe, avec motif conservé | 🟡 le motif est concaténé à la description, non stocké séparément — **historique dédié retenu** (décision produit n°4, 2026-08-05) |
| EF-18 | Consulter un incident par son identifiant | ✅ |
| EF-19 | Lister et filtrer les incidents (par équipe, état, criticité, actif) | ⛔ **bloquant pour toute interface** |
| EF-20 | Relire la description et le compte rendu d'un incident | ⛔ absents du contrat de sortie |
| EF-21 | Affecter un incident à une personne | ⛔ **écartée** (décision produit n°2, 2026-08-05) : la prise en charge reste collective. Seule l'**identité de l'auteur** d'une prise en charge ou d'une clôture sera enregistrée, pour l'audit (Lot 7) |
| EF-22 | Protéger l'incident contre les modifications concurrentes | ✅ concurrence optimiste |

### 4.3 Équipes et routage

| Id | Exigence | Statut |
|---|---|---|
| EF-23 | Créer une équipe portant un nom unique et un couple (type d'actif, criticité) | 🟡 unicité non contrôlée fonctionnellement (erreur technique) |
| EF-24 | Consulter une équipe par identifiant | ✅ |
| EF-25 | Modifier partiellement une équipe | ✅ |
| EF-26 | Supprimer une équipe, opération refusée si des incidents actifs lui sont affectés | ✅ |
| EF-27 | Lister les équipes | ⛔ **bloquant pour toute interface** |
| EF-28 | Activer / désactiver une équipe sans la supprimer | ⛔ l'état existe en base mais aucune opération ne l'expose — **retenue en complément de la suppression** (décision produit n°5, 2026-08-05) |
| EF-29 | Étendre les règles de routage sans modifier le code existant | ✅ par ajout d'une stratégie + données de référence |

### 4.4 Temps réel et assistance IA

| Id | Exigence | Statut |
|---|---|---|
| EF-30 | Notifier en temps réel l'équipe destinataire d'un nouvel incident | ✅ |
| EF-31 | Notifier les changements d'état ultérieurs (prise en charge, clôture, transfert) | ⛔ un seul événement émis |
| EF-32 | Produire une note d'assistance au diagnostic à partir de la description de l'incident | ✅ en tâche de fond |
| EF-33 | Enrichir cette note par des incidents similaires passés (recherche vectorielle) | 🟡 mécanisme présent mais **aucun incident n'est indexé** : la recherche ne retourne rien — **indexation à la clôture et rétro-indexation retenues** (décision produit n°7, 2026-08-05) |
| EF-34 | Consulter la note d'assistance depuis l'interface | ⛔ absente du contrat de sortie |
| EF-35 | Connaître l'avancement du traitement IA d'un incident | ⛔ l'indicateur existe en base mais n'est pas exposé |
| EF-36 | Ne pas perdre les demandes d'analyse en cas de redémarrage | ⛔ file en mémoire, perdue au redémarrage |

### 4.5 Interface utilisateur 🎯

| Id | Exigence | Statut |
|---|---|---|
| EF-37 | Interface web permettant à un technicien de mener un incident de l'ouverture à la clôture sans appeler l'API directement | 🎯 |
| EF-38 | Interface de gestion du parc (inventaire, enregistrement, rebut) | 🎯 |
| EF-39 | Interface d'administration du référentiel d'équipes | 🎯 dépend de EF-27 |
| EF-40 | Réception et affichage des notifications temps réel | 🎯 |
| EF-41 | Interface exploitable sur mobile et tablette (mobile-first) | 🎯 |
| EF-42 | Thème clair et sombre | 🎯 |

## 5. Exigences non fonctionnelles

### 5.1 Sécurité

| Id | Exigence | Statut |
|---|---|---|
| ENF-01 | **Authentifier les utilisateurs** avant toute opération | ⛔ **aucune authentification** : l'API est totalement ouverte. Schéma retenu le 2026-08-05 (décision produit n°6) : **annuaire d'entreprise, OIDC / `JWT Bearer`** — réalisation au Lot 7 |
| ENF-02 | Restreindre les opérations sensibles selon le rôle | ⛔ aucune autorisation |
| ENF-03 | Ne jamais exposer de secret dans le code ou la configuration versionnée | ✅ secrets via User Secrets / variables d'environnement |
| ENF-04 | Ne pas divulguer d'information technique en cas d'erreur | ⛔ le détail des erreurs 500 contient le message d'exception brut |
| ENF-05 | Prévenir l'injection SQL | ✅ requêtes paramétrées, ORM |
| ENF-06 | Valider strictement les entrées | 🟡 validation présente mais incomplète (voir [PRODUCT-SPECIFICATIONS.md](PRODUCT-SPECIFICATIONS.md#5-validation-des-saisies)) |

> **ENF-01 et ENF-02 constituent le principal risque produit.** Ils doivent être traités avant toute exposition au-delà d'un réseau de confiance, et avant la mise en service de l'interface web.

### 5.2 Performance

| Id | Exigence | Statut |
|---|---|---|
| ENF-07 | Réponse aux lectures d'inventaire sous la charge d'un parc de plusieurs centaines d'actifs | ✅ cache mémoire mesuré 62× à 1 153× plus rapide selon le volume |
| ENF-08 | Non-régression de performance vérifiée à chaque évolution | ✅ suite BenchmarkDotNet exécutée en intégration continue |
| ENF-09 | **Cohérence lecture/écriture** : une écriture doit être immédiatement visible en lecture | ⛔ le cache de 5 minutes n'est pas invalidé par les écritures d'actifs |
| ENF-10 | Ne pas bloquer la réponse HTTP par le traitement IA | ✅ traitement asynchrone en tâche de fond |

### 5.3 Fiabilité et exploitation

| Id | Exigence | Statut |
|---|---|---|
| ENF-11 | Atomicité de chaque opération métier | ✅ une seule validation transactionnelle par cas d'usage |
| ENF-12 | Détection des modifications concurrentes | ✅ jeton de concurrence sur les incidents |
| ENF-13 | Sondes de santé exploitables par l'orchestrateur | ⛔ sondes exposées **uniquement en développement**, alors que les conteneurs les interrogent en production |
| ENF-14 | Traces, métriques et journaux exploitables | ✅ OpenTelemetry (traces, métriques, journaux), export OTLP conditionnel |
| ENF-15 | Dégradation maîtrisée si le fournisseur d'IA est indisponible | 🟡 l'incident est réinjecté en file, mais la file est volatile |
| ENF-16 | Annulation du traitement serveur si le client abandonne | ⛔ aucun jeton d'annulation dans les points d'entrée |

### 5.4 Qualité

| Id | Exigence | Statut |
|---|---|---|
| ENF-17 | Règles d'architecture vérifiées automatiquement | ✅ tests ArchUnitNET bloquants en intégration continue |
| ENF-18 | Couverture de tests mesurée et contrôlée | ✅ 176 tests unitaires + tests d'intégration, portail qualité SonarCloud bloquant |
| ENF-19 | Format de code homogène | ✅ `dotnet format` bloquant en intégration continue |

### 5.5 Utilisabilité et accessibilité 🎯

| Id | Exigence | Statut |
|---|---|---|
| ENF-20 | Conformité WCAG 2.2 niveau AA de l'interface web | 🎯 |
| ENF-21 | Utilisation complète au clavier seul | 🎯 |
| ENF-22 | Interface **multilingue**, français en locale de référence ; les valeurs techniques de l'API sont en anglais et doivent être traduites à l'affichage | 🎯 décision technique du 2026-08-05 : mécanisme d'internationalisation posé **avant** le premier écran, aucun texte visible en dur dans un gabarit |
| ENF-23 | Rendu correct de 320 px à 200 % de zoom | 🎯 |

### 5.6 Contraintes techniques

- Backend .NET 8, base SQL Server, orchestration .NET Aspire en développement, déploiement conteneurisé.
- Frontend Angular 22 en composants standalone et Signals.
- L'API n'est **pas encore versionnée**, mais le sera : décision technique du 2026-08-05, URL sous `/api/v1/...` avant la construction des écrans. D'ici là, toute évolution de contrat est immédiatement visible par les consommateurs.
- Détail dans [TECHNICAL-SPECIFICATION.md](TECHNICAL-SPECIFICATION.md).

## 6. Prérequis de données

Le routage automatique repose entièrement sur des **données de référence** : il faut au moins une équipe active par couple (type d'actif, criticité) réellement utilisé, soit jusqu'à **9 combinaisons**. Sans elles, l'ouverture d'incident échoue. Les 9 combinaisons sont amorcées par la migration `SeedReferenceTeams` (Lot 1) ; l'application des migrations reste 🟡 **manuelle** au déploiement (étape 8.6).

⚠️ Conséquence de la décision produit n°5 : une équipe **désactivée** ne couvre plus sa combinaison. Désactiver la dernière équipe d'un couple (type, criticité) suffit à bloquer l'ouverture d'incidents pour ce couple — l'écran d'administration doit donc signaler la perte de couverture avant confirmation.

## 7. Manques bloquants pour la construction de l'interface

État réévalué le 2026-08-05, après la complétion du contrat d'API (Lot 2). Les cinq manques de contrat relevés au 2026-08-04 sont **levés** ; un seul manque bloquant subsiste.

| Manque | Écran empêché | État |
|---|---|---|
| ⛔ pas d'authentification (`ENF-01`) | tout écran nécessitant un contexte utilisateur : abonnement temps réel au groupe de son équipe, masquage des actions non autorisées, remise en service réservée à un administrateur | **subsiste** — schéma retenu (décision n°6), réalisation au Lot 7 |
| ~~pas de liste des incidents (`EF-19`)~~ | file de travail du technicien | ✅ levé — `GET /api/tickets`, filtré, trié, paginé |
| ~~pas de liste des équipes (`EF-27`)~~ | administration des équipes, sélecteur de transfert | ✅ levé — `GET /api/teams`, avec `?onlyActive=true` |
| ~~description et compte rendu absents du contrat (`EF-20`)~~ | consultation d'un incident | ✅ levé — `TicketResponseDto` enrichi |
| ~~note d'assistance IA non exposée (`EF-34`)~~ | aide au diagnostic | 🟡 exposée (`assistanceNote`, `isAiProcessing`), mais la **fin d'analyse n'est pas notifiée** — Lot 6 |
| ~~type d'actif et criticité absents du contrat des équipes~~ | formulaire d'édition d'une équipe | ✅ levé — `TeamResponseDto` enrichi |

## 8. Décisions produit ✅ tranchées le 2026-08-05

Les sept questions ouvertes ont été arbitrées (Lot 0 du [plan d'implémentation](IMPLEMENTATION-PLAN.md#3-lot-0--décisions-préalables--2026-08-05)). Aucune n'est réalisée à ce jour : chacune renvoie au lot qui la porte.

| # | Question | Décision | Portée par |
|---|---|---|---|
| 1 | La mise au rebut doit-elle rester **irréversible** ? | **Non — réversible, réservé à un rôle d'administrateur**, avec motif obligatoire et opération tracée. Motif déterminant : le numéro de série d'un actif au rebut reste réservé dans tout le parc, donc un rebut par erreur interdit définitivement de réenregistrer la machine. Ouvre `EF-09` | Lot 2 bis (endpoint), Lot 5 (interface), Lot 7 (habilitation) |
| 2 | Faut-il des **techniciens nominatifs** ? | **Non — la prise en charge reste collective**, au niveau de l'équipe. L'identité de qui prend en charge et de qui clôture est **enregistrée pour l'audit**, sans affectation dirigée ni file personnelle : `EF-21` reste hors périmètre, la planification l'étant déjà | Lot 7 (dépend de l'identité) |
| 3 | Que devient le statut `Resolved` ? | **Supprimé** du modèle. Le cycle reste `Opened → InProgress → Closed`. Une étape « résolu en attente de validation » exigerait un acteur validateur, que le produit n'a pas | Lot 2 bis |
| 4 | Le **motif de transfert** doit-il être historisé séparément ? | **Oui — historique de transferts dédié** (incident, équipe d'origine, équipe cible, motif, date). Aujourd'hui le motif est concaténé à la description, qui perd donc irréversiblement le texte saisi par le technicien | Lot 2 bis (contrat), Lot 5 (affichage) |
| 5 | La **désactivation** d'équipe doit-elle remplacer la suppression ? | **Non — elle s'y ajoute.** La désactivation devient le geste courant ; la suppression, de fait impossible dès qu'un incident même clôturé référence l'équipe, reste ouverte aux équipes créées par erreur. `EF-26` est conservée | Lot 2 bis, Lot 5 |
| 6 | Quel niveau d'authentification est visé ? | **Annuaire d'entreprise (Microsoft Entra ID)** — OIDC, jetons `JWT Bearer`, rôles dérivés des groupes d'annuaire. L'API ne gère aucun mot de passe. Prérequis d'exploitation : un tenant et un enregistrement d'application | Lot 7 |
| 7 | L'indexation des incidents clôturés est-elle au périmètre ? | **Oui — indexation à la clôture, avec rétro-indexation** des incidents déjà clos par une commande rejouable. Sans corpus, `EF-33` est nul. Limite assumée : la base vectorielle est un fichier **local** au processus, non partagé entre instances | Lot 6 |

**Décisions techniques associées** (mêmes date et lot, détail dans [TECHNICAL-SPECIFICATION.md](TECHNICAL-SPECIFICATION.md#26-décisions)) : URL d'API **versionnées** (`/api/v1/...`), frontend déployé en **conteneur nginx dédié** derrière un reverse proxy, interface **multilingue** dès le premier écran, état géré par **Signals natifs**.

## 9. Risques

| Risque | Impact | Probabilité | Atténuation |
|---|---|---|---|
| API ouverte sans authentification | destruction ou fuite de données | élevée si exposée | traiter ENF-01/ENF-02 avant mise en service |
| Incohérence lecture/écriture due au cache | l'utilisateur croit son action perdue et la répète | élevée | invalider le cache lors des écritures (ENF-09) |
| Assistance IA sans corpus indexé | fonctionnalité perçue comme inutile | certaine en l'état | indexer les incidents clôturés |
| File d'analyse IA volatile | analyses silencieusement perdues | moyenne | file persistante |
| Sondes de santé absentes en production | orchestrateur incapable de juger l'état du service | certaine en conteneur | exposer les sondes hors développement |
| Absence de liste d'incidents | interface inutilisable pour un technicien | certaine | endpoint de liste paginée |
| Erreurs métier remontées en 500 | interface incapable de guider l'utilisateur | moyenne | homogénéiser les exceptions métier |

## 10. Glossaire

| Terme | Définition |
|---|---|
| **Actif** (*asset*) | équipement matériel du parc, identifié par un numéro de série unique |
| **Incident** / **ticket** | demande de maintenance ouverte sur un actif |
| **Criticité** | niveau d'urgence déclaré à l'ouverture : `Low`, `Medium`, `High` |
| **Équipe d'astreinte** | groupe responsable du traitement des incidents pour un couple (type d'actif, criticité) |
| **Mise au rebut** (*decommission*) | sortie définitive d'un actif du parc actif |
| **Note d'assistance** | document de diagnostic généré par un modèle de langage |
| **Routage** | affectation automatique d'un incident à une équipe |
