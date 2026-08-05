import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('se crée', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it("affiche le nom du produit dans l'en-tête", async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const rendu = fixture.nativeElement as HTMLElement;
    expect(rendu.querySelector('header')?.textContent).toContain('AssetFlow Core');
  });

  it('réserve au routeur une zone de contenu identifiée', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const rendu = fixture.nativeElement as HTMLElement;
    expect(rendu.querySelector('main#contenu')).not.toBeNull();
  });
});
