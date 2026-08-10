import { describe, test, expect } from 'vitest';
import { create_undo_stack } from '../undo_stack.js';

describe('create_undo_stack', () => {
    test('cannot undo before any snapshot has been pushed', () => {
        const stack = create_undo_stack();
        expect(stack.can_undo()).toBe(false);
    });

    test('cannot undo with only one snapshot pushed (nothing before it)', () => {
        const stack = create_undo_stack();
        stack.push('{"a":1}');
        expect(stack.can_undo()).toBe(false);
    });

    test('can undo once a second snapshot has been pushed', () => {
        const stack = create_undo_stack();
        stack.push('{"a":1}');
        stack.push('{"a":2}');
        expect(stack.can_undo()).toBe(true);
    });

    test('undo returns the snapshot from just before the current one', () => {
        const stack = create_undo_stack();
        stack.push('{"a":1}');
        stack.push('{"a":2}');
        expect(stack.undo()).toBe('{"a":1}');
    });

    test('undoing twice steps back two changes, in order', () => {
        const stack = create_undo_stack();
        stack.push('{"a":1}');
        stack.push('{"a":2}');
        stack.push('{"a":3}');
        expect(stack.undo()).toBe('{"a":2}');
        expect(stack.undo()).toBe('{"a":1}');
    });

    test('cannot undo past the very first snapshot', () => {
        const stack = create_undo_stack();
        stack.push('{"a":1}');
        stack.push('{"a":2}');
        stack.undo();
        expect(stack.can_undo()).toBe(false);
    });

    test('pushing a new change after an undo drops the redone future', () => {
        const stack = create_undo_stack();
        stack.push('{"a":1}');
        stack.push('{"a":2}');
        stack.push('{"a":3}');
        stack.undo(); // now sitting on "{"a":2}"
        stack.push('{"a":4}'); // a fresh change from here
        expect(stack.undo()).toBe('{"a":2}');
    });
});
