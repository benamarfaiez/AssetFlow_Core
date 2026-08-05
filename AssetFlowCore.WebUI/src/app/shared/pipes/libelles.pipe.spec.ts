import {
  LIBELLES_ASSET_STATUS,
  LIBELLES_ASSET_TYPE,
  LIBELLES_TICKET_CRITICALITY,
  LIBELLES_TICKET_STATUS,
} from '../i18n/libelles';
import { ASSET_STATUSES, ASSET_TYPES } from '../models/asset.model';
import { TICKET_CRITICALITIES, TICKET_STATUSES } from '../models/ticket.model';
import {
  AssetStatusLabelPipe,
  AssetTypeLabelPipe,
  TicketCriticalityLabelPipe,
  TicketStatusLabelPipe,
} from './libelles.pipe';

describe('pipes de libellés', () => {
  it("traduit les types d'actifs", () => {
    const pipe = new AssetTypeLabelPipe();
    expect(pipe.transform('Server')).toBe('Serveur');
    expect(pipe.transform('NetworkDevice')).toBe('Équipement réseau');
  });

  it("traduit les états d'actifs", () => {
    const pipe = new AssetStatusLabelPipe();
    expect(pipe.transform('InService')).toBe('En service');
    expect(pipe.transform('Decommissioned')).toBe('Mis au rebut');
  });

  it("traduit les états d'incidents", () => {
    const pipe = new TicketStatusLabelPipe();
    expect(pipe.transform('Opened')).toBe('Ouvert');
    expect(pipe.transform('InProgress')).toBe('En cours');
  });

  it('traduit les criticités', () => {
    const pipe = new TicketCriticalityLabelPipe();
    expect(pipe.transform('Low')).toBe('Faible');
    expect(pipe.transform('High')).toBe('Haute');
  });

  it('couvre toutes les valeurs du contrat, sans libellé vide ni valeur laissée en anglais', () => {
    const tables = [
      { valeurs: ASSET_TYPES, libelles: LIBELLES_ASSET_TYPE },
      { valeurs: ASSET_STATUSES, libelles: LIBELLES_ASSET_STATUS },
      { valeurs: TICKET_STATUSES, libelles: LIBELLES_TICKET_STATUS },
      { valeurs: TICKET_CRITICALITIES, libelles: LIBELLES_TICKET_CRITICALITY },
    ];

    for (const { valeurs, libelles } of tables) {
      for (const valeur of valeurs) {
        const libelle = (libelles as Readonly<Record<string, string>>)[valeur];
        expect(libelle, `valeur non traduite : ${valeur}`).toBeTruthy();
        expect(libelle, `libellé identique à la valeur d'API : ${valeur}`).not.toBe(valeur);
      }
    }
  });

  it('se rabat sur la valeur brute si elle est inconnue de la table', () => {
    // Cas défensif : l'API gagnerait une valeur d'énumération absente du contrat compilé.
    const pipe = new AssetStatusLabelPipe();
    expect(pipe.transform('Inconnu' as never)).toBe('Inconnu');
  });
});
