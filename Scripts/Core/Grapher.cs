// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Generates a Mermaid.js flowchart string from a <see cref="Scenario"/> Node tree.
    /// <para>
    /// Each Node with children is rendered as a <c>subgraph</c>; leaf Nodes are rendered as regular nodes.
    /// Transitions are extracted from each Node's <see cref="Next"/> entries.
    /// </para>
    /// <para>Node styling:</para>
    /// <list type="bullet">
    ///   <item><description>Names containing "Title" or "Start" → pill shape <c>([...])</c>, <c>:::start</c> class.</description></item>
    ///   <item><description>Names containing "End" or "Over"   → pill shape <c>([...])</c>, <c>:::endNode</c> class.</description></item>
    ///   <item><description>All other nodes                    → rectangle <c>[...]</c>, default class.</description></item>
    /// </list>
    /// Node IDs are sanitized: non-alphanumeric characters are replaced with underscores to produce valid Mermaid identifiers.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Grapher {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Exports the Scenario as a Mermaid flowchart (using graph TD layout) string.
        /// Walks the Node tree recursively to emit subgraphs for internal nodes and regular nodes for leaves.
        /// Then emits all transitions (Next entries) as directed edges with optional condition labels.
        /// </summary>
        /// <param name="scenario">The Scenario to render.</param>
        /// <returns>A Mermaid flowchart string ready for rendering.</returns>
        public static string Export(Scenario scenario) {
            var sb = new StringBuilder();
            sb.AppendLine("graph TD");

            // Style definitions — always emitted so downstream tools can apply them.
            sb.AppendLine("    classDef default fill:#2B303A,stroke:#7D8597,color:#FFFFFF;");
            sb.AppendLine("    classDef start fill:#1E88E5,stroke:#005CB2,color:#FFFFFF;");
            sb.AppendLine("    classDef endNode fill:#D81159,stroke:#8F0031,color:#FFFFFF;");

            // Render the Node tree recursively, starting from root.
            if (scenario.root != null) {
                renderNodeTree(node: scenario.root, sb: sb, indent: 1);
            }

            // Collect all nodes in tree-walk order, then emit all transitions.
            var all_nodes = new List<Node>();
            if (scenario.root != null) {
                collectAll(node: scenario.root, list: all_nodes);
            }

            foreach (Node node in all_nodes) {
                foreach (Next transition in node.next) {
                    string from = sanitize(id: node.id);
                    string to = sanitize(id: transition.id);
                    if (string.IsNullOrEmpty(transition.condition)) {
                        sb.AppendLine($"    {from} --> {to}");
                    } else {
                        sb.AppendLine($"    {from} -->|\"{transition.condition}\"| {to}");
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        /// <summary>
        /// Recursively renders the Node tree as Mermaid syntax.
        /// - Nodes with children become subgraph blocks.
        /// - Leaf nodes become regular nodes (rectangles or pills based on name).
        /// </summary>
        static void renderNodeTree(Node node, StringBuilder sb, int indent) {
            string pad = new string(' ', indent * 4);
            string node_id = sanitize(id: node.id);

            if (node.children.Count == 0) {
                // Leaf node — render as regular node
                string node_class = getNodeClass(name: node.name);
                string node_decl = node_class.Length > 0
                    ? $"([\"{node.name}\"]):::{node_class}"
                    : $"[\"{node.name}\"]";
                sb.AppendLine($"{pad}{node_id}{node_decl}");
            } else {
                // Internal node — render as subgraph
                sb.AppendLine($"{pad}subgraph {node_id} [\"{node.name}\"]");
                foreach (Node child in node.children) {
                    renderNodeTree(node: child, sb: sb, indent: indent + 1);
                }
                sb.AppendLine($"{pad}end");
            }
        }

        /// <summary>
        /// Recursively collects all nodes in the tree (depth-first traversal).
        /// The root node is added first, then all children recursively.
        /// </summary>
        static void collectAll(Node node, List<Node> list) {
            list.Add(item: node);
            foreach (Node child in node.children) {
                collectAll(node: child, list: list);
            }
        }

        /// <summary>
        /// Returns the Mermaid classDef name for the given node display name.
        /// Nodes whose name contains "Title" or "Start" are tagged <c>start</c>;
        /// those containing "End" or "Over" are tagged <c>endNode</c>;
        /// all others return an empty string (default class).
        /// </summary>
        static string getNodeClass(string name) {
            if (name.Contains("Title") || name.Contains("Start")) { return "start"; }
            if (name.Contains("End")   || name.Contains("Over"))  { return "endNode"; }
            return string.Empty;
        }

        /// <summary>
        /// Converts an arbitrary string into a valid Mermaid node identifier.
        /// Non-alphanumeric characters are replaced with underscores.
        /// </summary>
        static string sanitize(string id) {
            var sb = new StringBuilder(id.Length);
            foreach (char c in id) {
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }
            return sb.ToString();
        }
    }
}