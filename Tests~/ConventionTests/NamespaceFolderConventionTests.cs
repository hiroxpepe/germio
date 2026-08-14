// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Germio.Tests.Convention {
    /// <summary>
    /// Verifies that every source file's declared namespace matches its physical
    /// folder, under this repo's source-root convention.
    ///
    /// Convention (root ns = "Germio"):
    ///   ConventionScan.TARGET_DIRS[0]/&lt;A&gt;/&lt;B&gt;/Foo.cs  ->  namespace Germio.&lt;A&gt;.&lt;B&gt;
    ///   - files directly in the source root map to "Germio" itself.
    ///   - COLLAPSE_FOLDERS: any folder at/under one of these collapses to that
    ///     folder's namespace, ignoring deeper sub-folders.
    ///
    /// PORTING: this rule uses ConventionScan.TARGET_DIRS for its root, so it
    /// follows whatever that file already points at in this repo.
    /// </summary>
    [TestFixture]
    [Category("Convention")]
    public class NamespaceFolderConventionTests
    {
        static readonly Regex NamespaceLine =
            new(@"^\s*namespace\s+([A-Za-z0-9_.]+)\s*(?:;|\{)?\s*$", RegexOptions.Compiled);

        /// <summary>A single folder-&gt;namespace convention to enforce over one source tree.</summary>
        sealed record ConventionRule(string RootDir, string RootNamespace, string[] CollapseFolders);

        static readonly ConventionRule ScriptsRule =
            new(ConventionScan.TARGET_DIRS[0], "Germio", new string[] { "Players" });

        // ── map a folder (relative to root) to the namespace it must declare ──
        static string expected_namespace(string rel_dir, ConventionRule rule)
        {
            rel_dir = rel_dir.Replace(Path.DirectorySeparatorChar, '/').Trim('/');
            if (rel_dir is "" or ".") return rule.RootNamespace;
            var parts = rel_dir.Split('/');
            if (rule.CollapseFolders.Contains(parts[0]))
                return rule.RootNamespace + "." + parts[0];
            return rule.RootNamespace + "." + string.Join(".", parts);
        }

        // ── first declared namespace in a file, or null ──
        static IEnumerable<string> all_namespaces(string path)
        {
            foreach (var line in File.ReadLines(path)) {
                var m = NamespaceLine.Match(line);
                if (m.Success) yield return m.Groups[1].Value;
            }
        }

        /// <summary>
        /// Walk the rule's source tree and return human-readable mismatch lines.
        /// Empty list == fully compliant. Files with no namespace are ignored (they
        /// are not folder-bound types); flip `strict` to flag them too.
        /// </summary>
        static List<string> scan_for_mismatches(string root_dir, ConventionRule rule, bool strict)
        {
            var mismatches = new List<string>();
            foreach (var path in Directory.EnumerateFiles(root_dir, "*.cs", SearchOption.AllDirectories)) {
                var rel = Path.GetRelativePath(root_dir, path).Replace(Path.DirectorySeparatorChar, '/');
                if (rel.StartsWith("bin/") || rel.Contains("/bin/") ||
                    rel.StartsWith("obj/") || rel.Contains("/obj/")) continue;

                var rel_dir = Path.GetDirectoryName(rel)?.Replace(Path.DirectorySeparatorChar, '/') ?? "";
                var expected = expected_namespace(rel_dir, rule);
                // A file may hold more than one namespace block — a real one
                // plus a conditionally-compiled shim of an outside type (see
                // #if !UNITY_5_3_OR_NEWER stubs). Any one of them matching
                // the expected value is enough; only flag a file where NONE
                // of its namespace blocks match.
                var namespaces = all_namespaces(path).ToList();
                var actual = namespaces.Count == 0 ? null
                    : (namespaces.Contains(expected) ? expected : namespaces[0]);

                if (actual == null) {
                    if (strict)
                        mismatches.Add($"{rel}: (no namespace), expected '{expected}'");
                    continue;
                }
                if (actual != expected)
                    mismatches.Add($"{rel}: declares '{actual}', expected '{expected}'");
            }
            mismatches.Sort(StringComparer.Ordinal);
            return mismatches;
        }

        [Test]
        public void Sources_NamespacesMatchFolders()
        {
            var root = ConventionScan.find_dir(ScriptsRule.RootDir);
            Assume.That(root, Is.Not.Empty, $"{ScriptsRule.RootDir}/ source root not found from test dir.");

            var mismatches = scan_for_mismatches(root, ScriptsRule, strict: false);

            Assert.That(mismatches, Is.Empty,
                "namespace/folder mismatches found:\n  " + string.Join("\n  ", mismatches));
        }

        [Test]
        public void Sources_NoFileMissingNamespace()
        {
            var root = ConventionScan.find_dir(ScriptsRule.RootDir);
            Assume.That(root, Is.Not.Empty, $"{ScriptsRule.RootDir}/ source root not found from test dir.");

            var missing = scan_for_mismatches(root, ScriptsRule, strict: true)
                .Where(line => line.Contains("(no namespace)"))
                // AssemblyInfo.cs holds only assembly-level attributes; C#
                // itself puts those outside any namespace, so this file is
                // correct exactly as it is, by long-standing convention.
                .Where(line => !line.StartsWith("AssemblyInfo.cs:"))
                .ToList();

            Assert.That(missing, Is.Empty,
                "files without a namespace declaration:\n  " + string.Join("\n  ", missing));
        }
    }
}
