import { focusPremierChampInvalide } from './focus-invalide';

describe('focusPremierChampInvalide', () => {
  afterEach(() => {
    // Rendre le focus à `body` avant de nettoyer, pour qu'aucun test ne parte d'un focus
    // hérité du précédent.
    (document.activeElement as HTMLElement | null)?.blur();
    document.body.replaceChildren();
  });

  it('donne le focus au premier des trois champs marqué invalide', () => {
    const conteneur = document.createElement('form');
    conteneur.innerHTML = `
      <label for="nom">Nom</label>
      <input id="nom" aria-invalid="false" />

      <label for="numero-serie">Numéro de série</label>
      <input id="numero-serie" aria-invalid="true" />

      <label for="type">Type</label>
      <select id="type" aria-invalid="false"><option value="Server">Serveur</option></select>
    `;
    document.body.appendChild(conteneur);

    const deplace = focusPremierChampInvalide(conteneur);

    expect(deplace).toBe(true);
    expect(document.activeElement?.id).toBe('numero-serie');
  });

  it("ne fait rien si aucun champ n'est invalide", () => {
    const conteneur = document.createElement('form');
    conteneur.innerHTML = `<input id="nom" aria-invalid="false" />`;
    document.body.appendChild(conteneur);

    const deplace = focusPremierChampInvalide(conteneur);

    expect(deplace).toBe(false);
    expect(document.activeElement).toBe(document.body);
  });

  it("remonte au premier descendant focusable si l'élément invalide est un conteneur non focusable", () => {
    const conteneur = document.createElement('div');
    conteneur.innerHTML = `
      <div role="group" aria-invalid="true" aria-label="Criticité">
        <label><input type="radio" name="criticite" value="Low" id="crit-basse" /> Faible</label>
        <label><input type="radio" name="criticite" value="High" id="crit-haute" /> Haute</label>
      </div>
    `;
    document.body.appendChild(conteneur);

    const deplace = focusPremierChampInvalide(conteneur);

    expect(deplace).toBe(true);
    expect(document.activeElement?.id).toBe('crit-basse');
  });

  it("remonte à un ancêtre focusable si ni l'élément invalide ni ses descendants ne le sont", () => {
    const conteneur = document.createElement('div');
    conteneur.innerHTML = `
      <div tabindex="-1" id="groupe-focusable">
        <span aria-invalid="true">Erreur signalée sans contrôle focusable à proximité</span>
      </div>
    `;
    document.body.appendChild(conteneur);

    const deplace = focusPremierChampInvalide(conteneur);

    expect(deplace).toBe(true);
    expect(document.activeElement?.id).toBe('groupe-focusable');
  });

  it('ignore un champ invalide désactivé et ses éventuels voisins désactivés', () => {
    const conteneur = document.createElement('form');
    conteneur.innerHTML = `<input id="verrouille" aria-invalid="true" disabled />`;
    document.body.appendChild(conteneur);

    const deplace = focusPremierChampInvalide(conteneur);

    expect(deplace).toBe(false);
  });
});
