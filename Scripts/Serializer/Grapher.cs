// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Text;

namespace Germio {
    /// <summary>
    /// Generates a Mermaid.js <c>flowchart LR</c> string from a <see cref="DataRoot"/>.
    /// <para>
    /// Each world is rendered as a <c>subgraph</c>; each level as a node; each
    /// <see cref="DataNext"/> entry as a directed edge with an optional condition label.
    /// </para>
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

            // Declare subgraphs — one per world, containing level nodes.
            foreach (var world in root.worlds) {
                sb.AppendLine($"    subgraph {sanitize(world.id)} [\"{world.name}\"]");
                foreach (var level in world.levels) {
                    sb.AppendLine($"        {sanitize(level.id)}[\"{level.name}\"]");
                }
                sb.AppendLine("    end");
            }

            // Declare edges after all nodes to avoid forward-reference issues.
            foreach (var world in root.worlds) {
                foreach (var level in world.levels) {
                    foreach (var next in level.next) {
                        string from = sanitize(level.id);
                        string to   = sanitize(next.id);
                        if (string.IsNullOrEmpty(next.condition)) {
                            sb.AppendLine($"    {from} --> {to}");
                        } else {
                            sb.AppendLine($"    {from} -->|\"{next.condition}\"| {to}");
                        }
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

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
