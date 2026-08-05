import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { TextField } from './text-field';

describe('TextField', () => {
  let fixture: ComponentFixture<TextField>;

  async function creer(controle: FormControl<string>, label = 'Numéro de série'): Promise<void> {
    fixture = TestBed.createComponent(TextField);
    fixture.componentRef.setInput('controle', controle);
    fixture.componentRef.setInput('label', label);
    await fixture.whenStable();
  }

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function champ(): HTMLInputElement {
    const trouve = element().querySelector<HTMLInputElement>('input');
    if (trouve === null) {
      throw new Error("Le champ de saisie n'a pas été rendu.");
    }
    return trouve;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TextField] });
  });

  it('associe le libellé au champ par for/id', async () => {
    await creer(new FormControl('', { nonNullable: true }));

    const label = element().querySelector('label');
    expect(label?.getAttribute('for')).toBe(champ().id);
    expect(label?.textContent).toContain('Numéro de série');
  });

  it("n'affiche aucune erreur sur un formulaire vierge, même invalide", async () => {
    await creer(new FormControl('', { nonNullable: true, validators: [Validators.required] }));

    expect(element().textContent).not.toContain('obligatoire');
    expect(champ().getAttribute('aria-invalid')).toBeNull();
  });

  it('affiche le message après interaction et le relie au champ par aria-describedby', async () => {
    const controle = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    });
    await creer(controle);

    // Ce que fait la soumission d'un formulaire : marquer les champs comme touchés.
    controle.markAsTouched();
    await fixture.whenStable();

    const message = element().querySelector('[id$="-erreur"]');
    expect(message?.textContent).toContain('obligatoire');
    expect(champ().getAttribute('aria-invalid')).toBe('true');
    expect(champ().getAttribute('aria-describedby')).toContain(message?.id ?? 'absent');
  });

  it('efface le message dès que la saisie devient valide', async () => {
    const controle = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    });
    await creer(controle);
    controle.markAsTouched();
    await fixture.whenStable();

    controle.setValue('SRV-00042');
    await fixture.whenStable();

    expect(element().querySelector('[id$="-erreur"]')).toBeNull();
    expect(champ().getAttribute('aria-invalid')).toBeNull();
  });

  it("privilégie un message fourni par la feature — le cas d'une erreur reportée par l'API", async () => {
    const controle = new FormControl('SRV-1', { nonNullable: true });
    await creer(controle);

    fixture.componentRef.setInput('messages', {
      serveur: 'Ce numéro de série est déjà enregistré dans le parc.',
    });
    controle.setErrors({ serveur: true });
    controle.markAsTouched();
    await fixture.whenStable();

    expect(element().textContent).toContain('déjà enregistré dans le parc');
  });

  it('déduit le message de longueur du validateur, sans le coder en dur', async () => {
    const controle = new FormControl('abc', {
      nonNullable: true,
      validators: [Validators.minLength(5)],
    });
    await creer(controle);
    controle.markAsTouched();
    await fixture.whenStable();

    expect(element().textContent).toContain('au moins 5 caractères');
  });

  it("annonce le caractère obligatoire au-delà de l'astérisque", async () => {
    await creer(new FormControl('', { nonNullable: true }));
    fixture.componentRef.setInput('requis', true);
    await fixture.whenStable();

    expect(element().querySelector('label')?.textContent).toContain('(obligatoire)');
    expect(champ().required).toBe(true);
  });

  it('relie la consigne de saisie au champ', async () => {
    await creer(new FormControl('', { nonNullable: true }));
    fixture.componentRef.setInput('aide', 'De 5 à 50 caractères.');
    await fixture.whenStable();

    const aide = element().querySelector('[id$="-aide"]');
    expect(aide?.textContent).toContain('De 5 à 50 caractères.');
    expect(champ().getAttribute('aria-describedby')).toBe(aide?.id);
  });

  it('compte les caractères saisis, en information purement visuelle', async () => {
    const controle = new FormControl('Serveur', { nonNullable: true });
    await creer(controle);
    fixture.componentRef.setInput('longueurMax', 150);
    fixture.componentRef.setInput('compteurCaracteres', true);
    await fixture.whenStable();

    const compteur = element().querySelector('[aria-hidden="true"]');
    expect(compteur?.textContent).toContain('7 / 150');
    expect(champ().getAttribute('maxlength')).toBe('150');
  });

  it("reflète l'état désactivé du contrôle sur le champ natif", async () => {
    const controle = new FormControl('', { nonNullable: true });
    await creer(controle);

    controle.disable();
    await fixture.whenStable();

    expect(champ().disabled).toBe(true);
  });
});
