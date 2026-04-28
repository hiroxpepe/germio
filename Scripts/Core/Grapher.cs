// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Text;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Generates a Mermaid.js <c>flowchart LR</c> string from a <see cref="Scenario"/>.
    /// <para>
    /// Each world is rendered as a <c>subgraph</c>; each level as a node.
    /// Transitions are extracted from both <see cref="Next"/> entries and
    /// <see cref="Rule"/> entries whose <c>command.request_transition</c> is set.
    /// </para>
    /// <para>Node styling:</para>
    /// <list type="bullet">
    ///   <item><description>Names containing "Title" or "Start" → pill shape <c>([...])</c>, <c>:::start</c> class.</description></item>
    ///   <item><description>Names containing "End" or "Over"   → pill shape <c>([...])</c>, <c>:::endNode</c> class.</description></item>
    ///   <item><description>All other levels                   → rectangle <c>[...]</c>, default class.</description></item>
    /// </list>
    /// Node and subgraph IDs are sanitized: non-alphanumeric characters are replaced with
    /// underscores to produce valid Mermaid identifiers.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Grapher {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Exports the Scenario as a Mermaid flowchart LR string.
        /// </summary>
        /// <param name="scenario">The Scenario to render.</param>
        /// <returns>A Mermaid flowchart LR string ready for rendering.</returns>
        public static string Export(Scenario scenario) {
            var sb = new StringBuilder();
            sb.AppendLine("flowchart LR");

            // Style definitions — always emitted so downstream tools can apply them.
            sb.AppendLine("    classDef default fill:#2B303A,stroke:#7D8597,color:#FFFFFF;");
            sb.AppendLine("    classDef start fill:#1E88E5,stroke:#005CB2,color:#FFFFFF;");
            sb.AppendLine("    classDef endNode fill:#D81159,stroke:#8F0031,color:#FFFFFF;");

            // Declare subgraphs — one per world, containing level nodes.
            foreach (var world in scenario.worlds) {
                sb.AppendLine($"    subgraph {sanitize(id: world.id)} [\"{world.name}\"]");
                foreach (var level in world.levels) {
                    string node_id   = sanitize(id: level.id);
                    string node_class = getNodeClass(name: level.name);
                    string node_decl  = node_class.Length > 0
                        ? $"([\"{level.name}\"]):::{node_class}"
                        : $"[\"{level.name}\"]";
                    sb.AppendLine($"        {node_id}{node_decl}");
                }
                sb.AppendLine("    end");
            }

            // Declare edges after all nodes to avoid forward-reference issues.
            foreach (var world in scenario.worlds) {
                foreach (var level in world.levels) {
                    string from = sanitize(id: level.id);

                    // Edges from Next (condition-gated static transitions).
                    foreach (var next in level.next) {
                        string to = sanitize(id: next.id);
                        if (string.IsNullOrEmpty(next.condition)) {
                            sb.AppendLine($"    {from} --> {to}");
                        } else {
                            sb.AppendLine($"    {from} -->|\"{next.condition}\"| {to}");
                        }
                    }

                    // Edges from Rule (rule-driven transitions via request_transition).
                    foreach (var rule in level.rules) {
                        if (rule.command == null || rule.command.request_transition == null) { continue; }
                        string to    = sanitize(id: rule.command.request_transition);
                        string label = rule.trigger;
                        if (!string.IsNullOrEmpty(rule.condition)) {
                            label = $"{label}\\n({rule.condition})";
                        }
                        sb.AppendLine($"    {from} -->|\"{label}\"| {to}");
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        /// <summary>
        /// Returns the Mermaid classDef name for the given level display name.
        /// Levels whose name contains "Title" or "Start" are tagged <c>start</c>;
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
