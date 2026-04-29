// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Parses and evaluates condition expressions against a <see cref="State"/> instance.
    /// Delegates to <see cref="ExprLexer"/> and <see cref="ExprParser"/> (recursive-descent AST).
    /// G4: == uses relative error abs(a-b) &lt;= 1e-6 * max(|a|, |b|, 1.0).
    /// Supports AND (&amp;&amp;), OR (||), NOT (!), parentheses, and variable-to-variable comparison.
    /// An empty/null condition always evaluates to true. Invalid expressions return false (backward compat).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Evaluator {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Evaluates the given condition expression against the provided state.
        /// </summary>
        /// <param name="condition">Condition string, e.g. "counters.score >= 100".</param>
        /// <param name="state">The current State to evaluate against.</param>
        /// <returns>True if the condition is satisfied or is null/empty; false on parse error.</returns>
        public static bool Evaluate(string? condition, State state) {
            if (string.IsNullOrWhiteSpace(condition)) { return true; }
            try {
                var tokens = ExprLexer.Tokenize(source: condition);
                var ast    = ExprParser.Parse(tokens: tokens);
                return ast.Evaluate(state: state);
            } catch (ExprParseException) {
                // Backward-compatible: unknown or invalid expressions return false.
                return false;
            }
        }
    }
}
