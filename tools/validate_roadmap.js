const SUMMARY_LINE = /^\+ \[([ x~])\] (PHASE-(?:\d{2}|XX)): (.+)$/;
const PLAN_LINE = /^\+ \[([ x~])\] (PLAN-\d{2}) \[(PHASE-(?:\d{2}|XX))\]: (.+)$/;
const MARK_V1 = '<!-- format: v1 | fields: status, phase, title -->';
const MARK_V2 = '<!-- format: v2 | fields: status, phase, title | plans: status, plan, phase, title -->';

export function validate_roadmap(text) {
  const errors = [];
  const lines = text.split('\n');
  const detail_index = lines.findIndex(l => l.trim() === '## Detail');
  const summary_lines = detail_index === -1 ? lines : lines.slice(0, detail_index);
  const is_v2 = text.includes(MARK_V2);

  if (!is_v2 && !text.includes(MARK_V1)) {
    errors.push('missing the format marker comment near the top of the file');
  }

  const phase_ids = [];
  const seen_phase_ids = new Set();
  const phase_lines = [];
  const plans = [];
  const seen_plan_ids = new Set();
  let open_count = 0;

  for (const line of summary_lines) {
    if (!line.startsWith('+ [')) continue;
    const plan_match = is_v2 ? line.match(PLAN_LINE) : null;
    if (plan_match) {
      const [, , id, phase] = plan_match;
      if (seen_plan_ids.has(id)) {
        errors.push(`duplicate PLAN id in the summary list: '${id}'`);
      }
      seen_plan_ids.add(id);
      plans.push({ id, phase });
      continue;
    }
    const match = line.match(SUMMARY_LINE);
    if (!match) {
      errors.push(`summary line does not match the checkbox+phase+title form: '${line}'`);
      continue;
    }
    const [, status, id] = match;
    if (seen_phase_ids.has(id)) {
      errors.push(`duplicate PHASE id in the summary list: '${id}'`);
    }
    seen_phase_ids.add(id);
    phase_ids.push(id);
    phase_lines.push(line);
    if (status === '~') open_count += 1;
  }

  if (phase_ids.length > 0 && open_count === 0 && !phase_ids_all_done(phase_lines)) {
    errors.push('no PHASE is marked "in progress" ([~]), and open PHASEs remain — mark the current one');
  }

  for (const id of phase_ids) {
    const heading = `### ${id}`;
    if (!text.includes(heading)) {
      errors.push(`no '${heading}' detail section found for a PHASE listed in the summary`);
    }
  }

  for (const plan of plans) {
    if (!seen_phase_ids.has(plan.phase)) {
      errors.push(`'${plan.id}' is under '${plan.phase}', a phase the roadmap does not list`);
    }
    const heading = `### ${plan.id}`;
    if (!lines.some(l => l.trim() === heading)) {
      errors.push(`no '${heading}' detail section found for a PLAN listed in the summary`);
    }
  }

  return errors;
}

// Read the plans of a v2 ROADMAP.md, each with the phase it sits under.
export function read_roadmap_plans(roadmap_text) {
  if (!roadmap_text || !roadmap_text.includes(MARK_V2)) return [];
  return [...roadmap_text.matchAll(/^\+ \[[ x~]\] (PLAN-\d{2}) \[(PHASE-(?:\d{2}|XX))\]:/gm)]
    .map(m => ({ id: m[1], phase: m[2] }));
}

function phase_ids_all_done(phase_lines) {
  return phase_lines.every(l => l.startsWith('+ [x]'));
}
