import { readFileSync, existsSync } from 'node:fs';

// Reads the "+ base form1 form2 ..." vocabulary files ConventionRules.cs
// already uses for identifier names (basic_words.md, standard_words.md,
// and the rest), and also reads tech_terms.md's own "**Term** —
// meaning" lines, flattening every word found into one plain word set
// for prose checking.
export function load_vocabulary(paths) {
  const words = new Set();
  for (const path of paths) {
    if (!existsSync(path)) continue;
    const text = readFileSync(path, 'utf-8');
    for (const line of text.split('\n')) {
      if (line.startsWith('+ ')) {
        for (const word of line.slice(2).trim().split(/\s+/)) {
          if (word) words.add(word.toLowerCase());
        }
        continue;
      }
      const term_match = line.match(/^\*\*([^*]+)\*\*/);
      if (term_match) {
        for (const word of term_match[1].split(/\s+/)) {
          const clean = word.replace(/[^A-Za-z]/g, '');
          if (clean) words.add(clean.toLowerCase());
        }
      }
    }
  }
  return words;
}
