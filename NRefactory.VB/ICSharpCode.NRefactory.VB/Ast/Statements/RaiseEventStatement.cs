// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// RaiseEvent Event(Arguments)
	/// </summary>
	/// <remarks>
	/// C# invokes an event like a delegate; VB requires the RaiseEvent statement, which is why
	/// this node exists. It is the statement form of <see cref="InvocationExpression"/>.
	/// </remarks>
	public class RaiseEventStatement : Statement
	{
		public Expression Event {
			get { return GetChildByRole(Roles.TargetExpression); }
			set { SetChildByRole(Roles.TargetExpression, value); }
		}

		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole<Expression>(Roles.Argument); }
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitRaiseEventStatement(this, data);
		}

		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			RaiseEventStatement o = other as RaiseEventStatement;
			return o != null && this.Event.DoMatch(o.Event, match) && this.Arguments.DoMatch(o.Arguments, match);
		}
	}
}
