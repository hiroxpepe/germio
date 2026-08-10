import { describe, test, expect } from 'vitest';
import { escape_html, escape_attr } from '../escape.js';

describe('escape_html', () => {
  test('escapes an angle bracket so it cannot open a new tag', () => {
    expect(escape_html('<script>')).not.toContain('<');
  });

  test('leaves a plain, harmless string untouched in meaning', () => {
    expect(escape_html('Level 1')).toBe('Level 1');
  });
});

describe('escape_attr', () => {
  test('escapes a double quote so it cannot break out of value="..."', () => {
    const escaped = escape_attr('" onfocus="alert(1)" autofocus="');
    expect(escaped).not.toContain('"');
  });

  test('leaves a plain, harmless string untouched in meaning', () => {
    expect(escape_attr('level_1')).toBe('level_1');
  });
});
