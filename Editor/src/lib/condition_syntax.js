// A light, best-effort sanity check for a condition string, standing
// in for the real V009 (a true DSL parse error) without a real
// parser behind it — porting ExprLexer/ExprParser stays its own,
// separate, much bigger task (see validator.js's own file comment).
// Returns a short message describing the problem, or null if nothing
// obviously wrong was found.

const KNOWN_PREFIXES = ['flags', 'counters', 'inventory', 'history', 'now'];
const BARE_PREFIX = /\b([A-Za-z_][A-Za-z0-9_]*)\.\s*(?![A-Za-z_])/g;

export function check_condition_syntax(condition) {
    if (!condition || condition.trim() === '') return null;

    const open_count = (condition.match(/\(/g) || []).length;
    const close_count = (condition.match(/\)/g) || []).length;
    if (open_count !== close_count) {
        return 'Unbalanced parentheses.';
    }

    for (const match of condition.matchAll(BARE_PREFIX)) {
        return `'${match[1]}.' has no key after the dot.`;
    }

    const words = condition.match(/\b[A-Za-z_][A-Za-z0-9_]*(?=\.)/g) || [];
    for (const word of words) {
        if (!KNOWN_PREFIXES.includes(word)) {
            return `Unknown prefix '${word}'. Valid prefixes are: flags, counters, inventory, history, now.`;
        }
    }

    return null;
}
