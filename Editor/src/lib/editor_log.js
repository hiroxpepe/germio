// A plain, in-memory operation log for the editor, matching germio's
// own GermioLog.Write: every real user action (open, save, undo, a
// selection, an edit) writes one line here, so the whole session can
// be copied out and handed over for debugging, the same way tonight's
// germio.log files were.

let lines = [];

function timestamp() {
  const now = new Date();
  const pad = (n, len = 2) => String(n).padStart(len, '0');
  return `${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}.${pad(now.getMilliseconds(), 3)}`;
}

export function write(message) {
  lines.push(`[${timestamp()}] ${message}`);
}

export function get_all_text() {
  return lines.length === 0 ? '' : lines.join('\n') + '\n';
}

export function clear() {
  lines = [];
}
