// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using Germio.Core;
using Germio.Model;

namespace Germio.Tests.Core {
    /// <summary>
    /// Cookbook debug tests (Phase 5.8).
    /// Tests for practical cookbook examples.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class CookbookDebugTests {
#nullable enable

        static readonly string DOCS_DIR = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory,
                         "../../../../../docs"));

        string? _cookbook;

        [SetUp]
        public void SetUp() {
            string path = Path.Combine(DOCS_DIR, "dsl_cookbook.md");
            _cookbook = File.ReadAllText(path: path);
        }

        [Test]
        public void Debug_FindFailingExamples() {
            var matches = Regex.Matches(_cookbook!,
                @"```json\s*\n(.*?)\n```", RegexOptions.Singleline);

            int exampleNum = 0;
            int passed = 0;
            var failed = new List<(int index, string reason)>();

            foreach (Match m in matches) {
                exampleNum++;
                string json = m.Groups[1].Value.Trim();
                try {
                    var scenario = JsonConvert.DeserializeObject<Scenario>(json);
                    if (scenario != null) {
                        var results = Validator.Validate(scenario: scenario);
                        var errors = results.Where(r => r.Severity == ValidationLevel.Error).ToList();
                        if (errors.Count == 0) {
                            passed++;
                        } else {
                            failed.Add((exampleNum, string.Join(" | ", errors.Select(e => $"{e.RuleID}: {e.Message}"))));
                        }
                    } else {
                        failed.Add((exampleNum, "Deserialization returned null"));
                    }
                } catch (Exception ex) {
                    failed.Add((exampleNum, $"Exception: {ex.Message}"));
                }
            }

            TestContext.Out.WriteLine($"\nTotal: {exampleNum}, Passed: {passed}, Failed: {failed.Count}");
            TestContext.Out.WriteLine($"Passed ratio: {passed}/{exampleNum} ({100.0 * passed / exampleNum:F1}%)");
            TestContext.Out.WriteLine($"Threshold: {matches.Count - 5} required\n");

            foreach (var (index, reason) in failed) {
                TestContext.Out.WriteLine($"[FAIL] Example {index}: {reason}");
            }

            Assert.That(passed, Is.GreaterThanOrEqualTo(matches.Count - 5),
                $"At least (count - 5) cookbook examples must pass validation; " +
                $"total={matches.Count}, passed={passed}");
        }
    }
}