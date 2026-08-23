// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for <see cref="ExprParser"/>.
    /// Covers accessor parsing, AND/OR/NOT, operator precedence, parentheses,
    /// variable-to-variable comparison, and error cases.
    /// Grammar:
    ///   expression = or_expr
    ///   or_expr    = and_expr  ('||' and_expr)*
    ///   and_expr   = unary_expr ('&&' unary_expr)*
    ///   unary_expr = '!' unary_expr | '(' expression ')' | comparison_or_accessor
    ///   comparison_or_accessor = accessor (op accessor_or_literal)?
    ///   accessor   = IDENT '.' IDENT
    ///   op         = '==' | '!=' | '>' | '<' | '>=' | '<='
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ExprParserTests {
#nullable enable
        State _state = null!;

        [SetUp]
        public void SetUp() {
            _state = new State();
            _state.flags["gate_open"]    = true;
            _state.flags["cleared"]      = false;
            _state.counters["score"]     = 150f;
            _state.counters["lives"]     = 3f;
            _state.counters["speed"]     = 1.5f;
            _state.inventory["key"]      = 2;
            _state.inventory["coin"]     = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Parser: simple accessor

        [Test]
        public void Parser_FlagAccessor_ReturnsAccessorNode() {
            var tokens = ExprLexer.Tokenize(source: "flags.gate_open");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<AccessorNode>());
            var node = (AccessorNode)ast;
            Assert.That(node.Prefix, Is.EqualTo("flags"));
            Assert.That(node.Key,    Is.EqualTo("gate_open"));
        }

        [Test]
        public void Parser_CounterComparison_ReturnsComparisonNode() {
            var tokens = ExprLexer.Tokenize(source: "counters.score >= 100");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<ComparisonNode>());
        }

        [Test]
        public void Parser_FlagBoolComparison_EvaluatesCorrectly() {
            var tokens = ExprLexer.Tokenize(source: "flags.gate_open == true");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.True);

            var tokens2 = ExprLexer.Tokenize(source: "flags.cleared == false");
            var ast2    = ExprParser.Parse(tokens: tokens2);
            Assert.That(ast2.Evaluate(state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Parser: AND, OR, NOT

        [Test]
        public void Parser_And_EvaluatesBothSides() {
            var tokens = ExprLexer.Tokenize(source: "flags.gate_open && counters.score >= 100");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<AndNode>());
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_And_ShortCircuitsFalseLeft() {
            var tokens = ExprLexer.Tokenize(source: "flags.cleared && counters.score >= 100");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void Parser_Or_ReturnsTrueIfEitherTrue() {
            var tokens = ExprLexer.Tokenize(source: "flags.cleared || flags.gate_open");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<OrNode>());
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_Or_ReturnsFalseIfBothFalse() {
            var tokens = ExprLexer.Tokenize(source: "flags.cleared || inventory.coin");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.False);
        }

        [Test]
        public void Parser_Not_InvertsBoolean() {
            var tokens = ExprLexer.Tokenize(source: "!flags.cleared");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<NotNode>());
            Assert.That(ast.Evaluate(state: _state), Is.True);

            var tokens2 = ExprLexer.Tokenize(source: "!flags.gate_open");
            var ast2    = ExprParser.Parse(tokens: tokens2);
            Assert.That(ast2.Evaluate(state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Parser: operator precedence (NOT > AND > OR)

        [Test]
        public void Parser_Precedence_NotBindsTighterThanAnd() {
            // "!flags.cleared && flags.gate_open" should parse as "(!flags.cleared) && flags.gate_open"
            // !(false) && true → true
            var tokens = ExprLexer.Tokenize(source: "!flags.cleared && flags.gate_open");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<AndNode>());
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_Precedence_AndBindsTighterThanOr() {
            // "flags.cleared || flags.gate_open && counters.score >= 100"
            // should parse as "flags.cleared || (flags.gate_open && counters.score >= 100)"
            // false || (true && true) → true
            var tokens = ExprLexer.Tokenize(source: "flags.cleared || flags.gate_open && counters.score >= 100");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<OrNode>());
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_Precedence_NotBindsTighterThanOr() {
            // "!flags.gate_open || flags.cleared"
            // !(true) || false → false
            var tokens = ExprLexer.Tokenize(source: "!flags.gate_open || flags.cleared");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Parser: parentheses

        [Test]
        public void Parser_Parens_OverridePrecedence() {
            // "(flags.cleared || flags.gate_open) && counters.score >= 100"
            // (false || true) && true → true
            var tokens = ExprLexer.Tokenize(source: "(flags.cleared || flags.gate_open) && counters.score >= 100");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<AndNode>());
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_Parens_NestedParens_EvaluatesCorrectly() {
            // "((flags.gate_open))" → true
            var tokens = ExprLexer.Tokenize(source: "((flags.gate_open))");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Parser: variable-to-variable comparison (P4-T2)

        [Test]
        public void Parser_VariableToVariable_CountersGe() {
            // "counters.score >= counters.lives" → 150 >= 3 → true
            var tokens = ExprLexer.Tokenize(source: "counters.score >= counters.lives");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast, Is.InstanceOf<ComparisonNode>());
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_VariableToVariable_CountersLt() {
            // "counters.lives < counters.score" → 3 < 150 → true
            var tokens = ExprLexer.Tokenize(source: "counters.lives < counters.score");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_VariableToVariable_SelfEqual() {
            // "counters.score == counters.score" → true (same value, within epsilon)
            var tokens = ExprLexer.Tokenize(source: "counters.score == counters.score");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Parser: error cases

        [Test]
        public void Parser_EmptyParens_ThrowsExprParseException() {
            var tokens = ExprLexer.Tokenize(source: "()");
            Assert.Throws<ExprParseException>(() => ExprParser.Parse(tokens: tokens));
        }

        [Test]
        public void Parser_UnclosedParen_ThrowsExprParseException() {
            var tokens = ExprLexer.Tokenize(source: "(flags.gate_open");
            Assert.Throws<ExprParseException>(() => ExprParser.Parse(tokens: tokens));
        }

        [Test]
        public void Parser_MissingRhs_ThrowsExprParseException() {
            var tokens = ExprLexer.Tokenize(source: "counters.score >=");
            Assert.Throws<ExprParseException>(() => ExprParser.Parse(tokens: tokens));
        }

        [Test]
        public void Parser_BareNumber_ThrowsExprParseException() {
            var tokens = ExprLexer.Tokenize(source: "100");
            Assert.Throws<ExprParseException>(() => ExprParser.Parse(tokens: tokens));
        }

        [Test]
        public void Parser_ExprParseException_HasColumnInfo() {
            try {
                ExprLexer.Tokenize(source: "flags.x @ 1");
                Assert.Fail("Should have thrown ExprParseException");
            } catch (ExprParseException ex) {
                Assert.That(ex.Column, Is.GreaterThanOrEqualTo(0));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Parser: complex real-world expressions

        [Test]
        public void Parser_Complex_ThreeWayAnd_EvaluatesCorrectly() {
            // "flags.gate_open && counters.score >= 100 && inventory.key"
            // true && true && (2 > 0) → true
            var tokens = ExprLexer.Tokenize(source: "flags.gate_open && counters.score >= 100 && inventory.key");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }

        [Test]
        public void Parser_Complex_NotWithComparison() {
            // "!(counters.score < 100)" → !(false) → true
            var tokens = ExprLexer.Tokenize(source: "!(counters.score < 100)");
            var ast    = ExprParser.Parse(tokens: tokens);
            Assert.That(ast.Evaluate(state: _state), Is.True);
        }
    }
}