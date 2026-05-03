// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Abstract base for all AST expression nodes produced by <see cref="ExprParser"/>.
    /// Each node can be evaluated against a <see cref="State"/> instance to yield a bool.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public abstract class ExprAst {
#nullable enable
        /// <summary>Evaluates this expression node against the given state.</summary>
        public abstract bool Evaluate(State state);

        /// <summary>
        /// Returns the numeric (double) representation of this node for comparisons.
        /// Throws <see cref="InvalidOperationException"/> if the node has no numeric value.
        /// </summary>
        public virtual double GetNumeric(State state) =>
            throw new InvalidOperationException($"Node type '{GetType().Name}' has no numeric value.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Logical nodes

    /// <summary>Logical AND: left &amp;&amp; right (short-circuit).</summary>
    public class AndNode : ExprAst {
#nullable enable
        readonly ExprAst _left;
        readonly ExprAst _right;
        public AndNode(ExprAst left, ExprAst right) { _left = left; _right = right; }
        public override bool Evaluate(State state) => _left.Evaluate(state: state) && _right.Evaluate(state: state);
    }

    /// <summary>Logical OR: left || right (short-circuit).</summary>
    public class OrNode : ExprAst {
#nullable enable
        readonly ExprAst _left;
        readonly ExprAst _right;
        public OrNode(ExprAst left, ExprAst right) { _left = left; _right = right; }
        public override bool Evaluate(State state) => _left.Evaluate(state: state) || _right.Evaluate(state: state);
    }

    /// <summary>Logical NOT: !operand.</summary>
    public class NotNode : ExprAst {
#nullable enable
        readonly ExprAst _operand;
        public NotNode(ExprAst operand) { _operand = operand; }
        public override bool Evaluate(State state) => !_operand.Evaluate(state: state);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Leaf nodes

    /// <summary>
    /// Accesses flags, counters, or inventory by key.
    /// Implicit bool: flags.KEY → true if set; inventory.KEY → true if > 0; counters.KEY → false.
    /// Numeric: flags → 1.0/0.0; counters → float value; inventory → int value.
    /// </summary>
    public class AccessorNode : ExprAst {
#nullable enable
        readonly string _prefix;
        readonly string _key;
        readonly int    _column;

        public string prefix => _prefix;
        public string key    => _key;
        public int    column => _column;

        public AccessorNode(string prefix, string key, int column = 0) {
            _prefix = prefix;
            _key    = key;
            _column = column;
        }

        public override bool Evaluate(State state) {
            return _prefix switch {
                "flags"     => state.flags.TryGetValue(_key, out bool bv) && bv,
                "inventory" => state.inventory.TryGetValue(_key, out int iv) && iv > 0,
                "counters"  => false,
                _           => false
            };
        }

        public override double GetNumeric(State state) {
            return _prefix switch {
                "flags"     => (state.flags.TryGetValue(_key, out bool bv) && bv) ? 1.0 : 0.0,
                "counters"  => state.counters.TryGetValue(_key, out float fv) ? (double)fv : 0.0,
                "inventory" => state.inventory.TryGetValue(_key, out int iv) ? (double)iv : 0.0,
                _           => 0.0
            };
        }
    }

    /// <summary>A literal numeric or boolean value.</summary>
    public class LiteralNode : ExprAst {
#nullable enable
        readonly double _numeric_value;
        readonly bool   _bool_value;
        readonly bool   _is_bool;

        /// <summary>Creates a numeric literal.</summary>
        public LiteralNode(double value) {
            _numeric_value = value;
            _bool_value    = value != 0.0;
            _is_bool       = false;
        }

        /// <summary>Creates a boolean literal.</summary>
        public LiteralNode(bool value) {
            _bool_value    = value;
            _numeric_value = value ? 1.0 : 0.0;
            _is_bool       = true;
        }

        public override bool   Evaluate   (State state) => _bool_value;
        public override double GetNumeric (State state) => _numeric_value;
        public bool            IsBool      => _is_bool;
        public bool            BoolValue   => _bool_value;
        public double          NumericValue => _numeric_value;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Comparison node

    /// <summary>
    /// Compares a left <see cref="AccessorNode"/> against a right node (accessor or literal).
    /// G4: == and != use relative error: |a-b| <= 1e-6 * max(|a|, |b|, 1.0).
    /// </summary>
    public class ComparisonNode : ExprAst {
#nullable enable
        const double EPSILON = 1e-6;

        readonly AccessorNode _left;
        readonly string       _op;
        readonly ExprAst      _right;

        public ComparisonNode(AccessorNode left, string op, ExprAst right) {
            _left  = left;
            _op    = op;
            _right = right;
        }

        public override bool Evaluate(State state) {
            // Bool comparison: flags == true/false
            if ((_op == "==" || _op == "!=") && _right is LiteralNode lit && lit.IsBool) {
                bool left_bool  = _left.Evaluate(state: state);
                bool right_bool = lit.BoolValue;
                return _op == "==" ? left_bool == right_bool : left_bool != right_bool;
            }

            double left_num  = _left.GetNumeric(state: state);
            double right_num = _right.GetNumeric(state: state);

            return _op switch {
                "==" => relativeEqual(a: left_num, b: right_num),
                "!=" => !relativeEqual(a: left_num, b: right_num),
                ">"  => left_num > right_num,
                "<"  => left_num < right_num,
                ">=" => left_num >= right_num || relativeEqual(a: left_num, b: right_num),
                "<=" => left_num <= right_num || relativeEqual(a: left_num, b: right_num),
                _    => throw new InvalidOperationException($"Unknown operator: {_op}")
            };
        }

        /// <summary>
        /// G4 relative-error equality: |a-b| <= eps * max(|a|, |b|, 1.0)
        /// NaN != NaN; Infinity only equals itself.
        /// </summary>
        static bool relativeEqual(double a, double b) {
            if (double.IsNaN(a) || double.IsNaN(b))         { return false; }
            if (double.IsInfinity(a) || double.IsInfinity(b)) { return a == b; }
            double diff  = Math.Abs(a - b);
            double scale = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)), 1.0);
            return diff <= EPSILON * scale;
        }
    }

    /// <summary>
    /// Generic comparison node that can compare any two ExprAst nodes.
    /// Supports history nodes on the left side.
    /// </summary>
    public class GenericComparisonNode : ExprAst {
#nullable enable
        const double EPSILON = 1e-6;

        readonly ExprAst _left;
        readonly string  _op;
        readonly ExprAst _right;

        public ExprAst left => _left;
        public string op => _op;
        public ExprAst right => _right;

        public GenericComparisonNode(ExprAst left, string op, ExprAst right) {
            _left = left;
            _op = op;
            _right = right;
        }

        public override bool Evaluate(State state) {
            // This will fail for history nodes, but that's expected.
            // The Evaluator should use evaluateWithHistory for this.
            throw new InvalidOperationException("GenericComparisonNode requires History context for evaluation if left side is a history node.");
        }

        static bool relativeEqual(double a, double b) {
            if (double.IsNaN(a) || double.IsNaN(b))         { return false; }
            if (double.IsInfinity(a) || double.IsInfinity(b)) { return a == b; }
            double diff  = Math.Abs(a - b);
            double scale = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)), 1.0);
            return diff <= EPSILON * scale;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // History function nodes

    /// <summary>
    /// AST node for history.count(kind=..., target_id=...) function call.
    /// </summary>
    public class HistoryCountNode : ExprAst {
#nullable enable
        public string kind { get; }
        public string? target_id { get; }

        public HistoryCountNode(string kind, string? target_id = null) {
            this.kind = kind;
            this.target_id = target_id;
        }

        public override bool Evaluate(State state) => throw new InvalidOperationException("HistoryCountNode requires History context for evaluation.");

        public override double GetNumeric(State state) => throw new InvalidOperationException("HistoryCountNode requires History context for evaluation.");
    }

    /// <summary>
    /// AST node for history.has(kind=..., target_id=...) function call.
    /// </summary>
    public class HistoryHasNode : ExprAst {
#nullable enable
        public string kind { get; }
        public string? target_id { get; }

        public HistoryHasNode(string kind, string? target_id = null) {
            this.kind = kind;
            this.target_id = target_id;
        }

        public override bool Evaluate(State state) => throw new InvalidOperationException("HistoryHasNode requires History context for evaluation.");
    }

    /// <summary>
    /// AST node for history.last(kind=..., target_id=...).property function call.
    /// </summary>
    public class HistoryLastNode : ExprAst {
#nullable enable
        public string kind { get; }
        public string? target_id { get; }
        public string? property { get; }

        public HistoryLastNode(string kind, string? target_id = null, string? property = null) {
            this.kind = kind;
            this.target_id = target_id;
            this.property = property;
        }

        public override bool Evaluate(State state) => throw new InvalidOperationException("HistoryLastNode requires History context for evaluation.");

        public override double GetNumeric(State state) => throw new InvalidOperationException("HistoryLastNode requires History context for evaluation.");
    }

    /// <summary>
    /// AST node for history.time_since(kind=..., target_id=...) function call.
    /// </summary>
    public class HistoryTimeSinceNode : ExprAst {
#nullable enable
        public string kind { get; }
        public string? target_id { get; }

        public HistoryTimeSinceNode(string kind, string? target_id = null) {
            this.kind = kind;
            this.target_id = target_id;
        }

        public override bool Evaluate(State state) => throw new InvalidOperationException("HistoryTimeSinceNode requires History context for evaluation.");

        public override double GetNumeric(State state) => throw new InvalidOperationException("HistoryTimeSinceNode requires History context for evaluation.");
    }

    /// <summary>
    /// AST node for history.session_count() function call.
    /// </summary>
    public class HistorySessionCountNode : ExprAst {
#nullable enable
        public override bool Evaluate(State state) => throw new InvalidOperationException("HistorySessionCountNode requires History context for evaluation.");

        public override double GetNumeric(State state) => throw new InvalidOperationException("HistorySessionCountNode requires History context for evaluation.");
    }

    /// <summary>
    /// AST node for history.total_play_time() function call.
    /// </summary>
    public class HistoryTotalPlayTimeNode : ExprAst {
#nullable enable
        public override bool Evaluate(State state) => throw new InvalidOperationException("HistoryTotalPlayTimeNode requires History context for evaluation.");

        public override double GetNumeric(State state) => throw new InvalidOperationException("HistoryTotalPlayTimeNode requires History context for evaluation.");
    }
}