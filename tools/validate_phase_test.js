// Tests for the phase id form of TASKLIST.md and ROADMAP.md: PHASE-01 to PHASE-99 and PHASE-XX.
// Run with: node --test tools/
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { validate_tasklist } from './validate_tasklist.js';
import { validate_roadmap } from './validate_roadmap.js';

const TOOLS = fileURLToPath(new URL('.', import.meta.url));
const TASK_MARK = '<!-- format: v1 | fields: status, id, title, phase -->';
const ROAD_MARK = '<!-- format: v1 | fields: status, phase, title -->';

function tasklist(lines) {
  return ['# TASKLIST', '', TASK_MARK, '', ...lines, '', '## Detail', '', '### TASK-001', '', 'Text.', ''].join('\n');
}

function roadmap(lines, details) {
  return ['# ROADMAP', '', ROAD_MARK, '', ...lines, '', '## Detail', '', ...details.map((d) => `### ${d}\n\nText.\n`)].join('\n');
}

test('a task line with a PHASE-01 tag is taken', () => {
  assert.deepEqual(validate_tasklist(tasklist(['+ [ ] TASK-001 [PHASE-01]: Do one thing'])), []);
});

test('a task line with a PHASE-XX tag is taken', () => {
  assert.deepEqual(validate_tasklist(tasklist(['+ [x] TASK-001 [PHASE-XX]: Do one thing'])), []);
});

test('a task line with the old P-01 tag is turned down', () => {
  const errors = validate_tasklist(tasklist(['+ [ ] TASK-001 [P-01]: Do one thing']));
  assert.ok(errors.some((e) => /does not match/.test(e)));
});

test('a PHASE tag not in the roadmap is turned down', () => {
  const errors = validate_tasklist(tasklist(['+ [ ] TASK-001 [PHASE-02]: Do one thing']), ['PHASE-01', 'PHASE-XX']);
  assert.deepEqual(errors, ["'PHASE-02' (on TASK-001) is not a phase in ROADMAP.md"]);
});

test('a roadmap line with a PHASE-01 id is taken', () => {
  assert.deepEqual(validate_roadmap(roadmap(['+ [~] PHASE-01: Build the base', '+ [ ] PHASE-XX: Work outside the phases'], ['PHASE-01', 'PHASE-XX'])), []);
});

test('a roadmap line with the old P-01 id is turned down', () => {
  const errors = validate_roadmap(roadmap(['+ [~] P-01: Build the base'], ['P-01']));
  assert.ok(errors.some((e) => /does not match/.test(e)));
});

test('the tasklist command reads PHASE ids from the roadmap next to it', () => {
  const dir = mkdtempSync(join(tmpdir(), 'phase_'));
  writeFileSync(join(dir, 'ROADMAP.md'), roadmap(['+ [~] PHASE-01: Build the base'], ['PHASE-01']));
  writeFileSync(join(dir, 'TASKLIST.md'), tasklist(['+ [ ] TASK-001 [PHASE-01]: Do one thing']));
  const out = execFileSync(process.execPath, [join(TOOLS, 'validate_tasklist_cli.js'), join(dir, 'TASKLIST.md')], { encoding: 'utf-8', stdio: ['ignore', 'pipe', 'pipe'] });
  assert.match(out, /format OK/);
});
