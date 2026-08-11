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

const SUMMARY_LINE = /^\+ \[( |~|x|xx)\] (TASK-\d+) \[(PHASE-\d{2})\]: (.+)$/;
const DETAIL_HEADING = /^### (TASK-\d+)$/;

export function validate_tasklist(text, roadmap_phase_ids = null) {
  const errors = [];
  const lines = text.split('\n');
  const detail_index = lines.findIndex(l => l.trim() === '## Detail');
  const summary_lines = detail_index === -1 ? lines : lines.slice(0, detail_index);

  if (!text.includes('<!-- format: v1 | fields: status, id, title, phase -->')) {
    errors.push('missing the format marker comment near the top of the file');
  }

  const summary_ids = [];
  const seen_summary_ids = new Set();
  const roadmap_set = roadmap_phase_ids ? new Set(roadmap_phase_ids) : null;

  for (const line of summary_lines) {
    if (!line.startsWith('+ [')) continue;
    const match = line.match(SUMMARY_LINE);
    if (!match) {
      errors.push(`summary line does not match the checkbox+id+phase+title form: '${line}'`);
      continue;
    }
    const [, , id, phase] = match;
    if (seen_summary_ids.has(id)) {
      errors.push(`duplicate TASK id in the summary list: '${id}'`);
    }
    seen_summary_ids.add(id);
    summary_ids.push(id);
    if (roadmap_set && !roadmap_set.has(phase)) {
      errors.push(`'${phase}' (on ${id}) is not a phase in ROADMAP.md`);
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
