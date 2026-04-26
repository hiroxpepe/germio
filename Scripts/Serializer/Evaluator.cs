// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Globalization;

namespace Germio {
    /// <summary>
    /// Parses and evaluates condition expressions against a DataState instance.
    /// G1 principle: uses only string.Split — no Regex, no LINQ, no heavy libraries.
    /// Supports:
    ///   flags.KEY              (implicit == true)
    ///   flags.KEY OP bool
    ///   counters.KEY OP float
    ///   inventory.KEY          (implicit > 0)
    ///   inventory.KEY OP int
    /// An empty or null condition always evaluates to true (unconditional pass).
    /// Unknown prefix safely returns false without throwing.
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
        /// <param name="state">The current DataState to evaluate against.</param>
        /// <returns>True if the condition is satisfied or is null/empty; false otherwise.</returns>
        public static bool Evaluate(string? condition, DataState state) {
            if (string.IsNullOrWhiteSpace(condition)) { return true; }

            var tokens = tokenize(condition.Trim());

            return tokens.prefix switch {
                "flags"     => evaluateFlag(tokens, state),
                "counters"  => evaluateCounter(tokens, state),
                "inventory" => evaluateInventory(tokens, state),
                _           => false
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        /// <summary>
        /// Splits the condition string into (prefix, key, op, rhs).
        /// G1: string.Split only. No Regex.
        /// </summary>
        static (string prefix, string key, string op, string rhs) tokenize(string condition) {
            // "counters.score >= 100" -> ["counters.score", ">=", "100"]
            var parts    = condition.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            var lhs_parts = parts[0].Split(new char[] { '.' }, 2);

            string prefix = lhs_parts[0];
            string key    = lhs_parts.Length > 1 ? lhs_parts[1] : string.Empty;
            string op     = parts.Length > 1 ? parts[1] : string.Empty;
            string rhs    = parts.Length > 2 ? parts[2] : string.Empty;

            return (prefix, key, op, rhs);
        }

        /// <summary>
        /// Evaluates a flags.KEY expression.
        /// Implicit (no op): true if flag is set and true.
        /// Explicit: supports == and !=.
        /// </summary>
        static bool evaluateFlag(
            (string prefix, string key, string op, string rhs) t, DataState state) {
            bool actual = state.flags.TryGetValue(t.key, out bool v) && v;

            if (string.IsNullOrEmpty(t.op)) { return actual; }

            // bool.TryParse is already case-insensitive ("true"/"false"/"True"/"False")
            bool target = bool.TryParse(t.rhs, out bool parsed) ? parsed : true;

            return t.op switch {
                "==" => actual == target,
                "!=" => actual != target,
                _    => false
            };
        }

        /// <summary>
        /// Evaluates a counters.KEY OP float expression.
        /// Missing key is treated as 0f.
        /// Uses InvariantCulture to avoid locale-dependent decimal separators.
        /// </summary>
        static bool evaluateCounter(
            (string prefix, string key, string op, string rhs) t, DataState state) {
            float actual = state.counters.TryGetValue(t.key, out float v) ? v : 0f;

            if (!float.TryParse(t.rhs, NumberStyles.Float, CultureInfo.InvariantCulture,
                out float target)) { return false; }

            return t.op switch {
                "==" => MathF.Abs(actual - target) < 0.0001f,
                "!=" => MathF.Abs(actual - target) >= 0.0001f,
                ">"  => actual > target,
                "<"  => actual < target,
                ">=" => actual >= target,
                "<=" => actual <= target,
                _    => false
            };
        }

        /// <summary>
        /// Evaluates an inventory.KEY [OP int] expression.
        /// Implicit (no op): true if quantity > 0.
        /// Missing key is treated as 0.
        /// </summary>
        static bool evaluateInventory(
            (string prefix, string key, string op, string rhs) t, DataState state) {
            int actual = state.inventory.TryGetValue(t.key, out int v) ? v : 0;

            if (string.IsNullOrEmpty(t.op)) { return actual > 0; }

            if (!int.TryParse(t.rhs, out int target)) { return false; }

            return t.op switch {
                "==" => actual == target,
                "!=" => actual != target,
                ">"  => actual > target,
                "<"  => actual < target,
                ">=" => actual >= target,
                "<=" => actual <= target,
                _    => false
            };
        }
    }
}
