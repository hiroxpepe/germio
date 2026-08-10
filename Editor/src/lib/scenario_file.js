// Pure read/write logic for a germio.json Scenario. Kept separate from
// any browser file-picker API (File System Access, <input type="file">)
// on purpose: this file holds only the part that can be tested with
// plain text in, plain text out — no browser needed at all.

/**
 * Turns raw germio.json text into a plain JS object. Throws a plain
 * Error (with the original JSON.parse message) if the text is not
 * valid JSON at all.
 *
 * Also fills in an empty array wherever `children`, `rules`, or
 * `next` is entirely missing from a Node: germio's own C# JSON
 * writer skips an empty array/collection field rather than writing
 * it out as `[]`, so a real leaf node in a real germio.json has no
 * "children" key at all — every part of this tool that walks the
 * tree can safely assume the array is always there once parsing is
 * done, instead of every single caller needing its own `|| []` guard.
 */
export function parse_scenario(json_text) {
    const scenario = JSON.parse(json_text);
    normalize_node(scenario.root);
    return scenario;
}

function normalize_node(node) {
    if (node.children === undefined) node.children = [];
    if (node.rules === undefined) node.rules = [];
    if (node.next === undefined) node.next = [];
    for (const child of node.children) normalize_node(child);
}

/**
 * Turns a Scenario object back into germio.json text, keeping the
 * field order germio.json itself always uses at the top level
 * (schema_version, then initial_state, then root).
 */
export function serialize_scenario(scenario) {
    const ordered = {
        schema_version: scenario.schema_version,
        initial_state: scenario.initial_state,
        root: scenario.root,
    };
    return JSON.stringify(ordered, null, 2);
}
