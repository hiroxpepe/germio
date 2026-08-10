// Id-uniqueness checks, matching the same rules the real Validator.cs
// enforces: V004 (Node.id unique across the whole Scenario) and V005
// (Rule.id unique within its own Node). excluding_id lets a rename
// check against every OTHER id without flagging the node's own,
// unchanged id as a false duplicate.

function collect_node_ids(node, ids) {
  ids.push(node.id);
  for (const child of node.children) collect_node_ids(child, ids);
}

/**
 * True if id does not already belong to some other node in the whole
 * tree. Pass excluding_id (the node's own current id, when renaming an
 * existing node) so the node is not flagged as a duplicate of itself.
 */
export function is_node_id_unique(root, id, excluding_id = null) {
  const ids = [];
  collect_node_ids(root, ids);
  return !ids.some(existing_id => existing_id === id && existing_id !== excluding_id);
}

/**
 * True if id does not already belong to some other rule on this SAME
 * node. A rule id on a different node never counts as a duplicate.
 */
export function is_rule_id_unique(node, id, excluding_id = null) {
  return !node.rules.some(rule => rule.id === id && rule.id !== excluding_id);
}
