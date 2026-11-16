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

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Formats the numeric IL operands. This is the 3.2 engine's equivalent of the 2.3
	/// NRefactory <c>NumberFormatter</c>; the hex layout (0x prefix, upper-case digits,
	/// decimal for values 0-9, sign + two's complement for negatives) is identical so the
	/// HexadecimalNumbers option produces the same text as 2.3.
	/// </summary>
	public abstract class NumberFormatter
	{
		public abstract string Format(sbyte value);
		public abstract string Format(byte value);
		public abstract string Format(short value);
		public abstract string Format(ushort value);
		public abstract string Format(int value);
		public abstract string Format(uint value);
		public abstract string Format(long value);
		public abstract string Format(ulong value);

		public static NumberFormatter GetCSharpInstance(bool hex, bool upper) {
			if (hex)
				return upper ? csharpHexUpper : csharpHexLower;
			return decimalFormatter;
		}

		static readonly DecimalNumberFormatter decimalFormatter = new DecimalNumberFormatter();
		static readonly HexNumberFormatter csharpHexUpper = new HexNumberFormatter("0x", upper: true);
		static readonly HexNumberFormatter csharpHexLower = new HexNumberFormatter("0x", upper: false);
	}

	sealed class DecimalNumberFormatter : NumberFormatter
	{
		public override string Format(sbyte value) => value.ToString();
		public override string Format(byte value) => value.ToString();
		public override string Format(short value) => value.ToString();
		public override string Format(ushort value) => value.ToString();
		public override string Format(int value) => value.ToString();
		public override string Format(uint value) => value.ToString();
		public override string Format(long value) => value.ToString();
		public override string Format(ulong value) => value.ToString();
	}

	sealed class HexNumberFormatter : NumberFormatter
	{
		readonly string prefix;
		readonly bool upper;

		public HexNumberFormatter(string prefix, bool upper) {
			this.prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
			this.upper = upper;
		}

		string Create(string s) => prefix + s;

		public override string Format(sbyte value) {
			if (value < 0)
				return "-" + Format(unchecked((byte)(-value)));
			return Format((byte)value);
		}

		public override string Format(byte value) => value <= 9 ? value.ToString() : Create(value.ToString(upper ? "X" : "x"));

		public override string Format(short value) {
			if (value < 0)
				return "-" + Format(unchecked((ushort)(-value)));
			return Format((ushort)value);
		}

		public override string Format(ushort value) => value <= 9 ? value.ToString() : Create(value.ToString(upper ? "X" : "x"));

		public override string Format(int value) {
			if (value < 0)
				return "-" + Format(unchecked((uint)(-value)));
			return Format((uint)value);
		}

		public override string Format(uint value) => value <= 9 ? value.ToString() : Create(value.ToString(upper ? "X" : "x"));

		public override string Format(long value) {
			if (value < 0)
				return "-" + Format(unchecked((ulong)(-value)));
			return Format((ulong)value);
		}

		public override string Format(ulong value) => value <= 9 ? value.ToString() : Create(value.ToString(upper ? "X" : "x"));
	}
}
