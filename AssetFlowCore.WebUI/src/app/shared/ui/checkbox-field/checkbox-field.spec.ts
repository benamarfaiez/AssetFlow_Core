import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { CheckboxField } from './checkbox-field';

describe('CheckboxField', () => {
  let fixture: ComponentFixture<CheckboxField>;

  async function creer(controle: FormControl<boolean>): Promise<void> {
    fixture = TestBed.createComponent(CheckboxField);
    fixture.componentRef.setInput('controle', controle);
    fixture.componentRef.setInput('label', 'Ne montrer que les équipes actives');
    await fixture.whenStable();
  }

  function case_(): HTMLInputElement {
    const trouve = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input[type="checkbox"]',
    );
    if (trouve === null) {
      throw new Error("La case à cocher n'a pas été rendue.");
    }
    return trouve;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [CheckboxField] });
  });

  it('associe le libellé cliquable à la case', async () => {
    await creer(new FormControl(false, { nonNullable: true }));

    const label = (fixture.nativeElement as HTMLElement).querySelector('label');
    expect(label?.getAttribute('for')).toBe(case_().id);
    expect(label?.textContent).toContain('équipes actives');
  });

  it('reflète la valeur du contrôle dans les deux sens', async () => {
    const controle = new FormControl(true, { nonNullable: true });
    await creer(controle);
    expect(case_().checked).toBe(true);

    controle.setValue(false);
    await fixture.whenStable();
    expect(case_().checked).toBe(false);
  });

  it('signale une case obligatoire non cochée', async () => {
    const controle = new FormControl(false, {
      nonNullable: true,
      validators: [Validators.requiredTrue],
    });
    await creer(controle);
    controle.markAsTouched();
    await fixture.whenStable();

    expect(case_().getAttribute('aria-invalid')).toBe('true');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('doit être cochée');
  });
});
