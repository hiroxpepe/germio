// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Text;

namespace Germio {
    /// <summary>
    /// Generates a Mermaid.js <c>flowchart LR</c> string from a <see cref="DataRoot"/>.
    /// <para>
    /// Each world is rendered as a <c>subgraph</c>; each level as a node.
    /// Transitions are extracted from both <see cref="DataNext"/> entries and
    /// <see cref="DataEvent"/> entries whose <c>action.requestTransition</c> is set.
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
        /// Exports the DataRoot as a Mermaid flowchart LR string.
        /// </summary>
        /// <param name="root">The DataRoot to render.</param>
        /// <returns>A Mermaid flowchart LR string ready for rendering.</returns>
        public static string Export(DataRoot root) {
            var sb = new StringBuilder();
            sb.AppendLine("flowchart LR");

            // Style definitions — always emitted so downstream tools can apply them.
            sb.AppendLine("    classDef default fill:#2B303A,stroke:#7D8597,color:#FFFFFF;");
            sb.AppendLine("    classDef start fill:#1E88E5,stroke:#005CB2,color:#FFFFFF;");
            sb.AppendLine("    classDef endNode fill:#D81159,stroke:#8F0031,color:#FFFFFF;");

            // Declare subgraphs — one per world, containing level nodes.
            foreach (var world in root.worlds) {
                sb.AppendLine($"    subgraph {sanitize(world.id)} [\"{world.name}\"]");
                foreach (var level in world.levels) {
                    string node_id   = sanitize(level.id);
                    string node_class = getNodeClass(level.name);
                    string node_decl  = node_class.Length > 0
                        ? $"([\"{level.name}\"]):::{node_class}"
                        : $"[\"{level.name}\"]";
                    sb.AppendLine($"        {node_id}{node_decl}");
                }
                sb.AppendLine("    end");
            }

            // Declare edges after all nodes to avoid forward-reference issues.
            foreach (var world in root.worlds) {
                foreach (var level in world.levels) {
                    string from = sanitize(level.id);

                    // Edges from DataNext (condition-gated static transitions).
                    foreach (var next in level.next) {
                        string to = sanitize(next.id);
                        if (string.IsNullOrEmpty(next.condition)) {
                            sb.AppendLine($"    {from} --> {to}");
                        } else {
                            sb.AppendLine($"    {from} -->|\"{next.condition}\"| {to}");
                        }
                    }

                    // Edges from DataEvent (event-driven transitions via requestTransition).
                    foreach (var evt in level.events) {
                        if (evt.action == null || evt.action.requestTransition == null) { continue; }
                        string to    = sanitize(evt.action.requestTransition);
                        string label = evt.trigger;
                        if (!string.IsNullOrEmpty(evt.condition)) {
                            label = $"{label}\\n({evt.condition})";
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
