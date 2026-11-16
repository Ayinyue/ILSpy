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
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Cecil equivalents of the 2.3 engine's dnSpy member comparers
	/// (dnSpy.Contracts.Logic MemberComparer). The sort keys are the same: name
	/// (OrdinalIgnoreCase), metadata token, hash code; methods first compare the
	/// parameter count and the generic parameter count. The engine sorts with
	/// <see cref="Array.Sort{T}(T[], IComparer{T})"/>, which is not stable, exactly
	/// like the 2.3 dnlib extension methods did.
	/// </summary>
	public static class CecilMemberComparer
	{
		public static IComparer<TypeDefinition> TypeDef { get; } = new TypeDefComparer();
		public static IComparer<FieldDefinition> FieldDef { get; } = new FieldDefComparer();
		public static IComparer<EventDefinition> EventDef { get; } = new EventDefComparer();
		public static IComparer<PropertyDefinition> PropertyDef { get; } = new PropertyDefComparer();
		public static IComparer<MethodDefinition> MethodDef { get; } = new MethodDefComparer();

		static int CompareName(string x, string y) => StringComparer.OrdinalIgnoreCase.Compare(x, y);

		static int CompareToken(IMetadataTokenProvider x, IMetadataTokenProvider y) =>
			x.MetadataToken.ToInt32().CompareTo(y.MetadataToken.ToInt32());

		sealed class TypeDefComparer : IComparer<TypeDefinition>
		{
			public int Compare(TypeDefinition x, TypeDefinition y) {
				if (ReferenceEquals(x, y))
					return 0;
				if (x is null)
					return -1;
				if (y is null)
					return 1;
				int c = CompareName(x.Name, y.Name);
				if (c != 0)
					return c;
				c = CompareToken(x, y);
				if (c != 0)
					return c;
				return x.GetHashCode().CompareTo(y.GetHashCode());
			}
		}

		sealed class FieldDefComparer : IComparer<FieldDefinition>
		{
			public int Compare(FieldDefinition x, FieldDefinition y) {
				if (ReferenceEquals(x, y))
					return 0;
				if (x is null)
					return -1;
				if (y is null)
					return 1;
				int c = CompareName(x.Name, y.Name);
				if (c != 0)
					return c;
				c = CompareToken(x, y);
				if (c != 0)
					return c;
				return x.GetHashCode().CompareTo(y.GetHashCode());
			}
		}

		sealed class EventDefComparer : IComparer<EventDefinition>
		{
			public int Compare(EventDefinition x, EventDefinition y) {
				if (ReferenceEquals(x, y))
					return 0;
				if (x is null)
					return -1;
				if (y is null)
					return 1;
				int c = CompareName(x.Name, y.Name);
				if (c != 0)
					return c;
				c = CompareToken(x, y);
				if (c != 0)
					return c;
				return x.GetHashCode().CompareTo(y.GetHashCode());
			}
		}

		sealed class PropertyDefComparer : IComparer<PropertyDefinition>
		{
			public int Compare(PropertyDefinition x, PropertyDefinition y) {
				if (ReferenceEquals(x, y))
					return 0;
				if (x is null)
					return -1;
				if (y is null)
					return 1;
				int c = CompareName(x.Name, y.Name);
				if (c != 0)
					return c;
				c = CompareToken(x, y);
				if (c != 0)
					return c;
				return x.GetHashCode().CompareTo(y.GetHashCode());
			}
		}

		sealed class MethodDefComparer : IComparer<MethodDefinition>
		{
			public int Compare(MethodDefinition x, MethodDefinition y) {
				if (ReferenceEquals(x, y))
					return 0;
				if (x is null)
					return -1;
				if (y is null)
					return 1;
				int c = CompareName(x.Name, y.Name);
				if (c != 0)
					return c;
				c = x.Parameters.Count.CompareTo(y.Parameters.Count);
				if (c != 0)
					return c;
				c = x.GenericParameters.Count.CompareTo(y.GenericParameters.Count);
				if (c != 0)
					return c;
				c = CompareToken(x, y);
				if (c != 0)
					return c;
				return x.GetHashCode().CompareTo(y.GetHashCode());
			}
		}
	}
}
