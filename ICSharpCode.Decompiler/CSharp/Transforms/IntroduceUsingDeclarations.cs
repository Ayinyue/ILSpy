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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Introduces using declarations.
	/// </summary>
	public class IntroduceUsingDeclarations : IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			// First determine all the namespaces that need to be imported:
			var requiredImports = new FindRequiredImports(context);
			rootNode.AcceptVisitor(requiredImports);

			// The 2.3 engine always imports System, even when not necessary
			// ("always import System" in its IntroduceUsingDeclarations), so a bare
			// using System; appears in every type that has any using block. System is
			// added to the imported set but NOT to VerifiedNamespaces: the superset
			// check then keeps it only for types that actually reference System, which
			// matches 2.3 output on normal types while leaving the empty-tree fallback
			// path (compiler-generated types) untouched.
			requiredImports.ImportedNamespaces.Add("System");

			var usingScope = new UsingScope();
			rootNode.AddAnnotation(usingScope);

			if (context.Settings.UsingDeclarations) {
				var insertionPoint = rootNode.Children.LastOrDefault(n => n is PreProcessorDirective p && p.Type == PreProcessorDirectiveType.Define);

				// Now add using declarations for those namespaces, in ascending order like the
				// 2.3 engine. Each declaration is inserted right after the insertion point, so
				// the loop must go backwards (descending) to produce ascending output order.
				// With SortSystemUsingStatementsFirst (dnSpy's 2.3 default) the System group
				// comes first and both groups are ordered case-insensitively.
				IOrderedEnumerable<string> ordered = requiredImports.ImportedNamespaces
					.OrderByDescending(n => n, StringComparer.Ordinal);
				if (context.Settings.SortSystemUsingStatementsFirst) {
					ordered = requiredImports.ImportedNamespaces
						.OrderBy(n => n == "System" || n.StartsWith("System.", StringComparison.Ordinal) ? 1 : 0)
						.ThenByDescending(n => n, StringComparer.OrdinalIgnoreCase);
				}
				foreach (string ns in ordered) {
					// In the dnSpy dnlib->cecil bridge some namespaces referenced from method
					// bodies are absent from the superset (the corresponding member reference
					// cannot be resolved within the same module). Namespaces the visitor itself
					// verified against the type system are trusted regardless of the superset.
					if (!context.RequiredNamespacesSuperset.Contains(ns) && !requiredImports.VerifiedNamespaces.Contains(ns))
						continue;
					// we go backwards (OrderByDescending) through the list of namespaces because we insert them backwards
					// (always inserting at the start of the list)
					string[] parts = ns.Split('.');
					AstType nsType = new SimpleType(parts[0]);
					for (int i = 1; i < parts.Length; i++) {
						nsType = new MemberType { Target = nsType, MemberName = parts[i] };
					}
					if (context.Settings.FullyQualifyAmbiguousTypeNames) {
						var reference = nsType.ToTypeReference(NameLookupMode.TypeInUsingDeclaration) as TypeOrNamespaceReference;
						if (reference != null)
							usingScope.Usings.Add(reference);
					}
					rootNode.InsertChildAfter(insertionPoint, new UsingDeclaration { Import = nsType }, SyntaxTree.MemberRole);
				}
			}

			if (!context.Settings.FullyQualifyAmbiguousTypeNames)
				return;

			// verify that the SimpleTypes refer to the correct type (no ambiguities)
			rootNode.AcceptVisitor(new FullyQualifyAmbiguousTypeNamesVisitor(context, usingScope));
			if (Environment.GetEnvironmentVariable("DNSPY_ILSPY32_DIAG") == "1") {
				var sbc = new System.Text.StringBuilder("IUD-CHILDREN");
				foreach (AstNode c in rootNode.Children)
					sbc.Append(" [").Append(c.GetType().Name).Append("]");
				Console.Error.WriteLine(sbc.ToString());
			}
		}
		
		sealed class FindRequiredImports : DepthFirstAstVisitor
		{
			string currentNamespace;
			readonly TransformContext context;

			public readonly HashSet<string> DeclaredNamespaces = new HashSet<string>() { string.Empty };
			public readonly HashSet<string> ImportedNamespaces = new HashSet<string>();
			/// <summary>Namespaces the visitor verified against the type system itself
			/// (qualified chains that carry no resolve results in the bridge-produced tree).</summary>
			public readonly HashSet<string> VerifiedNamespaces = new HashSet<string>();
			
			public FindRequiredImports(TransformContext context)
			{
				this.context = context;
				this.currentNamespace = context.DecompiledTypeDefinition?.Namespace ?? string.Empty;
			}

			
			bool IsParentOfCurrentNamespace(string ns)
			{
				if (ns.Length == 0)
					return true;
				if (currentNamespace.StartsWith(ns, StringComparison.Ordinal)) {
					if (currentNamespace.Length == ns.Length)
						return true;
					if (currentNamespace[ns.Length] == '.')
						return true;
				}
				return false;
			}
			
			public override void VisitSimpleType(SimpleType simpleType)
			{
				var trr = simpleType.Annotation<TypeResolveResult>();
				if (trr != null && !IsParentOfCurrentNamespace(trr.Type.Namespace)) {
					ImportedNamespaces.Add(trr.Type.Namespace);
				}
				base.VisitSimpleType(simpleType); // also visit type arguments
			}

			public override void VisitMemberType(MemberType memberType)
			{
				// The dnlib->cecil bridge emits fully qualified chains without resolve-result
				// annotations; resolve them against the type system so their namespaces are
				// imported like every other type reference.
				if (memberType.Annotation<TypeResolveResult>() == null && !(memberType.Parent is AstType)) {
					var type = FindTypeByQualifiedName(memberType);
					if (type != null && type.Namespace.Length > 0 && !IsParentOfCurrentNamespace(type.Namespace)) {
						ImportedNamespaces.Add(type.Namespace);
						VerifiedNamespaces.Add(type.Namespace);
					}
				}
				base.VisitMemberType(memberType);
			}

			/// <summary>
			/// Resolves a qualified name chain ('System.Threading.Thread') against the type
			/// system. The generic arity comes from the type arguments on each segment; nested
			/// types resolve through their dotted full name. Returns null when nothing matches.
			/// </summary>
			IType FindTypeByQualifiedName(AstType outermost)
			{
				var parts = new List<(string name, int arity)>();
				AstType t = outermost;
				while (t is MemberType memberType) {
					parts.Add((memberType.MemberName, memberType.TypeArguments.Count));
					t = memberType.Target;
				}
				if (t is SimpleType simpleType)
					parts.Add((simpleType.Identifier, simpleType.TypeArguments.Count));
				else
					return null;
				parts.Reverse();
				var fullName = new System.Text.StringBuilder();
				foreach (var (name, arity) in parts) {
					if (fullName.Length > 0)
						fullName.Append('.');
					fullName.Append(name);
					if (arity > 0)
						fullName.Append('`').Append(arity);
				}
				var compilation = context.TypeSystem.Compilation;
				var type = compilation.FindType(new FullTypeName(fullName.ToString()));
				if (type.Kind == TypeKind.Unknown && parts.Count == 1 && parts[0].arity == 0) {
					type = compilation.FindType(new FullTypeName(fullName.ToString() + "`1"));
				}
				return type.Kind != TypeKind.Unknown ? type : null;
			}
			
			public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
			{
				string oldNamespace = currentNamespace;
				foreach (string ident in namespaceDeclaration.Identifiers) {
					currentNamespace = NamespaceDeclaration.BuildQualifiedName(currentNamespace, ident);
					DeclaredNamespaces.Add(currentNamespace);
				}
				base.VisitNamespaceDeclaration(namespaceDeclaration);
				currentNamespace = oldNamespace;
			}
		}
		
		sealed class FullyQualifyAmbiguousTypeNamesVisitor : DepthFirstAstVisitor
		{
			Stack<CSharpTypeResolveContext> context;
			TypeSystemAstBuilder astBuilder;
			readonly TransformContext transformContext;
			
			public FullyQualifyAmbiguousTypeNamesVisitor(TransformContext context, UsingScope usingScope)
			{
				this.transformContext = context;
				this.context = new Stack<CSharpTypeResolveContext>();
				if (!string.IsNullOrEmpty(context.DecompiledTypeDefinition?.Namespace)) {
					foreach (string ns in context.DecompiledTypeDefinition.Namespace.Split('.')) {
						usingScope = new UsingScope(usingScope, ns);
					}
				}
				var currentContext = new CSharpTypeResolveContext(context.TypeSystem.MainAssembly, usingScope.Resolve(context.TypeSystem.Compilation), context.DecompiledTypeDefinition);
				this.context.Push(currentContext);
				this.astBuilder = CreateAstBuilder(currentContext);
			}

			static TypeSystemAstBuilder CreateAstBuilder(CSharpTypeResolveContext context)
			{
				return new TypeSystemAstBuilder(new CSharpResolver(context)) {
					AddResolveResultAnnotations = true,
					UseAliases = true
				};
			}
			
			public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
			{
				var previousContext = context.Peek();
				var usingScope = previousContext.CurrentUsingScope.UnresolvedUsingScope;
				foreach (string ident in namespaceDeclaration.Identifiers) {
					usingScope = new UsingScope(usingScope, ident);
				}
				var currentContext = new CSharpTypeResolveContext(previousContext.CurrentAssembly, usingScope.Resolve(previousContext.Compilation));
				context.Push(currentContext);
				try {
					astBuilder = CreateAstBuilder(currentContext);
					base.VisitNamespaceDeclaration(namespaceDeclaration);
				} finally {
					astBuilder = CreateAstBuilder(previousContext);
					context.Pop();
				}
			}
			
			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				var previousContext = context.Peek();
				var currentContext = previousContext.WithCurrentTypeDefinition(typeDeclaration.GetSymbol() as ITypeDefinition);
				context.Push(currentContext);
				try {
					astBuilder = CreateAstBuilder(currentContext);
					base.VisitTypeDeclaration(typeDeclaration);
				} finally {
					astBuilder = CreateAstBuilder(previousContext);
					context.Pop();
				}
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				if (methodDeclaration.GetSymbol() is IMethod method && CSharpDecompiler.IsWindowsFormsInitializeComponentMethod(method)) {
					var previousContext = context.Peek();
					var currentContext = new CSharpTypeResolveContext(previousContext.CurrentAssembly);
					context.Push(currentContext);
					try {
						astBuilder = CreateAstBuilder(currentContext);
						base.VisitMethodDeclaration(methodDeclaration);
					} finally {
						astBuilder = CreateAstBuilder(previousContext);
						context.Pop();
					}
				} else {
					base.VisitMethodDeclaration(methodDeclaration);
				}
			}

			public override void VisitSimpleType(SimpleType simpleType)
			{
				TypeResolveResult rr;
				if ((rr = simpleType.Annotation<TypeResolveResult>()) == null) {
					base.VisitSimpleType(simpleType);
					return;
				}
				ReplaceWithTypeReference(simpleType, rr);
			}

			public override void VisitMemberType(MemberType memberType)
			{
				// The dnlib->cecil bridge emits fully qualified chains without resolve results;
				// resolve them now that the using declarations are known so the names shorten.
				if (memberType.Annotation<TypeResolveResult>() == null && !(memberType.Parent is AstType)) {
					var type = ResolveQualifiedName(memberType);
					if (type != null) {
						ReplaceWithTypeReference(memberType, new TypeResolveResult(type));
						return;
					}
				}
				base.VisitMemberType(memberType);
			}

			void ReplaceWithTypeReference(AstType typeNode, TypeResolveResult rr)
			{
				astBuilder.NameLookupMode = typeNode.GetNameLookupMode();
				if (astBuilder.NameLookupMode == NameLookupMode.Type) {
					AstType outermostType = typeNode;
					while (outermostType.Parent is AstType)
						outermostType = (AstType)outermostType.Parent;
					if (outermostType.Parent is TypeReferenceExpression) {
						// ILSpy uses TypeReferenceExpression in expression context even when the C# parser
						// wouldn't know that it's a type reference.
						// Fall back to expression-mode lookup in these cases:
						astBuilder.NameLookupMode = NameLookupMode.Expression;
					}
				}
				if (typeNode.Parent is Syntax.Attribute) {
					typeNode.ReplaceWith(astBuilder.ConvertAttributeType(rr.Type));
				} else {
					typeNode.ReplaceWith(astBuilder.ConvertType(rr.Type));
				}
			}

			/// <summary>
			/// Resolves a qualified name chain against the type system; same rules as
			/// FindRequiredImports.FindTypeByQualifiedName.
			/// </summary>
			IType ResolveQualifiedName(AstType outermost)
			{
				var parts = new List<(string name, int arity)>();
				AstType t = outermost;
				while (t is MemberType memberType) {
					parts.Add((memberType.MemberName, memberType.TypeArguments.Count));
					t = memberType.Target;
				}
				if (t is SimpleType simpleType)
					parts.Add((simpleType.Identifier, simpleType.TypeArguments.Count));
				else
					return null;
				parts.Reverse();
				var fullName = new System.Text.StringBuilder();
				foreach (var (name, arity) in parts) {
					if (fullName.Length > 0)
						fullName.Append('.');
					fullName.Append(name);
					if (arity > 0)
						fullName.Append('`').Append(arity);
				}
				var compilation = transformContext.TypeSystem.Compilation;
				var type = compilation.FindType(new FullTypeName(fullName.ToString()));
				if (type.Kind == TypeKind.Unknown && parts.Count == 1 && parts[0].arity == 0) {
					type = compilation.FindType(new FullTypeName(fullName.ToString() + "`1"));
				}
				return type.Kind != TypeKind.Unknown ? type : null;
			}
		}
	}
}
