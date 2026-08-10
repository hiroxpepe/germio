import { describe, test, expect } from 'vitest';
import { is_node_id_unique, is_rule_id_unique } from '../validation.js';

function sample_tree() {
    return {
        id: 'title',
        rules: [],
        children: [
            {
                id: 'levels',
                rules: [{ id: 'rule_a' }, { id: 'rule_b' }],
                children: [
                    { id: 'level_1', rules: [{ id: 'rule_c' }], children: [] },
                ],
            },
        ],
    };
}

describe('is_node_id_unique', () => {
    test('a brand new id is unique', () => {
        expect(is_node_id_unique(sample_tree(), 'level_2')).toBe(true);
    });

    test('an id already used anywhere in the tree is not unique', () => {
        expect(is_node_id_unique(sample_tree(), 'level_1')).toBe(false);
    });

    test('a node keeping its own id while being renamed is not a false duplicate', () => {
    // Renaming "level_1" to "level_1" itself must not be flagged.
        expect(is_node_id_unique(sample_tree(), 'level_1', 'level_1')).toBe(true);
    });
});

describe('is_rule_id_unique', () => {
    test('a brand new rule id is unique within its own node', () => {
        const levels = sample_tree().children[0];
        expect(is_rule_id_unique(levels, 'rule_new')).toBe(true);
    });

    test('a rule id already used in the SAME node is not unique', () => {
        const levels = sample_tree().children[0];
        expect(is_rule_id_unique(levels, 'rule_a')).toBe(false);
    });

    test('a rule id used in a DIFFERENT node does not count as a duplicate', () => {
    // "rule_c" lives on level_1, not on levels — reusing it on levels is fine.
        const levels = sample_tree().children[0];
        expect(is_rule_id_unique(levels, 'rule_c')).toBe(true);
    });

    test('a rule keeping its own id while being renamed is not a false duplicate', () => {
        const levels = sample_tree().children[0];
        expect(is_rule_id_unique(levels, 'rule_a', 'rule_a')).toBe(true);
    });
});
