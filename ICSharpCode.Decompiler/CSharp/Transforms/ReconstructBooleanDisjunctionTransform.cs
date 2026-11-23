// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Rebuilds boolean disjunction chains that the IL lowering split into a shared-target
	/// brtrue chain. Roslyn compiles 'return a &amp;&amp; (b1 || ... || bn);' as a series of
	/// branches to one 'return true' block; once the optimizer flattens that chain, the
	/// surviving guard reads 'if (all previous operands are false) return bn;' and the
	/// trailing constant return turns it into
	///   if (a) { if (!(b1 || ... || b(n-1))) { return bn; } return true; } return false;
	/// This transform merges those guards back, pushing the negation inward
	/// (!!x -&gt; x, !(p &amp;&amp; q) -&gt; !p || !q, !(x &lt; y) -&gt; x &gt;= y).
	///
	/// The nested variant covers the case where the last operand is itself guarded:
	///   if (!(b1 || ... || b(n-1))) { if (c) return e; return false; } return true;
	/// which becomes 'return (b1 || ... || b(n-1)) || (c &amp;&amp; e);'.
	///
	/// Only guards with a trailing constant return are rewritten and only when pushing the
	/// negation inward removes negations, so ordinary guard clauses are left alone.
	/// </summary>
	public class ReconstructBooleanDisjunctionTransform : IAstTransform
	{
		TransformContext context;

		public void Run(AstNode rootNode, TransformContext context)
		{
			this.context = context;
			// Inner guards must be merged before the enclosing guard can be recognised, so the
			// deepest blocks go first and the whole pass is repeated.
			var blocks = rootNode.DescendantsAndSelf.OfType<BlockStatement>()
				.OrderByDescending(b => b.Ancestors.Count())
				.ToList();
			for (int pass = 0; pass < 2; pass++) {
				foreach (var block in blocks)
					RewriteBlock(block);
			}
		}

		void RewriteBlock(BlockStatement block)
		{
			var statements = block.Statements;
			for (int i = 0; i + 1 < statements.Count; i++) {
				if (TryMergeNegatedGuard(statements.ElementAt(i), statements.ElementAt(i + 1), out var mergedOr)) {
					ReplaceWithMerged(statements, i, mergedOr);
					i--;
					continue;
				}
				if (TryMergeGuardedDisjunction(statements.ElementAt(i), statements.ElementAt(i + 1), out var mergedAnd)) {
					ReplaceWithMerged(statements, i, mergedAnd);
					i--;
					continue;
				}
			}
		}

		static void ReplaceWithMerged(AstNodeCollection<Statement> statements, int i, Statement merged)
		{
			statements.ElementAt(i + 1).Remove();
			statements.ElementAt(i).ReplaceWith(merged);
		}

		/// <summary>'if (K) { return E; } return true;' == 'return !K || E;'
		/// and 'if (K) { if (C) { return E; } return false; } return true;' == 'return !K || (C &amp;&amp; E);'</summary>
		bool TryMergeNegatedGuard(Statement first, Statement second, out ReturnStatement merged)
		{
			merged = null;
			var iff = first as IfElseStatement;
			if (iff == null || !iff.FalseStatement.IsNull || !TryGetThenBody(iff.TrueStatement, out var tail))
				return false;
			if (tail.Return != null) {
				if (tail.Return.Expression == null || !IsBoolean(tail.Return.Expression))
					return false;
			} else {
				if (tail.InnerCondition == null || tail.InnerTrue == null || !IsBoolean(tail.InnerCondition) || !IsBoolean(tail.InnerTrue))
					return false;
			}
			if (!IsBoolean(iff.Condition))
				return false;
			if (!(second is ReturnStatement tailReturn) || !IsConstant(tailReturn.Expression, true))
				return false;

			var negated = Negate(iff.Condition);
			// Only rewrite when pushing the negation inward removes negations; otherwise the
			// original shape is kept (dnSpy 2.3 keeps that shape for plain guards too).
			if (CountNots(negated) >= CountNots(iff.Condition) + 1)
				return false;

			Expression right;
			if (tail.Return != null) {
				right = tail.Return.Expression;
			} else {
				right = MakeLogical(tail.InnerCondition, BinaryOperatorType.ConditionalAnd, tail.InnerTrue);
			}
			merged = MakeReturn(negated, BinaryOperatorType.ConditionalOr, right.Clone());
			return true;
		}

		/// <summary>'if (A) { return D || E; } return false;' == 'return A &amp;&amp; (D || E);'</summary>
		bool TryMergeGuardedDisjunction(Statement first, Statement second, out ReturnStatement merged)
		{
			merged = null;
			var iff = first as IfElseStatement;
			if (iff == null || !iff.FalseStatement.IsNull || !TryGetSingleReturn(iff.TrueStatement, out var returned))
				return false;
			if (!(returned.Expression is BinaryOperatorExpression disjunction)
					|| disjunction.Operator != BinaryOperatorType.ConditionalOr)
				return false;
			if (!IsBoolean(iff.Condition) || !IsBoolean(disjunction))
				return false;
			if (!(second is ReturnStatement tail) || !IsConstant(tail.Expression, false))
				return false;
			merged = MakeReturn(iff.Condition.Clone(), BinaryOperatorType.ConditionalAnd, disjunction.Clone());
			return true;
		}

		/// <summary>Either 'return E;' or 'if (C) { return E; } return false;'.</summary>
		struct GuardBody {
			public readonly ReturnStatement Return;
			public readonly Expression InnerCondition;
			public readonly Expression InnerTrue;

			public GuardBody(ReturnStatement single)
			{
				Return = single;
				InnerCondition = null;
				InnerTrue = null;
			}

			public GuardBody(Expression innerCondition, Expression innerTrue)
			{
				Return = null;
				InnerCondition = innerCondition;
				InnerTrue = innerTrue;
			}
		}

		static bool TryGetThenBody(Statement stmt, out GuardBody body)
		{
			body = default(GuardBody);
			if (TryGetSingleReturn(stmt, out var single)) {
				body = new GuardBody(single);
				return true;
			}
			var block = stmt as BlockStatement;
			if (block == null || block.Statements.Count() != 2)
				return false;
			var inner = block.Statements.ElementAt(0) as IfElseStatement;
			if (inner == null || !inner.FalseStatement.IsNull)
				return false;
			if (!TryGetSingleReturn(inner.TrueStatement, out var innerReturn) || innerReturn.Expression == null)
				return false;
			var trailing = block.Statements.ElementAt(1) as ReturnStatement;
			if (trailing == null || !IsConstant(trailing.Expression, false))
				return false;
			body = new GuardBody(inner.Condition, innerReturn.Expression);
			return true;
		}

		static bool TryGetSingleReturn(Statement stmt, out ReturnStatement returned)
		{
			returned = stmt as ReturnStatement;
			if (returned != null)
				return true;
			var block = stmt as BlockStatement;
			if (block != null && block.Statements.Count() == 1)
				returned = block.Statements.FirstOrDefault() as ReturnStatement;
			return returned != null;
		}

		/// <summary>Pushes a logical negation inward: !!x -&gt; x, !(p &amp;&amp; q) -&gt; !p || !q,
		/// !(p || q) -&gt; !p &amp;&amp; !q, and flips comparisons (!(x &lt; y) -&gt; x &gt;= y).</summary>
		Expression Negate(Expression expr)
		{
			// Work on a private copy: the result is built from these nodes, so they must not
			// stay attached to the original condition.
			expr = expr.Clone();
			var unary = expr as UnaryOperatorExpression;
			if (unary != null && unary.Operator == UnaryOperatorType.Not)
				return Detach(unary.Expression);
			var binary = expr as BinaryOperatorExpression;
			if (binary != null) {
				if (binary.Operator == BinaryOperatorType.ConditionalAnd)
					return MakeLogical(Negate(binary.Left), BinaryOperatorType.ConditionalOr, Negate(binary.Right));
				if (binary.Operator == BinaryOperatorType.ConditionalOr)
					return MakeLogical(Negate(binary.Left), BinaryOperatorType.ConditionalAnd, Negate(binary.Right));
				var flipped = FlipComparison(binary.Operator);
				if (flipped != BinaryOperatorType.Any)
					return MakeComparison(binary.Left, flipped, binary.Right);
			}
			return new UnaryOperatorExpression(UnaryOperatorType.Not, Detach(expr));
		}

		/// <summary>Detaches a node from the tree it currently belongs to, so it can be reused.</summary>
		static Expression Detach(Expression expr)
		{
			if (expr != null)
				expr.Remove();
			return expr;
		}

		static BinaryOperatorType FlipComparison(BinaryOperatorType op)
		{
			switch (op) {
				case BinaryOperatorType.Equality: return BinaryOperatorType.InEquality;
				case BinaryOperatorType.InEquality: return BinaryOperatorType.Equality;
				case BinaryOperatorType.LessThan: return BinaryOperatorType.GreaterThanOrEqual;
				case BinaryOperatorType.LessThanOrEqual: return BinaryOperatorType.GreaterThan;
				case BinaryOperatorType.GreaterThan: return BinaryOperatorType.LessThanOrEqual;
				case BinaryOperatorType.GreaterThanOrEqual: return BinaryOperatorType.LessThan;
				default: return BinaryOperatorType.Any;
			}
		}

		Expression MakeLogical(Expression left, BinaryOperatorType op, Expression right)
		{
			var logical = new BinaryOperatorExpression(Detach(left), op, Detach(right));
			logical.AddAnnotation(new OperatorResolveResult(
				context.TypeSystem.Compilation.FindType(KnownTypeCode.Boolean),
				op == BinaryOperatorType.ConditionalAnd
					? System.Linq.Expressions.ExpressionType.AndAlso
					: System.Linq.Expressions.ExpressionType.OrElse,
				left.GetResolveResult(),
				right.GetResolveResult()));
			return logical;
		}

		Expression MakeComparison(Expression left, BinaryOperatorType op, Expression right)
		{
			var comparison = new BinaryOperatorExpression(Detach(left), op, Detach(right));
			comparison.AddAnnotation(new OperatorResolveResult(
				context.TypeSystem.Compilation.FindType(KnownTypeCode.Boolean),
				GetComparisonExpressionType(op),
				left.GetResolveResult(),
				right.GetResolveResult()));
			return comparison;
		}

		static System.Linq.Expressions.ExpressionType GetComparisonExpressionType(BinaryOperatorType op)
		{
			switch (op) {
				case BinaryOperatorType.Equality: return System.Linq.Expressions.ExpressionType.Equal;
				case BinaryOperatorType.InEquality: return System.Linq.Expressions.ExpressionType.NotEqual;
				case BinaryOperatorType.LessThan: return System.Linq.Expressions.ExpressionType.LessThan;
				case BinaryOperatorType.LessThanOrEqual: return System.Linq.Expressions.ExpressionType.LessThanOrEqual;
				case BinaryOperatorType.GreaterThan: return System.Linq.Expressions.ExpressionType.GreaterThan;
				case BinaryOperatorType.GreaterThanOrEqual: return System.Linq.Expressions.ExpressionType.GreaterThanOrEqual;
				default: return System.Linq.Expressions.ExpressionType.Equal;
			}
		}

		static ReturnStatement MakeReturn(Expression left, BinaryOperatorType op, Expression right)
		{
			var logical = new BinaryOperatorExpression(Detach(left), op, Detach(right));
			logical.AddAnnotation(new OperatorResolveResult(
				left.GetResolveResult().Type,
				op == BinaryOperatorType.ConditionalAnd
					? System.Linq.Expressions.ExpressionType.AndAlso
					: System.Linq.Expressions.ExpressionType.OrElse,
				left.GetResolveResult(),
				right.GetResolveResult()));
			return new ReturnStatement(logical);
		}

		static int CountNots(Expression expr)
		{
			if (expr == null)
				return 0;
			int count = 0;
			foreach (var node in expr.DescendantsAndSelf) {
				if (node is UnaryOperatorExpression u && u.Operator == UnaryOperatorType.Not)
					count++;
			}
			return count;
		}

		static bool IsBoolean(Expression expr)
		{
			return expr.GetResolveResult()?.Type?.IsKnownType(KnownTypeCode.Boolean) == true;
		}

		static bool IsConstant(Expression expr, bool value)
		{
			var primitive = expr as PrimitiveExpression;
			if (primitive == null)
				return false;
			if (primitive.Value is bool b)
				return b == value;
			return primitive.Value is int i && (i != 0) == value;
		}
	}
}
