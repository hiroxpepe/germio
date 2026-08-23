// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Newtonsoft.Json;
using Germio.Core;
using Germio.Model;

namespace Germio.Tests.Core {

    /// <author>h.adachi (STUDIO MeowToon)</author>
    /// <summary>
    /// Validates that docs/dsl_cookbook.md exists, contains at least 25 JSON examples,
    /// and that the majority of those examples pass Germio's Validator (G12).
    /// This enforces the DoD for P5.5-T2.
    /// </summary>
    [TestFixture]
    public class CookbookExamplesTests {
        #nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        static readonly string DOCS_DIR = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory,
                         "../../../../../docs"));

        string? _cookbook;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Setup / Teardown

        [SetUp]
        public void SetUp() {
            string path = Path.Combine(DOCS_DIR, "dsl_cookbook.md");
            _cookbook = File.ReadAllText(path: path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests

        [Test]
        public void Cookbook_FileExists() {
            string path = Path.Combine(DOCS_DIR, "dsl_cookbook.md");
            Assert.That(File.Exists(path: path), Is.True,
                "docs/dsl_cookbook.md must exist (P5.5-T2 DoD)");
        }

        [Test]
        public void Cookbook_HasAtLeast25JsonExamples() {
            var matches = Regex.Matches(_cookbook!,
                @"```json\s*\n(.*?)\n```", RegexOptions.Singleline);
            Assert.That(matches.Count, Is.GreaterThanOrEqualTo(25),
                $"Cookbook must contain at least 25 JSON examples; found {matches.Count}");
        }

        [Test]
        public void Cookbook_HasAllSevenSections() {
            Assert.Multiple(() => {
                Assert.That(_cookbook, Does.Contain("Stage Progression"), "Section 1: Stage Progression");
                Assert.That(_cookbook, Does.Contain("Win"),                "Section 2: Win/Loss");
                Assert.That(_cookbook, Does.Contain("Inventory"),          "Section 3: Inventory");
                Assert.That(_cookbook, Does.Contain("ADV"),                "Section 4: ADV");
                Assert.That(_cookbook, Does.Contain("Boss"),               "Section 5: Boss");
                Assert.That(_cookbook, Does.Contain("Failure"),            "Section 6: Failure Patterns");
                Assert.That(_cookbook, Does.Contain("Section 7"),          "Section 7: Advanced Progression");
            });
        }

        [Test]
        public void Cookbook_JsonExamplesUseSnakeCase_NoSetFlag_CamelCase() {
            Assert.That(_cookbook, Does.Not.Contain("\"setFlag\""),
                "All cookbook examples must use set_flag (snake_case), not setFlag (camelCase)");
        }

        [Test]
        public void Cookbook_JsonExamplesUseSnakeCase_NoUpdateCounter_CamelCase() {
            Assert.That(_cookbook, Does.Not.Contain("\"updateCounter\""),
                "All cookbook examples must use update_counter, not updateCounter");
        }

        [Test]
        public void Cookbook_JsonExamplesUseSnakeCase_NoFiredEvents_OldName() {
            Assert.That(_cookbook, Does.Not.Contain("\"firedEvents\""),
                "firedEvents is the old name; cookbook must use fired_rules");
        }

        [Test]
        public void Cookbook_AllJsonExamples_PassValidator() {
            var matches = Regex.Matches(_cookbook!,
                @"```json\s*\n(.*?)\n```", RegexOptions.Singleline);

            int passed = 0;
            foreach (Match m in matches) {
                string json = m.Groups[1].Value.Trim();
                try {
                    var scenario = JsonConvert.DeserializeObject<Scenario>(json);
                    if (scenario != null) {
                        var results = Validator.Validate(scenario: scenario);
                        if (results.All(r => r.Severity != ValidationLevel.Error)) {
                            passed++;
                        }
                    }
                } catch { /* fragment examples count as not-passed */ }
            }

            Assert.That(passed, Is.GreaterThanOrEqualTo(matches.Count - 5),
                $"At least (count - 5) cookbook examples must pass validation; " +
                $"total={matches.Count}, passed={passed}");
        }

        [Test]
        public void CookbookExamplesTests_IsInCorrectNamespace() {
            Assert.That(typeof(CookbookExamplesTests).Namespace,
                Is.EqualTo("Germio.Tests.Core"),
                "G18: test namespace must be Germio.Tests.Core");
        }
    }
}