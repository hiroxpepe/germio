import { describe, test, expect } from 'vitest';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { parse_scenario, serialize_scenario } from '../scenario_file.js';
import { validate_scenario } from '../validator.js';

// A real, unmodified germio.json pulled straight from stemic's own
// game/Assets/StreamingAssets/. Every hand-crafted fixture used
// elsewhere in this test suite spelled out "children": [] on every
// leaf node — the real file never does (germio's own C# JSON writer
// skips an empty array field). Nothing in this tool was checked
// against a real file's actual shape until this one bug was found
// live, in a real browser, with a real error in the console.
const real_json_text = readFileSync(
    join(dirname(fileURLToPath(import.meta.url)), 'real_stemic_germio.json'),
    'utf-8'
);

describe('a real, unmodified germio.json from stemic', () => {
    test('parses without throwing', () => {
        expect(() => parse_scenario(real_json_text)).not.toThrow();
    });

    test('every node, including every leaf, ends up with a real children array', () => {
        const scenario = parse_scenario(real_json_text);
        function check(node) {
            expect(Array.isArray(node.children)).toBe(true);
            node.children.forEach(check);
        }
        check(scenario.root);
    });

    test('validate_scenario runs on it without throwing', () => {
        const scenario = parse_scenario(real_json_text);
        expect(() => validate_scenario(scenario)).not.toThrow();
    });

    test('round-trips through serialize_scenario and parses again cleanly', () => {
        const scenario = parse_scenario(real_json_text);
        const text = serialize_scenario(scenario);
        expect(() => parse_scenario(text)).not.toThrow();
    });
});
