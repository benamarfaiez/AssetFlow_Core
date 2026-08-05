import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { OptionSelecteur, SelectField } from './select-field';

const OPTIONS: readonly OptionSelecteur[] = [
  { valeur: 'Server', libelle: 'Serveur' },
  { valeur: 'Laptop', libelle: 'Ordinateur portable' },
  { valeur: 'NetworkDevice', libelle: 'Équipement réseau', desactivee: true },
];

describe('SelectField', () => {
  let fixture: ComponentFixture<SelectField>;

  async function creer(controle: FormControl<string>): Promise<void> {
    fixture = TestBed.createComponent(SelectField);
    fixture.componentRef.setInput('controle', controle);
    fixture.componentRef.setInput('label', "Type d'actif");
    fixture.componentRef.setInput('options', OPTIONS);
    await fixture.whenStable();
  }

  function selecteur(): HTMLSelectElement {
    const trouve = (fixture.nativeElement as HTMLElement).querySelector('select');
    if (trouve === null) {
      throw new Error("Le sélecteur n'a pas été rendu.");
    }
    return trouve;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [SelectField] });
  });

  it("rend les options avec leurs libellés traduits, précédées d'une entrée vide", async () => {
    await creer(new FormControl('', { nonNullable: true }));

    const options = [...selecteur().options].map((option) => option.textContent?.trim());
    expect(options).toEqual([
      'Sélectionnez…',
      'Serveur',
      'Ordinateur portable',
      'Équipement réseau',
    ]);
    expect(selecteur().options[3].disabled).toBe(true);
  });

  it("supprime l'entrée vide lorsqu'un choix est imposé", async () => {
    await creer(new FormControl('Server', { nonNullable: true }));
    fixture.componentRef.setInput('libelleVide', null);
    await fixture.whenStable();

    expect(selecteur().options).toHaveLength(3);
  });

  it("associe le libellé au sélecteur et signale l'erreur après interaction", async () => {
    const controle = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    });
    await creer(controle);

    const label = (fixture.nativeElement as HTMLElement).querySelector('label');
    expect(label?.getAttribute('for')).toBe(selecteur().id);
    expect(selecteur().getAttribute('aria-invalid')).toBeNull();

    controle.markAsTouched();
    await fixture.whenStable();

    expect(selecteur().getAttribute('aria-invalid')).toBe('true');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('obligatoire');
  });

  it('reflète la valeur du contrôle', async () => {
    const controle = new FormControl('Laptop', { nonNullable: true });
    await creer(controle);

    expect(selecteur().value).toBe('Laptop');
  });
});
