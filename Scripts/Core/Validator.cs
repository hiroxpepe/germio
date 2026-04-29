// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Severity level of a validation finding produced by <see cref="Validator"/>.
    /// </summary>
    public enum ValidationLevel { Error, Warning }

    /// <summary>
    /// Source location within a germio_config JSON document.
    /// json_path is the primary LLM-facing field (MCP / prompt injection).
    /// line and column default to 0 when source-position tracking is not available.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Location {
#nullable enable
        /// <summary>JSONPath expression pointing to the offending JSON node.
        /// Example: "$.worlds[w1].levels[lv1].next[0].condition"</summary>
        public string json_path       { get; set; } = string.Empty;

        /// <summary>1-based source line number. 0 when not available.</summary>
        public int    line            { get; set; } = 0;

        /// <summary>1-based source column number. 0 when not available.</summary>
        public int    column          { get; set; } = 0;

        /// <summary>Short excerpt from the source for human context.</summary>
        public string context_snippet { get; set; } = string.Empty;
    }

    /// <summary>
    /// A single finding produced by <see cref="Validator.Validate"/>.
    /// G12: LLM-friendly format — every finding includes rule_id, severity, cause_detail,
    /// fix_suggestion, and a ToLlmReadable() method for direct LLM prompt injection.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class ValidationResult {
#nullable enable
        /// <summary>Severity: Error halts integrity; Warning flags potential data issues. (G17: single name)</summary>
        public ValidationLevel severity { get; }

        /// <summary>Rule identifier: V001 – V012.</summary>
        public string rule_id { get; }

        /// <summary>Human-readable short description of the finding.</summary>
        public string message { get; }

        /// <summary>Detailed explanation of why this finding was raised.</summary>
        public string cause_detail { get; }

        /// <summary>Actionable suggestion for how to fix the finding.</summary>
        public string fix_suggestion { get; }

        /// <summary>Optional JSON snippet illustrating the fix. May be empty.</summary>
        public string suggested_json { get; }

        /// <summary>Optional JSON source location. json_path is the key field for LLM self-correction.</summary>
        public Location location { get; }

        /// <summary>Initializes a new ValidationResult with all G12 fields.</summary>
        public ValidationResult(
            ValidationLevel level,
            string          rule_id,
            string          message,
            string          cause_detail,
            string          fix_suggestion,
            string          suggested_json = "",
            Location?       location       = null) {
            this.severity       = level;
            this.rule_id        = rule_id;
            this.message        = message;
            this.cause_detail   = cause_detail;
            this.fix_suggestion = fix_suggestion;
            this.suggested_json = suggested_json;
            this.location       = location ?? new Location();
        }

        /// <summary>
        /// Backward-compatible 2-parameter constructor.
        /// Equivalent to ValidationResult(level, "LEGACY", message, "", "").
        /// </summary>
        public ValidationResult(ValidationLevel level, string message) {
            this.severity       = level;
            this.rule_id        = "LEGACY";
            this.message        = message;
            this.cause_detail   = string.Empty;
            this.fix_suggestion = string.Empty;
            this.suggested_json = string.Empty;
            this.location       = new Location();
        }

        /// <summary>
        /// Returns an LLM-friendly multi-line string containing all diagnostic info.
        /// Format: "[RULE_ID][SEVERITY] message\nCause: cause_detail\nFix: fix_suggestion"
        /// </summary>
        public string ToLlmReadable() {
            var sb = new StringBuilder();
            sb.Append($"[{rule_id}][{severity}] {message}");
            if (!string.IsNullOrEmpty(location.json_path)) {
                sb.Append($"\nPath: {location.json_path}");
                if (location.line > 0) {
                    sb.Append($" (line {location.line}, col {location.column})");
                }
            }
            if (!string.IsNullOrEmpty(cause_detail)) {
                sb.Append($"\nCause: {cause_detail}");
            }
            if (!string.IsNullOrEmpty(fix_suggestion)) {
                sb.Append($"\nFix: {fix_suggestion}");
            }
            if (!string.IsNullOrEmpty(suggested_json)) {
                sb.Append($"\nSuggested JSON:\n{suggested_json}");
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{rule_id}][{severity}] {message}";
    }

    /// <summary>
    /// Performs static analysis on a <see cref="Scenario"/> and returns a list of
    /// <see cref="ValidationResult"/> items. An empty list means the data is sound.
    ///
    /// Rules enforced (V001 – V012):
    ///   V001: condition references a flag key absent from initial state.flags (Warning)
    ///   V002: condition references a counter key absent from initial state.counters (Warning)
    ///   V003: condition references an inventory key absent from initial state.inventory (Warning)
    ///   V004: duplicate level.id within a world (Error)
    ///   V005: duplicate rule.id within a level (Error)
    ///   V006: Next.id references a level that does not exist in the same world (Error)
    ///   V007: rule.condition is empty — rule always fires (Warning)
    ///   V008: once=false with set_flag command — infinite-loop risk (Warning)
    ///   V009: condition DSL parse error (Error)
    ///   V010: command has no fields set — rule has no effect (Error)
    ///   V011: level has no rules and no next entries — dead end (Warning)
    ///   V012: circular transition chain detected via DFS (Error)
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Validator {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Validates the given Scenario and returns all findings.
        /// Returns an empty list when the data is structurally sound.
        /// </summary>
        /// <param name="scenario">The Scenario to validate.</param>
        /// <returns>A list of ValidationResult items, possibly empty.</returns>
        public static List<ValidationResult> Validate(Scenario scenario) {
            var results = new List<ValidationResult>();

            // Early-exit rule: worlds must not be null or empty.
            if (scenario.worlds == null || scenario.worlds.Count == 0) {
                results.Add(new ValidationResult(
                    level:          ValidationLevel.Error,
                    rule_id:        "V006",  // covered as "worlds empty" structural error
                    message:        "worlds list is empty.",
                    cause_detail:   "The scenario has no worlds defined, so no levels can be loaded.",
                    fix_suggestion: "Add at least one world with one level to the worlds array."));
                return results;
            }

            foreach (var world in scenario.worlds) {
                // Build level-id → level map for quick lookup.
                var level_map = new Dictionary<string, Level>();
                var level_ids_seen = new HashSet<string>();

                // V004: duplicate level.id
                foreach (var level in world.levels) {
                    if (!level_ids_seen.Add(level.id)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Error,
                            rule_id:        "V004",
                            message:        $"Duplicate level.id '{level.id}' in world '{world.id}'.",
                            cause_detail:   $"Two levels share the same id '{level.id}'. IDs must be unique within a world.",
                            fix_suggestion: $"Rename one of the duplicate levels to a unique id.",
                            location:       new Location { json_path = $"$.worlds[{world.id}].levels" }));
                    } else {
                        level_map[level.id] = level;
                    }
                }

                // V012: circular transition detection via DFS
                detectCycles(world: world, level_map: level_map, results: results);

                foreach (var level in world.levels) {

                    // V011: dead end — no rules and no next
                    if ((level.rules == null || level.rules.Count == 0) &&
                        (level.next  == null || level.next.Count  == 0)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V011",
                            message:        $"Level '{level.id}' in world '{world.id}' has no rules and no next entries (dead end).",
                            cause_detail:   "The player can arrive at this level but nothing will happen and they cannot progress.",
                            fix_suggestion: "Add at least one next entry or a rule with a request_transition command.",
                            location:       new Location { json_path = $"$.worlds[{world.id}].levels[{level.id}]" }));
                    }

                    // Validate Next entries
                    if (level.next != null) {
                        for (int n_idx = 0; n_idx < level.next.Count; n_idx++) {
                            var next = level.next[n_idx];
                            // V006: dangling next.id
                            if (!level_map.ContainsKey(next.id)) {
                                results.Add(new ValidationResult(
                                    level:          ValidationLevel.Error,
                                    rule_id:        "V006",
                                    message:        $"Level '{level.id}' → next.id '{next.id}' does not exist in world '{world.id}'.",
                                    cause_detail:   $"No level with id '{next.id}' was found in world '{world.id}'.{suggestSimilar(next.id, level_map.Keys)}",
                                    fix_suggestion: $"Add a level with id '{next.id}' to world '{world.id}', or correct the typo.",
                                    location:       new Location { json_path = $"$.worlds[{world.id}].levels[{level.id}].next[{n_idx}].id" }));
                            }
                            // V009 / V001 / V002 / V003: validate condition
                            validateCondition(
                                condition: next.condition, state: scenario.state,
                                world_id: world.id, level_id: level.id,
                                json_path: $"$.worlds[{world.id}].levels[{level.id}].next[{n_idx}].condition",
                                results: results);
                        }
                    }

                    // Validate Rule entries
                    if (level.rules != null) {
                        var rule_ids_seen = new HashSet<string>();
                        foreach (var rule in level.rules) {
                            // V005: duplicate rule.id
                            if (!rule_ids_seen.Add(rule.id)) {
                                results.Add(new ValidationResult(
                                    level:          ValidationLevel.Error,
                                    rule_id:        "V005",
                                    message:        $"Duplicate rule.id '{rule.id}' in level '{level.id}'.",
                                    cause_detail:   $"Two rules in level '{level.id}' share id '{rule.id}'. Rule IDs must be unique within a level.",
                                    fix_suggestion: $"Rename one of the duplicate rules to a unique id.",
                                    location:       new Location { json_path = $"$.worlds[{world.id}].levels[{level.id}].rules" }));
                            }

                            // V007: empty condition (always fires)
                            if (string.IsNullOrWhiteSpace(rule.condition)) {
                                results.Add(new ValidationResult(
                                    level:          ValidationLevel.Warning,
                                    rule_id:        "V007",
                                    message:        $"Rule '{rule.id}' in level '{level.id}' has an empty condition — it fires unconditionally.",
                                    cause_detail:   "A rule with no condition fires every time its trigger is received, regardless of state.",
                                    fix_suggestion: "Add a condition if the rule should only fire under specific circumstances.",
                                    location:       new Location { json_path = $"$.worlds[{world.id}].levels[{level.id}].rules[{rule.id}].condition" }));
                            } else {
                                // V009 / V001 / V002 / V003
                                validateCondition(
                                    condition: rule.condition, state: scenario.state,
                                    world_id: world.id, level_id: level.id,
                                    json_path: $"$.worlds[{world.id}].levels[{level.id}].rules[{rule.id}].condition",
                                    results: results);
                            }

                            // V008: once=false with set_flag
                            if (!rule.once && rule.command?.set_flag != null) {
                                results.Add(new ValidationResult(
                                    level:          ValidationLevel.Warning,
                                    rule_id:        "V008",
                                    message:        $"Rule '{rule.id}' in level '{level.id}' has once=false with a set_flag command — infinite-loop risk.",
                                    cause_detail:   "Setting a flag repeatedly without a once guard can cause the rule to fire every tick.",
                                    fix_suggestion: "Set once=true unless you intentionally want the flag set on every trigger.",
                                    location:       new Location { json_path = $"$.worlds[{world.id}].levels[{level.id}].rules[{rule.id}]" }));
                            }

                            // V010: command has no effect
                            if (rule.command == null ||
                                (rule.command.set_flag        == null &&
                                 rule.command.update_counter  == null &&
                                 rule.command.update_inventory == null &&
                                 rule.command.request_transition == null)) {
                                results.Add(new ValidationResult(
                                    level:          ValidationLevel.Error,
                                    rule_id:        "V010",
                                    message:        $"Rule '{rule.id}' in level '{level.id}' has an empty command — it has no effect.",
                                    cause_detail:   "The command object has no fields set (set_flag, update_counter, update_inventory, request_transition are all null).",
                                    fix_suggestion: "Add at least one command field to give the rule an effect.",
                                    location:       new Location { json_path = $"$.worlds[{world.id}].levels[{level.id}].rules[{rule.id}].command" }));
                            }
                        }
                    }
                }
            }

            return results;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        /// <summary>
        /// Validates a condition string: V009 (parse error), V001/V002/V003 (undefined keys).
        /// </summary>
        static void validateCondition(
            string? condition, State state, string world_id, string level_id, string json_path,
            List<ValidationResult> results) {

            if (string.IsNullOrWhiteSpace(condition)) { return; }

            // V009: try to parse with the DSL parser
            try {
                var tokens = ExprLexer.Tokenize(source: condition);
                ExprParser.Parse(tokens: tokens);

                // Semantic validation (after successful parse)
                string? semantic_err = findSemanticError(tokens: tokens);
                if (semantic_err != null) {
                    results.Add(new ValidationResult(
                        level:          ValidationLevel.Error,
                        rule_id:        "V009",
                        message:        $"Level '{level_id}' → {json_path} '{condition}': {semantic_err}",
                        cause_detail:   semantic_err,
                        fix_suggestion: "Use only 'flags', 'counters', or 'inventory' as prefixes. Counters require comparison operators. Flags support == and != only. Inventory values are integers.",
                        location:       new Location { json_path = json_path }));
                    return;
                }
            } catch (ExprParseException ex) {
                results.Add(new ValidationResult(
                    level:          ValidationLevel.Error,
                    rule_id:        "V009",
                    message:        $"Level '{level_id}' → {json_path} '{condition}' has a DSL syntax error: {ex.Message}",
                    cause_detail:   $"The condition could not be parsed. Column: {ex.Column}.",
                    fix_suggestion: "Check the condition DSL syntax. Valid forms: 'flags.KEY', 'counters.KEY >= N', 'inventory.KEY', combined with &&, ||, !.",
                    location:       new Location { json_path = json_path, column = ex.Column }));
                return;  // skip undefined-key checks for invalid DSL
            }

            // V001/V002/V003: check for undefined keys (walk the token stream directly)
            checkUndefinedKeyWarnings(
                condition: condition, state: state,
                world_id: world_id, level_id: level_id, json_path: json_path,
                results: results);
        }

        /// <summary>
        /// Walks the token stream to find all accessor nodes and warn on undefined keys.
        /// </summary>
        static void checkUndefinedKeyWarnings(
            string condition, State state, string world_id, string level_id, string json_path,
            List<ValidationResult> results) {

            List<Token> tokens;
            try { tokens = ExprLexer.Tokenize(source: condition); }
            catch { return; }  // already caught by V009

            for (int i = 0; i + 2 < tokens.Count; i++) {
                if (tokens[i].kind == TokenKind.Identifier &&
                    tokens[i + 1].kind == TokenKind.Dot &&
                    tokens[i + 2].kind == TokenKind.Identifier) {

                    string prefix = tokens[i].value;
                    string key    = tokens[i + 2].value;

                    if (prefix == "flags" && !state.flags.ContainsKey(key)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V001",
                            message:        $"Level '{level_id}' → {json_path}: flag key '{key}' is not defined in initial state.flags.",
                            cause_detail:   $"The condition references flags.{key} but this key is absent from scenario.state.flags.{suggestSimilar(key, state.flags.Keys)}",
                            fix_suggestion: $"Add '\"{ key }\": false' to scenario.state.flags, or fix the key name.",
                            location:       new Location { json_path = json_path }));
                    } else if (prefix == "counters" && !state.counters.ContainsKey(key)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V002",
                            message:        $"Level '{level_id}' → {json_path}: counter key '{key}' is not defined in initial state.counters.",
                            cause_detail:   $"The condition references counters.{key} but this key is absent from scenario.state.counters.{suggestSimilar(key, state.counters.Keys)}",
                            fix_suggestion: $"Add '\"{ key }\": 0' to scenario.state.counters, or fix the key name.",
                            location:       new Location { json_path = json_path }));
                    } else if (prefix == "inventory" && !state.inventory.ContainsKey(key)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V003",
                            message:        $"Level '{level_id}' → {json_path}: inventory key '{key}' is not defined in initial state.inventory.",
                            cause_detail:   $"The condition references inventory.{key} but this key is absent from scenario.state.inventory.{suggestSimilar(key, state.inventory.Keys)}",
                            fix_suggestion: $"Add '\"{ key }\": 0' to scenario.state.inventory, or fix the key name.",
                            location:       new Location { json_path = json_path }));
                    }
                    i += 2;  // skip past the dot and key we just processed
                }
            }
        }

        /// <summary>
        /// Semantic validation: walks the token stream and returns an error message for the
        /// first semantic violation, or null if all checks pass.
        /// Checks: unknown prefix, counters standalone (no comparison op), flags with ordering
        /// operators, inventory with float RHS.
        /// </summary>
        static string? findSemanticError(List<Token> tokens) {
            for (int i = 0; i + 2 < tokens.Count; i++) {
                if (tokens[i].kind   != TokenKind.Identifier) { continue; }
                if (tokens[i+1].kind != TokenKind.Dot)        { continue; }
                if (tokens[i+2].kind != TokenKind.Identifier) { continue; }

                string prefix   = tokens[i].value;
                string key      = tokens[i+2].value;
                int    next_idx = i + 3;
                TokenKind next_kind = (next_idx < tokens.Count) ? tokens[next_idx].kind : TokenKind.EOF;

                // Is this accessor on the RHS of another comparison (token before is a comparison op)?
                bool is_rhs = (i > 0) && isComparisonOpKind(kind: tokens[i-1].kind);

                // Unknown prefix
                if (prefix != "flags" && prefix != "counters" && prefix != "inventory") {
                    return $"Unknown condition prefix '{prefix}'. Valid prefixes are: flags, counters, inventory.";
                }

                // counters.KEY used standalone (no comparison op before or after)
                if (prefix == "counters" && !is_rhs && !isComparisonOpKind(kind: next_kind)) {
                    return $"Counter accessor 'counters.{key}' used without a comparison operator. " +
                           "Counters require an operator such as >=, <=, ==, !=, <, >.";
                }

                // flags.KEY with ordering operators (>, <, >=, <=)
                if (prefix == "flags" && !is_rhs && isComparisonOpKind(kind: next_kind)) {
                    if (next_kind == TokenKind.Gt  || next_kind == TokenKind.Lt ||
                        next_kind == TokenKind.GtEq || next_kind == TokenKind.LtEq) {
                        return $"Flag accessor 'flags.{key}' used with unsupported operator '{tokens[next_idx].value}'. " +
                               "Flags only support == and !=.";
                    }
                }

                // inventory.KEY with a float (non-integer) numeric RHS
                if (prefix == "inventory" && !is_rhs && isComparisonOpKind(kind: next_kind)) {
                    int rhs_idx = next_idx + 1;
                    if (rhs_idx < tokens.Count &&
                        tokens[rhs_idx].kind == TokenKind.Number &&
                        tokens[rhs_idx].value.Contains('.')) {
                        return $"Inventory accessor 'inventory.{key}' compared with float '{tokens[rhs_idx].value}'. " +
                               "Inventory values are integers; use an integer literal.";
                    }
                }

                i += 2;  // skip past DOT and KEY tokens already processed
            }
            return null;
        }

        static bool isComparisonOpKind(TokenKind kind) =>
            kind == TokenKind.EqEq || kind == TokenKind.NotEq ||
            kind == TokenKind.Gt   || kind == TokenKind.Lt    ||
            kind == TokenKind.GtEq || kind == TokenKind.LtEq;

        /// <summary>
        /// V012: DFS cycle detection across Next transitions.
        /// One ValidationResult per detected cycle.
        /// </summary>
        static void detectCycles(
            World world, Dictionary<string, Level> level_map,
            List<ValidationResult> results) {

            var visited  = new HashSet<string>();
            var in_stack = new HashSet<string>();

            foreach (var level in world.levels) {
                if (!visited.Contains(level.id)) {
                    var path = new List<string>();
                    dfsCycles(
                        level_id: level.id, world: world,
                        level_map: level_map, visited: visited,
                        in_stack: in_stack, path: path,
                        results: results);
                }
            }
        }

        static void dfsCycles(
            string level_id, World world, Dictionary<string, Level> level_map,
            HashSet<string> visited, HashSet<string> in_stack,
            List<string> path, List<ValidationResult> results) {

            visited.Add(level_id);
            in_stack.Add(level_id);
            path.Add(level_id);

            if (level_map.TryGetValue(level_id, out var level) && level.next != null) {
                foreach (var next in level.next) {
                    if (!level_map.ContainsKey(next.id)) { continue; }  // dangling — V006 handles it

                    if (!visited.Contains(next.id)) {
                        dfsCycles(
                            level_id: next.id, world: world,
                            level_map: level_map, visited: visited,
                            in_stack: in_stack, path: path,
                            results: results);
                    } else if (in_stack.Contains(next.id)) {
                        // Found a cycle
                        int cycle_start = path.IndexOf(next.id);
                        var cycle_path  = path.GetRange(cycle_start, path.Count - cycle_start);
                        cycle_path.Add(next.id);
                        string path_str = string.Join(" → ", cycle_path);
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Error,
                            rule_id:        "V012",
                            message:        $"Circular transition detected in world '{world.id}': {path_str}.",
                            cause_detail:   $"The transition chain forms a cycle: {path_str}. This will cause an infinite loop.",
                            fix_suggestion: "Break the cycle by removing one of the transition entries or adding a condition that stops firing.",
                            location:       new Location { json_path = $"$.worlds[{world.id}].levels[{next.id}].next" }));
                    }
                }
            }

            in_stack.Remove(level_id);
            path.RemoveAt(path.Count - 1);
        }

        /// <summary>
        /// Returns a hint string if a similar key exists (substring or prefix match).
        /// Returns empty string if no suggestion found.
        /// </summary>
        static string suggestSimilar(string target, IEnumerable<string> candidates) {
            string target_lower = target.ToLowerInvariant();
            foreach (var c in candidates) {
                string c_lower = c.ToLowerInvariant();
                if (c_lower.Contains(target_lower) || target_lower.Contains(c_lower)) {
                    return $" Did you mean '{c}'?";
                }
            }
            return string.Empty;
        }
    }
}
