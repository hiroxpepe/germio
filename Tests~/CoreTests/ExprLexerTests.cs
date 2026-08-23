// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for <see cref="ExprLexer"/>.
    /// Covers token kinds, literal values, column positions, and error cases.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ExprLexerTests {
#nullable enable

        [Test]
        public void Lexer_SimpleFlag_ProducesTokens() {
            var tokens = ExprLexer.Tokenize(source: "flags.gate_open");
            // Expected: Identifier("flags"), Dot, Identifier("gate_open"), EOF
            Assert.That(tokens.Count, Is.EqualTo(4));
            Assert.That(tokens[0].Kind,  Is.EqualTo(TokenKind.Identifier));
            Assert.That(tokens[0].Value, Is.EqualTo("flags"));
            Assert.That(tokens[1].Kind,  Is.EqualTo(TokenKind.Dot));
            Assert.That(tokens[2].Kind,  Is.EqualTo(TokenKind.Identifier));
            Assert.That(tokens[2].Value, Is.EqualTo("gate_open"));
            Assert.That(tokens[3].Kind,  Is.EqualTo(TokenKind.EOF));
        }

        [Test]
        public void Lexer_NumberLiteral_ProducesNumberToken() {
            var tokens = ExprLexer.Tokenize(source: "100");
            Assert.That(tokens[0].Kind,  Is.EqualTo(TokenKind.Number));
            Assert.That(tokens[0].Value, Is.EqualTo("100"));
        }

        [Test]
        public void Lexer_FloatLiteral_ProducesNumberToken() {
            var tokens = ExprLexer.Tokenize(source: "3.14");
            Assert.That(tokens[0].Kind,  Is.EqualTo(TokenKind.Number));
            Assert.That(tokens[0].Value, Is.EqualTo("3.14"));
        }

        [Test]
        public void Lexer_BoolLiterals_ProduceBoolTokens() {
            var t_tokens = ExprLexer.Tokenize(source: "true");
            var f_tokens = ExprLexer.Tokenize(source: "false");
            Assert.That(t_tokens[0].Kind, Is.EqualTo(TokenKind.BoolTrue));
            Assert.That(f_tokens[0].Kind, Is.EqualTo(TokenKind.BoolFalse));
        }

        [Test]
        public void Lexer_Operators_ProduceCorrectKinds() {
            Assert.That(ExprLexer.Tokenize(source: "==")[0].Kind, Is.EqualTo(TokenKind.EqEq));
            Assert.That(ExprLexer.Tokenize(source: "!=")[0].Kind, Is.EqualTo(TokenKind.NotEq));
            Assert.That(ExprLexer.Tokenize(source: ">=")[0].Kind, Is.EqualTo(TokenKind.GtEq));
            Assert.That(ExprLexer.Tokenize(source: "<=")[0].Kind, Is.EqualTo(TokenKind.LtEq));
            Assert.That(ExprLexer.Tokenize(source: ">")[0].Kind,  Is.EqualTo(TokenKind.Gt));
            Assert.That(ExprLexer.Tokenize(source: "<")[0].Kind,  Is.EqualTo(TokenKind.Lt));
            Assert.That(ExprLexer.Tokenize(source: "&&")[0].Kind, Is.EqualTo(TokenKind.And));
            Assert.That(ExprLexer.Tokenize(source: "||")[0].Kind, Is.EqualTo(TokenKind.Or));
            Assert.That(ExprLexer.Tokenize(source: "!")[0].Kind,  Is.EqualTo(TokenKind.Not));
        }

        [Test]
        public void Lexer_Parens_ProduceCorrectKinds() {
            var tokens = ExprLexer.Tokenize(source: "()");
            Assert.That(tokens[0].Kind, Is.EqualTo(TokenKind.LeftParen));
            Assert.That(tokens[1].Kind, Is.EqualTo(TokenKind.RightParen));
        }

        [Test]
        public void Lexer_UnknownChar_ThrowsExprParseException() {
            Assert.Throws<ExprParseException>(() => ExprLexer.Tokenize(source: "flags.x @ 1"));
        }

        [Test]
        public void Lexer_ColumnPosition_IsCorrect() {
            // "flags" starts at column 0; "." at 5; "gate_open" at 6
            var tokens = ExprLexer.Tokenize(source: "flags.gate_open");
            Assert.That(tokens[0].Column, Is.EqualTo(0));
            Assert.That(tokens[1].Column, Is.EqualTo(5));
            Assert.That(tokens[2].Column, Is.EqualTo(6));
        }
    }
}