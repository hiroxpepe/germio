// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for <see cref="ExprAst"/> node types:
    /// <see cref="AccessorNode"/>, <see cref="LiteralNode"/>, <see cref="ComparisonNode"/>,
    /// <see cref="AndNode"/>, <see cref="OrNode"/>, <see cref="NotNode"/>.
    /// Tests exercise Evaluate() and GetNumeric() directly without going through ExprParser.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ExprAstTests {
#nullable enable
        State _state = null!;

        [SetUp]
        public void SetUp() {
            _state = new State();
            _state.flags["gate_open"]   = true;
            _state.flags["cleared"]     = false;
            _state.counters["score"]    = 150f;
            _state.counters["lives"]    = 3f;
            _state.counters["near_zero"] = 1e-7f;
            _state.inventory["key"]     = 2;
            _state.inventory["coin"]    = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // AccessorNode: implicit bool semantics

        [Test]
        public void AccessorNode_FlagTrue_EvaluatesTrue() {
            var node = new AccessorNode(prefix: "flags", key: "gate_open");
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void AccessorNode_FlagFalse_EvaluatesFalse() {
            var node = new AccessorNode(prefix: "flags", key: "cleared");
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void AccessorNode_FlagMissing_EvaluatesFalse() {
            var node = new AccessorNode(prefix: "flags", key: "nonexistent");
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void AccessorNode_InventoryPositive_EvaluatesTrue() {
            var node = new AccessorNode(prefix: "inventory", key: "key");
            Assert.That(node.Evaluate(state: _state), Is.True);   // key == 2 > 0
        }

        [Test]
        public void AccessorNode_InventoryZero_EvaluatesFalse() {
            var node = new AccessorNode(prefix: "inventory", key: "coin");
            Assert.That(node.Evaluate(state: _state), Is.False);  // coin == 0
        }

        [Test]
        public void AccessorNode_CounterImplicit_EvaluatesFalse() {
            // counters have no implicit bool — always false without comparison
            var node = new AccessorNode(prefix: "counters", key: "score");
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // AccessorNode: GetNumeric

        [Test]
        public void AccessorNode_FlagTrue_GetNumericReturnsOne() {
            var node = new AccessorNode(prefix: "flags", key: "gate_open");
            Assert.That(node.GetNumeric(state: _state), Is.EqualTo(1.0));
        }

        [Test]
        public void AccessorNode_FlagFalse_GetNumericReturnsZero() {
            var node = new AccessorNode(prefix: "flags", key: "cleared");
            Assert.That(node.GetNumeric(state: _state), Is.EqualTo(0.0));
        }

        [Test]
        public void AccessorNode_Counter_GetNumericReturnsValue() {
            var node = new AccessorNode(prefix: "counters", key: "score");
            Assert.That(node.GetNumeric(state: _state), Is.EqualTo(150.0));
        }

        [Test]
        public void AccessorNode_Inventory_GetNumericReturnsValue() {
            var node = new AccessorNode(prefix: "inventory", key: "key");
            Assert.That(node.GetNumeric(state: _state), Is.EqualTo(2.0));
        }

        [Test]
        public void AccessorNode_MissingCounter_GetNumericReturnsZero() {
            var node = new AccessorNode(prefix: "counters", key: "missing");
            Assert.That(node.GetNumeric(state: _state), Is.EqualTo(0.0));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // LiteralNode

        [Test]
        public void LiteralNode_NumericNonZero_EvaluatesTrue() {
            var node = new LiteralNode(value: 1.5);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void LiteralNode_NumericZero_EvaluatesFalse() {
            var node = new LiteralNode(value: 0.0);
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void LiteralNode_BoolTrue_EvaluatesTrue() {
            var node = new LiteralNode(value: true);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void LiteralNode_BoolFalse_EvaluatesFalse() {
            var node = new LiteralNode(value: false);
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void LiteralNode_NumericType_IsBoolFalse() {
            var node = new LiteralNode(value: 42.0);
            Assert.That(node.IsBool, Is.False);
        }

        [Test]
        public void LiteralNode_BoolType_IsBoolTrue() {
            var node = new LiteralNode(value: true);
            Assert.That(node.IsBool, Is.True);
        }

        [Test]
        public void LiteralNode_NumericValue_IsPreserved() {
            var node = new LiteralNode(value: 3.14);
            Assert.That(node.NumericValue, Is.EqualTo(3.14));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ComparisonNode: numeric operators

        [Test]
        public void ComparisonNode_GreaterOrEqual_True() {
            var left  = new AccessorNode(prefix: "counters", key: "score");
            var right = new LiteralNode(value: 100.0);
            var node  = new ComparisonNode(left: left, op: ">=", right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);   // 150 >= 100
        }

        [Test]
        public void ComparisonNode_GreaterOrEqual_False() {
            var left  = new AccessorNode(prefix: "counters", key: "score");
            var right = new LiteralNode(value: 200.0);
            var node  = new ComparisonNode(left: left, op: ">=", right: right);
            Assert.That(node.Evaluate(state: _state), Is.False);  // 150 >= 200 false
        }

        [Test]
        public void ComparisonNode_LessThan_True() {
            var left  = new AccessorNode(prefix: "counters", key: "lives");
            var right = new LiteralNode(value: 10.0);
            var node  = new ComparisonNode(left: left, op: "<", right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);   // 3 < 10
        }

        [Test]
        public void ComparisonNode_ExactEqual_True() {
            // 150 == 150 — within G4 relative epsilon
            var left  = new AccessorNode(prefix: "counters", key: "score");
            var right = new LiteralNode(value: 150.0);
            var node  = new ComparisonNode(left: left, op: "==", right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void ComparisonNode_NotEqual_True() {
            // 150 != 3 → true
            var left  = new AccessorNode(prefix: "counters", key: "score");
            var right = new LiteralNode(value: 3.0);
            var node  = new ComparisonNode(left: left, op: "!=", right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ComparisonNode: bool comparison (flags == true/false)

        [Test]
        public void ComparisonNode_FlagEqTrue_True() {
            var left  = new AccessorNode(prefix: "flags", key: "gate_open");
            var right = new LiteralNode(value: true);
            var node  = new ComparisonNode(left: left, op: "==", right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);   // true == true
        }

        [Test]
        public void ComparisonNode_FlagEqFalse_True() {
            var left  = new AccessorNode(prefix: "flags", key: "cleared");
            var right = new LiteralNode(value: false);
            var node  = new ComparisonNode(left: left, op: "==", right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);   // false == false
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ComparisonNode: G4 relative error

        [Test]
        public void ComparisonNode_RelativeError_NearZeroEqualsZero() {
            // near_zero ≈ 1e-7, |1e-7 - 0| = 1e-7 <= 1e-6 * max(1e-7, 0, 1.0) = 1e-6 → true
            var left  = new AccessorNode(prefix: "counters", key: "near_zero");
            var right = new LiteralNode(value: 0.0);
            var node  = new ComparisonNode(left: left, op: "==", right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // AndNode

        [Test]
        public void AndNode_BothTrue_ReturnsTrue() {
            var left  = new LiteralNode(value: true);
            var right = new LiteralNode(value: true);
            var node  = new AndNode(left: left, right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void AndNode_LeftFalse_ReturnsFalse() {
            var left  = new LiteralNode(value: false);
            var right = new LiteralNode(value: true);
            var node  = new AndNode(left: left, right: right);
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void AndNode_RightFalse_ReturnsFalse() {
            var left  = new LiteralNode(value: true);
            var right = new LiteralNode(value: false);
            var node  = new AndNode(left: left, right: right);
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // OrNode

        [Test]
        public void OrNode_LeftTrue_ReturnsTrue() {
            var left  = new LiteralNode(value: true);
            var right = new LiteralNode(value: false);
            var node  = new OrNode(left: left, right: right);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void OrNode_BothFalse_ReturnsFalse() {
            var left  = new LiteralNode(value: false);
            var right = new LiteralNode(value: false);
            var node  = new OrNode(left: left, right: right);
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // NotNode

        [Test]
        public void NotNode_TrueOperand_ReturnsFalse() {
            var operand = new LiteralNode(value: true);
            var node    = new NotNode(operand: operand);
            Assert.That(node.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void NotNode_FalseOperand_ReturnsTrue() {
            var operand = new LiteralNode(value: false);
            var node    = new NotNode(operand: operand);
            Assert.That(node.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void NotNode_FlagAccessor_Inverts() {
            var operand = new AccessorNode(prefix: "flags", key: "cleared"); // false
            var node    = new NotNode(operand: operand);
            Assert.That(node.Evaluate(state: _state), Is.True);  // !false → true
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // HistoryCountNode

        [Test, Description("ExprAst_HistoryCountNode_HasKindAndTargetId")]
        public void ExprAst_HistoryCountNode_HasKindAndTargetId() {
            var node = new HistoryCountNode(kind: "level_enter", target_id: "level_01");
            Assert.That(node.Kind, Is.EqualTo("level_enter"));
            Assert.That(node.TargetID, Is.EqualTo("level_01"));
        }

        [Test, Description("ExprAst_HistoryCountNode_TargetIdOptional")]
        public void ExprAst_HistoryCountNode_TargetIdOptional() {
            var node = new HistoryCountNode(kind: "level_enter", target_id: null);
            Assert.That(node.Kind, Is.EqualTo("level_enter"));
            Assert.That(node.TargetID, Is.Null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // HistoryHasNode

        [Test, Description("ExprAst_HistoryHasNode_HasKindAndTargetId")]
        public void ExprAst_HistoryHasNode_HasKindAndTargetId() {
            var node = new HistoryHasNode(kind: "boss_defeated", target_id: "boss_01");
            Assert.That(node.Kind, Is.EqualTo("boss_defeated"));
            Assert.That(node.TargetID, Is.EqualTo("boss_01"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // HistoryLastNode

        [Test, Description("ExprAst_HistoryLastNode_HasKindTargetIdAndProperty")]
        public void ExprAst_HistoryLastNode_HasKindTargetIdAndProperty() {
            var node = new HistoryLastNode(kind: "item_collected", target_id: "key", property: "timestamp");
            Assert.That(node.Kind, Is.EqualTo("item_collected"));
            Assert.That(node.TargetID, Is.EqualTo("key"));
            Assert.That(node.Property, Is.EqualTo("timestamp"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // HistoryTimeSinceNode

        [Test, Description("ExprAst_HistoryTimeSinceNode_HasKindAndTargetId")]
        public void ExprAst_HistoryTimeSinceNode_HasKindAndTargetId() {
            var node = new HistoryTimeSinceNode(kind: "checkpoint_reached", target_id: "checkpoint_02");
            Assert.That(node.Kind, Is.EqualTo("checkpoint_reached"));
            Assert.That(node.TargetID, Is.EqualTo("checkpoint_02"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // HistorySessionCountNode

        [Test, Description("ExprAst_HistorySessionCountNode_Instantiates")]
        public void ExprAst_HistorySessionCountNode_Instantiates() {
            var node = new HistorySessionCountNode();
            Assert.That(node, Is.Not.Null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // HistoryTotalPlayTimeNode

        [Test, Description("ExprAst_HistoryTotalPlayTimeNode_Instantiates")]
        public void ExprAst_HistoryTotalPlayTimeNode_Instantiates() {
            var node = new HistoryTotalPlayTimeNode();
            Assert.That(node, Is.Not.Null);
        }
    }
}