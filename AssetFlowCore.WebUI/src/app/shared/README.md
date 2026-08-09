# `shared/` — briques réutilisables

| Dossier   | Contenu                                                        | Propriétaire (§13.1 du plan) |
| --------- | -------------------------------------------------------------- | ---------------------------- |
| `models/` | types du contrat d'API dérivés du C#                           | `dotnet-api-bridge`          |
| `ui/`     | composants de présentation du design system                    | `ui-ux-designer`             |
| `i18n/`   | libellés français des énumérations, messages de validation     | `ui-ux-designer`             |
| `pipes/`  | pipes de traduction des valeurs d'API                          | `ui-ux-designer`             |
| `forms/`  | utilitaires de formulaire (réactivité de l'état d'un contrôle) | `ui-ux-designer`             |

## Règles

- `shared/` **ne dépend ni de `core/` ni de `features/`** (vérifié par
  `npm run verifier:dependances`) : ni HTTP, ni logique métier, ni état applicatif.
- Les composants de `ui/` reçoivent des données par `input()` et émettent des intentions par
  `output()`. Ils ne connaissent aucune ressource de l'API.
- Un composant dupliqué dans une feature est une erreur : ce qui se répète remonte ici.
- **Écart assumé** : `Breadcrumb` importe `RouterLink`. Un fil d'Ariane accessible exige de vraies
  ancres (clic du milieu, ouverture dans un onglet) ; c'est une directive de présentation, et aucun
  composant n'injecte `Router` ni ne navigue de lui-même.

## `models/` — contrat d'API

Types **dérivés du C#**, en-tête de fichier indiquant les sources et la commande de
resynchronisation (`/sync-api-dtos <controller>`). Ne pas les modifier à la main : toute
évolution du backend passe par le skill, sans quoi la dérive se propage silencieusement.

Correspondances retenues : `Guid` → `string` · `DateTime` → `string` ISO 8601 (**jamais `Date`**
au niveau du transport) · `T?` → `T | null` (la propriété est présente) · `IEnumerable<T>` →
`readonly T[]` · `enum` → **union de littéraux `PascalCase`** (le backend enregistre
`JsonStringEnumConverter` sans politique de nommage : seuls les **noms de propriétés** passent
en `camelCase`, pas les **valeurs d'énumérations**).

`api-error.model.ts` est le seul type de `models/` qui ne provienne pas du C# : c'est le modèle
d'erreur applicatif produit par `errorInterceptor`.

## `ui/` — design system (Lot 4)

Style : **Tailwind 4**, jetons déclarés dans `src/styles.css`. Aucun composant ne porte de
feuille de styles ; aucune couleur, taille ou durée n'y est codée en dur. Accessibilité :
`@angular/cdk` fournit le piège de focus de la modale.

### Composants de base

| Composant            | Entrées                                                                                                                                                                  | Sorties        |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------- |
| `app-button`         | `type` (`button`) · `variante` (`primaire`\|`secondaire`\|`discret`\|`danger`) · `taille` (`compact`\|`normal`) · `disabled` · `enCours` · `pleineLargeur` · `ariaLabel` | — (clic natif) |
| `app-text-field`     | `controle`\* · `label`\* · `type` · `aide` · `placeholder` · `autocomplete` · `longueurMax` · `requis` · `compteurCaracteres` · `messages`                               | —              |
| `app-select-field`   | `controle`\* · `label`\* · `options`\* · `libelleVide` · `aide` · `requis` · `messages`                                                                                  | —              |
| `app-textarea-field` | `controle`\* · `label`\* · `lignes` · `aide` · `placeholder` · `longueurMax` · `requis` · `compteurCaracteres` · `messages`                                              | —              |
| `app-checkbox-field` | `controle`\* · `label`\* · `aide` · `messages`                                                                                                                           | —              |

\* entrée obligatoire.

**Formulaires — approche A** : le champ reçoit le `FormControl` en entrée. L'API reste typée,
`control.disable()` agit sans code supplémentaire, et le composant **n'ajoute jamais de
validateur** : la validation appartient à la feature. Les erreurs n'apparaissent qu'après
`touched` ou `dirty`, sont reliées au champ par `aria-describedby`, et le champ porte
`aria-invalid`. Reporter une erreur d'API sur un champ : `controle.setErrors({ serveur: true })`
avec `[messages]="{ serveur: '…' }"`.

> À la charge de la feature (Lot 5) : **déplacer le focus sur le premier champ invalide** à la
> soumission. Les champs n'utilisent pas de région `aria-live` — sans focus déplacé, un lecteur
> d'écran n'annoncerait rien après un échec de validation.

### Composants de structure

| Composant        | Entrées                                                                                             | Sorties     |
| ---------------- | --------------------------------------------------------------------------------------------------- | ----------- |
| `app-card`       | `ariaLabel` ; projections `[slot=entete]`, contenu, `[slot=actions]`                                | —           |
| `app-data-table` | `lignes`\* · `colonnes`\* · `cleLigne`\* · `legende`\* · `messageVide`                              | —           |
| `app-modal`      | `ouverte`\* · `titre`\* · `description` · `taille` · `fermetureParArrierePlan` · `libelleFermeture` | `fermeture` |
| `app-breadcrumb` | `etapes`\* · `ariaLabel`                                                                            | —           |

`app-data-table` rend **deux vues** : une table à partir de `md`, une liste de cartes en dessous.
Le CSS choisit (`display: none`), donc rien n'est annoncé deux fois. La bascule suit aussi le zoom.
`ColonneTable<T>` accepte un `gabarit` (`TemplateRef<{ $implicit: T }>`) pour un rendu riche.

`app-modal` ne se ferme pas d'elle-même : elle **émet** `fermeture` et l'appelant remet `ouverte`
à `false` — ce qui permet de refuser la fermeture d'un formulaire non enregistré. En mode
zoneless, `ouverte` doit être piloté par un **signal**. Le focus entre dans le panneau à
l'ouverture, y est piégé, et **revient au déclencheur** à la fermeture.

### Composants d'état

| Composant                      | Entrées                                           | Sorties       |
| ------------------------------ | ------------------------------------------------- | ------------- |
| `app-badge`                    | `libelle`\* · `tonalite` · `avecPastille`         | —             |
| `app-asset-status-badge`       | `statut`\* (`AssetStatus`)                        | —             |
| `app-ticket-status-badge`      | `statut`\* (`TicketStatus`)                       | —             |
| `app-ticket-criticality-badge` | `criticite`\* (`TicketCriticality`)               | —             |
| `app-spinner`                  | `libelle` · `taille` · `libelleVisible`           | —             |
| `app-empty-state`              | `titre`\* · `description` ; action projetée       | —             |
| `app-error-message`            | `message`\* · `titre` · `traceId` · `reessayable` | `reessai`     |
| `app-notification-list`        | `notifications`\*                                 | `rejet`       |
| `app-theme-toggle`             | `theme`\*                                         | `themeChange` |

Les badges du domaine encapsulent **la traduction du libellé et le choix de la tonalité** : ces
correspondances sont définies une seule fois. Aucun état n'est porté par la seule couleur.

## `i18n/` — libellés

`libelles.ts` traduit les valeurs d'énumérations de l'API (`ENF-22`). Les tables sont typées
`Record<Union, string>` et donc **exhaustives par construction** : une valeur ajoutée au contrat
casse la compilation tant qu'elle n'est pas traduite.

`messages-validation.ts` fournit les messages de validation par défaut. La clé `serveur` est
réservée aux erreurs rapportées par l'API.

Depuis le Lot 5.0, les littéraux de `libelles.ts` et de `messages-validation.ts` sont portés par
`$localize` (`@angular/localize`), avec un `@@id` explicite et stable par valeur — voir
`npm run verifier:i18n`.
