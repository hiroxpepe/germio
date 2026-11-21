// Tests for plans: a ROADMAP.md of format v2 lists plans (PLAN-01 to PLAN-99) under phases, and a
// TASKLIST.md of format v2 puts a plan tag on every open task. PLAN-XX is for a task that fits no plan.
// Run with: node --test tools/
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { validate_tasklist } from './validate_tasklist.js';
import { validate_roadmap, read_roadmap_plans } from './validate_roadmap.js';

const TOOLS = fileURLToPath(new URL('.', import.meta.url));
const TASK_V2 = '<!-- format: v2 | fields: status, id, title, phase, plan -->';
const ROAD_V2 = '<!-- format: v2 | fields: status, phase, title | plans: status, plan, phase, title -->';

function roadmap(lines, headings) {
  return ['# ROADMAP', '', ROAD_V2, '', ...lines, '', '## Detail', '', ...headings.map((h) => `### ${h}\n\nText.\n`)].join('\n');
}

function tasklist(lines, ids) {
  return ['# TASKLIST', '', TASK_V2, '', ...lines, '', '## Detail', '', ...ids.map((id) => `### ${id}\n\nText.\n`)].join('\n');
}

const ROAD_OK = roadmap(
  ['+ [~] PHASE-01: Build the base', '+ [ ] PHASE-02: Add the next part', '+ [ ] PLAN-01 [PHASE-01]: Make the first part', '+ [ ] PLAN-02 [PHASE-02]: Make the second part'],
  ['PHASE-01', 'PHASE-02', 'PLAN-01', 'PLAN-02']);
const PLANS = [{ id: 'PLAN-01', phase: 'PHASE-01' }, { id: 'PLAN-02', phase: 'PHASE-02' }];
const PHASES = ['PHASE-01', 'PHASE-02'];

test('a v2 roadmap with plans under its phases is taken', () => {
  assert.deepEqual(validate_roadmap(ROAD_OK), []);
});

test('the plans of a v2 roadmap are read with their phases', () => {
  assert.deepEqual(read_roadmap_plans(ROAD_OK), PLANS);
});

test('a plan under a phase the roadmap does not list is turned down', () => {
  const text = roadmap(['+ [~] PHASE-01: Build the base', '+ [ ] PLAN-01 [PHASE-09]: Make it'], ['PHASE-01', 'PLAN-01']);
  assert.deepEqual(validate_roadmap(text), ["'PLAN-01' is under 'PHASE-09', a phase the roadmap does not list"]);
});

test('a plan id given twice is turned down', () => {
  const text = roadmap(['+ [~] PHASE-01: Build the base', '+ [ ] PLAN-01 [PHASE-01]: Make it', '+ [ ] PLAN-01 [PHASE-01]: Make it again'], ['PHASE-01', 'PLAN-01']);
  assert.ok(validate_roadmap(text).includes("duplicate PLAN id in the summary list: 'PLAN-01'"));
});

test('a plan with no detail heading is turned down', () => {
  const text = roadmap(['+ [~] PHASE-01: Build the base', '+ [ ] PLAN-01 [PHASE-01]: Make it'], ['PHASE-01']);
  assert.ok(validate_roadmap(text).includes("no '### PLAN-01' detail section found for a PLAN listed in the summary"));
});

test('a plan done while its phase is open leaves the in-progress check alone', () => {
  const text = roadmap(['+ [~] PHASE-01: Build the base', '+ [x] PLAN-01 [PHASE-01]: Make it'], ['PHASE-01', 'PLAN-01']);
  assert.deepEqual(validate_roadmap(text), []);
});

test('a plan id with one figure, or in small letters, is turned down', () => {
  for (const line of ['+ [ ] PLAN-1 [PHASE-01]: Make it', '+ [ ] plan-01 [PHASE-01]: Make it']) {
    const text = roadmap(['+ [~] PHASE-01: Build the base', line], ['PHASE-01']);
    assert.ok(validate_roadmap(text).some((e) => /does not match/.test(e)), line);
  }
});

test('an open task with a plan of its own phase is taken', () => {
  const text = tasklist(['+ [ ] TASK-001 [PHASE-01] [PLAN-01]: Do one thing', '+ [~] TASK-002 [PHASE-02] [PLAN-02]: Do the next thing'], ['TASK-001', 'TASK-002']);
  assert.deepEqual(validate_tasklist(text, PHASES, PLANS), []);
});

test('an open task with no plan is turned down in a v2 tasklist', () => {
  const text = tasklist(['+ [ ] TASK-001 [PHASE-01]: Do one thing'], ['TASK-001']);
  assert.deepEqual(validate_tasklist(text, PHASES, PLANS), ["'TASK-001' is open but has no plan tag"]);
});

test('a done task with no plan is taken', () => {
  for (const mark of ['x', 'xx']) {
    const text = tasklist([`+ [${mark}] TASK-001 [PHASE-01]: Did one thing`], ['TASK-001']);
    assert.deepEqual(validate_tasklist(text, PHASES, PLANS), [], mark);
  }
});

test('a done task with a plan is taken', () => {
  const text = tasklist(['+ [x] TASK-001 [PHASE-01] [PLAN-01]: Did one thing'], ['TASK-001']);
  assert.deepEqual(validate_tasklist(text, PHASES, PLANS), []);
});

test('a plan the roadmap does not list is turned down', () => {
  const text = tasklist(['+ [ ] TASK-001 [PHASE-01] [PLAN-07]: Do one thing'], ['TASK-001']);
  assert.deepEqual(validate_tasklist(text, PHASES, PLANS), ["'PLAN-07' (on TASK-001) is not a plan in ROADMAP.md"]);
});

test('a plan of another phase is turned down', () => {
  const text = tasklist(['+ [ ] TASK-001 [PHASE-01] [PLAN-02]: Do one thing'], ['TASK-001']);
  assert.deepEqual(validate_tasklist(text, PHASES, PLANS), ["'PLAN-02' (on TASK-001) is under 'PHASE-02', not 'PHASE-01'"]);
});

test('PLAN-XX is taken under any phase with no roadmap line for it', () => {
  const text = tasklist(['+ [ ] TASK-001 [PHASE-02] [PLAN-XX]: Do one thing'], ['TASK-001']);
  assert.deepEqual(validate_tasklist(text, PHASES, PLANS), []);
});

test('a plan tag in a v1 tasklist is turned down', () => {
  const text = tasklist(['+ [ ] TASK-001 [PHASE-01] [PLAN-01]: Do one thing'], ['TASK-001'])
    .replace(TASK_V2, '<!-- format: v1 | fields: status, id, title, phase -->');
  assert.ok(validate_tasklist(text, PHASES, PLANS).some((e) => /does not match/.test(e)));
});

test('the tasklist command reads the plans of the roadmap next to it', () => {
  const dir = mkdtempSync(join(tmpdir(), 'plan_'));
  writeFileSync(join(dir, 'ROADMAP.md'), ROAD_OK);
  writeFileSync(join(dir, 'TASKLIST.md'), tasklist(['+ [ ] TASK-001 [PHASE-01] [PLAN-01]: Do one thing'], ['TASK-001']));
  const out = execFileSync(process.execPath, [join(TOOLS, 'validate_tasklist_cli.js'), join(dir, 'TASKLIST.md')], { encoding: 'utf-8', stdio: ['ignore', 'pipe', 'pipe'] });
  assert.match(out, /format OK/);
});
