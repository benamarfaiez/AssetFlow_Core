// Vérifie les contrastes des jetons de couleur, dans les **deux thèmes**, selon la formule de
// luminance relative de WCAG 2.x. Un jeton conforme en clair peut échouer en sombre : la seule
// façon de le savoir sans le lire à l'œil est de le calculer.
//
// Usage : npm run verifier:contrastes
// Sortie : code 0 si toutes les paires atteignent leur seuil, 1 sinon, avec le détail des écarts.
//
// Les valeurs sont lues dans src/styles.css — la source de vérité — et non recopiées ici : une
// couleur modifiée sans repasser la vérification fait échouer la commande.

import { readFileSync } from 'node:fs';

const FICHIER = 'src/styles.css';

/** Seuils WCAG 2.2 : 4,5:1 pour le texte courant, 3:1 pour les éléments d'interface. */
const SEUIL_TEXTE = 4.5;
const SEUIL_INTERFACE = 3;

/**
 * Paires à vérifier : `contenu` doit se lire sur `fond`.
 * `seuil` distingue le texte (4,5:1) des éléments non textuels (3:1).
 */
const PAIRES = [
  // Texte courant sur les trois fonds de l'application.
  { contenu: 'texte', fond: 'page', seuil: SEUIL_TEXTE },
  { contenu: 'texte', fond: 'surface', seuil: SEUIL_TEXTE },
  { contenu: 'texte', fond: 'surface-creuse', seuil: SEUIL_TEXTE },

  // Texte secondaire — c'est la paire la plus exposée : elle reste du texte courant.
  { contenu: 'texte-discret', fond: 'page', seuil: SEUIL_TEXTE },
  { contenu: 'texte-discret', fond: 'surface', seuil: SEUIL_TEXTE },
  { contenu: 'texte-discret', fond: 'surface-creuse', seuil: SEUIL_TEXTE },

  // Libellés portés par une surface d'action, au repos comme au survol.
  { contenu: 'texte-inverse', fond: 'primaire', seuil: SEUIL_TEXTE },
  { contenu: 'texte-inverse', fond: 'primaire-survol', seuil: SEUIL_TEXTE },
  { contenu: 'texte-inverse', fond: 'danger', seuil: SEUIL_TEXTE },
  { contenu: 'texte-inverse', fond: 'danger-survol', seuil: SEUIL_TEXTE },

  // Tonalités d'état : le contenu d'un badge ou d'un message est du texte.
  { contenu: 'neutre-contenu', fond: 'neutre-fond', seuil: SEUIL_TEXTE },
  { contenu: 'info-contenu', fond: 'info-fond', seuil: SEUIL_TEXTE },
  { contenu: 'succes-contenu', fond: 'succes-fond', seuil: SEUIL_TEXTE },
  { contenu: 'alerte-contenu', fond: 'alerte-fond', seuil: SEUIL_TEXTE },
  { contenu: 'danger-contenu', fond: 'danger-fond', seuil: SEUIL_TEXTE },

  // Éléments d'interface : bordure d'un champ et anneau de focus délimitent des contrôles.
  { contenu: 'bordure-controle', fond: 'surface', seuil: SEUIL_INTERFACE },
  { contenu: 'bordure-controle', fond: 'page', seuil: SEUIL_INTERFACE },
  { contenu: 'focus', fond: 'page', seuil: SEUIL_INTERFACE },
  { contenu: 'focus', fond: 'surface', seuil: SEUIL_INTERFACE },
];

/**
 * Relève les jetons `--color-*` du fichier de styles.
 * Deux formes sont acceptées : `light-dark(clair, sombre)` et une couleur unique, identique dans
 * les deux thèmes.
 */
function lireJetons(source) {
  const jetons = new Map();

  const avecLightDark =
    /--color-([\w-]+):\s*light-dark\(\s*(#[0-9a-fA-F]{3,8})\s*,\s*(#[0-9a-fA-F]{3,8})\s*\)/g;
  for (const trouve of source.matchAll(avecLightDark)) {
    jetons.set(trouve[1], { clair: trouve[2], sombre: trouve[3] });
  }

  const couleurUnique = /--color-([\w-]+):\s*(#[0-9a-fA-F]{3,8})\s*;/g;
  for (const trouve of source.matchAll(couleurUnique)) {
    if (!jetons.has(trouve[1])) {
      jetons.set(trouve[1], { clair: trouve[2], sombre: trouve[2] });
    }
  }

  return jetons;
}

/** Composantes 0–255 d'une couleur hexadécimale (formes #rgb et #rrggbb). */
function composantes(hex) {
  const valeur = hex.slice(1);
  const complet =
    valeur.length === 3
      ? valeur
          .split('')
          .map((caractere) => caractere + caractere)
          .join('')
      : valeur;

  return [
    Number.parseInt(complet.slice(0, 2), 16),
    Number.parseInt(complet.slice(2, 4), 16),
    Number.parseInt(complet.slice(4, 6), 16),
  ];
}

/** Luminance relative WCAG. */
function luminance(hex) {
  const canaux = composantes(hex).map((valeur) => {
    const proportion = valeur / 255;
    return proportion <= 0.04045 ? proportion / 12.92 : Math.pow((proportion + 0.055) / 1.055, 2.4);
  });

  return 0.2126 * canaux[0] + 0.7152 * canaux[1] + 0.0722 * canaux[2];
}

/** Rapport de contraste entre deux couleurs, de 1:1 à 21:1. */
function contraste(premiere, seconde) {
  const a = luminance(premiere);
  const b = luminance(seconde);
  const clair = Math.max(a, b);
  const sombre = Math.min(a, b);

  return (clair + 0.05) / (sombre + 0.05);
}

const jetons = lireJetons(readFileSync(FICHIER, 'utf8'));
const echecs = [];
const resultats = [];

for (const { contenu, fond, seuil } of PAIRES) {
  const jetonContenu = jetons.get(contenu);
  const jetonFond = jetons.get(fond);

  if (jetonContenu === undefined || jetonFond === undefined) {
    echecs.push(
      `jeton introuvable dans ${FICHIER} : ${jetonContenu === undefined ? contenu : fond}`,
    );
    continue;
  }

  for (const theme of ['clair', 'sombre']) {
    const rapport = contraste(jetonContenu[theme], jetonFond[theme]);
    const arrondi = Math.round(rapport * 100) / 100;
    resultats.push({ contenu, fond, theme, rapport: arrondi, seuil });

    if (rapport < seuil) {
      echecs.push(
        `${contenu} sur ${fond} — thème ${theme} : ${arrondi}:1, en dessous du seuil de ${seuil}:1 ` +
          `(${jetonContenu[theme]} sur ${jetonFond[theme]})`,
      );
    }
  }
}

if (echecs.length > 0) {
  console.error(`\n${echecs.length} contraste(s) insuffisant(s) :\n`);
  for (const echec of echecs) {
    console.error(`  - ${echec}`);
  }
  console.error('');
  process.exit(1);
}

const minimum = resultats.reduce(
  (pire, resultat) => (resultat.rapport < pire.rapport ? resultat : pire),
  resultats[0],
);

console.log(
  `${resultats.length} paires vérifiées dans les deux thèmes : toutes conformes. ` +
    `Marge la plus faible : ${minimum.contenu} sur ${minimum.fond} (${minimum.theme}) à ` +
    `${minimum.rapport}:1 pour un seuil de ${minimum.seuil}:1.`,
);
