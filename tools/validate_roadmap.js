const SUMMARY_LINE = /^\+ \[([ x~])\] (PHASE-\d{2}): (.+)$/;

export function validate_roadmap(text) {
  const errors = [];
  const lines = text.split('\n');
  const detail_index = lines.findIndex(l => l.trim() === '## Detail');
  const summary_lines = detail_index === -1 ? lines : lines.slice(0, detail_index);

  if (!text.includes('<!-- format: v1 | fields: status, phase, title -->')) {
    errors.push('missing the format marker comment near the top of the file');
  }

  const phase_ids = [];
  const seen_phase_ids = new Set();
  let open_count = 0;

  for (const line of summary_lines) {
    if (!line.startsWith('+ [')) continue;
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
    if (status === '~') open_count += 1;
  }

  if (phase_ids.length > 0 && open_count === 0 && !phase_ids_all_done(summary_lines)) {
    errors.push('no PHASE is marked "in progress" ([~]), and open PHASEs remain — mark the current one');
  }

  for (const id of phase_ids) {
    const heading = `### ${id}`;
    if (!text.includes(heading)) {
      errors.push(`no '${heading}' detail section found for a PHASE listed in the summary`);
    }
  }

  return errors;
}

function phase_ids_all_done(summary_lines) {
  return summary_lines
    .filter(l => l.startsWith('+ ['))
    .every(l => l.startsWith('+ [x]'));
}
