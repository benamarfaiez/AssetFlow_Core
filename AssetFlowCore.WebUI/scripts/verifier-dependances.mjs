// Vérifie mécaniquement les règles de dépendances de l'arborescence frontend, à la manière des
// tests ArchUnitNET du backend (AssetFlowCore.ArchitectureTests) : une convention qu'aucune
// commande ne contrôle finit par être enfreinte sans que personne ne le voie.
//
// Usage : npm run verifier:dependances
// Sortie : code 0 si aucune violation, 1 sinon, avec la liste fichier par fichier.
//
// Volontairement écrit en Node pur, hors de la suite Vitest : introduire `@types/node` dans
// `tsconfig.spec.json` exposerait les globales Node aux tests de composants, qui s'exécutent
// dans un environnement navigateur simulé.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const RACINE = 'src/app';

/**
 * Règles issues de doc/ARCHITECTURE.md §3.1 et doc/TECHNICAL-SPECIFICATION.md §2.2.
 * `zone` : préfixe de chemin surveillé. `interdits` : fragments de chemin d'import proscrits.
 */
const REGLES = [
  {
    zone: 'shared',
    interdits: ['core/', 'features/'],
    motif:
      'shared/ est purement présentationnel : aucune dépendance métier ni réseau (core/), aucune dépendance à un écran (features/).',
  },
  {
    zone: 'core',
    interdits: ['features/'],
    motif: 'core/ porte des singletons transverses : il ignore les écrans.',
  },
];

/** Les features ne s'importent pas entre elles : ce qui est partagé remonte dans shared/. */
const ZONE_FEATURES = 'features';

const ERREURS = [];

/** Chemins de tous les fichiers TypeScript sous `dossier`. */
function fichiersTypeScript(dossier) {
  const trouves = [];

  for (const entree of readdirSync(dossier)) {
    const chemin = join(dossier, entree);

    if (statSync(chemin).isDirectory()) {
      trouves.push(...fichiersTypeScript(chemin));
    } else if (entree.endsWith('.ts')) {
      trouves.push(chemin);
    }
  }

  return trouves;
}

/** Spécificateurs de module importés par un fichier (`import ... from '...'` et imports dynamiques). */
function importsDe(chemin) {
  const source = readFileSync(chemin, 'utf8');
  const specificateurs = [];

  for (const found of source.matchAll(/from\s+['"]([^'"]+)['"]/g)) {
    specificateurs.push(found[1]);
  }
  for (const found of source.matchAll(/import\(\s*['"]([^'"]+)['"]\s*\)/g)) {
    specificateurs.push(found[1]);
  }

  // Seuls les imports **relatifs** franchissent les zones de l'application. Écarter les paquets
  // évite un faux positif que le contraire produisait : `@angular/core/testing` contient
  // « /core/ » sans avoir le moindre rapport avec `src/app/core/`.
  return specificateurs.filter((specificateur) => specificateur.startsWith('.'));
}

for (const fichier of fichiersTypeScript(RACINE)) {
  const cheminRelatif = relative(RACINE, fichier).split(sep).join('/');
  const zone = cheminRelatif.split('/')[0];
  const specificateurs = importsDe(fichier);

  for (const regle of REGLES) {
    if (zone !== regle.zone) continue;

    for (const specificateur of specificateurs) {
      // Un import relatif remontant vers une autre zone contient son nom : `../core/api/...`.
      if (regle.interdits.some((interdit) => specificateur.includes(`/${interdit}`))) {
        ERREURS.push(`${RACINE}/${cheminRelatif} → ${specificateur}\n    ${regle.motif}`);
      }
    }
  }

  if (zone === ZONE_FEATURES) {
    const featureCourante = cheminRelatif.split('/')[1];

    for (const specificateur of specificateurs) {
      const cible = /\/features\/([^/]+)\//.exec(specificateur);

      if (cible !== null && cible[1] !== featureCourante) {
        ERREURS.push(
          `${RACINE}/${cheminRelatif} → ${specificateur}\n` +
            `    Import croisé entre les features « ${featureCourante} » et « ${cible[1]} » : ce qui est partagé remonte dans shared/.`,
        );
      }
    }
  }
}

if (ERREURS.length > 0) {
  console.error(`\n${ERREURS.length} violation(s) des règles de dépendances :\n`);
  for (const erreur of ERREURS) {
    console.error(`  - ${erreur}`);
  }
  console.error('');
  process.exit(1);
}

console.log(
  'Règles de dépendances respectées : shared/ ⊄ core|features, core/ ⊄ features, features/ sans import croisé.',
);
