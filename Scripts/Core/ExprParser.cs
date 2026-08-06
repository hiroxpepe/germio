// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Germio.Core {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>Thrown when the condition DSL fails to parse.</summary>
    public class ExprParseException : Exception {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        public ExprParseException(string message, int column = 0) : base(message) {
            Column = column;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>Zero-based column index where the error was detected.</summary>
        public int Column { get; }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Parses the list of tokens (produced by <see cref="ExprLexer"/>) into an AST.
        /// </summary>
        /// <param name="tokens">List of tokens including trailing EOF.</param>
        /// <returns>Root AST node.</returns>
        /// <exception cref="ExprParseException">Thrown on any syntax error.</exception>
        public static ExprAST Parse(List<Token> tokens) {
            var parser = new Parser(tokens: tokens);
            var ast    = parser.ParseExpression();
            parser.ExpectEof();
            return ast;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        // ──────────────────────────────────────────────────────────────────────
        // Inner parser state

        sealed class Parser {
            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            readonly List<Token> _tokens;
            int _pos;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            public Parser(List<Token> tokens) {
                _tokens = tokens;
                _pos    = 0;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjective]

            Token current => _tokens[_pos];

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            // ── Grammar rules ────────────────────────────────────────────────

            // expression = or_expr
            public ExprAST ParseExpression() => parseOrExpr();

            public void ExpectEof() {
                if (current.Kind != TokenKind.EOF) {
                    throw new ExprParseException(
                        message: $"Unexpected token '{current.Value}' after expression.",
                        column: current.Column);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // private static Methods [verb]

            static bool isComparisonOp(TokenKind kind) => kind switch {
                TokenKind.EqEq  => true,
                TokenKind.NotEq => true,
                TokenKind.Gt    => true,
                TokenKind.Lt    => true,
                TokenKind.GtEq  => true,
                TokenKind.LtEq  => true,
                _               => false
            };

            ///////////////////////////////////////////////////////////////////////////////////////////
            // private Methods [verb]

            Token consume() {
                var t = _tokens[_pos];
                _pos++;
                return t;
            }

            // or_expr = and_expr ('||' and_expr)*
            ExprAST parseOrExpr() {
                var left = parseAndExpr();
                while (current.Kind == TokenKind.Or) {
                    consume();
                    var right = parseAndExpr();
                    left = new OrNode(left: left, right: right);
                }
                return left;
            }

            // and_expr = unary_expr ('&&' unary_expr)*
            ExprAST parseAndExpr() {
                var left = parseUnaryExpr();
                while (current.Kind == TokenKind.And) {
                    consume();
                    var right = parseUnaryExpr();
                    left = new AndNode(left: left, right: right);
                }
                return left;
            }

            // unary_expr = '!' unary_expr | '(' expression ')' | comparison_or_accessor
            ExprAST parseUnaryExpr() {
                if (current.Kind == TokenKind.Not) {
                    consume();
                    return new NotNode(operand: parseUnaryExpr());
                }
                if (current.Kind == TokenKind.LeftParen) {
                    consume();
                    if (current.Kind == TokenKind.RightParen) {
                        throw new ExprParseException(
                            message: "Empty parentheses '()' are not valid.",
                            column: current.Column);
                    }
                    var inner = ParseExpression();
                    if (current.Kind != TokenKind.RightParen) {
                        throw new ExprParseException(
                            message: "Missing closing parenthesis ')'.",
                            column: current.Column);
                    }
                    consume();
                    return inner;
                }
                return parseComparisonOrAccessor();
            }

            // comparison_or_accessor = history_or_accessor (op rhs)?
            ExprAST parseComparisonOrAccessor() {
                // Check if this is a history function call (history.*)
                ExprAST left;
                if (current.Kind == TokenKind.Identifier && current.Value == "history" &&
                    _pos + 1 < _tokens.Count && _tokens[_pos + 1].Kind == TokenKind.Dot) {
                    left = parseHistoryFunction();
                } else {
                    left = parseAccessor();
                }

                var op_kind = current.Kind;
                if (!isComparisonOp(kind: op_kind)) { return left; }

                // For history nodes, we need a more generic comparison node
                // Since ComparisonNode only accepts AccessorNode, wrap it in a way that works
                string op  = current.Value;
                consume();
                var right = parseRhs();
                
                if (left is AccessorNode accessor_left) {
                    return new ComparisonNode(left: accessor_left, op: op, right: right);
                } else {
                    // For history nodes, create a generic comparison wrapper
                    return new GenericComparisonNode(left: left, op: op, right: right);
                }
            }

            // history_function = history.COUNT | history.HAS | history.LAST | history.TIME_SINCE | history.SESSION_COUNT | history.TOTAL_PLAY_TIME
            ExprAST parseHistoryFunction() {
                int col = current.Column;
                consume(); // consume "history"
                
                if (current.Kind != TokenKind.Dot) {
                    throw new ExprParseException(
                        message: "Expected '.' after 'history'.",
                        column: current.Column);
                }
                consume(); // consume "."

                if (current.Kind != TokenKind.Identifier) {
                    throw new ExprParseException(
                        message: "Expected function name after 'history.'.",
                        column: current.Column);
                }
                string func_name = consume().Value;

                // Parse function call based on function name
                return func_name switch {
                    "count" => parseHistoryCount(col: col),
                    "has" => parseHistoryHas(col: col),
                    "last" => parseHistoryLast(col: col),
                    "time_since" => parseHistoryTimeSince(col: col),
                    "session_count" => parseHistorySessionCount(col: col),
                    "total_play_time" => parseHistoryTotalPlayTime(col: col),
                    _ => throw new ExprParseException(
                        message: $"Unknown history function: history.{func_name}",
                        column: col)
                };
            }

            // history.count(kind=..., target_id=...)
            ExprAST parseHistoryCount(int col) {
                if (current.Kind != TokenKind.LeftParen) {
                    throw new ExprParseException(
                        message: "Expected '(' after 'history.count'.",
                        column: current.Column);
                }
                consume();

                string kind = parseNamedParameter(name: "kind");
                string? target_id = null;

                if (current.Kind == TokenKind.Comma) {
                    consume();
                    target_id = parseNamedParameter(name: "target_id");
                }

                if (current.Kind != TokenKind.RightParen) {
                    throw new ExprParseException(
                        message: "Expected ')' after history.count parameters.",
                        column: current.Column);
                }
                consume();

                return new HistoryCountNode(kind: kind, target_id: target_id);
            }

            // history.has(kind=..., target_id=...)
            ExprAST parseHistoryHas(int col) {
                if (current.Kind != TokenKind.LeftParen) {
                    throw new ExprParseException(
                        message: "Expected '(' after 'history.has'.",
                        column: current.Column);
                }
                consume();

                string kind = parseNamedParameter(name: "kind");
                string? target_id = null;

                if (current.Kind == TokenKind.Comma) {
                    consume();
                    target_id = parseNamedParameter(name: "target_id");
                }

                if (current.Kind != TokenKind.RightParen) {
                    throw new ExprParseException(
                        message: "Expected ')' after history.has parameters.",
                        column: current.Column);
                }
                consume();

                return new HistoryHasNode(kind: kind, target_id: target_id);
            }

            // history.last(kind=..., target_id=...).property
            ExprAST parseHistoryLast(int col) {
                if (current.Kind != TokenKind.LeftParen) {
                    throw new ExprParseException(
                        message: "Expected '(' after 'history.last'.",
                        column: current.Column);
                }
                consume();

                string kind = parseNamedParameter(name: "kind");
                string? target_id = null;

                if (current.Kind == TokenKind.Comma) {
                    consume();
                    target_id = parseNamedParameter(name: "target_id");
                }

                if (current.Kind != TokenKind.RightParen) {
                    throw new ExprParseException(
                        message: "Expected ')' after history.last parameters.",
                        column: current.Column);
                }
                consume();

                string? property = null;
                if (current.Kind == TokenKind.Dot) {
                    consume();
                    if (current.Kind != TokenKind.Identifier) {
                        throw new ExprParseException(
                            message: "Expected property name after '.' in history.last.",
                            column: current.Column);
                    }
                    property = consume().Value;
                }

                return new HistoryLastNode(kind: kind, target_id: target_id, property: property);
            }

            // history.time_since(kind=..., target_id=...)
            ExprAST parseHistoryTimeSince(int col) {
                if (current.Kind != TokenKind.LeftParen) {
                    throw new ExprParseException(
                        message: "Expected '(' after 'history.time_since'.",
                        column: current.Column);
                }
                consume();

                string kind = parseNamedParameter(name: "kind");
                string? target_id = null;

                if (current.Kind == TokenKind.Comma) {
                    consume();
                    target_id = parseNamedParameter(name: "target_id");
                }

                if (current.Kind != TokenKind.RightParen) {
                    throw new ExprParseException(
                        message: "Expected ')' after history.time_since parameters.",
                        column: current.Column);
                }
                consume();

                return new HistoryTimeSinceNode(kind: kind, target_id: target_id);
            }

            // history.session_count()
            ExprAST parseHistorySessionCount(int col) {
                if (current.Kind != TokenKind.LeftParen) {
                    throw new ExprParseException(
                        message: "Expected '(' after 'history.session_count'.",
                        column: current.Column);
                }
                consume();

                if (current.Kind != TokenKind.RightParen) {
                    throw new ExprParseException(
                        message: "Expected ')' after 'history.session_count()'.",
                        column: current.Column);
                }
                consume();

                return new HistorySessionCountNode();
            }

            // history.total_play_time()
            ExprAST parseHistoryTotalPlayTime(int col) {
                if (current.Kind != TokenKind.LeftParen) {
                    throw new ExprParseException(
                        message: "Expected '(' after 'history.total_play_time'.",
                        column: current.Column);
                }
                consume();

                if (current.Kind != TokenKind.RightParen) {
                    throw new ExprParseException(
                        message: "Expected ')' after 'history.total_play_time()'.",
                        column: current.Column);
                }
                consume();

                return new HistoryTotalPlayTimeNode();
            }

            // Parse named parameter: name=value
            string parseNamedParameter(string name) {
                if (current.Kind != TokenKind.Identifier || current.Value != name) {
                    throw new ExprParseException(
                        message: $"Expected '{name}='.",
                        column: current.Column);
                }
                consume();

                if (current.Kind != TokenKind.Equals) {
                    throw new ExprParseException(
                        message: $"Expected '=' after '{name}'.",
                        column: current.Column);
                }
                consume();

                if (current.Kind != TokenKind.Identifier) {
                    throw new ExprParseException(
                        message: "Expected identifier value after '='.",
                        column: current.Column);
                }
                string value = consume().Value;
                return value;
            }

            // rhs = accessor | literal
            ExprAST parseRhs() {
                if (current.Kind == TokenKind.EOF) {
                    throw new ExprParseException(
                        message: "Unexpected end of expression: missing right-hand side value.",
                        column: current.Column);
                }
                // If it looks like an accessor (IDENT DOT), parse as accessor
                if (current.Kind == TokenKind.Identifier && _pos + 1 < _tokens.Count &&
                    _tokens[_pos + 1].Kind == TokenKind.Dot) {
                    return parseAccessor();
                }
                return parseLiteral();
            }

            // accessor = IDENT '.' IDENT
            AccessorNode parseAccessor() {
                if (current.Kind != TokenKind.Identifier) {
                    throw new ExprParseException(
                        message: $"Expected identifier, got '{current.Value}'.",
                        column: current.Column);
                }
                int    col    = current.Column;
                string prefix = consume().Value;

                if (current.Kind != TokenKind.Dot) {
                    throw new ExprParseException(
                        message: $"Expected '.' after '{prefix}', got '{current.Value}'.",
                        column: current.Column);
                }
                consume();

                if (current.Kind != TokenKind.Identifier) {
                    throw new ExprParseException(
                        message: $"Expected key after '.', got '{current.Value}'.",
                        column: current.Column);
                }
                string key = consume().Value;
                return new AccessorNode(prefix: prefix, key: key, column: col);
            }

            // literal = NUMBER | 'true' | 'false'
            LiteralNode parseLiteral() {
                if (current.Kind == TokenKind.BoolTrue)  { consume(); return new LiteralNode(value: true);  }
                if (current.Kind == TokenKind.BoolFalse) { consume(); return new LiteralNode(value: false); }
                if (current.Kind == TokenKind.Number) {
                    string raw = current.Value;
                    consume();
                    if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) {
                        throw new ExprParseException(
                            message: $"Invalid numeric literal '{raw}'.",
                            column: current.Column);
                    }
                    return new LiteralNode(value: d);
                }
                throw new ExprParseException(
                    message: $"Expected a literal (number, true, false), got '{current.Value}'.",
                    column: current.Column);
            }
        }
    }
}