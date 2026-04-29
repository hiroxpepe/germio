// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Germio.Core {
    /// <summary>Thrown when the condition DSL fails to parse.</summary>
    public class ExprParseException : Exception {
#nullable enable
        /// <summary>Zero-based column index where the error was detected.</summary>
        public int Column { get; }

        public ExprParseException(string message, int column = 0) : base(message) {
            Column = column;
        }
    }

    /// <summary>
    /// Recursive-descent parser for the Germio condition DSL.
    ///
    /// EBNF:
    ///   expression = or_expr
    ///   or_expr    = and_expr  ('||' and_expr)*
    ///   and_expr   = unary_expr ('&amp;&amp;' unary_expr)*
    ///   unary_expr = '!' unary_expr | '(' expression ')' | comparison_or_accessor
    ///   comparison_or_accessor = accessor (op rhs)?
    ///   rhs        = accessor | literal
    ///   accessor   = IDENT '.' IDENT
    ///   literal    = NUMBER | 'true' | 'false'
    ///   op         = '==' | '!=' | '>' | '&lt;' | '>=' | '&lt;='
    ///
    /// Operator precedence (highest to lowest): ! > &amp;&amp; > ||
    ///
    /// Throws <see cref="ExprParseException"/> on any syntax error.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class ExprParser {
#nullable enable

        /// <summary>
        /// Parses the list of tokens (produced by <see cref="ExprLexer"/>) into an AST.
        /// </summary>
        /// <param name="tokens">List of tokens including trailing EOF.</param>
        /// <returns>Root AST node.</returns>
        /// <exception cref="ExprParseException">Thrown on any syntax error.</exception>
        public static ExprAst Parse(List<Token> tokens) {
            var parser = new Parser(tokens: tokens);
            var ast    = parser.parseExpression();
            parser.expectEof();
            return ast;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Inner parser state

        sealed class Parser {
            readonly List<Token> _tokens;
            int _pos;

            public Parser(List<Token> tokens) {
                _tokens = tokens;
                _pos    = 0;
            }

            Token current => _tokens[_pos];

            Token consume() {
                var t = _tokens[_pos];
                _pos++;
                return t;
            }

            // ── Grammar rules ────────────────────────────────────────────────

            // expression = or_expr
            public ExprAst parseExpression() => parseOrExpr();

            // or_expr = and_expr ('||' and_expr)*
            ExprAst parseOrExpr() {
                var left = parseAndExpr();
                while (current.kind == TokenKind.Or) {
                    consume();
                    var right = parseAndExpr();
                    left = new OrNode(left: left, right: right);
                }
                return left;
            }

            // and_expr = unary_expr ('&&' unary_expr)*
            ExprAst parseAndExpr() {
                var left = parseUnaryExpr();
                while (current.kind == TokenKind.And) {
                    consume();
                    var right = parseUnaryExpr();
                    left = new AndNode(left: left, right: right);
                }
                return left;
            }

            // unary_expr = '!' unary_expr | '(' expression ')' | comparison_or_accessor
            ExprAst parseUnaryExpr() {
                if (current.kind == TokenKind.Not) {
                    consume();
                    return new NotNode(operand: parseUnaryExpr());
                }
                if (current.kind == TokenKind.LParen) {
                    consume();
                    if (current.kind == TokenKind.RParen) {
                        throw new ExprParseException(
                            message: "Empty parentheses '()' are not valid.",
                            column: current.column);
                    }
                    var inner = parseExpression();
                    if (current.kind != TokenKind.RParen) {
                        throw new ExprParseException(
                            message: "Missing closing parenthesis ')'.",
                            column: current.column);
                    }
                    consume();
                    return inner;
                }
                return parseComparisonOrAccessor();
            }

            // comparison_or_accessor = accessor (op rhs)?
            ExprAst parseComparisonOrAccessor() {
                var left = parseAccessor();
                var op_kind = current.kind;
                if (!isComparisonOp(kind: op_kind)) { return left; }

                string op  = current.value;
                consume();
                var right = parseRhs();
                return new ComparisonNode(left: left, op: op, right: right);
            }

            // rhs = accessor | literal
            ExprAst parseRhs() {
                if (current.kind == TokenKind.EOF) {
                    throw new ExprParseException(
                        message: "Unexpected end of expression: missing right-hand side value.",
                        column: current.column);
                }
                // If it looks like an accessor (IDENT DOT), parse as accessor
                if (current.kind == TokenKind.Identifier && _pos + 1 < _tokens.Count &&
                    _tokens[_pos + 1].kind == TokenKind.Dot) {
                    return parseAccessor();
                }
                return parseLiteral();
            }

            // accessor = IDENT '.' IDENT
            AccessorNode parseAccessor() {
                if (current.kind != TokenKind.Identifier) {
                    throw new ExprParseException(
                        message: $"Expected identifier, got '{current.value}'.",
                        column: current.column);
                }
                int    col    = current.column;
                string prefix = consume().value;

                if (current.kind != TokenKind.Dot) {
                    throw new ExprParseException(
                        message: $"Expected '.' after '{prefix}', got '{current.value}'.",
                        column: current.column);
                }
                consume();

                if (current.kind != TokenKind.Identifier) {
                    throw new ExprParseException(
                        message: $"Expected key after '.', got '{current.value}'.",
                        column: current.column);
                }
                string key = consume().value;
                return new AccessorNode(prefix: prefix, key: key, column: col);
            }

            // literal = NUMBER | 'true' | 'false'
            LiteralNode parseLiteral() {
                if (current.kind == TokenKind.BoolTrue)  { consume(); return new LiteralNode(value: true);  }
                if (current.kind == TokenKind.BoolFalse) { consume(); return new LiteralNode(value: false); }
                if (current.kind == TokenKind.Number) {
                    string raw = current.value;
                    consume();
                    if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) {
                        throw new ExprParseException(
                            message: $"Invalid numeric literal '{raw}'.",
                            column: current.column);
                    }
                    return new LiteralNode(value: d);
                }
                throw new ExprParseException(
                    message: $"Expected a literal (number, true, false), got '{current.value}'.",
                    column: current.column);
            }

            public void expectEof() {
                if (current.kind != TokenKind.EOF) {
                    throw new ExprParseException(
                        message: $"Unexpected token '{current.value}' after expression.",
                        column: current.column);
                }
            }

            static bool isComparisonOp(TokenKind kind) => kind switch {
                TokenKind.EqEq  => true,
                TokenKind.NotEq => true,
                TokenKind.Gt    => true,
                TokenKind.Lt    => true,
                TokenKind.GtEq  => true,
                TokenKind.LtEq  => true,
                _               => false
            };
        }
    }
}
