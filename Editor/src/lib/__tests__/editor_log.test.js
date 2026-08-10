import { describe, test, expect, beforeEach } from 'vitest';
import { write, get_all_text, clear } from '../editor_log.js';

describe('editor_log', () => {
    beforeEach(() => { clear(); });

    test('starts empty', () => {
        expect(get_all_text()).toBe('');
    });

    test('a written line appears in the full text', () => {
        write('opened germio.json');
        expect(get_all_text()).toContain('opened germio.json');
    });

    test('each written line carries its own timestamp', () => {
        write('a message');
        // A plain HH:MM:SS.mmm-shaped stamp at the start of the line.
        expect(get_all_text()).toMatch(/^\[\d{2}:\d{2}:\d{2}\.\d{3}\] a message/);
    });

    test('more than one line keeps them all, in order', () => {
        write('first');
        write('second');
        const lines = get_all_text().trim().split('\n');
        expect(lines).toHaveLength(2);
        expect(lines[0]).toContain('first');
        expect(lines[1]).toContain('second');
    });

    test('clear empties the log again', () => {
        write('something');
        clear();
        expect(get_all_text()).toBe('');
    });
});
