import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join, relative } from 'node:path';

const repos = {
  germio: 'Tests~/ConventionTests/vocabulary',
  stemic: 'game/tests/ConventionTests/vocabulary',
  promeno: 'game/tests/ConventionTests/vocabulary',
  explore: 'game/tests/ConventionTests/vocabulary',
  briko: 'Tests~/ConventionTests/vocabulary',
  animo: 'Tests~/EditModeTests/Convention/vocabulary',
  opinio: 'app/tests/Webio.Core.Tests/vocabulary',
};

function load_vocabulary(paths) {
  const words = new Set();
  for (const path of paths) {
    if (!existsSync(path)) continue;
    const text = readFileSync(path, 'utf-8');
    for (const line of text.split('\n')) {
      if (line.startsWith('+ ')) {
        for (const word of line.slice(2).trim().split(/\s+/)) if (word) words.add(word.toLowerCase());
        continue;
      }
      const term_match = line.match(/^\*\*([^*]+)\*\*/);
      if (term_match) for (const word of term_match[1].split(/\s+/)) {
        const clean = word.replace(/[^A-Za-z]/g, '');
        if (clean) words.add(clean.toLowerCase());
      }
    }
  }
  return words;
}

function strip_non_prose(text) {
  return text
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/`[^`]*`/g, ' ')
    .replace(/<!--[\s\S]*?-->/g, ' ')
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/^#+\s*/gm, ' ')
    .replace(/^\+\s*/gm, ' ')
    .replace(/'s\b/g, '');
}

function check_basic_english(text, vocabulary) {
  const prose = strip_non_prose(text);
  const errors = [];
  const seen = new Set();
  const chunks = prose.split(/(?<=[.!?\n])\s*/);
  for (const chunk of chunks) {
    let is_first_word = true;
    for (const match of chunk.matchAll(/[A-Za-z]+/g)) {
      const word = match[0];
      const is_capitalized = /^[A-Z]/.test(word);
      if (is_capitalized && !is_first_word) { is_first_word = false; continue; }
      is_first_word = false;
      const lower = word.toLowerCase();
      if (vocabulary.has(lower)) continue;
      if (seen.has(lower)) continue;
      seen.add(lower);
      errors.push(lower);
    }
  }
  return errors;
}

function find_md_files(dir, base, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === 'node_modules' || entry.name.startsWith('.git') || entry.name === 'bin' || entry.name === 'obj') continue;
    const full = join(dir, entry.name);
    if (entry.isDirectory()) find_md_files(full, base, out);
    else if (entry.name.endsWith('.md') && !entry.name.endsWith('_JP.md')) out.push(relative(base, full));
  }
  return out;
}

const results = {};
for (const [repo, vd] of Object.entries(repos)) {
  const base = `/home/claude/${repo}`;
  const vocab_files = readdirSync(join(base, vd)).filter(f => f.endsWith('.md')).map(f => join(base, vd, f));
  const tech_terms = join(base, 'docs/standard/tech_terms.md');
  if (existsSync(tech_terms)) vocab_files.push(tech_terms);
  const vocab = load_vocabulary(vocab_files);

  const md_files = find_md_files(base, base).filter(f => !f.startsWith('Editor/') && !f.startsWith('.githooks/'));
  let pass = 0, fail = 0;
  const failing = [];
  for (const f of md_files) {
    const text = readFileSync(join(base, f), 'utf-8');
    const errors = check_basic_english(text, vocab);
    if (errors.length === 0) pass++;
    else { fail++; failing.push([f, errors.length]); }
  }
  results[repo] = { total: md_files.length, pass, fail, failing };
}

console.log('| リポジトリ | 全.mdファイル数 | 通過 | 違反あり |');
console.log('|---|---|---|---|');
for (const [repo, r] of Object.entries(results)) {
  console.log(`| ${repo} | ${r.total} | ${r.pass} | ${r.fail} |`);
}
console.log('');
for (const [repo, r] of Object.entries(results)) {
  if (r.failing.length === 0) continue;
  console.log(`=== ${repo} の違反ファイル ===`);
  for (const [f, n] of r.failing.sort((a,b) => b[1]-a[1])) console.log(`  ${f}: ${n}件`);
}
