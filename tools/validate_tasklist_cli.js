#!/usr/bin/env node
// CLI wrapper: node validate_tasklist_cli.js <TASKLIST.md path> <vocabulary dir> <tech_terms.md path>
// The last two arguments are optional; if given, the file's own prose
// is also checked against the shared Basic English word lists
// (basic_words.md, standard_words.md, and the rest already used for
// identifier naming, plus tech_terms.md), the same standard every
// document in this repo family follows.
//
// A ROADMAP.md file, if found next to TASKLIST.md, is read too, and
// every [PHASE-XX] tag in TASKLIST.md is checked against the real
// phase ids it lists.
import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
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

let roadmap_phase_ids = null;
const roadmap_path = join(dirname(path), 'ROADMAP.md');
if (existsSync(roadmap_path)) {
  const roadmap_text = readFileSync(roadmap_path, 'utf-8');
  roadmap_phase_ids = [...roadmap_text.matchAll(/^\+ \[[ x~]\] (PHASE-\d{2}):/gm)].map(m => m[1]);
}

let errors = validate_tasklist(text, roadmap_phase_ids);

if (vocab_dir && existsSync(vocab_dir)) {
  const vocab_files = readdirSync(vocab_dir)
    .filter(f => f.endsWith('.md'))
    .map(f => join(vocab_dir, f));
  if (tech_terms_path && existsSync(tech_terms_path)) vocab_files.push(tech_terms_path);
  const vocab = load_vocabulary(vocab_files);
  // A checkbox mark is a state sign, not a word: strip every one of
  // them before the Basic English check, so 'xx' (an archived task,
  // kept out of the dashboard) is never read as a word.
  const text_without_checkbox = text.replace(/^\+ \[(?: |x|xx|~)\] /gm, '+ ');
  errors = errors.concat(
    check_basic_english(text_without_checkbox, vocab).map(e => `not Basic English: ${e}`)
  );
}

if (errors.length > 0) {
  console.error(`TASKLIST.md format errors in ${path}:`);
  for (const error of errors) console.error(`  - ${error}`);
  console.error('');
  console.error('What to do next for a "not Basic English" line:');
  console.error('  - A plain English word: add it to draft_words.md in the');
  console.error('    same vocabulary folder. Stop there.');
  console.error('  - Never move a word from draft_words.md to');
  console.error('    standard_words.md yourself. That move needs the');
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
console.log(`${path}: TASKLIST.md format OK`);
