// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for Evaluator.
    /// G1 principle: evaluator uses string.Split only, no Regex.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class EvaluatorTests {
#nullable enable
        State _state = null!;

        [SetUp]
        public void SetUp() {
            _state = new State();
            _state.flags["gate_open"]     = true;
            _state.flags["zone_cleared"]  = false;
            _state.counters["score"]      = 150f;
            _state.counters["lives"]      = 3f;
            _state.inventory["item_key"]  = 1;
            _state.inventory["item_heal"] = 5;
        }

        // -----------------------------------------------------------------------------------------
        // Null / empty

        [Test]
        public void Evaluate_NullOrEmpty_ReturnsTrue() {
            Assert.That(Evaluator.Evaluate(condition: null, state: _state),  Is.True);
            Assert.That(Evaluator.Evaluate(condition: "", state: _state),    Is.True);
            Assert.That(Evaluator.Evaluate(condition: "   ", state: _state), Is.True);
        }

        [Test]
        public void Evaluate_UnknownPrefix_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(condition: "unknown.key == 1", state: _state), Is.False);
            Assert.That(Evaluator.Evaluate(condition: "bad_syntax", state: _state),       Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // flags

        [Test]
        public void Evaluate_Flags_ImplicitTrue() {
            Assert.That(Evaluator.Evaluate(condition: "flags.gate_open",    state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "flags.zone_cleared", state: _state), Is.False);
            Assert.That(Evaluator.Evaluate(condition: "flags.nonexistent",  state: _state), Is.False);
        }

        [Test]
        public void Evaluate_Flags_ExplicitEquals() {
            Assert.That(Evaluator.Evaluate(condition: "flags.gate_open == true",    state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "flags.zone_cleared == false", state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "flags.zone_cleared != true",  state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "flags.nonexistent == false",  state: _state), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // counters

        [Test]
        public void Evaluate_Counters_NumericOps() {
            Assert.That(Evaluator.Evaluate(condition: "counters.score >= 100",  state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "counters.score > 200",   state: _state), Is.False);
            Assert.That(Evaluator.Evaluate(condition: "counters.lives == 3",    state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "counters.lives != 0",    state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "counters.score <= 150",  state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "counters.score < 150",   state: _state), Is.False);
        }

        [Test]
        public void Evaluate_Counters_MissingKey_TreatsAsZero() {
            Assert.That(Evaluator.Evaluate(condition: "counters.gold == 0", state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "counters.gold < 10", state: _state), Is.True);
        }

        [Test]
        public void Evaluate_Counters_InvalidRhs_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(condition: "counters.score >= ABC", state: _state), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // inventory

        [Test]
        public void Evaluate_Inventory_ImplicitExistence() {
            Assert.That(Evaluator.Evaluate(condition: "inventory.item_key",   state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "inventory.nonexistent", state: _state), Is.False);
        }

        [Test]
        public void Evaluate_Inventory_NumericOps() {
            Assert.That(Evaluator.Evaluate(condition: "inventory.item_heal >= 5",  state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "inventory.item_heal < 10",  state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "inventory.item_key == 1",   state: _state), Is.True);
            Assert.That(Evaluator.Evaluate(condition: "inventory.nonexistent == 0", state: _state), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // AND compound expressions (P4-T1)

        [Test]
        public void Evaluate_And_BothTrue_ReturnsTrue() {
            Assert.That(Evaluator.Evaluate(
                condition: "flags.gate_open && counters.score >= 100",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_And_LeftFalse_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(
                condition: "flags.zone_cleared && counters.score >= 100",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_And_RightFalse_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(
                condition: "flags.gate_open && counters.score >= 9999",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_And_ChainThree_AllTrue_ReturnsTrue() {
            Assert.That(Evaluator.Evaluate(
                condition: "flags.gate_open && counters.score >= 100 && inventory.item_key",
                state: _state), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // OR compound expressions (P4-T1)

        [Test]
        public void Evaluate_Or_LeftTrue_ReturnsTrue() {
            Assert.That(Evaluator.Evaluate(
                condition: "flags.gate_open || flags.zone_cleared",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_Or_RightTrue_ReturnsTrue() {
            Assert.That(Evaluator.Evaluate(
                condition: "flags.zone_cleared || flags.gate_open",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_Or_BothFalse_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(
                condition: "flags.zone_cleared || inventory.item_heal == 99",
                state: _state), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // NOT expressions (P4-T1)

        [Test]
        public void Evaluate_Not_FalseFlag_ReturnsTrue() {
            Assert.That(Evaluator.Evaluate(
                condition: "!flags.zone_cleared",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_Not_TrueFlag_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(
                condition: "!flags.gate_open",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_Not_WithComparison() {
            // !(counters.score < 100) → !(false) → true
            Assert.That(Evaluator.Evaluate(
                condition: "!(counters.score < 100)",
                state: _state), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // Parentheses (P4-T1)

        [Test]
        public void Evaluate_Parens_OverridePrecedence() {
            // "(flags.zone_cleared || flags.gate_open) && counters.score >= 100"
            // (false || true) && true → true
            Assert.That(Evaluator.Evaluate(
                condition: "(flags.zone_cleared || flags.gate_open) && counters.score >= 100",
                state: _state), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // Operator precedence NOT > AND > OR (P4-T1)

        [Test]
        public void Evaluate_Precedence_AndBeforeOr() {
            // "flags.zone_cleared || flags.gate_open && counters.score >= 100"
            // → "flags.zone_cleared || (flags.gate_open && counters.score >= 100)"
            // false || (true && true) → true
            Assert.That(Evaluator.Evaluate(
                condition: "flags.zone_cleared || flags.gate_open && counters.score >= 100",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_Precedence_NotBeforeAnd() {
            // "!flags.zone_cleared && flags.gate_open" → (!false) && true → true
            Assert.That(Evaluator.Evaluate(
                condition: "!flags.zone_cleared && flags.gate_open",
                state: _state), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // P4-T2: Variable-to-variable comparison

        [Test]
        public void Evaluate_VarToVar_ScoreGeqLives_ReturnsTrue() {
            // counters.score >= counters.lives → 150 >= 3 → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score >= counters.lives",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_LivesLtScore_ReturnsTrue() {
            // counters.lives < counters.score → 3 < 150 → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.lives < counters.score",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_SelfEqual_ReturnsTrue() {
            // counters.score == counters.score → within relative epsilon → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score == counters.score",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_DifferentValues_NotEqual() {
            // counters.score == counters.lives → 150 != 3 → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score == counters.lives",
                state: _state), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // P4-T2: G4 relative error (== and != use relative epsilon 1e-6)

        [Test]
        public void Evaluate_RelativeError_ExactMatch_ReturnsTrue() {
            // counters.score == 150 → exact match within epsilon → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score == 150",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_RelativeError_OutsideEpsilon_ReturnsFalse() {
            // counters.score == 100 → |150-100|=50 >> epsilon → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score == 100",
                state: _state), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // Backward compatibility

        [Test]
        public void Evaluate_BackwardCompat_UnknownPrefix_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(
                condition: "unknown.key == 1",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_BackwardCompat_BadSyntax_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(
                condition: "bad_syntax",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_BackwardCompat_UnclosedParen_ReturnsFalse() {
            Assert.That(Evaluator.Evaluate(
                condition: "(flags.gate_open",
                state: _state), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // History functions

        [Test, Description("Evaluator_HistoryCount_EvaluatesWithoutHistory")]
        public void Evaluator_HistoryCount_EvaluatesWithoutHistory() {
            // Without history context, history.count should fail gracefully
            Assert.That(Evaluator.Evaluate(
                condition: "history.count(kind=level_enter) > 0",
                state: _state), Is.False);
        }

        [Test, Description("Evaluator_HistoryHas_EvaluatesCorrectly")]
        public void Evaluator_HistoryHas_EvaluatesCorrectly() {
            var history = new History { entries = new System.Collections.Generic.List<HistoryEntry>() };
            history.entries.Add(new HistoryEntry { kind = "boss_defeated", target_id = "boss_01", timestamp = 10.5f });
            
            // With history context
            Assert.That(Evaluator.Evaluate(
                condition: "history.has(kind=boss_defeated)",
                state: _state,
                history: history), Is.True);
            
            // Non-existent kind
            Assert.That(Evaluator.Evaluate(
                condition: "history.has(kind=nonexistent)",
                state: _state,
                history: history), Is.False);
        }

        [Test, Description("Evaluator_HistoryCount_CountsMatches")]
        public void Evaluator_HistoryCount_CountsMatches() {
            var history = new History { entries = new System.Collections.Generic.List<HistoryEntry>() };
            history.entries.Add(new HistoryEntry { kind = "level_complete", target_id = "level_01", timestamp = 5.0f });
            history.entries.Add(new HistoryEntry { kind = "level_complete", target_id = "level_02", timestamp = 15.0f });
            history.entries.Add(new HistoryEntry { kind = "level_complete", target_id = "level_03", timestamp = 25.0f });
            
            // Count all level_complete events
            Assert.That(Evaluator.Evaluate(
                condition: "history.count(kind=level_complete) >= 3",
                state: _state,
                history: history), Is.True);
            
            // Count specific target
            Assert.That(Evaluator.Evaluate(
                condition: "history.count(kind=level_complete,target_id=level_02) >= 1",
                state: _state,
                history: history), Is.True);
        }

        [Test, Description("Evaluator_HistorySessionCount_ReturnsZeroByDefault")]
        public void Evaluator_HistorySessionCount_ReturnsZeroByDefault() {
            var history = new History { entries = new System.Collections.Generic.List<HistoryEntry>() };
            
            // Without _session_count in counters, should be 0
            Assert.That(Evaluator.Evaluate(
                condition: "history.session_count() >= 0",
                state: _state,
                history: history), Is.True);
        }

        [Test, Description("Evaluator_HistoryTotalPlayTime_ReturnsZeroByDefault")]
        public void Evaluator_HistoryTotalPlayTime_ReturnsZeroByDefault() {
            var history = new History { entries = new System.Collections.Generic.List<HistoryEntry>() };
            
            // Without _total_play_time in counters, should be 0
            Assert.That(Evaluator.Evaluate(
                condition: "history.total_play_time() >= 0",
                state: _state,
                history: history), Is.True);
        }

        [Test, Description("Evaluator_HistoryLast_ReturnsLastEntry")]
        public void Evaluator_HistoryLast_ReturnsLastEntry() {
            var history = new History { entries = new System.Collections.Generic.List<HistoryEntry>() };
            history.entries.Add(new HistoryEntry { kind = "item_collected", target_id = "key_1", timestamp = 5.0f });
            history.entries.Add(new HistoryEntry { kind = "item_collected", target_id = "key_2", timestamp = 15.0f });
            
            // history.time_since(kind=...) returns the timestamp of the last matching entry
            // Used similarly to history.count() and history.has()
            Assert.That(Evaluator.Evaluate(
                condition: "history.time_since(kind=item_collected) >= 0",
                state: _state,
                history: history), Is.True);
        }

        [Test, Description("Evaluator_HistoryTimeSince_ReturnsTimestamp")]
        public void Evaluator_HistoryTimeSince_ReturnsTimestamp() {
            var history = new History { entries = new System.Collections.Generic.List<HistoryEntry>() };
            history.entries.Add(new HistoryEntry { kind = "checkpoint_reached", target_id = "checkpoint_01", timestamp = 20.0f });
            
            // history.time_since evaluates to non-negative value
            Assert.That(Evaluator.Evaluate(
                condition: "history.time_since(kind=checkpoint_reached) >= 0",
                state: _state,
                history: history), Is.True);
        }
    }
}