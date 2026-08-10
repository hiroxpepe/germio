#!/usr/bin/env node
// CLI wrapper: node validate_tasklist_cli.js <TASKLIST.md path> <vocabulary dir> <tech_terms.md path>
// The last two arguments are optional; if given, the file's own prose
// is also checked against the shared Basic English word lists
// (basic_words.md, plain_words.md, and the rest already used for
// identifier naming, plus tech_terms.md), the same standard every
// document in this repo family follows.
import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { validate_tasklist } from './validate_tasklist.js';
import { load_vocabulary } from './load_vocabulary.js';
import { check_basic_english } from './basic_english_check.js';

const path = process.argv[2];
const vocab_dir = process.argv[3];
const tech_terms_path = process.argv[4];
if (!path) {
  console.error('usage: node validate_tasklist_cli.js <path-to-TASKLIST.md> [vocabulary-dir] [tech_terms.md path]');
  process.exit(1);
}

const text = readFileSync(path, 'utf-8');
let errors = validate_tasklist(text);

if (vocab_dir && existsSync(vocab_dir)) {
  const vocab_files = readdirSync(vocab_dir)
    .filter(f => f.endsWith('.md'))
    .map(f => join(vocab_dir, f));
  if (tech_terms_path && existsSync(tech_terms_path)) vocab_files.push(tech_terms_path);
  const vocab = load_vocabulary(vocab_files);
  errors = errors.concat(
    check_basic_english(text, vocab).map(e => `not Basic English: ${e}`)
  );
}

if (errors.length > 0) {
  console.error(`TASKLIST.md format errors in ${path}:`);
  for (const error of errors) console.error(`  - ${error}`);
  process.exit(1);
}
console.log(`${path}: TASKLIST.md format OK`);
