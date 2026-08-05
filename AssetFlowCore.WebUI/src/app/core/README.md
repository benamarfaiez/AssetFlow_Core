# `core/` — services transverses

Singletons de l'application : accès réseau, normalisation des erreurs, temps réel, jeton.
**Aucun composant de présentation** ici.

| Dossier     | Contenu                                                              | Propriétaire (§13.1 du plan) |
| ----------- | -------------------------------------------------------------------- | ---------------------------- |
| `api/`      | un service `HttpClient` typé par ressource, une méthode par endpoint | `dotnet-api-bridge`          |
| `http/`     | interceptors **fonctionnels** (`HttpInterceptorFn`)                  | `dotnet-api-bridge`          |
| `realtime/` | client typé du hub SignalR `/ticketHub`                              | `dotnet-api-bridge`          |
| `auth/`     | détenteur du jeton d'accès — sans source jusqu'au Lot 7              | `dotnet-api-bridge`          |
| `guards/`   | guards fonctionnels (`CanActivateFn`) — créés au Lot 7               | `angular-feature-dev`        |

## Règles

- `core/` **ne dépend jamais de `features/`** (vérifié par `npm run verifier:dependances`).
- Les types du contrat d'API vivent dans `shared/models/`, jamais redéfinis ici.
- Tout appel `HttpClient` passe par `core/api/` : aucun composant n'injecte `HttpClient`.
- Aucune URL d'API en dur : la racine vient de `environment.apiBaseUrl`.
- `inject()` exclusivement, jamais d'injection par constructeur.
- Chaque méthode d'un service d'API porte un bloc JSDoc indiquant verbe, route, code de succès
  **réel**, codes d'erreur possibles et fichier C# d'origine — c'est ce qui rend une
  désynchronisation visible à la lecture.

## Erreurs

`errorInterceptor` est le **seul** point où le format `ProblemDetails` de l'API est interprété.
Il produit une `ApiError` (`shared/models/api-error.model.ts`) portant une nature
(`validation`, `business`, `notFound`, `conflict`, `server`, `network`) et, pour une validation,
un dictionnaire `fieldErrors` dont les clés sont converties en `camelCase` pour correspondre aux
noms de contrôles d'un formulaire. Les écrans ne manipulent donc jamais `HttpErrorResponse`.
