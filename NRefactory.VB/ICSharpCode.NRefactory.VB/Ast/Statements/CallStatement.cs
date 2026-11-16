// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Call Expression
	/// </summary>
	public class CallStatement : Statement
	{
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCallStatement(this, data);
		}

		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			CallStatement o = other as CallStatement;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}
}
