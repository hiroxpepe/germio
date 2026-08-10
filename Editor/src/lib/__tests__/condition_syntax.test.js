import { describe, test, expect } from 'vitest';
import { check_condition_syntax } from '../condition_syntax.js';

describe('check_condition_syntax', () => {
  // Not a real DSL parse (ExprLexer/ExprParser stay out of scope, see
  // validator.js's own file comment). This is a light, best-effort
  // sanity check: unbalanced parens, an accessor with no key, and an
  // unknown prefix — the same class of mistake the real V009 catches,
  // just without a true parser behind it.
  test('an empty condition is fine (V007 covers that case elsewhere)', () => {
    expect(check_condition_syntax('')).toBeNull();
  });

  test('a plain, well-formed condition passes', () => {
    expect(check_condition_syntax('flags.is_beat && counters.score >= 10')).toBeNull();
  });

  test('flags an unbalanced open parenthesis', () => {
    expect(check_condition_syntax('(flags.is_beat')).not.toBeNull();
  });

  test('flags an unbalanced close parenthesis', () => {
    expect(check_condition_syntax('flags.is_beat)')).not.toBeNull();
  });

  test('flags an accessor with no key after the dot', () => {
    expect(check_condition_syntax('flags.')).not.toBeNull();
  });

  test('flags an unknown prefix', () => {
    expect(check_condition_syntax('scores.is_beat')).not.toBeNull();
  });

  test('does not flag the history and now prefixes, which are real', () => {
    expect(check_condition_syntax('history.total_play_time() > 60')).toBeNull();
    expect(check_condition_syntax('now() > 100')).toBeNull();
  });
});
