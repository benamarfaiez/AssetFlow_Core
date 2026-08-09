import { ValidationErrors } from '@angular/forms';

/**
 * Messages de validation par défaut, en français.
 *
 * Les champs de `shared/ui` s'en servent quand la feature n'a pas fourni de message propre au
 * contexte. La clé `serveur` est réservée aux erreurs rapportées par l'API : les features y
 * déposent le message du dictionnaire `errors` d'une réponse 400, tel quel — il est déjà
 * rédigé en français par le backend.
 */
const MESSAGES: Readonly<Record<string, (detail: unknown) => string>> = {
  // `Validators.requiredTrue` publie aussi son erreur sous la clé `required` : il n'existe donc
  // pas de clé `requiredTrue` à traiter. `CheckboxField` remplace ce message par une formulation
  // adaptée à une case à cocher.
  required: () => $localize`:@@messagesValidation.required:Ce champ est obligatoire.`,
  email: () => $localize`:@@messagesValidation.email:L'adresse électronique n'est pas valide.`,
  minlength: (detail) => {
    const attendu = (detail as { requiredLength?: number } | null)?.requiredLength;
    return attendu === undefined
      ? $localize`:@@messagesValidation.minlength.sansDetail:La valeur saisie est trop courte.`
      : $localize`:@@messagesValidation.minlength:Saisissez au moins ${attendu}:valeur: caractères.`;
  },
  maxlength: (detail) => {
    const attendu = (detail as { requiredLength?: number } | null)?.requiredLength;
    return attendu === undefined
      ? $localize`:@@messagesValidation.maxlength.sansDetail:La valeur saisie est trop longue.`
      : $localize`:@@messagesValidation.maxlength:Ne dépassez pas ${attendu}:valeur: caractères.`;
  },
  min: (detail) => {
    const attendu = (detail as { min?: number } | null)?.min;
    return attendu === undefined
      ? $localize`:@@messagesValidation.min.sansDetail:La valeur est trop petite.`
      : $localize`:@@messagesValidation.min:La valeur doit être supérieure ou égale à ${attendu}:valeur:.`;
  },
  max: (detail) => {
    const attendu = (detail as { max?: number } | null)?.max;
    return attendu === undefined
      ? $localize`:@@messagesValidation.max.sansDetail:La valeur est trop grande.`
      : $localize`:@@messagesValidation.max:La valeur doit être inférieure ou égale à ${attendu}:valeur:.`;
  },
  pattern: () => $localize`:@@messagesValidation.pattern:Le format attendu n'est pas respecté.`,
  // Échappe volontairement à la conversion `$localize` : ce message affiche tel quel le texte
  // déjà en français renvoyé par l'API (`errors` d'une réponse 400), et le repli ne sert qu'à
  // défaut de detail exploitable — traduire une chaîne qui n'est pas la nôtre n'aurait pas de sens.
  serveur: (detail) => (typeof detail === 'string' ? detail : "L'opération a été refusée."),
};

/**
 * Rend le message correspondant à la **première** erreur du contrôle.
 *
 * N'afficher qu'un message à la fois est délibéré : empiler « obligatoire » et « trop court »
 * sur le même champ n'aide personne à corriger sa saisie.
 *
 * @param erreurs Dictionnaire d'erreurs du contrôle (`control.errors`).
 * @param surcharges Messages propres au contexte, prioritaires sur les messages par défaut.
 */
export function premierMessageDeValidation(
  erreurs: ValidationErrors | null,
  surcharges: Readonly<Record<string, string>> = {},
): string | null {
  if (erreurs === null) {
    return null;
  }

  const cles = Object.keys(erreurs);
  if (cles.length === 0) {
    return null;
  }

  const cle = cles[0];
  const surcharge = surcharges[cle];
  if (surcharge !== undefined) {
    return surcharge;
  }

  const fabrique = MESSAGES[cle];
  return fabrique === undefined ? "La valeur saisie n'est pas valide." : fabrique(erreurs[cle]);
}
