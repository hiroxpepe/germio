// Pure read/write logic for a germio.json Scenario. Kept separate from
// any browser file-picker API (File System Access, <input type="file">)
// on purpose: this file holds only the part that can be tested with
// plain text in, plain text out — no browser needed at all.

/**
 * Turns raw germio.json text into a plain JS object. Throws a plain
 * Error (with the original JSON.parse message) if the text is not
 * valid JSON at all.
 */
export function parse_scenario(json_text) {
  return JSON.parse(json_text);
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
