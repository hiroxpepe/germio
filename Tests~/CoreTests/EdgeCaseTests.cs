// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Edge-case tests covering null/empty inputs, malformed expressions,
    /// empty worlds, and action payloads with null fields.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class EdgeCaseTests {
#nullable enable

        // -----------------------------------------------------------------------------------------
        // ConditionEvaluator edge cases

        [Test]
        public void Evaluate_OnlyPrefix_ReturnsFalse() {
            // "flags" with no key is treated as bad_syntax -> false
            var state = new State();
            Assert.That(Evaluator.Evaluate(condition: "flags", state: state), Is.False);
        }

        [Test]
        public void Evaluate_Counters_FloatLocale_InvariantCulture() {
            // Ensure "1.5" parses correctly regardless of system locale
            var state = new State();
            state.counters["ratio"] = 1.5f;
            Assert.That(Evaluator.Evaluate(condition: "counters.ratio >= 1.5", state: state), Is.True);
        }

        [Test]
        public void Evaluate_Counters_ExtraWhitespace_Tokenizes() {
            var state = new State();
            state.counters["score"] = 100f;
            // Leading/trailing spaces handled by Trim() in Evaluate
            Assert.That(Evaluator.Evaluate(condition: "  counters.score == 100  ", state: state), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // Executor edge cases

        [Test]
        public void Execute_NullAction_DoesNotThrow() {
            var store = new Store(new Scenario());
            Assert.DoesNotThrow(() => Executor.Execute(command: new Command(), store: store));
        }

        [Test]
        public void Execute_AllNullFields_DoesNotMarkDirty() {
            var store = new Store(new Scenario());
            Executor.Execute(command: new Command(), store: store);
            Assert.That(store.IsDirty, Is.False,
                "No-op Command should not mark dirty");
        }

        // -----------------------------------------------------------------------------------------
        // Store edge cases

        [Test]
        public void Store_EmptyWorlds_GetNextNode_ReturnsNull() {
            var root  = new Scenario();
            var store = new Store(root);
            Assert.That(store.GetNextNode(current_node_id: "level_01"), Is.Null);
        }

        [Test]
        public void Store_EmptyWorlds_DispatchTrigger_DoesNotThrow() {
            var root = new Scenario();
            root.initial_state.current_node = "level_01";
            var store = new Store(root);
            Assert.DoesNotThrow(() => store.DispatchTrigger(trigger_id: "any_trigger"));
        }

        [Test]
        public void Store_NodeWithNoRules_DispatchTrigger_DoesNotThrow() {
            var root = new Scenario();
            root.initial_state.current_node = "level_01";
            root.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id = "level_01",
                        name = "Level 1",
                        kind = "level",
                        scene = "level_01"
                    }
                }
            };
            var store = new Store(root);
            Assert.DoesNotThrow(() => store.DispatchTrigger(trigger_id: "vol_trigger"));
        }

        [Test]
        public void Store_NodeWithNoNextEntries_GetNextNode_ReturnsNull() {
            var root = new Scenario();
            root.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id = "level_01",
                        name = "Level 1",
                        kind = "level",
                        scene = "level_01"
                    }
                }
            };
            var store = new Store(root);
            Assert.That(store.GetNextNode(current_node_id: "level_01"), Is.Null);
        }
    }
}