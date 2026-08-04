---
name: sync-api-dtos
description: Synchronise le contrat backend .NET vers le frontend Angular — lit un Controller C#, un DTO, une Request, une entité EF Core ou un enum, puis génère ou met à jour les interfaces TypeScript et le service Angular HttpClient correspondants. Utiliser quand l'utilisateur invoque /sync-api-dtos, demande de générer les modèles TypeScript d'une API, de créer le service Angular d'une ressource, ou de resynchroniser le front après une modification d'un DTO backend.
user-invocable: true
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - PowerShell
  - Bash
---

# /sync-api-dtos — Synchronisation Backend .NET → Frontend Angular

**Argument** : `[PathToControllerOrDto]` — chemin d'un Controller, DTO, Request, entité ou enum C#.
Sans argument : liste les controllers de `AssetFlowCore.WebApi/Controllers/` et demande lequel synchroniser (ou confirme une synchronisation complète si l'utilisateur la demande explicitement).

Le **code C# est la seule source de vérité**. [doc/API-Specification.md](../../../doc/API-Specification.md) sert de repère, mais en cas de divergence, c'est le code qui gagne — et tu signales l'écart.

## Règles absolues

1. **Backend en lecture seule.** Aucun fichier `.cs`, `.csproj` ou `.slnx` n'est modifié. Un endpoint manquant ou un contrat incohérent se **signale**, ne s'invente pas.
2. **Zéro invention.** Chaque champ, type, verbe, route et code de statut généré doit être lu dans le code. Si un élément n'est pas déterminable, écris-le dans le rapport plutôt que de deviner.
3. **Pas de `any`** ni d'assertion `as` de complaisance. `object` / `dynamic` C# → `unknown`.
4. Interfaces TypeScript **sans préfixe `I`**, en `PascalCase` ; fichiers en `kebab-case`.
5. Commentaires et documentation **en français**.

## Étape 1 — Localiser le frontend

Cherche le workspace Angular (`angular.json` à la racine ou dans un sous-dossier). Détermine les dossiers cibles **en t'alignant sur l'existant** :

| Contenu | Emplacement par défaut | Alternative acceptée |
|---|---|---|
| Types du contrat | `src/app/shared/models/` | `src/app/core/models/` si déjà utilisé |
| Services d'API | `src/app/core/api/` | dossier existant équivalent |

Ne crée jamais une seconde convention à côté d'une convention déjà en place.

**Si aucun workspace Angular n'existe** : arrête-toi, dis-le, et indique que la création du workspace relève de l'agent `angular-architect`. Ne scaffolde rien.

## Étape 2 — Extraire le contrat depuis le C#

Selon la nature du fichier fourni :

- **Controller** : route de base (`[Route("api/[controller]")]` → `/api/<nom sans suffixe Controller>`, en minuscules), puis pour chaque action : verbe HTTP et gabarit (`[HttpPut("{id:guid}/close")]`), paramètres de route, type du corps `[FromBody]`, et **code de succès réellement renvoyé**.
- **Record de `Requests/`** : contrat d'entrée. Note les attributs `[JsonRequired]`.
- **DTO de `Application/DTOs/`** : contrat de sortie.
- **Entité de `Domain/Entities/`** : ne l'expose **pas** telle quelle. Génère uniquement les champs réellement sérialisés par un DTO ; signale si l'utilisateur demande un type calqué sur une entité non exposée.
- **Enum** : union de littéraux (voir mapping).

Suis récursivement les types référencés (DTO imbriqués, enums).

⚠️ **Deux pièges vérifiés sur ce dépôt** :
- Le **type de retour réel** n'est pas celui des attributs `ProducesResponseType`. Remonte au handler MediatR (`IRequest<T>` de la commande/requête) pour connaître le type produit.
- Le **code de succès réel** est celui de l'appel `StatusCode(...)` / `Ok()` / `NoContent()` dans l'action, pas celui déclaré en attribut. Exemple actuel : `PUT /api/teams/{id}` renvoie **201**, et aucun endpoint ne renvoie 404 (les ressources introuvables remontent en **400**).

## Étape 3 — Mapping des types C# → TypeScript

| C# | TypeScript | Remarque |
|---|---|---|
| `int`, `short`, `byte`, `float`, `double` | `number` | |
| `long`, `ulong`, `decimal` | `number` | **signale le risque de précision** au-delà de 2^53 ; propose `string` si le backend sérialise en chaîne |
| `bool` | `boolean` | |
| `string` | `string` | |
| `Guid` | `string` | format UUID |
| `DateTime`, `DateTimeOffset` | `string` | ISO 8601 — **jamais `Date`** dans le type de transport ; la conversion en `Date` se fait dans un mapper dédié, documenté |
| `DateOnly`, `TimeOnly`, `TimeSpan` | `string` | |
| `byte[]` | `string` | base64 |
| `enum` | union de littéraux | voir ci-dessous |
| `T?` (valeur ou référence nullable) | `T \| null` | la propriété **est présente** avec `null` ; n'utilise `?:` que si elle peut être absente de la charge utile |
| `List<T>`, `IEnumerable<T>`, `ICollection<T>`, `T[]` | `T[]` | |
| `Dictionary<string, T>` | `Record<string, T>` | |
| `object`, `dynamic` | `unknown` | |

**Casing** — la règle n'est pas uniforme, c'est l'erreur classique :

- **Noms de propriétés** : `PascalCase` C# → **`camelCase`** TS (comportement par défaut d'ASP.NET Core).
- **Valeurs d'enums** : **inchangées, en `PascalCase`**. `Program.cs` enregistre `JsonStringEnumConverter` **sans politique de nommage**. Vérifie cette configuration dans `Program.cs` à chaque exécution ; si une politique camelCase y est ajoutée, adapte les unions.
- **Clés du dictionnaire `errors`** de `ProblemDetails` : noms de propriétés C# en **`PascalCase`**.

```ts
// ✅ correct — les valeurs transitent en PascalCase
export type AssetType = 'Server' | 'Laptop' | 'NetworkDevice';

// ❌ faux — enum numérique : ne correspond à rien sur le réseau
export enum AssetType { Server, Laptop, NetworkDevice }
```

## Étape 4 — Générer ou mettre à jour les modèles

Un fichier par ressource (`asset.model.ts`, `ticket.model.ts`, `team.model.ts`) et un fichier partagé pour l'erreur (`problem-details.model.ts`).

En-tête de chaque fichier généré :

```ts
// Contrat synchronisé depuis le backend .NET — ne pas modifier à la main.
// Sources : AssetFlowCore.Application/DTOs/AssetResponseDto.cs
//           AssetFlowCore.Domain/Enums/AssetType.cs, AssetStatus.cs
// Resynchroniser avec : /sync-api-dtos AssetFlowCore.WebApi/Controllers/AssetsController.cs
```

**Idempotence et détection de dérive.** Si le fichier existe déjà, **compare avant d'écrire** et présente les écarts (champ ajouté, supprimé, renommé, type ou nullabilité modifiée, valeur d'enum nouvelle ou retirée) :

```
Dérive détectée sur TicketResponseDto :
  + description: string            (nouveau champ backend)
  ~ assignedTeamId: string → string | null   (devenu nullable)
  - legacyCode                     (supprimé du DTO backend)
```

Une **suppression ou un renommage** casse le code appelant : signale les usages (`Grep` sur le champ) avant d'écraser. Les types écrits à la main en dehors des fichiers générés ne sont jamais supprimés.

## Étape 5 — Générer le service Angular

Un service par ressource, `providedIn: 'root'` (aucun enregistrement dans `app.config.ts` n'est alors nécessaire). Structure imposée :

```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AssetResponse, RegisterAssetRequest } from '../../shared/models/asset.model';

/**
 * Accès à la ressource « assets » de l'API AssetFlow Core.
 * Contrat : AssetFlowCore.WebApi/Controllers/AssetsController.cs
 */
@Injectable({ providedIn: 'root' })
export class AssetsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/assets`;

  /**
   * Liste l'inventaire complet des actifs.
   * `GET /api/assets` → 200 AssetResponse[]
   * Erreurs : 500. Réponse servie depuis un cache serveur de 5 minutes.
   */
  getAll(): Observable<AssetResponse[]> {
    return this.http.get<AssetResponse[]>(this.baseUrl);
  }

  /**
   * Enregistre un nouvel actif.
   * `POST /api/assets` → **201** AssetResponse (aucun en-tête Location)
   * Erreurs : 400 (numéro de série déjà pris, longueur 5–50, type hors énumération).
   */
  register(requete: RegisterAssetRequest): Observable<AssetResponse> {
    return this.http.post<AssetResponse>(this.baseUrl, requete);
  }

  /**
   * Met un actif au rebut (irréversible).
   * `PUT /api/assets/{id}/decommission` → 204
   * Erreurs : 400 (actif introuvable, incidents en cours).
   */
  decommission(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/decommission`, null);
  }
}
```

Contraintes de génération :

- `inject(HttpClient)` uniquement — jamais d'injection par constructeur.
- Retour **`Observable<T>`** ; `Observable<void>` pour un 204. La conversion en Signal (`toSignal()`) ou en ressource se fait chez le consommateur, pas ici.
- **Un bloc JSDoc par méthode**, contenant : verbe et route, code de succès **réel**, codes d'erreur possibles, et le fichier C# d'origine. C'est ce qui rend une désynchronisation visible à la lecture.
- URL de base via `environment` ; aucune URL en dur. Si `environment.apiBaseUrl` n'existe pas encore, signale-le comme une clé à ajouter par `angular-architect` (ou utilise un chemin relatif si un proxy de dev est configuré).
- Une méthode par action du controller, **et rien de plus**.

## Étape 6 — Vérifier

Exécute la vérification et **rapporte la sortie réelle** :

```powershell
npx ng build
# ou, plus rapide, la seule vérification de types :
npx tsc --noEmit -p tsconfig.app.json
```

Si les dépendances ne sont pas installées ou que la commande échoue pour une raison indépendante de tes fichiers, dis-le explicitement au lieu de conclure au succès. N'affirme jamais que la synchronisation est bonne sans exécution à l'appui.

## Étape 7 — Rapport final

1. **Fichiers créés / mis à jour** (chemins).
2. **Endpoints couverts**, avec verbe, route et code de succès.
3. **Dérive détectée** entre le contrat backend et les types front préexistants, et usages impactés.
4. **Vérification** : commande lancée et résultat réel.
5. **À faire ailleurs** : clés d'environnement ou providers à confier à `angular-architect`.
6. **Limites backend rencontrées** — à signaler, jamais à masquer par des données fictives. État actuel connu : pas de `GET /api/teams` ni `GET /api/tickets` (aucune liste), pas de `GET /api/assets/{id}`, `TicketResponseDto` sans `description` / `assistanceNote` / `isAiProcessing`, `TeamResponseDto` sans `assetType` / `ticketCriticality`, et aucune authentification côté API.

## Modèle d'erreur partagé

À générer une seule fois, réutilisé par tous les services :

```ts
/** Erreur d'API au format ProblemDetails (RFC 7807), produite par ExceptionHandlingMiddleware. */
export interface ProblemDetails {
  title: string;
  status: number;
  detail: string;
  /** Erreurs de validation FluentValidation. Clés = noms de propriétés C# en PascalCase. */
  errors?: Record<string, string[]>;
  type?: string;
  instance?: string;
}
```
