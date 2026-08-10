#!/usr/bin/env node
// CLI wrapper: node basic_english_cli.js <path> <vocabulary dir> <tech_terms.md path>
// Checks a file's own prose against the shared Basic English word
// lists, with no TASKLIST.md-specific structure check — for files
// such as HANDOFF.md that hold free prose, not a checkbox summary.
import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { load_vocabulary } from './load_vocabulary.js';
import { check_basic_english } from './basic_english_check.js';

const path = process.argv[2];
const vocab_dir = process.argv[3];
const tech_terms_path = process.argv[4];
if (!path || !vocab_dir) {
  console.error('usage: node basic_english_cli.js <path> <vocabulary-dir> [tech_terms.md path]');
  process.exit(1);
}

const vocab_files = readdirSync(vocab_dir)
  .filter(f => f.endsWith('.md'))
  .map(f => join(vocab_dir, f));
if (tech_terms_path && existsSync(tech_terms_path)) vocab_files.push(tech_terms_path);
const vocab = load_vocabulary(vocab_files);

const text = readFileSync(path, 'utf-8');
const errors = check_basic_english(text, vocab);

if (errors.length > 0) {
  console.error(`Basic English errors in ${path}:`);
  for (const error of errors) console.error(`  - ${error}`);
  console.error('');
  console.error('What to do next:');
  console.error('  - A plain English word: add it to draft_words.md in the');
  console.error('    same vocabulary folder. Stop there.');
  console.error('  - Never move a word from draft_words.md to');
  console.error('    plain_words.md yourself. That move needs the');
  console.error('    master\'s own, direct GO, every time — not a rule an');
  console.error('    agent applies on its own, even when a word looks');
  console.error('    needed in more than one repository.');
  console.error('  - A real technical word, with a real reason for it: add');
  console.error('    it to docs/standard/tech_terms.md instead, with a short,');
  console.error('    plain sense for it, before you use the word again.');
  console.error('  - Never widen basic_words.md. That file holds only');
  console.error('    Ogden\'s own 850 words, checked against the source, not');
  console.error('    a guess.');
  process.exit(1);
}
console.log(`${path}: Basic English OK`);
