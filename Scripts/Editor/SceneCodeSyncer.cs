// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Germio.Core;
using Germio.Model;

namespace Germio.Editor {
    /// <summary>
    /// Generator that synchronises C# Scene classes under <c>Assets/Scripts/Scenes/</c>
    /// with the node tree declared in <c>germio.json</c>.
    /// 
    /// See <c>docs/development_plan_phase_5_19_spec_JP.md</c> for the full specification.
    /// 
    /// Generator-managed regions are limited to four single-line patterns (§4.3, §5.3):
    ///   L1: namespace declaration
    ///   L2: class declaration
    ///   L3: GermioSceneHandler attribute
    ///   L4: handler signature (always immediately after L3)
    /// 
    /// All other lines (method bodies, fields, using directives, XMLDocs, file headers)
    /// are read once for parsing the four patterns above, but never rewritten.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class SceneCodeSyncer {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Run a sync pass against the given germio.json and Scenes root.
        /// Returns a result object summarising created / moved / modified files,
        /// warnings, and errors. The Generator never throws on file conflicts;
        /// errors are reported in the returned object instead.
        /// </summary>
        public static SyncResult Sync(string germio_json_path, string scenes_root) {
            var result = new SyncResult();

            // Step 1: Load germio.json and run the Validator. Bail out on V004.
            Scenario scenario;
            try {
                string json = File.ReadAllText(path: germio_json_path);
                scenario = JsonConvert.DeserializeObject<Scenario>(value: json) ?? new Scenario();
            } catch (Exception ex) {
                result.errors.Add(item: $"Failed to read germio.json: {ex.Message}");
                return result;
            }

            var validation = Validator.Validate(scenario: scenario);
            foreach (var v in validation) {
                if (v.severity == ValidationLevel.Error && v.rule_id == "V004") {
                    result.errors.Add(item: $"V004: {v.message}");
                }
            }
            if (result.errors.Count > 0) {
                return result;
            }

            // Step 2: Compute expected state for every node in the tree.
            var expected_index = new Dictionary<string, ExpectedNode>();
            walk_node_with_parent(node: scenario.root, parent: null, scenes_root: scenes_root, index: expected_index);

            // Step 3: Scan existing C# files and build an attribute-id index.
            var actual_index = new Dictionary<string, List<string>>();
            scan_existing_files(scenes_root: scenes_root, index: actual_index, result: result);
            foreach (var kv in actual_index) {
                if (kv.Value.Count > 1) {
                    result.errors.Add(
                        item: $"Duplicate [GermioSceneHandler(id:\"{kv.Key}\")] in: {string.Join(separator: ", ", values: kv.Value)}");
                }
            }
            if (result.errors.Count > 0) {
                return result;
            }

            // Step 4: Process each expected node.
            foreach (var expected in expected_index.Values) {
                bool has_actual = actual_index.TryGetValue(key: expected.node_id, value: out var actual_paths);
                if (!has_actual || actual_paths == null || actual_paths.Count == 0) {
                    handle_new_or_recreate(expected: expected, result: result);
                } else {
                    string current_path = actual_paths[0];
                    handle_existing(expected: expected, current_path: current_path, result: result);
                }
            }

            // Step 5: Detect orphans (C# id values not present in JSON).
            foreach (var kv in actual_index) {
                if (!expected_index.ContainsKey(key: kv.Key)) {
                    foreach (string path in kv.Value) {
                        result.warnings.Add(item: $"Orphan handler: id='{kv.Key}' in {path}");
                        mark_as_orphan(file_path: path, result: result);
                    }
                }
            }

            // Step 6: Remove now-empty directories.
            cleanup_empty_directories(scenes_root: scenes_root, result: result);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (node walk) [verb]

        static void walk_node_with_parent(Node node, ExpectedNode? parent, string scenes_root, Dictionary<string, ExpectedNode> index) {
            if (string.IsNullOrEmpty(value: node.id)) { return; }
            string class_name = compute_class_name(node: node);
            string parent_class = parent != null ? parent.class_name : "Scene";
            string handler_name = "On" + pascal_case(input: node.id);
            string file_path = compute_file_path(node: node, parent: parent, scenes_root: scenes_root, class_name: class_name);

            var expected = new ExpectedNode {
                node_id      = node.id,
                class_name   = class_name,
                parent_class = parent_class,
                handler_name = handler_name,
                file_path    = file_path,
            };
            index[node.id] = expected;

            if (node.children != null) {
                foreach (var child in node.children) {
                    walk_node_with_parent(node: child, parent: expected, scenes_root: scenes_root, index: index);
                }
            }
        }

        static string compute_class_name(Node node) {
            string source = string.IsNullOrEmpty(value: node.scene) ? node.id : node.scene;
            return pascal_case(input: source);
        }

        static string compute_file_path(Node node, ExpectedNode? parent, string scenes_root, string class_name) {
            // Root node sits directly under scenes_root. Otherwise, sit inside the parent's directory.
            if (parent == null) {
                return Path.Combine(path1: scenes_root, path2: class_name + ".cs");
            }
            string parent_dir = Path.Combine(path1: Path.GetDirectoryName(path: parent.file_path)!, path2: parent.class_name);
            return Path.Combine(path1: parent_dir, path2: class_name + ".cs");
        }

        static string pascal_case(string input) {
            if (string.IsNullOrEmpty(value: input)) { return string.Empty; }
            var parts = input.Split(separator: new[] { '_' }, options: StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (string part in parts) {
                if (part.Length > 0) {
                    sb.Append(value: char.ToUpperInvariant(c: part[0]));
                    sb.Append(value: part.Substring(startIndex: 1));
                }
            }
            return sb.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (file scan) [verb]

        static readonly Regex _attr_regex = new Regex(
            pattern: @"^\s*\[GermioSceneHandler\(\s*id\s*:\s*""(?<id>[^""]*)""\s*\)\s*\]\s*$",
            options: RegexOptions.Multiline | RegexOptions.Compiled);

        static void scan_existing_files(string scenes_root, Dictionary<string, List<string>> index, SyncResult result) {
            if (!Directory.Exists(path: scenes_root)) { return; }
            foreach (string path in Directory.EnumerateFiles(path: scenes_root, searchPattern: "*.cs", searchOption: SearchOption.AllDirectories)) {
                string text;
                try {
                    text = File.ReadAllText(path: path);
                } catch (Exception ex) {
                    result.errors.Add(item: $"Failed to read {path}: {ex.Message}");
                    continue;
                }
                foreach (Match m in _attr_regex.Matches(input: text)) {
                    string id = m.Groups["id"].Value;
                    if (!index.TryGetValue(key: id, value: out var list)) {
                        list = new List<string>();
                        index[id] = list;
                    }
                    list.Add(item: path);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (per-node handlers) [verb]

        static void handle_new_or_recreate(ExpectedNode expected, SyncResult result) {
            string dir = Path.GetDirectoryName(path: expected.file_path)!;
            if (!Directory.Exists(path: dir)) {
                Directory.CreateDirectory(path: dir);
                result.created_directories.Add(item: dir);
            }
            string skeleton = build_skeleton(expected: expected);
            File.WriteAllText(path: expected.file_path, contents: skeleton);
            result.created_files.Add(item: expected.file_path);
        }

        static void handle_existing(ExpectedNode expected, string current_path, SyncResult result) {
            string current_text = File.ReadAllText(path: current_path);
            var (line_ending, has_bom) = detect_format(text: current_text, raw_bytes: File.ReadAllBytes(path: current_path));
            string updated = update_managed_lines(
                source: current_text,
                expected: expected,
                line_ending: line_ending);

            bool needs_path_change = !string.Equals(a: current_path, b: expected.file_path, comparisonType: StringComparison.Ordinal);
            bool content_changed = !string.Equals(a: current_text, b: updated, comparisonType: StringComparison.Ordinal);

            if (needs_path_change) {
                string new_dir = Path.GetDirectoryName(path: expected.file_path)!;
                if (!Directory.Exists(path: new_dir)) {
                    Directory.CreateDirectory(path: new_dir);
                    result.created_directories.Add(item: new_dir);
                }

                if (content_changed) {
                    write_with_format(path: expected.file_path, text: updated, has_bom: has_bom);
                    result.modified_files.Add(item: expected.file_path);
                } else {
                    File.Move(sourceFileName: current_path, destFileName: expected.file_path);
                }

                if (File.Exists(path: current_path)) {
                    File.Delete(path: current_path);
                }
                // Move .cs.meta if present.
                string old_meta = current_path + ".meta";
                string new_meta = expected.file_path + ".meta";
                if (File.Exists(path: old_meta) && !File.Exists(path: new_meta)) {
                    File.Move(sourceFileName: old_meta, destFileName: new_meta);
                }
                result.moved_files.Add(item: $"{current_path} -> {expected.file_path}");
            } else if (content_changed) {
                write_with_format(path: expected.file_path, text: updated, has_bom: has_bom);
                result.modified_files.Add(item: expected.file_path);
            }
        }

        static void mark_as_orphan(string file_path, SyncResult result) {
            string text = File.ReadAllText(path: file_path);
            byte[] raw = File.ReadAllBytes(path: file_path);
            var (line_ending, has_bom) = detect_format(text: text, raw_bytes: raw);

            // If marker already present (anywhere before the class line), do nothing.
            if (text.Contains(value: "germio: orphan")) {
                return;
            }

            // Insert marker on the line just above the class declaration.
            string marker = $"// germio: orphan (removed from germio.json on {DateTime.UtcNow:yyyy-MM-dd}){line_ending}";
            string updated = Regex.Replace(
                input: text,
                pattern: @"(?m)^(?<indent>\s*)(public\s+)?(partial\s+)?(abstract\s+)?class\s+\w+",
                evaluator: (MatchEvaluator)(m => {
                    string indent = m.Groups["indent"].Value;
                    return indent + marker + m.Value;
                }),
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(value: 5));

            if (!ReferenceEquals(objA: updated, objB: text) && updated != text) {
                write_with_format(path: file_path, text: updated, has_bom: has_bom);
                result.modified_files.Add(item: file_path);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (line-pattern rewriting) [verb]

        static string update_managed_lines(string source, ExpectedNode expected, string line_ending) {
            // L2: class declaration. Preserve modifiers, only rewrite class name and parent class.
            source = Regex.Replace(
                input: source,
                pattern: @"(?m)^(?<indent>\s*)(?<modifiers>(?:public\s+|partial\s+|abstract\s+|sealed\s+|static\s+|internal\s+)*)class\s+(?<class>\w+)\s*:\s*(?<parent>\w+)",
                evaluator: (MatchEvaluator)(m => {
                    string indent = m.Groups["indent"].Value;
                    string modifiers = m.Groups["modifiers"].Value;
                    return $"{indent}{modifiers}class {expected.class_name} : {expected.parent_class}";
                }),
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(value: 5));

            // L3: GermioSceneHandler attribute id value.
            source = Regex.Replace(
                input: source,
                pattern: @"(?m)^(?<indent>\s*)\[GermioSceneHandler\(\s*id\s*:\s*""[^""]*""\s*\)\s*\]",
                evaluator: (MatchEvaluator)(m => {
                    string indent = m.Groups["indent"].Value;
                    return $"{indent}[GermioSceneHandler(id: \"{expected.node_id}\")]";
                }),
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(value: 5));

            // L4: handler signature (only the line directly following an L3 attribute).
            // Rewrite method name "OnXxx" while preserving modifiers.
            source = Regex.Replace(
                input: source,
                pattern: @"(?m)^(?<attr_line>\s*\[GermioSceneHandler\([^\)]*\)\s*\]\s*\r?\n)(?<sig_indent>\s*)(?<modifiers>(?:protected\s+|public\s+|private\s+|internal\s+|virtual\s+|override\s+)*)void\s+(?<name>\w+)\s*\(\s*\)",
                evaluator: (MatchEvaluator)(m => {
                    string attr_line = m.Groups["attr_line"].Value;
                    string sig_indent = m.Groups["sig_indent"].Value;
                    string modifiers = m.Groups["modifiers"].Value;
                    return $"{attr_line}{sig_indent}{modifiers}void {expected.handler_name}()";
                }),
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(value: 5));

            return source;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (format detection) [verb]

        static (string line_ending, bool has_bom) detect_format(string text, byte[] raw_bytes) {
            bool has_bom = raw_bytes.Length >= 3 && raw_bytes[0] == 0xEF && raw_bytes[1] == 0xBB && raw_bytes[2] == 0xBF;
            string line_ending = text.Contains(value: "\r\n") ? "\r\n" : "\n";
            return (line_ending, has_bom);
        }

        static void write_with_format(string path, string text, bool has_bom) {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: has_bom);
            File.WriteAllText(path: path, contents: text, encoding: encoding);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (skeleton generation) [verb]

        static string build_skeleton(ExpectedNode expected) {
            var sb = new StringBuilder();
            sb.Append(value: "// Copyright (c) STUDIO MeowToon. All rights reserved.\n");
            sb.Append(value: "// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.\n");
            sb.Append(value: "\n");
            sb.Append(value: "using Germio;\n");
            sb.Append(value: "\n");
            sb.Append(value: "namespace GameDev {\n");
            sb.Append(value: "    /// <summary>\n");
            sb.Append(value: $"    /// Scene controller for the {expected.node_id} node (id=\"{expected.node_id}\").\n");
            sb.Append(value: "    /// Generated by Germio SceneCodeSyncer (Phase 5.19).\n");
            sb.Append(value: "    /// </summary>\n");
            sb.Append(value: "    /// <author>h.adachi (STUDIO MeowToon)</author>\n");
            sb.Append(value: $"    public class {expected.class_name} : {expected.parent_class} {{\n");
            sb.Append(value: "#nullable enable\n");
            sb.Append(value: "\n");
            sb.Append(value: $"        [GermioSceneHandler(id: \"{expected.node_id}\")]\n");
            sb.Append(value: $"        protected void {expected.handler_name}() {{\n");
            sb.Append(value: $"            // Empty placeholder. Add {expected.class_name}-specific logic here.\n");
            sb.Append(value: "        }\n");
            sb.Append(value: "    }\n");
            sb.Append(value: "}\n");
            return sb.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (cleanup) [verb]

        static void cleanup_empty_directories(string scenes_root, SyncResult result) {
            if (!Directory.Exists(path: scenes_root)) { return; }
            // Walk bottom-up.
            var dirs = Directory.EnumerateDirectories(path: scenes_root, searchPattern: "*", searchOption: SearchOption.AllDirectories)
                .OrderByDescending(keySelector: d => d.Length)
                .ToList();
            foreach (string dir in dirs) {
                if (!Directory.Exists(path: dir)) { continue; }
                bool is_empty = !Directory.EnumerateFileSystemEntries(path: dir).Any();
                if (is_empty) {
                    Directory.Delete(path: dir);
                    string dir_meta = dir + ".meta";
                    if (File.Exists(path: dir_meta)) {
                        File.Delete(path: dir_meta);
                    }
                    result.deleted_directories.Add(item: dir);
                }
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // public types

    /// <summary>Computed expectation for a single node, derived from germio.json.</summary>
    internal class ExpectedNode {
        public string node_id      = string.Empty;
        public string class_name   = string.Empty;
        public string parent_class = string.Empty;
        public string handler_name = string.Empty;
        public string file_path    = string.Empty;
    }

    /// <summary>Outcome of a single <see cref="SceneCodeSyncer.Sync"/> invocation.</summary>
    public class SyncResult {
        public List<string> created_files       = new List<string>();
        public List<string> created_directories = new List<string>();
        public List<string> modified_files      = new List<string>();
        public List<string> moved_files         = new List<string>();
        public List<string> deleted_directories = new List<string>();
        public List<string> warnings            = new List<string>();
        public List<string> errors              = new List<string>();
    }
}

#endif
