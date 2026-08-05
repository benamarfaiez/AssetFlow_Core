import { Signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { switchMap } from 'rxjs';

/**
 * Rend le rendu d'un champ réactif aux changements d'état de son contrôle.
 *
 * `FormControl` n'expose ni signal ni notification de rendu : `invalid`, `touched` et `errors`
 * sont de simples propriétés. En mode *zoneless* et avec `OnPush`, une validation asynchrone ou
 * un `markAsTouched()` déclenché par la soumission d'un formulaire ne provoquerait donc **aucun
 * nouveau rendu** : le message d'erreur n'apparaîtrait pas.
 *
 * Le signal renvoyé change à chaque événement du contrôle (valeur, état, `touched`). Un
 * `computed()` qui le lit se recalcule alors au bon moment. Le `switchMap` suit le contrôle
 * lui-même : remplacer le contrôle en entrée réabonne au nouveau.
 *
 * À appeler depuis un **contexte d'injection** (initialiseur de champ ou constructeur).
 */
export function suivreEtatControle(controle: Signal<AbstractControl>): Signal<unknown> {
  return toSignal(toObservable(controle).pipe(switchMap((c) => c.events)), { initialValue: null });
}
