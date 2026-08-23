// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {

    /// <summary>
    /// Advanced boundary and extreme-value tests for Evaluator.
    /// Covers: variable-to-variable extended operators, cross-prefix comparison,
    /// G4 relative-error boundary (at-epsilon / just-over-epsilon), large floats,
    /// NaN, and Infinity.
    /// Does NOT duplicate the cases already in EvaluatorTests.cs.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class EvaluatorAdvancedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        State _state = null!;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // SetUp

        [SetUp]
        public void SetUp() {
            _state = new State();
            _state.counters["score"]     = 150f;
            _state.counters["lives"]     = 3f;
            _state.counters["nan_val"]   = float.NaN;
            _state.counters["inf_pos"]   = float.PositiveInfinity;
            _state.counters["inf_neg"]   = float.NegativeInfinity;
            // Boundary values for relative error tests using integer-exact floats:
            // diff = 1, scale = max(1000001, 1000000, 1) = 1000001, 1e-6 * scale ≈ 1.000001 → 1.0 ≤ 1.000001 → TRUE
            _state.counters["large_a"]   = 1000000f;
            _state.counters["large_b"]   = 1000001f;
            // diff = 2, scale = max(1000002, 1000000, 1) = 1000002, 1e-6 * scale ≈ 1.000002 → 2.0 > 1.000002 → FALSE
            _state.counters["large_c"]   = 1000002f;
            _state.inventory["item_heal"] = 5;
            _state.inventory["item_key"]  = 1;
            _state.flags["gate_open"]    = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: variable-to-variable — extended operators

        [Test]
        public void Evaluate_VarToVar_NotEqual_ReturnsTrue() {
            // counters.score != counters.lives → !relativeEqual(150, 3) → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score != counters.lives",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_GreaterThan_ReturnsTrue() {
            // counters.score > counters.lives → 150 > 3 → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score > counters.lives",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_LessThanOrEqual_ReturnsFalse() {
            // counters.score <= counters.lives → 150 <= 3 (and not within epsilon) → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score <= counters.lives",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_VarToVar_SameVarGreaterThanOrEqual_ReturnsTrue() {
            // counters.lives >= counters.lives → 3 >= 3 (exact or relativeEqual) → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.lives >= counters.lives",
                state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: cross-prefix variable-to-variable

        [Test]
        public void Evaluate_VarToVar_CrossPrefix_CounterGreaterThanInventory() {
            // counters.score > inventory.item_heal → 150 > 5 → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score > inventory.item_heal",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_InventoryToInventory_GreaterThan_ReturnsTrue() {
            // inventory.item_heal > inventory.item_key → 5 > 1 → true
            Assert.That(Evaluator.Evaluate(
                condition: "inventory.item_heal > inventory.item_key",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_InventoryToInventory_Equal_ReturnsFalse() {
            // inventory.item_heal == inventory.item_key → 5 == 1 → false
            Assert.That(Evaluator.Evaluate(
                condition: "inventory.item_heal == inventory.item_key",
                state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: missing variable treated as zero in var-to-var

        [Test]
        public void Evaluate_VarToVar_BothMissing_Equal_ReturnsTrue() {
            // counters.ghost == counters.phantom → 0 == 0, diff=0, scale=1 → 0 ≤ 1e-6 → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.ghost == counters.phantom",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_MissingVsNonzero_NotEqual_ReturnsTrue() {
            // counters.ghost != counters.score → 0 != 150 → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.ghost != counters.score",
                state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: NOT applied to var-to-var comparison

        [Test]
        public void Evaluate_Not_VarToVar_Equal_ReturnsTrue() {
            // !(counters.score == counters.lives) → !(false) → true
            Assert.That(Evaluator.Evaluate(
                condition: "!(counters.score == counters.lives)",
                state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: G4 relative error — boundary (literal RHS)

        [Test]
        public void Evaluate_RelativeError_AtBoundary_Literal_ReturnsTrue() {
            // counters.large_a == 1000001 → diff=1, scale=1000001, 1e-6*scale≈1.000001, 1.0 ≤ 1.000001 → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.large_a == 1000001",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_RelativeError_JustOverBoundary_Literal_ReturnsFalse() {
            // counters.large_a == 1000002 → diff=2, scale=1000002, 1e-6*scale≈1.000002, 2.0 > 1.000002 → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.large_a == 1000002",
                state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: G4 relative error — boundary (var-to-var)

        [Test]
        public void Evaluate_RelativeError_AtBoundary_VarToVar_ReturnsTrue() {
            // counters.large_a == counters.large_b → 1000000 vs 1000001 → at boundary → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.large_a == counters.large_b",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_RelativeError_JustOverBoundary_VarToVar_ReturnsFalse() {
            // counters.large_a == counters.large_c → 1000000 vs 1000002 → just over → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.large_a == counters.large_c",
                state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: NaN (G4 rule: NaN == anything → false)

        [Test]
        public void Evaluate_NaN_EqualSelf_ReturnsFalse() {
            // counters.nan_val == counters.nan_val → relativeEqual(NaN, NaN) → isNaN → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.nan_val == counters.nan_val",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_NaN_EqualLiteral_ReturnsFalse() {
            // counters.nan_val == 0 → relativeEqual(NaN, 0) → isNaN → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.nan_val == 0",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_NaN_NotEqual_ReturnsTrue() {
            // counters.nan_val != counters.score → !relativeEqual(NaN, 150) → !false → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.nan_val != counters.score",
                state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: Infinity (G4 rule: Inf == Inf → true via exact equality)

        [Test]
        public void Evaluate_Infinity_PositiveEqualSelf_ReturnsTrue() {
            // counters.inf_pos == counters.inf_pos → isInfinity → +Inf == +Inf → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.inf_pos == counters.inf_pos",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_Infinity_PositiveEqualNegative_ReturnsFalse() {
            // counters.inf_pos == counters.inf_neg → isInfinity → +Inf == -Inf → false
            Assert.That(Evaluator.Evaluate(
                condition: "counters.inf_pos == counters.inf_neg",
                state: _state), Is.False);
        }

        [Test]
        public void Evaluate_Infinity_GreaterThanFinite_ReturnsTrue() {
            // counters.inf_pos > counters.score → +Inf > 150 → true (raw > comparison)
            Assert.That(Evaluator.Evaluate(
                condition: "counters.inf_pos > counters.score",
                state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: compound expressions combining var-to-var with flags

        [Test]
        public void Evaluate_VarToVar_And_Flag_AllTrue_ReturnsTrue() {
            // counters.score >= counters.lives && flags.gate_open → true && true → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.score >= counters.lives && flags.gate_open",
                state: _state), Is.True);
        }

        [Test]
        public void Evaluate_VarToVar_Or_MissingKey_OneTrue_ReturnsTrue() {
            // counters.ghost > 0 || counters.score >= counters.lives → false || true → true
            Assert.That(Evaluator.Evaluate(
                condition: "counters.ghost > 0 || counters.score >= counters.lives",
                state: _state), Is.True);
        }
    }
}