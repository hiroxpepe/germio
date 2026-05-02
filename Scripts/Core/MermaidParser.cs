// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Parses a Mermaid <c>graph TD</c> string (as produced by <see cref="Grapher.Export"/>)
    /// back into a <see cref="Scenario"/> object with Node tree structure.
    /// Mermaid subgraphs map to internal Nodes; leaf nodes map to leaf Nodes.
    /// Next entries are extracted from directed edges (-->, -->|condition|).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class MermaidParser {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Regex patterns (compiled once at class-load time)

        static readonly Regex _rx_subgraph = new Regex(
            @"^\s*subgraph\s+([a-zA-Z0-9_]+)\s+\[""([^""]*)""\]",
            RegexOptions.Compiled);

        // pill node: nodeId(["Name"]):::class or nodeId(["Name"])
        static readonly Regex _rx_pill = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\(\[""([^""]+)""\]\)",
            RegexOptions.Compiled);

        // rect node: nodeId["Name"]
        static readonly Regex _rx_rect = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\[""([^""]+)""\]",
            RegexOptions.Compiled);

        // labeled edge: from -->|"condition"| to
        static readonly Regex _rx_edge_lbl = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\s+-->\|""([^""]*)""\|\s+([a-zA-Z0-9_]+)",
            RegexOptions.Compiled);

        // unlabeled edge: from --> to
        static readonly Regex _rx_edge_plain = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\s+-->\s+([a-zA-Z0-9_]+)",
            RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Parses <paramref name="mermaid"/> and returns the reconstructed <see cref="Scenario"/>.
        /// </summary>
        /// <param name="mermaid">A Mermaid graph TD string.</param>
        /// <returns>The parsed <see cref="Scenario"/>.</returns>
        /// <exception cref="FormatException">Thrown when parsing fails.</exception>
        public static Scenario Parse(string mermaid) {
            var result = TryParse(mermaid: mermaid);
            if (!result.success) {
                string msg = result.errors.Count > 0 ? result.errors[0].message : "Mermaid parse failed.";
                throw new FormatException(msg);
            }
            return result.scenario!;
        }

        /// <summary>
        /// Attempts to parse <paramref name="mermaid"/> without throwing.
        /// </summary>
        /// <param name="mermaid">A Mermaid graph TD string.</param>
        /// <returns>A <see cref="ParseResult"/> with success status, optional scenario, and any errors.</returns>
        public static ParseResult TryParse(string mermaid) {
            var errors = new List<ParseError>();

            if (string.IsNullOrWhiteSpace(mermaid)) {
                errors.Add(new ParseError { line_number = 0, message = "Input is empty." });
                return new ParseResult { success = false, errors = errors };
            }

            var lines = mermaid.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0 || 
                (!lines[0].TrimStart().StartsWith("graph") && 
                 !lines[0].TrimStart().StartsWith("flowchart"))) {
                errors.Add(new ParseError {
                    line_number = 1,
                    message = "Expected 'graph' or 'flowchart' declaration on first line."
                });
                return new ParseResult { success = false, errors = errors };
            }

            // Create synthetic root node to serve as the tree root.
            var synthetic_root = new Node { id = "root", name = "root", kind = "world" };
            var stack = new Stack<Node>();
            stack.Push(item: synthetic_root);

            var node_lookup = new Dictionary<string, Node>();
            node_lookup[synthetic_root.id] = synthetic_root;

            // Deferred edges — resolved after all nodes are collected.
            var pending_edges = new List<(string from, string to, string condition)>();

            for (int i = 1; i < lines.Length; i++) {
                string line = lines[i].TrimStart();
                int line_num = i + 1;

                // Skip empty lines and style declarations.
                if (string.IsNullOrEmpty(line) || line.StartsWith("classDef")) {
                    continue;
                }

                // Close a subgraph.
                if (line == "end") {
                    if (stack.Count > 1) {
                        stack.Pop();
                    }
                    continue;
                }

                // Open a subgraph (internal node).
                var m = _rx_subgraph.Match(line);
                if (m.Success) {
                    var node = new Node {
                        id = m.Groups[1].Value,
                        name = m.Groups[2].Value,
                        kind = "world"
                    };
                    stack.Peek().children.Add(item: node);
                    stack.Push(item: node);
                    node_lookup[node.id] = node;
                    continue;
                }

                // Labeled edge (must check before plain edge to avoid misclassification).
                m = _rx_edge_lbl.Match(line);
                if (m.Success) {
                    pending_edges.Add(item: (m.Groups[1].Value, m.Groups[3].Value, m.Groups[2].Value));
                    continue;
                }

                // Unlabeled edge.
                m = _rx_edge_plain.Match(line);
                if (m.Success) {
                    pending_edges.Add(item: (m.Groups[1].Value, m.Groups[2].Value, string.Empty));
                    continue;
                }

                // Pill-shaped leaf node.
                m = _rx_pill.Match(line);
                if (m.Success) {
                    var node = new Node {
                        id = m.Groups[1].Value,
                        name = m.Groups[2].Value,
                        kind = "level"
                    };
                    stack.Peek().children.Add(item: node);
                    node_lookup[node.id] = node;
                    continue;
                }

                // Rectangular leaf node.
                m = _rx_rect.Match(line);
                if (m.Success) {
                    var node = new Node {
                        id = m.Groups[1].Value,
                        name = m.Groups[2].Value,
                        kind = "level"
                    };
                    stack.Peek().children.Add(item: node);
                    node_lookup[node.id] = node;
                    continue;
                }
            }

            if (synthetic_root.children.Count == 0) {
                errors.Add(new ParseError {
                    line_number = 1,
                    message = "No subgraph or leaf node declarations found in the Mermaid input."
                });
                return new ParseResult { success = false, errors = errors };
            }

            // Resolve deferred edges → Next entries on source nodes.
            foreach (var (from_id, to_id, condition) in pending_edges) {
                if (node_lookup.TryGetValue(from_id, out var from_node)) {
                    from_node.next.Add(item: new Next { id = to_id, condition = condition });
                }
            }

            var scenario = new Scenario { root = synthetic_root };
            return new ParseResult { success = true, scenario = scenario, errors = errors };
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Result types

    /// <summary>Result of a <see cref="MermaidParser.TryParse"/> operation.</summary>
    public class ParseResult {
#nullable enable
        public bool success { get; set; }
        public Scenario? scenario { get; set; }
        public List<ParseError> errors { get; set; } = new List<ParseError>();
    }

    /// <summary>A single parse error with a line number and human-readable message.</summary>
    public class ParseError {
        public int line_number { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
