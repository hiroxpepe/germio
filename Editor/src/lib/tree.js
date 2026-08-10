// A small, pure set of functions for germio.json's real Node tree shape:
// a nested `children: []` array, the same shape Store.FindNode walks in
// the real C# Validator.cs and Store.cs. No parent pointer is kept
// anywhere — a parent is only ever found by walking down from the root.

/**
 * Finds a node by id, walking down from the given root. Returns the
 * node itself, or null if no node in the tree has that id.
 */
export function find_node(root, id) {
    if (root.id === id) return root;
    for (const child of root.children) {
        const found = find_node(child, id);
        if (found !== null) return found;
    }
    return null;
}

/**
 * True if candidate_id names a node somewhere below ancestor_id (a
 * child, a grandchild, and so on). A node is never its own descendant.
 */
export function is_descendant(root, ancestor_id, candidate_id) {
    const ancestor = find_node(root, ancestor_id);
    if (ancestor === null) return false;
    for (const child of ancestor.children) {
        if (child.id === candidate_id) return true;
        if (is_descendant(root, child.id, candidate_id)) return true;
    }
    return false;
}

/**
 * Removes the node whose id is node_id from wherever it sits today,
 * and adds it as a new child of the node whose id is new_parent_id.
 * Throws if this move would put a node inside its own descendant (a
 * loop), matching what the real V026 rule checks for in Validator.cs.
 * Returns a brand new tree; the one passed in is never changed.
 */
export function move_node(root, node_id, new_parent_id) {
    if (is_descendant(root, node_id, new_parent_id) || node_id === new_parent_id) {
        throw new Error(
            `moving '${node_id}' under '${new_parent_id}' would create a loop`
        );
    }
    const moving_node = find_node(root, node_id);

    function remove_from(node) {
        return {
            ...node,
            children: node.children
                .filter(c => c.id !== node_id)
                .map(remove_from),
        };
    }

    function add_to(node) {
        if (node.id === new_parent_id) {
            return { ...node, children: [...node.children, moving_node] };
        }
        return { ...node, children: node.children.map(add_to) };
    }

    return add_to(remove_from(root));
}
