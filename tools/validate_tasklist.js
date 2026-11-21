// Validates the structure of a TASKLIST.md, matching the shared
// format used across every personal repo: a machine-readable summary
// list at the top (one checkbox line per task, a fixed
// checkbox+id+title shape), and a Detail section below with one
// heading per task, in step with that same summary list.
//
// Returns a plain array of error strings, empty if the file is well
// formed. This file has zero dependencies on purpose, so it can run
// with a plain `node` call from a git hook, with no npm install step
// in the way.

const SUMMARY_LINE = /^\+ \[( |~|x|xx)\] (TASK-\d+) \[(PHASE-(?:\d{2}|XX))\]: (.+)$/;
const SUMMARY_LINE_V2 = /^\+ \[( |~|x|xx)\] (TASK-\d+) \[(PHASE-(?:\d{2}|XX))\](?: \[(PLAN-(?:\d{2}|XX))\])?: (.+)$/;
const MARK_V1 = '<!-- format: v1 | fields: status, id, title, phase -->';
const MARK_V2 = '<!-- format: v2 | fields: status, id, title, phase, plan -->';
const DETAIL_HEADING = /^### (TASK-\d+)$/;

export function validate_tasklist(text, roadmap_phase_ids = null, roadmap_plans = null) {
  const errors = [];
  const lines = text.split('\n');
  const detail_index = lines.findIndex(l => l.trim() === '## Detail');
  const summary_lines = detail_index === -1 ? lines : lines.slice(0, detail_index);

  const is_v2 = text.includes(MARK_V2);
  if (!is_v2 && !text.includes(MARK_V1)) {
    errors.push('missing the format marker comment near the top of the file');
  }

  const summary_ids = [];
  const seen_summary_ids = new Set();
  const roadmap_set = roadmap_phase_ids ? new Set(roadmap_phase_ids) : null;
  const plan_phase = roadmap_plans ? new Map(roadmap_plans.map(p => [p.id, p.phase])) : null;

  for (const line of summary_lines) {
    if (!line.startsWith('+ [')) continue;
    const match = line.match(is_v2 ? SUMMARY_LINE_V2 : SUMMARY_LINE);
    if (!match) {
      errors.push(`summary line does not match the checkbox+id+phase+title form: '${line}'`);
      continue;
    }
    const [, status, id, phase] = match;
    const plan = is_v2 ? match[4] : undefined;
    if (seen_summary_ids.has(id)) {
      errors.push(`duplicate TASK id in the summary list: '${id}'`);
    }
    seen_summary_ids.add(id);
    summary_ids.push(id);
    if (roadmap_set && !roadmap_set.has(phase)) {
      errors.push(`'${phase}' (on ${id}) is not a phase in ROADMAP.md`);
    }
    if (is_v2 && !plan && (status === ' ' || status === '~')) {
      errors.push(`'${id}' is open but has no plan tag`);
    }
    if (plan && plan !== 'PLAN-XX' && plan_phase) {
      if (!plan_phase.has(plan)) {
        errors.push(`'${plan}' (on ${id}) is not a plan in ROADMAP.md`);
      } else if (plan_phase.get(plan) !== phase) {
        errors.push(`'${plan}' (on ${id}) is under '${plan_phase.get(plan)}', not '${phase}'`);
      }
    }
  }

  const detail_ids = new Set();
  for (const line of lines) {
    const match = line.match(DETAIL_HEADING);
    if (match) detail_ids.add(match[1]);
  }

  for (const id of summary_ids) {
    if (!detail_ids.has(id)) {
      errors.push(`'${id}' is in the summary list but has no matching detail heading`);
    }
  }
  for (const id of detail_ids) {
    if (!seen_summary_ids.has(id)) {
      errors.push(`'${id}' has a detail heading but no matching summary line`);
    }
  }

  return errors;
}
