// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Severity level of a validation finding produced by <see cref="Validator"/>.
    /// </summary>
    public enum ValidationLevel { Error, Warning }

    /// <summary>
    /// Source location within a germio JSON document.
    /// json_path is the primary LLM-facing field (MCP / prompt injection).
    /// line and column default to 0 when source-position tracking is not available.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Location {
#nullable enable
        /// <summary>JSONPath expression pointing to the offending JSON node.
        /// Example: "$.root.children[0].next[0].condition"</summary>
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

        /// <summary>Rule identifier: V001 – V026.</summary>
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
    /// Rules enforced (V001 – V026):
    ///   V001: condition references a flag key absent from initial state.flags (Warning)
    ///   V002: condition references a counter key absent from initial state.counters (Warning)
    ///   V003: condition references an inventory key absent from initial state.inventory (Warning)
    ///   V004: Node.id must be globally unique in Scenario (Error)
    ///   V005: duplicate rule.id within a node (Error)
    ///   V006: Next.id references a node that does not exist in the Scenario (Error)
    ///   V007: rule.condition is empty — rule always fires (Warning)
    ///   V008: once=false with set_flag command — infinite-loop risk (Warning)
    ///   V009: condition DSL parse error (Error)
    ///   V010: command has no fields set — rule has no effect (Error)
    ///   V011: node has no rules and no next entries — dead end (Warning)
    ///   V012: circular transition chain detected via DFS (Error)
    ///   V020: Scenario全体で Node.scene がユニーク (空文字列を除く) (Error)
    ///   V021: 葉ノード (children 空) は scene 必須 (Error)
    ///   V023: 完全に空のノード (children も scene も空) は禁止 (Error)
    ///   V024: ノード階層が MAX_NODE_DEPTH を超過 (Error)
    ///   V025: ノード階層が warning_node_depth を超過 (Warning)
    ///   V026: 循環参照 (children に祖先 ID を含む) (Error)
    /// 
    /// Phase 5.8 v2 fix6 changes:
    ///   - V010 now also recognises reset_flags / reset_counters / reset_inventory
    ///     as valid command effects.
    ///   - The reserved trigger '_on_enter_node' is auto-fired by SceneLoader on
    ///     every transition (no Validator change required; trigger ids are free-form).
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

            // Early-exit rule: root must not be null.
            if (scenario.root == null) {
                results.Add(new ValidationResult(
                    level:          ValidationLevel.Error,
                    rule_id:        "V000",
                    message:        "root node is null.",
                    cause_detail:   "The scenario has no root node defined.",
                    fix_suggestion: "Add a root node to the scenario."));
                return results;
            }

            // Collect all nodes for uniqueness checks and circular reference detection.
            var all_nodes = new List<Node>();
            collectNodesRecursive(node: scenario.root, nodes: all_nodes);

            // V004: Node.id must be globally unique
            var node_ids_seen = new HashSet<string>();
            var node_map = new Map<string, Node>();
            foreach (var node in all_nodes) {
                if (!node_ids_seen.Add(node.id)) {
                    results.Add(new ValidationResult(
                        level:          ValidationLevel.Error,
                        rule_id:        "V004",
                        message:        $"Duplicate node.id '{node.id}' in scenario.",
                        cause_detail:   $"Two nodes share the same id '{node.id}'. Node IDs must be globally unique within the scenario.",
                        fix_suggestion: $"Rename one of the duplicate nodes to a unique id.",
                        location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')]" }));
                } else {
                    node_map[node.id] = node;
                }
            }

            // V020: Node.scene must be unique (excluding empty strings).
            var scene_to_node = new Map<string, Node>();
            foreach (var node in all_nodes) {
                if (!string.IsNullOrEmpty(node.scene)) {
                    if (scene_to_node.TryGetValue(node.scene, out var other)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Error,
                            rule_id:        "V020",
                            message:        $"Duplicate node.scene '{node.scene}' (node ids: '{other.id}' and '{node.id}').",
                            cause_detail:   $"Two nodes reference the same scene '{node.scene}'. Scene names must be unique.",
                            fix_suggestion: $"Rename one of the nodes' scene or update the node id.",
                            location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].scene" }));
                    } else {
                        scene_to_node[node.scene] = node;
                    }
                }
            }

            // V021 & V023: Leaf nodes must have a scene, and no node should be completely empty.
            foreach (var node in all_nodes) {
                bool is_leaf = node.children == null || node.children.Count == 0;
                bool has_scene = !string.IsNullOrEmpty(node.scene);

                if (is_leaf && !has_scene) {
                    results.Add(new ValidationResult(
                        level:          ValidationLevel.Error,
                        rule_id:        "V021",
                        message:        $"Leaf node '{node.id}' has no scene defined.",
                        cause_detail:   "A leaf node (no children) must correspond to a Unity scene.",
                        fix_suggestion: $"Add a scene name to node '{node.id}'.",
                        location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].scene" }));
                }
            }

            // V024 & V025: Check node depth constraints.
            checkNodeDepthConstraints(node: scenario.root, depth: 0, results: results);

            // V026: Circular reference detection (children referencing ancestors).
            checkCircularReferences(node: scenario.root, ancestors: new List<string>(), results: results);

            // V012: Circular transition chain detection (via next[]).
            checkCircularTransitions(
                node_map: node_map,
                results: results);

            // Traverse tree and validate each node.
            validateNodeRecursive(
                node: scenario.root,
                node_map: node_map,
                state: scenario.initial_state,
                results: results);

            return results;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        /// <summary>
        /// Recursively collects all nodes from the scenario tree.
        /// </summary>
        static void collectNodesRecursive(Node node, List<Node> nodes) {
            nodes.Add(item: node);
            if (node.children != null) {
                foreach (var child in node.children) {
                    collectNodesRecursive(node: child, nodes: nodes);
                }
            }
        }

        /// <summary>
        /// Recursively validates a node and all its descendants.
        /// </summary>
        static void validateNodeRecursive(
            Node node, Map<string, Node> node_map, State state,
            List<ValidationResult> results) {

            // V011: dead end — no rules and no next
            if ((node.rules == null || node.rules.Count == 0) &&
                (node.next == null || node.next.Count == 0)) {
                if (node.children == null || node.children.Count == 0) {
                    // This is a leaf with no rules and no transitions
                    results.Add(new ValidationResult(
                        level:          ValidationLevel.Warning,
                        rule_id:        "V011",
                        message:        $"Node '{node.id}' is a dead end (no rules, no next transitions, no children).",
                        cause_detail:   "The player can arrive at this node but nothing will happen and they cannot progress.",
                        fix_suggestion: "Add at least one next entry or a rule with a request_transition command.",
                        location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')]" }));
                }
            }

            // Validate Next entries
            if (node.next != null) {
                for (int n_idx = 0; n_idx < node.next.Count; n_idx++) {
                    var next = node.next[n_idx];
                    // V006: dangling next.id
                    if (!node_map.ContainsKey(next.id)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Error,
                            rule_id:        "V006",
                            message:        $"Node '{node.id}' → next.id '{next.id}' does not exist in scenario.",
                            cause_detail:   $"No node with id '{next.id}' was found.{suggestSimilar(next.id, node_map.Keys)}",
                            fix_suggestion: $"Add a node with id '{next.id}', or correct the typo.",
                            location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].next[{n_idx}].id" }));
                    }
                    // V009 / V001 / V002 / V003: validate condition
                    validateCondition(
                        condition: next.condition, state: state,
                        node_id: node.id,
                        json_path: $"$.root..[?(@.id='{node.id}')].next[{n_idx}].condition",
                        results: results);
                }
            }

            // Validate Rule entries
            if (node.rules != null) {
                var rule_ids_seen = new HashSet<string>();
                foreach (var rule in node.rules) {
                    // V005: duplicate rule.id
                    if (!rule_ids_seen.Add(rule.id)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Error,
                            rule_id:        "V005",
                            message:        $"Duplicate rule.id '{rule.id}' in node '{node.id}'.",
                            cause_detail:   $"Two rules in node '{node.id}' share id '{rule.id}'. Rule IDs must be unique within a node.",
                            fix_suggestion: $"Rename one of the duplicate rules to a unique id.",
                            location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].rules" }));
                    }

                    // V007: empty condition (always fires)
                    if (string.IsNullOrWhiteSpace(rule.condition)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V007",
                            message:        $"Rule '{rule.id}' in node '{node.id}' has an empty condition — it fires unconditionally.",
                            cause_detail:   "A rule with no condition fires every time its trigger is received, regardless of state.",
                            fix_suggestion: "Add a condition if the rule should only fire under specific circumstances.",
                            location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].rules[{rule.id}].condition" }));
                    } else {
                        // V009 / V001 / V002 / V003
                        validateCondition(
                            condition: rule.condition, state: state,
                            node_id: node.id,
                            json_path: $"$.root..[?(@.id='{node.id}')].rules[{rule.id}].condition",
                            results: results);
                    }

                    // V008: once=false with set_flag
                    if (!rule.once && rule.command?.set_flag != null) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V008",
                            message:        $"Rule '{rule.id}' in node '{node.id}' has once=false with a set_flag command — infinite-loop risk.",
                            cause_detail:   "Setting a flag repeatedly without a once guard can cause the rule to fire every tick.",
                            fix_suggestion: "Set once=true unless you intentionally want the flag set on every trigger.",
                            location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].rules[{rule.id}]" }));
                    }

                    // V010: command has no effect
                    if (rule.command == null ||
                        (rule.command.set_flag        == null &&
                         rule.command.update_counter  == null &&
                         rule.command.update_inventory == null &&
                         rule.command.request_transition == null &&
                         rule.command.set_persistence == null &&
                         rule.command.record_event == null &&
                         !rule.command.reset_flags &&
                         !rule.command.reset_counters &&
                         !rule.command.reset_inventory)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Error,
                            rule_id:        "V010",
                            message:        $"Rule '{rule.id}' in node '{node.id}' has an empty command — it has no effect.",
                            cause_detail:   "The command object has no fields set.",
                            fix_suggestion: "Add at least one command field to give the rule an effect.",
                            location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].rules[{rule.id}].command" }));
                    }
                }
            }

            // Recursively validate child nodes
            if (node.children != null) {
                foreach (var child in node.children) {
                    validateNodeRecursive(
                        node: child,
                        node_map: node_map,
                        state: state,
                        results: results);
                }
            }
        }

        /// <summary>
        /// Validates a condition string: V009 (parse error), V001/V002/V003 (undefined keys).
        /// </summary>
        static void validateCondition(
            string? condition, State state, string node_id, string json_path,
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
                        message:        $"Node '{node_id}' → {json_path} '{condition}': {semantic_err}",
                        cause_detail:   semantic_err,
                        fix_suggestion: "Use only 'flags', 'counters', or 'inventory' as prefixes. Counters require comparison operators. Flags support == and != only. Inventory values are integers.",
                        location:       new Location { json_path = json_path }));
                    return;
                }
            } catch (ExprParseException ex) {
                results.Add(new ValidationResult(
                    level:          ValidationLevel.Error,
                    rule_id:        "V009",
                    message:        $"Node '{node_id}' → {json_path} '{condition}' has a DSL syntax error: {ex.Message}",
                    cause_detail:   $"The condition could not be parsed. Column: {ex.Column}.",
                    fix_suggestion: "Check the condition DSL syntax. Valid forms: 'flags.KEY', 'counters.KEY >= N', 'inventory.KEY', combined with &&, ||, !.",
                    location:       new Location { json_path = json_path, column = ex.Column }));
                return;  // skip undefined-key checks for invalid DSL
            }

            // V001/V002/V003: check for undefined keys (walk the token stream directly)
            checkUndefinedKeyWarnings(
                condition: condition, state: state,
                node_id: node_id, json_path: json_path,
                results: results);
        }

        /// <summary>
        /// Walks the token stream to find all accessor nodes and warn on undefined keys.
        /// </summary>
        static void checkUndefinedKeyWarnings(
            string condition, State state, string node_id, string json_path,
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
                            message:        $"Node '{node_id}' → {json_path}: flag key '{key}' is not defined in initial state.flags.",
                            cause_detail:   $"The condition references flags.{key} but this key is absent from scenario.initial_state.flags.{suggestSimilar(key, state.flags.Keys)}",
                            fix_suggestion: $"Add '\"{ key }\": false' to scenario.initial_state.flags, or fix the key name.",
                            location:       new Location { json_path = json_path }));
                    } else if (prefix == "counters" && !state.counters.ContainsKey(key)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V002",
                            message:        $"Node '{node_id}' → {json_path}: counter key '{key}' is not defined in initial state.counters.",
                            cause_detail:   $"The condition references counters.{key} but this key is absent from scenario.initial_state.counters.{suggestSimilar(key, state.counters.Keys)}",
                            fix_suggestion: $"Add '\"{ key }\": 0' to scenario.initial_state.counters, or fix the key name.",
                            location:       new Location { json_path = json_path }));
                    } else if (prefix == "inventory" && !state.inventory.ContainsKey(key)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Warning,
                            rule_id:        "V003",
                            message:        $"Node '{node_id}' → {json_path}: inventory key '{key}' is not defined in initial state.inventory.",
                            cause_detail:   $"The condition references inventory.{key} but this key is absent from scenario.initial_state.inventory.{suggestSimilar(key, state.inventory.Keys)}",
                            fix_suggestion: $"Add '\"{ key }\": 0' to scenario.initial_state.inventory, or fix the key name.",
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
                if (prefix != "flags" && prefix != "counters" && prefix != "inventory" && prefix != "history" && prefix != "now") {
                    return $"Unknown condition prefix '{prefix}'. Valid prefixes are: flags, counters, inventory, history, now.";
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
        /// V024 & V025: Check node depth constraints.
        /// </summary>
        static void checkNodeDepthConstraints(
            Node node, int depth, List<ValidationResult> results) {

            if (depth > Env.MAX_NODE_DEPTH) {
                results.Add(new ValidationResult(
                    level:          ValidationLevel.Error,
                    rule_id:        "V024",
                    message:        $"Node '{node.id}' at depth {depth} exceeds MAX_NODE_DEPTH ({Env.MAX_NODE_DEPTH}).",
                    cause_detail:   $"The node tree is deeper than {Env.MAX_NODE_DEPTH} levels.",
                    fix_suggestion: "Restructure the node hierarchy to reduce depth, or increase MAX_NODE_DEPTH if appropriate.",
                    location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')]" }));
            } else if (depth > Env.warning_node_depth) {
                results.Add(new ValidationResult(
                    level:          ValidationLevel.Warning,
                    rule_id:        "V025",
                    message:        $"Node '{node.id}' at depth {depth} exceeds warning_node_depth ({Env.warning_node_depth}).",
                    cause_detail:   $"The node tree reaches {depth} levels, approaching the soft limit of {Env.warning_node_depth}.",
                    fix_suggestion: "Consider restructuring for better performance, or increase warning_node_depth if intentional.",
                    location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')]" }));
            }

            if (node.children != null) {
                foreach (var child in node.children) {
                    checkNodeDepthConstraints(node: child, depth: depth + 1, results: results);
                }
            }
        }

        /// <summary>
        /// V026: Circular reference detection (children referencing ancestors).
        /// </summary>
        static void checkCircularReferences(
            Node node, List<string> ancestors, List<ValidationResult> results) {

            // Check if any child id matches an ancestor id
            if (node.children != null) {
                foreach (var child in node.children) {
                    if (ancestors.Contains(child.id)) {
                        results.Add(new ValidationResult(
                            level:          ValidationLevel.Error,
                            rule_id:        "V026",
                            message:        $"Circular reference detected: node '{child.id}' references ancestor.",
                            cause_detail:   $"Node '{child.id}' appears in the children of node '{node.id}', but '{child.id}' is an ancestor of '{node.id}'.",
                            fix_suggestion: $"Remove the child node '{child.id}' from node '{node.id}', or restructure the hierarchy.",
                            location:       new Location { json_path = $"$.root..[?(@.id='{node.id}')].children" }));
                    }
                }
            }

            // Traverse deeper
            if (node.children != null) {
                foreach (var child in node.children) {
                    var new_ancestors = new List<string>(ancestors);
                    new_ancestors.Add(node.id);
                    checkCircularReferences(node: child, ancestors: new_ancestors, results: results);
                }
            }
        }

        /// <summary>
        /// V012: Circular transition chain detection via DFS.
        /// Detects cycles in the next[] transition graph.
        /// </summary>
        static void checkCircularTransitions(
            Map<string, Node> node_map,
            List<ValidationResult> results) {

            var visited = new HashSet<string>();
            var rec_stack = new HashSet<string>();

            foreach (var node_id in node_map.Keys) {
                if (!visited.Contains(node_id)) {
                    if (hasCycle(
                        current_id: node_id,
                        node_map: node_map,
                        visited: visited,
                        rec_stack: rec_stack,
                        path: new List<string>(),
                        results: results)) {
                        // Cycle found and reported
                    }
                }
            }
        }

        /// <summary>
        /// DFS helper: detects if there's a cycle from current_id through next[] transitions.
        /// Returns true if cycle is found, false otherwise.
        /// </summary>
        static bool hasCycle(
            string current_id,
            Map<string, Node> node_map,
            HashSet<string> visited,
            HashSet<string> rec_stack,
            List<string> path,
            List<ValidationResult> results) {

            visited.Add(current_id);
            rec_stack.Add(current_id);
            path.Add(current_id);

            if (node_map.ContainsKey(current_id)) {
                var node = node_map[current_id];
                if (node.next != null) {
                    foreach (var next_entry in node.next) {
                        if (!node_map.ContainsKey(next_entry.id)) {
                            // V006 already caught this
                            continue;
                        }

                        if (!visited.Contains(next_entry.id)) {
                            if (hasCycle(
                                current_id: next_entry.id,
                                node_map: node_map,
                                visited: visited,
                                rec_stack: rec_stack,
                                path: path,
                                results: results)) {
                                return true;
                            }
                        } else if (rec_stack.Contains(next_entry.id)) {
                            // Cycle detected
                            int cycle_start = path.IndexOf(next_entry.id);
                            string cycle_path = string.Join(" → ", path.Skip(cycle_start).Append(next_entry.id));

                            results.Add(new ValidationResult(
                                level:          ValidationLevel.Error,
                                rule_id:        "V012",
                                message:        $"Circular transition chain detected: {cycle_path}",
                                cause_detail:   $"The transition chain forms a loop: {cycle_path}. The player can get stuck in an infinite loop between these nodes.",
                                fix_suggestion: $"Remove or modify one of the next[] entries to break the cycle, or add a condition to ensure the cycle is eventually exited.",
                                location:       new Location { json_path = $"$.root..[?(@.id='{current_id}')].next[?(@.id='{next_entry.id}')]" }));
                            return true;
                        }
                    }
                }
            }

            rec_stack.Remove(current_id);
            return false;
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