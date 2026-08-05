# `shared/` — briques réutilisables

| Dossier                  | Contenu                                                         | Propriétaire (§13.1 du plan) |
| ------------------------ | --------------------------------------------------------------- | ---------------------------- |
| `models/`                | types du contrat d'API dérivés du C#                            | `dotnet-api-bridge`          |
| `ui/`                    | composants de présentation du design system — Lot 4             | `ui-ux-designer`             |
| `pipes/` · `directives/` | dont la traduction française des valeurs d'énumérations — Lot 4 | `ui-ux-designer`             |

## Règles

- `shared/` **ne dépend ni de `core/` ni de `features/`** (vérifié par
  `npm run verifier:dependances`) : ni HTTP, ni routeur, ni logique métier.
- Les composants de `ui/` reçoivent des données par `input()` et émettent des intentions par
  `output()`. Ils ne connaissent aucune ressource de l'API.
- Un composant dupliqué dans une feature est une erreur : ce qui se répète remonte ici.

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
