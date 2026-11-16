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
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Disassembles a method body.
	/// </summary>
	public class MethodBodyDisassembler
	{
		readonly ITextOutput output;
		readonly CancellationToken cancellationToken;

		/// <summary>
		/// Show .try/finally as blocks in IL code; indent loops.
		/// </summary>
		public bool DetectControlStructure { get; set; } = true;

		/// <summary>
		/// Show sequence points if debug information is loaded in Cecil.
		/// </summary>
		public bool ShowSequencePoints { get; set; }

		/// <summary>
		/// Optional provider of the method-body header comment lines (the 2.3 engine's
		/// <c>// Header Size / Code Size / LocalVarSig Token</c> block). When set, the default
		/// <c>// Method begins at RVA ...</c> / <c>// Code size ...</c> lines are replaced by the
		/// returned lines (which include their own <c>//</c> prefix).
		/// </summary>
		public Func<MethodDefinition, IEnumerable<string>> GetBodyHeaderComments;

		/// <summary>
		/// Optional provider of local variable names, indexed by local slot. A null entry (or a
		/// null array) leaves the name out, matching the 2.3 engine for methods without symbols.
		/// </summary>
		public Func<MethodDefinition, string[]> GetLocalNames;

		/// <summary>
		/// dnSpy's 2.3 IL layout: pad the operand column to
		/// <see cref="OperandAlignment"/> and use upper-case IL labels. Off by default, which
		/// matches upstream ILSpy 3.2 (single space, lower-case labels).
		/// </summary>
		public bool UseDnSpyLayout { get; set; }

		/// <summary>Operand column width used when <see cref="UseDnSpyLayout"/> is set.</summary>
		public const int OperandAlignment = 10;

		/// <summary>
		/// Max length of a string operand. 2.3's option; the default (int.MaxValue) keeps the
		/// engine's original untruncated output.
		/// </summary>
		public int MaxStringLength { get; set; } = int.MaxValue;

		/// <summary>
		/// Format integer operands (and the .maxstack / local index numbers) as hexadecimal,
		/// matching 2.3's HexadecimalNumbers option. Off by default.
		/// </summary>
		public bool HexadecimalNumbers { get; set; }

		/// <summary>
		/// Optional opcode documentation provider (2.3's ShowILComments). When set, every
		/// instruction is followed by a tab and a <c>// </c> comment with the documentation.
		/// </summary>
		public Func<OpCode, string> GetOpCodeDocumentation { get; set; }

		NumberFormatter OperandNumberFormatter => HexadecimalNumbers ? NumberFormatter.GetCSharpInstance(hex: true, upper: true) : null;

		Collection<SequencePoint> sequencePoints;
		int nextSequencePointIndex;
		string[] localNames;

		public MethodBodyDisassembler(ITextOutput output, CancellationToken cancellationToken)
		{
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.cancellationToken = cancellationToken;
		}

		public virtual void Disassemble(MethodBody body)
		{
			// start writing IL code
			MethodDefinition method = body.Method;
			localNames = GetLocalNames?.Invoke(method);
			if (GetBodyHeaderComments != null) {
				foreach (var line in GetBodyHeaderComments(method))
					output.WriteLine(line);
			}
			else {
				output.WriteLine("// Method begins at RVA 0x{0:x4}", method.RVA);
				output.WriteLine("// Code size {0} (0x{0:x})", body.CodeSize);
			}
			var numberFormatter = OperandNumberFormatter;
			output.Write(".maxstack ");
			if (numberFormatter != null)
				output.Write(numberFormatter.Format(body.MaxStackSize));
			else
				output.Write(body.MaxStackSize.ToString());
			output.WriteLine();
			if (method.DeclaringType.Module.Assembly != null && method.DeclaringType.Module.Assembly.EntryPoint == method)
				output.WriteLine(".entrypoint");

			DisassembleLocalsBlock(body);
			output.WriteLine();

			sequencePoints = method.DebugInformation?.SequencePoints;
			nextSequencePointIndex = 0;
			if (DetectControlStructure && body.Instructions.Count > 0) {
				Instruction inst = body.Instructions[0];
				HashSet<int> branchTargets = GetBranchTargets(body.Instructions);
				WriteStructureBody(new ILStructure(body), branchTargets, ref inst, method.Body.CodeSize);
			} else {
				foreach (var inst in method.Body.Instructions) {
					WriteInstruction(output, inst);
					output.WriteLine();
				}
				WriteExceptionHandlers(body);
			}
			sequencePoints = null;
		}

		private void DisassembleLocalsBlock(MethodBody body)
		{
			if (body.HasVariables) {
				output.Write(".locals ");
				if (body.InitLocals)
					output.Write("init ");
				output.WriteLine("(");
				output.Indent();
				var localsNumberFormatter = OperandNumberFormatter;
				foreach (var v in body.Variables) {
					var indexText = localsNumberFormatter != null ? localsNumberFormatter.Format(v.Index) : v.Index.ToString();
					output.WriteDefinition("[" + indexText + "] ", v);
					v.VariableType.WriteTo(output);
					string localName = localNames != null && v.Index >= 0 && v.Index < localNames.Length ? localNames[v.Index] : null;
					if (!string.IsNullOrEmpty(localName))
						output.Write(" " + DisassemblerHelpers.Escape(localName));
					if (v.Index + 1 < body.Variables.Count)
						output.Write(',');
					output.WriteLine();
				}
				output.Unindent();
				output.WriteLine(")");
			}
		}

		internal void WriteExceptionHandlers(MethodBody body)
		{
			if (body.HasExceptionHandlers) {
				output.WriteLine();
				foreach (var eh in body.ExceptionHandlers) {
					eh.WriteTo(output, UseDnSpyLayout);
					output.WriteLine();
				}
			}
		}

		HashSet<int> GetBranchTargets(IEnumerable<Instruction> instructions)
		{
			HashSet<int> branchTargets = new HashSet<int>();
			foreach (var inst in instructions) {
				Instruction target = inst.Operand as Instruction;
				if (target != null)
					branchTargets.Add(target.Offset);
				Instruction[] targets = inst.Operand as Instruction[];
				if (targets != null)
					foreach (Instruction t in targets)
						branchTargets.Add(t.Offset);
			}
			return branchTargets;
		}

		void WriteStructureHeader(ILStructure s)
		{
			switch (s.Type) {
				case ILStructureType.Loop:
					output.Write("// loop start");
					if (s.LoopEntryPoint != null) {
						output.Write(" (head: ");
						DisassemblerHelpers.WriteOffsetReference(output, s.LoopEntryPoint, UseDnSpyLayout);
						output.Write(')');
					}
					output.WriteLine();
					break;
				case ILStructureType.Try:
					output.WriteLine(".try");
					output.WriteLine("{");
					break;
				case ILStructureType.Handler:
					switch (s.ExceptionHandler.HandlerType) {
						case Mono.Cecil.Cil.ExceptionHandlerType.Catch:
						case Mono.Cecil.Cil.ExceptionHandlerType.Filter:
							output.Write("catch");
							if (s.ExceptionHandler.CatchType != null) {
								output.Write(' ');
								s.ExceptionHandler.CatchType.WriteTo(output, ILNameSyntax.TypeName);
							}
							output.WriteLine();
							break;
						case Mono.Cecil.Cil.ExceptionHandlerType.Finally:
							output.WriteLine("finally");
							break;
						case Mono.Cecil.Cil.ExceptionHandlerType.Fault:
							output.WriteLine("fault");
							break;
						default:
							throw new NotSupportedException();
					}
					output.WriteLine("{");
					break;
				case ILStructureType.Filter:
					output.WriteLine("filter");
					output.WriteLine("{");
					break;
				default:
					throw new NotSupportedException();
			}
			output.Indent();
		}

		void WriteStructureBody(ILStructure s, HashSet<int> branchTargets, ref Instruction inst, int codeSize)
		{
			bool isFirstInstructionInStructure = true;
			bool prevInstructionWasBranch = false;
			int childIndex = 0;
			while (inst != null && inst.Offset < s.EndOffset) {
				int offset = inst.Offset;
				if (childIndex < s.Children.Count && s.Children[childIndex].StartOffset <= offset && offset < s.Children[childIndex].EndOffset) {
					ILStructure child = s.Children[childIndex++];
					WriteStructureHeader(child);
					WriteStructureBody(child, branchTargets, ref inst, codeSize);
					WriteStructureFooter(child);
				} else {
					if (!isFirstInstructionInStructure && (prevInstructionWasBranch || branchTargets.Contains(offset))) {
						output.WriteLine();	// put an empty line after branches, and in front of branch targets
					}
					WriteInstruction(output, inst);
					output.WriteLine();

					prevInstructionWasBranch = inst.OpCode.FlowControl == FlowControl.Branch
						|| inst.OpCode.FlowControl == FlowControl.Cond_Branch
						|| inst.OpCode.FlowControl == FlowControl.Return
						|| inst.OpCode.FlowControl == FlowControl.Throw;

					inst = inst.Next;
				}
				isFirstInstructionInStructure = false;
			}
		}

		void WriteStructureFooter(ILStructure s)
		{
			output.Unindent();
			switch (s.Type) {
				case ILStructureType.Loop:
					output.WriteLine("// end loop");
					break;
				case ILStructureType.Try:
					output.WriteLine("} // end .try");
					break;
				case ILStructureType.Handler:
					output.WriteLine("} // end handler");
					break;
				case ILStructureType.Filter:
					output.WriteLine("} // end filter");
					break;
				default:
					throw new NotSupportedException();
			}
		}

		protected virtual void WriteInstruction(ITextOutput output, Instruction instruction)
		{
			if (ShowSequencePoints && nextSequencePointIndex < sequencePoints?.Count) {
				SequencePoint sp = sequencePoints[nextSequencePointIndex];
				if (sp.Offset <= instruction.Offset) {
					output.Write("// sequence point: ");
					if (sp.Offset != instruction.Offset) {
						output.Write("!! at " + DisassemblerHelpers.OffsetToString(sp.Offset) + " !!");
					}
					if (sp.IsHidden) {
						output.WriteLine("hidden");
					} else {
						output.WriteLine($"(line {sp.StartLine}, col {sp.StartColumn}) to (line {sp.EndLine}, col {sp.EndColumn}) in {sp.Document?.Url}");
					}
					nextSequencePointIndex++;
				}
			}
			instruction.WriteTo(output, UseDnSpyLayout, UseDnSpyLayout ? OperandAlignment : 0, localNames, MaxStringLength, OperandNumberFormatter, GetOpCodeDocumentation);
		}
	}
}
