// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// (a, b, c)
	/// </summary>
	public class TupleExpression : Expression
	{
		public AstNodeCollection<Expression> Elements {
			get { return GetChildrenByRole<Expression>(Roles.Argument); }
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTupleExpression(this, data);
		}

		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			TupleExpression o = other as TupleExpression;
			return o != null && this.Elements.DoMatch(o.Elements, match);
		}
	}

	/// <summary>
	/// (x As Integer, y As String)
	/// </summary>
	public class TupleType : AstType
	{
		public AstNodeCollection<TupleTypeElement> Elements {
			get { return GetChildrenByRole(TupleTypeElement.ElementRole); }
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTupleType(this, data);
		}

		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			TupleType o = other as TupleType;
			return o != null && this.Elements.DoMatch(o.Elements, match);
		}
	}

	/// <summary>
	/// One element of a <see cref="TupleType"/>; the name is optional.
	/// </summary>
	public class TupleTypeElement : AstNode
	{
		public static readonly Role<TupleTypeElement> ElementRole = new Role<TupleTypeElement>("Element");

		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}

		public Identifier Identifier {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTupleTypeElement(this, data);
		}

		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			TupleTypeElement o = other as TupleTypeElement;
			return o != null && this.Type.DoMatch(o.Type, match) && this.Identifier.DoMatch(o.Identifier, match);
		}
	}
}
