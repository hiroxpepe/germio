// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;

namespace Germio.Core {
    /// <summary>Identifies the kind of a single lexical token produced by <see cref="ExprLexer"/>.</summary>
    public enum TokenKind {
        Identifier,  // flags, counters, inventory, key names
        Number,      // integer or float literal
        BoolTrue,    // "true" keyword
        BoolFalse,   // "false" keyword
        EqEq,        // ==
        NotEq,       // !=
        Gt,          // >
        Lt,          // <
        GtEq,        // >=
        LtEq,        // <=
        And,         // &&
        Or,          // ||
        Not,         // !
        LParen,      // (
        RParen,      // )
        Dot,         // .
        EOF          // end-of-input sentinel
    }

    /// <summary>A single lexical token produced by <see cref="ExprLexer"/>.</summary>
    public class Token {
#nullable enable
        readonly TokenKind _kind;
        readonly string    _value;
        readonly int       _column;

        public TokenKind kind   => _kind;
        public string    value  => _value;
        public int       column => _column;

        public Token(TokenKind kind, string value, int column) {
            _kind   = kind;
            _value  = value;
            _column = column;
        }
    }

    /// <summary>
    /// Tokenizer for the Germio condition DSL.
    /// G1 principle: no Regex — hand-written character-by-character scanner.
    /// Throws <see cref="ExprParseException"/> on unrecognized characters.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class ExprLexer {
#nullable enable

        /// <summary>
        /// Converts the input source string into a flat list of <see cref="Token"/>s.
        /// The last token is always an <see cref="TokenKind.EOF"/> sentinel.
        /// </summary>
        /// <param name="source">The raw condition expression string.</param>
        /// <returns>List of tokens including trailing EOF.</returns>
        /// <exception cref="ExprParseException">Thrown on any unrecognized character.</exception>
        public static List<Token> Tokenize(string source) {
            var tokens = new List<Token>();
            int i = 0;

            while (i < source.Length) {
                // Skip whitespace
                if (char.IsWhiteSpace(source[i])) { i++; continue; }

                int col = i;

                // Identifier or keyword: [a-zA-Z_][a-zA-Z0-9_-]*
                if (char.IsLetter(source[i]) || source[i] == '_') {
                    int start = i;
                    while (i < source.Length &&
                           (char.IsLetterOrDigit(source[i]) || source[i] == '_' || source[i] == '-')) {
                        i++;
                    }
                    string word = source.Substring(start, i - start);
                    TokenKind kind = word switch {
                        "true"  => TokenKind.BoolTrue,
                        "false" => TokenKind.BoolFalse,
                        _       => TokenKind.Identifier
                    };
                    tokens.Add(new Token(kind: kind, value: word, column: col));
                    continue;
                }

                // Number: [0-9]+([.][0-9]+)?
                if (char.IsDigit(source[i])) {
                    int start = i;
                    while (i < source.Length && char.IsDigit(source[i])) { i++; }
                    if (i < source.Length && source[i] == '.') {
                        i++;
                        while (i < source.Length && char.IsDigit(source[i])) { i++; }
                    }
                    tokens.Add(new Token(kind: TokenKind.Number, value: source.Substring(start, i - start), column: col));
                    continue;
                }

                // Two-character operators first (order matters: >= before >)
                if (i + 1 < source.Length) {
                    string two = source.Substring(i, 2);
                    TokenKind? two_kind = two switch {
                        "==" => TokenKind.EqEq,
                        "!=" => TokenKind.NotEq,
                        ">=" => TokenKind.GtEq,
                        "<=" => TokenKind.LtEq,
                        "&&" => TokenKind.And,
                        "||" => TokenKind.Or,
                        _    => null
                    };
                    if (two_kind.HasValue) {
                        tokens.Add(new Token(kind: two_kind.Value, value: two, column: col));
                        i += 2;
                        continue;
                    }
                }

                // Single-character
                TokenKind? one_kind = source[i] switch {
                    '>' => TokenKind.Gt,
                    '<' => TokenKind.Lt,
                    '!' => TokenKind.Not,
                    '(' => TokenKind.LParen,
                    ')' => TokenKind.RParen,
                    '.' => TokenKind.Dot,
                    _   => null
                };
                if (one_kind.HasValue) {
                    tokens.Add(new Token(kind: one_kind.Value, value: source[i].ToString(), column: col));
                    i++;
                    continue;
                }

                throw new ExprParseException(
                    message: $"Unexpected character '{source[i]}' at column {i}.",
                    column: i);
            }

            tokens.Add(new Token(kind: TokenKind.EOF, value: string.Empty, column: i));
            return tokens;
        }
    }
}
