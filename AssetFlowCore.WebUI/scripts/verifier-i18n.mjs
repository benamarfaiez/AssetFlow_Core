// Vérifie que tout texte visible d'un gabarit Angular passe par `@angular/localize`, et qu'aucun
// littéral accentué ne s'est glissé dans le code TypeScript d'une feature sans y passer non plus.
// `@angular/localize` ne garantit RIEN à la compilation contre un texte oublié : un
// `<p>Bonjour</p>` compile aussi bien qu'un `<p i18n>Bonjour</p>`. Ce script est donc le seul
// garde-fou réel de la règle 5.0.2 (poser l'i18n avant les écrans du Lot 5, pas après).
//
// Usage : npm run verifier:i18n
// Sortie : code 0 si aucune violation, 1 sinon, avec la liste fichier par fichier.
//
// Périmètre : gabarits `.html` de `src/app/features/**` et `src/app/shared/**` ; code `.ts` de
// `src/app/features/**` uniquement (voir plus bas pourquoi `shared/` en est exclu pour cette
// seconde règle). `features/diagnostic/` et `features/design-system/` sont explicitement
// exclus : ce sont des preuves d'exécution du socle promises à disparaître au Lot 5, pas des
// écrans produits.
//
// Volontairement écrit en Node pur, hors de la suite Vitest (voir verifier-dependances.mjs :
// `@types/node` dans `tsconfig.spec.json` exposerait les globales Node aux tests de composants).
//
// Limites assumées, par cohérence avec la sophistication des scripts voisins (analyse texte,
// pas un vrai parseur Angular — voir verifier-dependances.mjs qui ne suit, lui non plus, que des
// motifs d'import plutôt que de résoudre un graphe de modules) :
// - une interpolation `{{ expression }}` est toujours considérée conforme. Retracer si la valeur
//   qu'elle affiche provient bien d'un appel `$localize` demanderait une analyse de flux de
//   données hors de portée d'un script texte ; c'est pour cela que le code TypeScript est lui
//   aussi vérifié (règle 3) — mais seulement dans `features/`, jamais dans `shared/` : les
//   composants partagés exposent leurs textes par `input()`, fournis par l'écran appelant (déjà
//   couvert par la règle 3 une fois ce dernier écrit), et non codés en dur dans le composant lui
//   même (les quelques exceptions ponctuelles — valeurs par défaut d'`input()`, tableaux
//   d'options — sont converties manuellement, cf. shared/README.md) ;
// - un attribut `i18n` sur un élément couvre tout son contenu, y compris ses enfants — comme
//   dans Angular lui-même ;
// - les fichiers `*.spec.ts` sont exclus de la vérification des littéraux accentués : un test
//   compare légitimement un texte déjà traduit (assertion sur le rendu), sans en être la source
//   pour l'écran.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, sep } from 'node:path';

const RACINES_HTML = ['src/app/features', 'src/app/shared'];
const RACINE_TS_ACCENTS = 'src/app/features';

/** Dossiers ignorés : preuves d'exécution du socle, promises à disparaître au Lot 5. */
const DOSSIERS_EXCLUS = ['/features/diagnostic/', '/features/design-system/'];

/** Attributs natifs dont la version littérale doit être doublée d'un `i18n-*` jumeau. */
const ATTRIBUTS_TRADUISIBLES = ['aria-label', 'title', 'placeholder'];

/** Typographie française : voyelles accentuées, cédille, ligature, guillemets. */
const CARACTERE_ACCENTUE = /[àâäéèêëïîôöùûüÿçñÀÂÄÉÈÊËÏÎÔÖÙÛÜŸÇÑœŒ«»]/;

/** Une occurrence `$localize\`...\`` complète, y compris ses interpolations `${...}`. */
const MOTIF_LOCALIZE = /\$localize\s*`(?:\\.|\$\{[^}]*\}|[^`\\])*`/g;

/** Éléments HTML sans balise fermante : ils ne doivent jamais être empilés. */
const ELEMENTS_ORPHELINS = new Set([
  'area',
  'base',
  'br',
  'col',
  'embed',
  'hr',
  'img',
  'input',
  'link',
  'meta',
  'param',
  'source',
  'track',
  'wbr',
]);

const ERREURS = [];

/** Chemin d'affichage, toujours en `/`, quel que soit le séparateur de la plateforme. */
function aff(chemin) {
  return chemin.split(sep).join('/');
}

/** Chemins de tous les fichiers d'une extension donnée sous `dossier`, hors dossiers exclus. */
function fichiers(dossier, extension) {
  const trouves = [];

  for (const entree of readdirSync(dossier)) {
    const chemin = join(dossier, entree);
    const chemises = `${aff(chemin)}/`;

    if (DOSSIERS_EXCLUS.some((exclu) => chemises.includes(exclu))) {
      continue;
    }

    if (statSync(chemin).isDirectory()) {
      trouves.push(...fichiers(chemin, extension));
    } else if (entree.endsWith(extension)) {
      trouves.push(chemin);
    }
  }

  return trouves;
}

/** Numéro de ligne (1-based) d'un index dans une chaîne. */
function numeroLigne(source, index) {
  let ligne = 1;
  for (let i = 0; i < index && i < source.length; i += 1) {
    if (source[i] === '\n') ligne += 1;
  }
  return ligne;
}

// --- Règles 1 et 2 : gabarits HTML --------------------------------------------------------

/**
 * Découpe un gabarit en une séquence alternée texte / balise (ou commentaire) : à la manière
 * d'un `split` à motif capturant, les délimiteurs (balises, commentaires) sont conservés dans
 * le tableau résultat, aux index impairs.
 */
function decouper(source) {
  return source.split(/(<!--[\s\S]*?-->|<[^>]+>)/);
}

/** Vrai si la balise ouvrante porte un attribut `i18n` bare (pas `i18n-xxx`, qui cible un attribut). */
function porteI18n(balise) {
  return /\si18n(?=[\s=/>])/.test(balise);
}

/** Nettoie un nœud de texte de tout ce qui n'est pas un texte destiné à l'utilisateur. */
function texteResiduel(texte) {
  return (
    texte
      // Interpolations : `{{ expression }}` — la source de la traduction est ailleurs (voir
      // limites assumées en tête de fichier).
      .replace(/\{\{[\s\S]*?\}\}/g, ' ')
      // Ouvertures de bloc de contrôle Angular avec condition : `@if (...) {`, `} @else if (...) {`,
      // `@for (...) {`, `@switch (...) {`, `} @case (...) {`, `@defer (...) {`, sous-blocs de
      // `@defer` compris. Non glouton, arrêt à la première parenthèse fermante suivie de `{` :
      // suffisant tant que la condition ne contient pas elle-même un bloc `{ }` littéral.
      .replace(
        /\}?\s*@(if|else\s+if|for|switch|case|defer|placeholder|loading|error)\s*\([\s\S]*?\)\s*\{/g,
        ' ',
      )
      // Continuations sans condition : `} @else {`, `} @empty {`, `} @default {`.
      .replace(/\}?\s*@(else|empty|default)\s*\{/g, ' ')
      // Entités HTML (`&times;`, `&amp;`, `&#215;`…) : jamais un mot à traduire.
      .replace(/&[a-zA-Z][a-zA-Z0-9]*;|&#\d+;|&#x[0-9a-fA-F]+;/g, ' ')
      // Accolades résiduelles (fermeture de bloc seule sur sa ligne, par exemple).
      .replace(/[{}]/g, ' ')
      .trim()
  );
}

function verifierGabarit(chemin) {
  const source = readFileSync(chemin, 'utf8');
  const jetons = decouper(source);
  const pile = [];
  let position = 0;

  for (const jeton of jetons) {
    if (jeton.startsWith('<!--')) {
      // Commentaire : ni balise ni texte visible — rien à vérifier, rien à empiler.
    } else if (jeton.startsWith('</')) {
      pile.pop();
    } else if (jeton.startsWith('<')) {
      const nomBalise = /^<([a-zA-Z0-9-]+)/.exec(jeton)?.[1] ?? '';
      const estOrpheline = ELEMENTS_ORPHELINS.has(nomBalise.toLowerCase());
      const estAutoFermee = /\/\s*>\s*$/.test(jeton);
      const parentCouvert = pile.length > 0 && pile[pile.length - 1];
      const couverte = parentCouvert || porteI18n(jeton);

      if (!estAutoFermee && !estOrpheline) {
        pile.push(couverte);
      }

      // Règle 2 : attributs littéraux aria-label / title / placeholder sans `i18n-*` jumeau.
      for (const attribut of ATTRIBUTS_TRADUISIBLES) {
        const trouve = new RegExp(`\\s${attribut}="([^"]*)"`).exec(jeton);
        if (trouve === null) continue;
        if (trouve[1].includes('{{')) continue; // interpolé : la source est ailleurs.
        if (new RegExp(`\\si18n-${attribut}(?=[\\s=/>])`).test(jeton)) continue;

        ERREURS.push(
          `${aff(chemin)}:${numeroLigne(source, position)} → attribut « ${attribut} » littéral ` +
            `(« ${trouve[1]} ») sans « i18n-${attribut} » correspondant.`,
        );
      }
    } else {
      // Texte : couvert si l'élément englobant (ou un ancêtre) porte `i18n`.
      const couvert = pile.length > 0 && pile[pile.length - 1];
      if (!couvert) {
        const residuel = texteResiduel(jeton);
        // Une ponctuation ou un symbole isolé (l'astérisque d'un champ requis, le séparateur
        // « / » d'un fil d'Ariane…) n'est pas un mot à traduire — a fortiori lorsqu'il est déjà
        // `aria-hidden`. Seule la présence d'au moins une lettre déclenche la règle.
        if (/\p{L}/u.test(residuel)) {
          ERREURS.push(
            `${aff(chemin)}:${numeroLigne(source, position)} → texte non couvert par ` +
              `\`i18n\` : « ${residuel.slice(0, 60)} »`,
          );
        }
      }
    }

    position += jeton.length;
  }
}

// --- Règle 3 : littéraux accentués dans le TypeScript des features -------------------------

/**
 * Neutralise les commentaires `//` et `/* *\/` d'une source TypeScript avant la recherche de
 * littéraux, sans jamais les confondre avec une séquence identique à l'intérieur d'un littéral de
 * chaîne ou d'un template literal (une chaîne peut contenir `//`, un commentaire peut contenir un
 * guillemet non apparié). Sans ce passage, un commentaire français ordinaire — qui contient
 * presque toujours une apostrophe (« qu'il », « l'API », « d'un ») — se ferait lire comme
 * l'ouverture d'un littéral `'...'` et engloutirait tout le texte jusqu'à l'apostrophe suivante,
 * potentiellement plusieurs lignes de commentaire ou de code plus loin.
 *
 * Préserve la longueur et la position des retours à la ligne de la source d'origine (chaque
 * caractère neutralisé est remplacé par un espace, sauf `\n` toujours conservé), afin que
 * `numeroLigne` reste exact sur le résultat.
 *
 * Limite assumée, cohérente avec le reste du fichier (analyse texte, pas un vrai parseur) : un
 * littéral de regex contenant un guillemet (ex. `/['"]/`) serait mal interprété comme l'ouverture
 * d'une chaîne. Cas jugé assez rare dans du code d'écran pour ne pas justifier un tokenizer
 * complet — un vrai littéral accentué mal détecté resterait de toute façon visible en revue.
 */
function retirerCommentaires(source) {
  let resultat = '';
  let i = 0;

  while (i < source.length) {
    const caractere = source[i];
    const suivant = source[i + 1];

    if (caractere === '/' && suivant === '/') {
      while (i < source.length && source[i] !== '\n') {
        resultat += ' ';
        i += 1;
      }
      continue;
    }

    if (caractere === '/' && suivant === '*') {
      resultat += '  ';
      i += 2;
      while (i < source.length && !(source[i] === '*' && source[i + 1] === '/')) {
        resultat += source[i] === '\n' ? '\n' : ' ';
        i += 1;
      }
      if (i < source.length) {
        resultat += '  ';
        i += 2;
      }
      continue;
    }

    if (caractere === "'" || caractere === '"' || caractere === '`') {
      const guillemet = caractere;
      resultat += caractere;
      i += 1;
      while (i < source.length && source[i] !== guillemet) {
        if (source[i] === '\\' && i + 1 < source.length) {
          resultat += source[i] + source[i + 1];
          i += 2;
          continue;
        }
        resultat += source[i];
        i += 1;
      }
      if (i < source.length) {
        resultat += source[i];
        i += 1;
      }
      continue;
    }

    resultat += caractere;
    i += 1;
  }

  return resultat;
}

function verifierLitteraux(chemin) {
  const source = readFileSync(chemin, 'utf8');
  const sansCommentaires = retirerCommentaires(source);
  const sansLocalize = sansCommentaires.replace(MOTIF_LOCALIZE, (correspondance) =>
    ' '.repeat(correspondance.length),
  );

  const motifLitteral = /'(?:[^'\\]|\\.)*'|"(?:[^"\\]|\\.)*"|`(?:[^`\\]|\\.)*`/g;
  for (const trouve of sansLocalize.matchAll(motifLitteral)) {
    if (!CARACTERE_ACCENTUE.test(trouve[0])) continue;

    ERREURS.push(
      `${aff(chemin)}:${numeroLigne(sansLocalize, trouve.index)} → littéral accentué hors ` +
        `\`$localize\` : ${trouve[0].slice(0, 60)}`,
    );
  }
}

// --- Exécution -------------------------------------------------------------------------------

for (const racine of RACINES_HTML) {
  for (const chemin of fichiers(racine, '.html')) {
    verifierGabarit(chemin);
  }
}

for (const chemin of fichiers(RACINE_TS_ACCENTS, '.ts')) {
  if (chemin.endsWith('.spec.ts')) continue;
  verifierLitteraux(chemin);
}

if (ERREURS.length > 0) {
  console.error(`\n${ERREURS.length} violation(s) i18n :\n`);
  for (const erreur of ERREURS) {
    console.error(`  - ${erreur}`);
  }
  console.error('');
  process.exit(1);
}

console.log(
  'Aucun texte oublié : gabarits de features/ et shared/ conformes à `i18n`, aucun littéral ' +
    'accentué hors `$localize` dans features/.',
);
