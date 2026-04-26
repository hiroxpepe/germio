// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Germio {
    /// <summary>
    /// Severity level of a validation finding produced by <see cref="Validator"/>.
    /// </summary>
    public enum ValidationLevel { Error, Warning }

    /// <summary>
    /// A single finding (error or warning) produced by <see cref="Validator.Validate"/>.
    /// </summary>
    public class ValidationResult {
        /// <summary>Severity: Error halts integrity; Warning flags potential data issues.</summary>
        public ValidationLevel level { get; }

        /// <summary>Human-readable description of the finding.</summary>
        public string message { get; }

        /// <summary>
        /// Initializes a new ValidationResult with the given severity and message.
        /// </summary>
        public ValidationResult(ValidationLevel level, string message) {
            this.level   = level;
            this.message = message;
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{level}] {message}";
    }

    /// <summary>
    /// Performs static analysis on a <see cref="DataRoot"/> and returns a list of
    /// <see cref="ValidationResult"/> items. An empty list means the data is structurally sound.
    /// <para>Rules enforced:</para>
    /// <list type="bullet">
    ///   <item><description>Error: worlds list is empty.</description></item>
    ///   <item><description>Error: DataNext.id references a level that does not exist in the same world.</description></item>
    ///   <item><description>Error: A condition string in DataNext or DataEvent has invalid syntax.</description></item>
    ///   <item><description>Warning: A condition references a flags or counters key absent from initial DataState.</description></item>
    /// </list>
    /// G1 principle: condition parsing uses only string.Split — no Regex, no LINQ.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Validator {
#nullable enable

        static readonly HashSet<string> VALID_OPS = new() { "==", "!=", ">", "<", ">=", "<=" };

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Validates the given DataRoot and returns all findings.
        /// Returns an empty list when the data is structurally sound.
        /// </summary>
        /// <param name="root">The DataRoot to validate.</param>
        /// <returns>A list of ValidationResult items, possibly empty.</returns>
        public static List<ValidationResult> Validate(DataRoot root) {
            var results = new List<ValidationResult>();

            // Rule 1: worlds must not be null or empty.
            if (root.worlds == null || root.worlds.Count == 0) {
                results.Add(new ValidationResult(ValidationLevel.Error, "worlds list is empty."));
                return results;
            }

            foreach (var world in root.worlds) {
                // Build a set of all level IDs in this world for quick lookup.
                var level_ids = new HashSet<string>();
                foreach (var level in world.levels) { level_ids.Add(level.id); }

                foreach (var level in world.levels) {

                    // Validate all DataNext entries.
                    foreach (var next in level.next) {
                        // Rule 2: next.id must resolve to an existing level in the same world.
                        if (!level_ids.Contains(next.id)) {
                            results.Add(new ValidationResult(ValidationLevel.Error,
                                $"Level '{level.id}' → next.id '{next.id}' does not exist in world '{world.id}'."));
                        }
                        // Rule 3: condition syntax must be valid.
                        if (!isValidConditionSyntax(next.condition, out string cond_detail)) {
                            results.Add(new ValidationResult(ValidationLevel.Error,
                                $"Level '{level.id}' → next['{next.id}'].condition '{next.condition}' is invalid: {cond_detail}"));
                        }
                        // Rule 4: warn on undefined flags/counters keys.
                        checkUndefinedKeyWarning(next.condition, root.state, level.id, "next.condition", results);
                    }

                    // Validate all DataEvent entries.
                    foreach (var evt in level.events) {
                        // Rule 3: event condition syntax must be valid.
                        if (!isValidConditionSyntax(evt.condition, out string cond_detail)) {
                            results.Add(new ValidationResult(ValidationLevel.Error,
                                $"Level '{level.id}' → event['{evt.id}'].condition '{evt.condition}' is invalid: {cond_detail}"));
                        }
                        // Rule 4: warn on undefined flags/counters keys.
                        checkUndefinedKeyWarning(evt.condition, root.state, level.id, $"event['{evt.id}'].condition", results);
                    }
                }
            }

            return results;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        /// <summary>
        /// Returns true if the condition string has valid syntax or is empty/null.
        /// Mirrors the Evaluator tokenize logic (G1: string.Split only).
        /// <para>Valid forms:</para>
        /// <list type="bullet">
        ///   <item><description>Empty / null — unconditional pass, always valid.</description></item>
        ///   <item><description>flags.KEY — implicit == true check.</description></item>
        ///   <item><description>flags.KEY == bool | flags.KEY != bool</description></item>
        ///   <item><description>counters.KEY OP float (operator required)</description></item>
        ///   <item><description>inventory.KEY — implicit > 0 check.</description></item>
        ///   <item><description>inventory.KEY OP int</description></item>
        /// </list>
        /// </summary>
        static bool isValidConditionSyntax(string? condition, out string error_detail) {
            error_detail = string.Empty;
            if (string.IsNullOrWhiteSpace(condition)) { return true; }

            var parts     = condition.Trim().Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            var lhs_parts = parts[0].Split(new char[] { '.' }, 2);

            // LHS must be "prefix.key".
            if (lhs_parts.Length < 2 || string.IsNullOrEmpty(lhs_parts[1])) {
                error_detail = "expected 'prefix.key' format (e.g. flags.door_open)";
                return false;
            }

            string prefix = lhs_parts[0];
            if (prefix != "flags" && prefix != "counters" && prefix != "inventory") {
                error_detail = $"unknown prefix '{prefix}' (must be flags, counters, or inventory)";
                return false;
            }

            // 2 tokens means "prefix.key op" — missing RHS.
            if (parts.Length == 2) {
                error_detail = "missing right-hand side value";
                return false;
            }

            // 1 token: implicit form — valid only for flags and inventory.
            if (parts.Length == 1) {
                if (prefix == "counters") {
                    error_detail = "counters condition requires an operator and value (e.g. counters.score >= 100)";
                    return false;
                }
                return true;
            }

            // 3 tokens: validate operator and RHS type.
            string op  = parts[1];
            string rhs = parts[2];

            if (!VALID_OPS.Contains(op)) {
                error_detail = $"invalid operator '{op}' (must be ==, !=, >, <, >=, <=)";
                return false;
            }

            if (prefix == "flags") {
                if (op != "==" && op != "!=") {
                    error_detail = $"operator '{op}' is not valid for flags (only == and != are supported)";
                    return false;
                }
                if (!bool.TryParse(rhs, out _)) {
                    error_detail = $"invalid boolean value '{rhs}' (must be 'true' or 'false')";
                    return false;
                }
                return true;
            }

            if (prefix == "counters") {
                if (!float.TryParse(rhs, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
                    error_detail = $"invalid numeric value '{rhs}' for counters condition";
                    return false;
                }
                return true;
            }

            if (prefix == "inventory") {
                if (!int.TryParse(rhs, out _)) {
                    error_detail = $"invalid integer value '{rhs}' for inventory condition";
                    return false;
                }
                return true;
            }

            return true;
        }

        /// <summary>
        /// Adds a Warning result if the condition references a flags or counters key
        /// that is not present in the initial DataState dictionaries.
        /// </summary>
        static void checkUndefinedKeyWarning(
            string? condition, DataState state, string levelId, string location,
            List<ValidationResult> results) {

            if (string.IsNullOrWhiteSpace(condition)) { return; }

            var parts     = condition.Trim().Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            var lhs_parts = parts[0].Split(new char[] { '.' }, 2);

            if (lhs_parts.Length < 2 || string.IsNullOrEmpty(lhs_parts[1])) { return; }

            string prefix = lhs_parts[0];
            string key    = lhs_parts[1];

            if (prefix == "flags" && !state.flags.ContainsKey(key)) {
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"Level '{levelId}' → {location}: flag key '{key}' is not defined in initial state.flags."));
            } else if (prefix == "counters" && !state.counters.ContainsKey(key)) {
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"Level '{levelId}' → {location}: counter key '{key}' is not defined in initial state.counters."));
            }
        }
    }
}
