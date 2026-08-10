// Builds the one-line summary text shown on a rule card in the tree
// view: the trigger, every command field that is actually set, and a
// once/repeats badge — so a person can tell what a rule does without
// opening its full editor.

const COMMAND_FIELDS = [
  'set_flag', 'update_counter', 'update_inventory',
  'request_transition', 'request_notify', 'set_persistence', 'record_event',
];

function command_parts(command) {
  const parts = [];
  for (const field of COMMAND_FIELDS) {
    if (command[field] !== undefined && command[field] !== null) {
      const value = command[field];
      const shown = typeof value === 'object' ? JSON.stringify(value) : value;
      parts.push(`${field}: ${shown}`);
    }
  }
  return parts;
}

export function format_rule_summary(rule) {
  const parts = command_parts(rule.command);
  const command_text = parts.length === 0 ? '(no command)' : parts.join(', ');
  const badge = rule.once ? 'once' : 'repeats';
  return `${rule.trigger} \u2192 ${command_text} \u00b7 ${badge}`;
}
