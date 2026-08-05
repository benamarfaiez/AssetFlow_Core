import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { TextareaField } from './textarea-field';

describe('TextareaField', () => {
  let fixture: ComponentFixture<TextareaField>;

  async function creer(controle: FormControl<string>): Promise<void> {
    fixture = TestBed.createComponent(TextareaField);
    fixture.componentRef.setInput('controle', controle);
    fixture.componentRef.setInput('label', 'Compte rendu de résolution');
    await fixture.whenStable();
  }

  function zone(): HTMLTextAreaElement {
    const trouve = (fixture.nativeElement as HTMLElement).querySelector('textarea');
    if (trouve === null) {
      throw new Error("La zone de texte n'a pas été rendue.");
    }
    return trouve;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TextareaField] });
  });

  it('associe le libellé à la zone et respecte la hauteur demandée', async () => {
    await creer(new FormControl('', { nonNullable: true }));
    fixture.componentRef.setInput('lignes', 6);
    await fixture.whenStable();

    const label = (fixture.nativeElement as HTMLElement).querySelector('label');
    expect(label?.getAttribute('for')).toBe(zone().id);
    expect(zone().rows).toBe(6);
  });

  it('signale un compte rendu vide après interaction', async () => {
    const controle = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    });
    await creer(controle);
    controle.markAsTouched();
    await fixture.whenStable();

    expect(zone().getAttribute('aria-invalid')).toBe('true');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('obligatoire');
  });

  it('compte les caractères et borne la saisie', async () => {
    await creer(new FormControl('Ventilateur remplacé.', { nonNullable: true }));
    fixture.componentRef.setInput('longueurMax', 500);
    fixture.componentRef.setInput('compteurCaracteres', true);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('21 / 500');
    expect(zone().getAttribute('maxlength')).toBe('500');
  });
});
