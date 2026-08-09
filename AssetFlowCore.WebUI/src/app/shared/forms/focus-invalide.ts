/**
 * Sélecteur des éléments considérés comme focusables par cet utilitaire. Pragmatique plutôt
 * qu'exhaustif : couvre les contrôles natifs de formulaire, les liens, et tout élément qui porte
 * explicitement un `tabindex` — y compris `tabindex="-1"`, focusable par script bien
 * qu'absent de l'ordre de tabulation (c'est ainsi que `Modal` pose le focus sur son propre
 * panneau).
 */
const SELECTEUR_FOCUSABLE = [
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  'button:not([disabled])',
  'a[href]',
  '[tabindex]',
].join(', ');

function estFocusable(element: Element): element is HTMLElement {
  return element instanceof HTMLElement && element.matches(SELECTEUR_FOCUSABLE);
}

function rechercherDescendantFocusable(element: Element): HTMLElement | null {
  return element.querySelector<HTMLElement>(SELECTEUR_FOCUSABLE);
}

/** Remonte les ancêtres de `depuis`, jusqu'à `limite` inclus, en quête d'un élément focusable. */
function rechercherAncetreFocusable(depuis: Element, limite: HTMLElement): HTMLElement | null {
  // Type explicite : `Element`, plus large que `HTMLElement` renvoyé par `parentElement`. Sans
  // cela, `estFocusable` (qui affine en `HTMLElement`) réduirait la branche négative à `never`
  // — son type déclaré serait déjà exactement celui affiné, ne laissant rien d'autre possible.
  let courant: Element | null = depuis.parentElement;

  while (courant !== null) {
    if (estFocusable(courant)) {
      return courant;
    }
    if (courant === limite) {
      return null;
    }
    courant = courant.parentElement;
  }

  return null;
}

/**
 * Déplace le focus sur le premier champ invalide d'un conteneur, à l'usage des features au
 * moment de la soumission d'un formulaire.
 *
 * Aucun champ de `shared/ui` ne le fait de lui-même, et ils n'utilisent pas de région
 * `aria-live` : sans ce déplacement de focus après un `markAllAsTouched()`, un échec de
 * validation reste entièrement silencieux pour un lecteur d'écran. C'est une charge documentée
 * comme propre à chaque écran (voir `shared/README.md`).
 *
 * Cherche le premier élément `[aria-invalid="true"]` dans l'ordre du document. S'il n'est pas
 * lui-même focusable — un conteneur `role="group"` englobant, par exemple —, se rabat d'abord
 * sur son premier descendant focusable (le cas le plus courant : le contrôle réel est à
 * l'intérieur du groupe en erreur), puis sur le premier ancêtre focusable jusqu'à `conteneur`.
 *
 * À appeler après un rendu à jour (`aria-invalid` reflète déjà la validation en cours) : en mode
 * zoneless, `markAllAsTouched()` seul ne suffit pas, il faut avoir laissé le temps au signal de
 * réactivité des champs (`suivreEtatControle`) de provoquer un nouveau rendu.
 *
 * @param conteneur Élément dans lequel chercher, typiquement la racine du formulaire.
 * @returns `true` si un champ invalide focusable a été trouvé et a reçu le focus.
 */
export function focusPremierChampInvalide(conteneur: HTMLElement): boolean {
  const champInvalide = conteneur.querySelector<HTMLElement>('[aria-invalid="true"]');
  if (champInvalide === null) {
    return false;
  }

  const cible =
    (estFocusable(champInvalide) ? champInvalide : null) ??
    rechercherDescendantFocusable(champInvalide) ??
    rechercherAncetreFocusable(champInvalide, conteneur);

  if (cible === null) {
    return false;
  }

  cible.focus();
  return true;
}
