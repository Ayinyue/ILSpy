// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

// compile:
//     csc BooleanDisjunction.cs /t:Library /langversion:latest && ildasm /text BooleanDisjunction.dll >BooleanDisjunction.il

using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public static class BooleanDisjunction
	{
		public static bool RangeTable(uint cp)
		{
			return cp >= 768U && (cp <= 879U || cp == 1648 || cp == 3328);
		}

		public static bool GuardedLastOperand(uint cp)
		{
			return cp <= 879U || cp == 1648 || (cp >= 917760U && cp <= 917999U);
		}
	}
}
