// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Germio.Model;

namespace Germio.Core
{
    /// <summary>
    /// Parses a Mermaid <c>flowchart LR</c> string (as produced by <see cref="Grapher.Export"/>)
    /// back into a <see cref="Scenario"/> object (P5-T6, G11 bidirectional conversion).
    /// Worlds map to subgraphs; levels map to nodes; Next entries map to edges.
    /// Only the structural graph is restored — scene names, rules, and runtime state
    /// are not encoded in the Mermaid format and will be empty after parsing.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class MermaidParser
    {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Regex patterns (compiled once at class-load time)

        static readonly Regex _rx_subgraph  = new Regex(
            @"^\s*subgraph\s+([a-zA-Z0-9_]+)\s+\[""([^""]*)""\]",
            RegexOptions.Compiled);

        // pill node: nodeId(["Name"]):::class  or  nodeId(["Name"])
        // No $ anchor — suffix :::class is ignored so colon-count issues are avoided.
        static readonly Regex _rx_pill = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\(\[""([^""]+)""\]\)",
            RegexOptions.Compiled);

        // rect node: nodeId["Name"]
        static readonly Regex _rx_rect = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\[""([^""]+)""\]$",
            RegexOptions.Compiled);

        // labeled edge: from -->|"condition"| to
        static readonly Regex _rx_edge_lbl = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\s+-->\|""([^""]*)""\|\s+([a-zA-Z0-9_]+)\s*$",
            RegexOptions.Compiled);

        // unlabeled edge: from --> to
        static readonly Regex _rx_edge_plain = new Regex(
            @"^\s*([a-zA-Z0-9_]+)\s+-->\s+([a-zA-Z0-9_]+)\s*$",
            RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Parses <paramref name="mermaid"/> and returns the reconstructed <see cref="Scenario"/>.
        /// </summary>
        /// <param name="mermaid">A Mermaid flowchart LR string.</param>
        /// <returns>The parsed <see cref="Scenario"/>.</returns>
        /// <exception cref="FormatException">Thrown when parsing fails.</exception>
        public static Scenario Parse(string mermaid)
        {
            var result = TryParse(mermaid: mermaid);
            if (!result.success)
            {
                string msg = result.errors.Count > 0 ? result.errors[0].message : "Mermaid parse failed.";
                throw new FormatException(msg);
            }
            return result.scenario!;
        }

        /// <summary>
        /// Attempts to parse <paramref name="mermaid"/> without throwing.
        /// </summary>
        /// <param name="mermaid">A Mermaid flowchart LR string.</param>
        /// <returns>A <see cref="ParseResult"/> with success status, optional scenario, and any errors.</returns>
        public static ParseResult TryParse(string mermaid)
        {
            var errors = new List<ParseError>();

            if (string.IsNullOrWhiteSpace(mermaid))
            {
                errors.Add(new ParseError { line_number = 0, message = "Input is empty." });
                return new ParseResult { success = false, errors = errors };
            }

            var lines = mermaid.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0 || !lines[0].TrimStart().StartsWith("flowchart"))
            {
                errors.Add(new ParseError
                {
                    line_number = 1,
                    message = "Expected 'flowchart' declaration on first line."
                });
                return new ParseResult { success = false, errors = errors };
            }

            var scenario    = new Scenario();
            var level_map   = new Dictionary<string, Level>();   // sanitized id → Level object
            World? cur_world = null;

            // Deferred edges — resolved after all nodes are collected.
            var edges = new List<(string from_id, string to_id, string condition)>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line     = lines[i];
                int    line_num = i + 1;

                string trimmed = line.TrimStart();

                // Skip header and style declarations.
                if (trimmed.StartsWith("flowchart") || trimmed.StartsWith("classDef"))
                {
                    continue;
                }

                // Close a world subgraph.
                if (trimmed == "end")
                {
                    cur_world = null;
                    continue;
                }

                // Open a world subgraph.
                var m = _rx_subgraph.Match(line);
                if (m.Success)
                {
                    cur_world = new World { id = m.Groups[1].Value, name = m.Groups[2].Value };
                    scenario.worlds.Add(cur_world);
                    continue;
                }

                // Labeled edge (must be checked before nodes to avoid misclassification).
                m = _rx_edge_lbl.Match(line);
                if (m.Success)
                {
                    edges.Add((m.Groups[1].Value, m.Groups[3].Value, m.Groups[2].Value));
                    continue;
                }

                // Unlabeled edge.
                m = _rx_edge_plain.Match(line);
                if (m.Success)
                {
                    edges.Add((m.Groups[1].Value, m.Groups[2].Value, string.Empty));
                    continue;
                }

                // Node declarations — only valid inside a subgraph.
                if (cur_world == null) { continue; }

                // Pill node: nodeId(["Name"]):::class
                m = _rx_pill.Match(line);
                if (m.Success)
                {
                    var level = new Level { id = m.Groups[1].Value, name = m.Groups[2].Value };
                    cur_world.levels.Add(level);
                    level_map[level.id] = level;
                    continue;
                }

                // Rect node: nodeId["Name"]
                m = _rx_rect.Match(line);
                if (m.Success)
                {
                    var level = new Level { id = m.Groups[1].Value, name = m.Groups[2].Value };
                    cur_world.levels.Add(level);
                    level_map[level.id] = level;
                    continue;
                }
            }

            if (scenario.worlds.Count == 0)
            {
                errors.Add(new ParseError
                {
                    line_number = 1,
                    message = "No subgraph (world) declarations found in the Mermaid input."
                });
                return new ParseResult { success = false, errors = errors };
            }

            // Resolve edges → Next entries on source levels.
            foreach (var (from_id, to_id, condition) in edges)
            {
                if (level_map.TryGetValue(from_id, out var src))
                {
                    src.next.Add(new Next { id = to_id, condition = condition });
                }
            }

            return new ParseResult { success = true, scenario = scenario, errors = errors };
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Result types

    /// <summary>Result of a <see cref="MermaidParser.TryParse"/> operation.</summary>
    public class ParseResult
    {
#nullable enable
        public bool      success  { get; set; }
        public Scenario? scenario { get; set; }
        public List<ParseError> errors { get; set; } = new List<ParseError>();
    }

    /// <summary>A single parse error with a line number and human-readable message.</summary>
    public class ParseError
    {
        public int    line_number { get; set; }
        public string message     { get; set; } = string.Empty;
    }
}
