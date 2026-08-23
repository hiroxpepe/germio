// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {

    /// <summary>
    /// Verifies that every rule V001–V012 produces a ValidationResult with:
    ///   • correct rule_id
    ///   • non-empty location.JSONPath
    ///   • ToLlmReadable() output containing "Path:", "Cause:", "Fix:" sections
    ///
    /// One test class per source class (1:1 mapping with Validator.cs / ValidatorTests.cs).
    /// These tests focus exclusively on the G12 LLM-readable format contract.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorLlmFormatTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Private helpers — scenario builders

        static Rule makeRule(string id, string condition = "flags.gate_open", bool once = true, Command? command = null) =>
            new Rule {
                id        = id,
                trigger   = "zone_trigger",
                condition = condition,
                once      = once,
                command   = command ?? new Command { request_transition = "next_level" }
            };

        static Node makeNode(string id, string kind = "level", List<Rule>? rules = null, List<Next>? next = null, List<Node>? children = null) =>
            new Node {
                id       = id,
                name     = id,
                kind     = kind,
                scene    = id,
                rules    = rules ?? new List<Rule>(),
                next     = next ?? new List<Next>(),
                children = children ?? new List<Node>()
            };

        static State makeState(string flag_key = "gate_open") {
            var state = new State();
            state.flags[flag_key]       = false;
            state.counters["score"]     = 0f;
            state.inventory["item_key"] = 0;
            return state;
        }

        static ValidationResult? findResult(List<ValidationResult> results, string rule_id) =>
            results.FirstOrDefault(r => r.RuleID == rule_id);

        static void assertLlmFormat(ValidationResult result) {
            string text = result.ToLlmReadable();
            Assert.That(text, Does.Contain("Path:"),  $"[{result.RuleID}] ToLlmReadable() missing 'Path:'");
            Assert.That(text, Does.Contain("Cause:"), $"[{result.RuleID}] ToLlmReadable() missing 'Cause:'");
            Assert.That(text, Does.Contain("Fix:"),   $"[{result.RuleID}] ToLlmReadable() missing 'Fix:'");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V001: undefined flag key

        [Test]
        public void V001_UndefinedFlagKey_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                rules: new List<Rule> { makeRule(id: "r1", condition: "flags.undefined_flag") });
            var scenario = new Scenario { initial_state = new State(), root = root };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V001");
            Assert.That(result, Is.Not.Null, "Expected V001 result");
            Assert.That(result!.RuleID, Is.EqualTo("V001"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V002: undefined counter key

        [Test]
        public void V002_UndefinedCounterKey_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                rules: new List<Rule> { makeRule(id: "r1", condition: "counters.missing_score >= 10") });
            var scenario = new Scenario { initial_state = new State(), root = root };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V002");
            Assert.That(result, Is.Not.Null, "Expected V002 result");
            Assert.That(result!.RuleID, Is.EqualTo("V002"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V003: undefined inventory key

        [Test]
        public void V003_UndefinedInventoryKey_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                rules: new List<Rule> { makeRule(id: "r1", condition: "inventory.missing_item") });
            var scenario = new Scenario {
                initial_state = new State(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V003");
            Assert.That(result, Is.Not.Null, "Expected V003 result");
            Assert.That(result!.RuleID, Is.EqualTo("V003"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V004: duplicate node.id

        [Test]
        public void V004_DuplicateNodeId_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1"),
                    makeNode(id: "lv1")  // duplicate
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V004");
            Assert.That(result, Is.Not.Null, "Expected V004 result");
            Assert.That(result!.RuleID, Is.EqualTo("V004"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V005: duplicate rule.id

        [Test]
        public void V005_DuplicateRuleId_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1",
                        rules: new List<Rule> {
                            makeRule(id: "r_dup"),
                            makeRule(id: "r_dup") })  // duplicate rule id
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V005");
            Assert.That(result, Is.Not.Null, "Expected V005 result");
            Assert.That(result!.RuleID, Is.EqualTo("V005"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V006: dangling next.id

        [Test]
        public void V006_DanglingNextId_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1",
                        next: new List<Next> { new Next { id = "nonexistent_level" } })
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V006");
            Assert.That(result, Is.Not.Null, "Expected V006 result");
            Assert.That(result!.RuleID, Is.EqualTo("V006"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V007: empty condition (always fires)

        [Test]
        public void V007_EmptyCondition_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1",
                        rules: new List<Rule> { makeRule(id: "r1", condition: "") })
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V007");
            Assert.That(result, Is.Not.Null, "Expected V007 result");
            Assert.That(result!.RuleID, Is.EqualTo("V007"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V008: once=false with set_flag (infinite-loop risk)

        [Test]
        public void V008_OnceFalseWithSetFlag_HasLlmFormat() {
            var cmd = new Command { set_flag = new SetFlag { key = "gate_open", value = true } };
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1",
                        rules: new List<Rule> {
                            makeRule(id: "r1", once: false, command: cmd) })
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V008");
            Assert.That(result, Is.Not.Null, "Expected V008 result");
            Assert.That(result!.RuleID, Is.EqualTo("V008"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V009: DSL parse error

        [Test]
        public void V009_ParseError_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1",
                        rules: new List<Rule> { makeRule(id: "r1", condition: "((( bad syntax") })
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V009");
            Assert.That(result, Is.Not.Null, "Expected V009 result");
            Assert.That(result!.RuleID, Is.EqualTo("V009"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V010: empty command (no effect)

        [Test]
        public void V010_EmptyCommand_HasLlmFormat() {
            var empty_cmd = new Command();  // all fields null → no effect
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1",
                        rules: new List<Rule> { makeRule(id: "r1", command: empty_cmd) })
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V010");
            Assert.That(result, Is.Not.Null, "Expected V010 result");
            Assert.That(result!.RuleID, Is.EqualTo("V010"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V011: dead end (no rules, no next)

        [Test]
        public void V011_DeadEndNode_HasLlmFormat() {
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> {
                    makeNode(id: "lv1")  // no rules, no next
                });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V011");
            Assert.That(result, Is.Not.Null, "Expected V011 result");
            Assert.That(result!.RuleID, Is.EqualTo("V011"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // V012: circular transition chain

        [Test]
        public void V012_CircularTransition_HasLlmFormat() {
            // lv1 → lv2 → lv1 forms a cycle
            var lv1 = makeNode(id: "lv1", next: new List<Next> { new Next { id = "lv2" } });
            var lv2 = makeNode(id: "lv2", next: new List<Next> { new Next { id = "lv1" } });
            var root = makeNode(id: "root", kind: "root",
                children: new List<Node> { lv1, lv2 });
            var scenario = new Scenario {
                initial_state = makeState(),
                root          = root
            };

            var results = Validator.Validate(scenario: scenario);
            var result  = findResult(results: results, rule_id: "V012");
            Assert.That(result, Is.Not.Null, "Expected V012 result");
            Assert.That(result!.RuleID, Is.EqualTo("V012"));
            Assert.That(result.Location.JSONPath, Is.Not.Null.And.Not.Empty);
            assertLlmFormat(result: result);
        }
    }
}
