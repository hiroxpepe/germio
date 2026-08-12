// Checks that a file's own prose (not its code, links, or markup)
// only uses words from a given vocabulary — normally basic_words.md
// plus a repo's own standard_words.md/tech_terms.md, the same
// vocabulary files ConventionRules.cs already uses for identifier
// names, now applied to prose text instead.

function strip_non_prose(text) {
  return text
    .replace(/```[\s\S]*?```/g, ' ')       // fenced code blocks
    .replace(/`[^`]*`/g, ' ')              // inline code spans
    .replace(/<!--[\s\S]*?-->/g, ' ')      // HTML comments
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1') // markdown links: keep the link text, drop the URL
    .replace(/^#+\s*/gm, ' ')              // heading markers
    .replace(/^\+\s*/gm, ' ')              // "+" list markers
    .replace(/'s\b/g, '');                 // a possessive 's: drop it, not the word before it
}

export function check_basic_english(text, vocabulary) {
  const prose = strip_non_prose(text);
  const errors = [];
  const seen = new Set();

  // Split into sentence-like chunks so a mid-sentence capitalized word
  // (a likely proper noun: Mario, Unity, Vitest) can be told apart from
  // an ordinary word that only happens to open a sentence.
  const chunks = prose.split(/(?<=[.!?\n])\s*/);
  for (const chunk of chunks) {
    let is_first_word = true;
    for (const match of chunk.matchAll(/[A-Za-z]+/g)) {
      const word = match[0];
      const is_capitalized = /^[A-Z]/.test(word);
      if (is_capitalized && !is_first_word) {
        is_first_word = false;
        continue; // a likely proper noun; never checked against the vocabulary
      }
      is_first_word = false;
      const lower = word.toLowerCase();
      if (vocabulary.has(lower)) continue;
      if (seen.has(lower)) continue;
      seen.add(lower);
      errors.push(`word not in the allowed vocabulary: '${word}'`);
    }
  }
  return errors;
}
