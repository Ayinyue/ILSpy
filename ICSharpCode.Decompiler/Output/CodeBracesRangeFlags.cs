using System;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Flags describing a pair of matching braces (or other delimiting characters) in
	/// decompiler output. The numeric values are kept identical to
	/// dnSpy.Contracts.Decompiler.CodeBracesRangeFlags so the two enums can be mapped
	/// by a plain cast.
	/// </summary>
	[Flags]
	public enum CodeBracesRangeFlags
	{
		BraceKind_None				= 0,
		BraceKind_Parentheses		= 0x00000001,
		BraceKind_CurlyBraces		= 0x00000002,
		BraceKind_SquareBrackets	= 0x00000003,
		BraceKind_AngleBrackets		= 0x00000004,
		BraceKind_DoubleQuotes		= 0x00000005,
		BraceKind_SingleQuotes		= 0x00000006,
		BraceKind_OtherBraces		= 0x00000007,

		BlockKind_None				= 0,
		BlockKind_Namespace			= 0x00000100,
		BlockKind_Type				= 0x00000200,
		BlockKind_Module			= 0x00000300,
		BlockKind_ValueType			= 0x00000400,
		BlockKind_Interface			= 0x00000500,
		BlockKind_Method			= 0x00000600,
		BlockKind_Accessor			= 0x00000700,
		BlockKind_AnonymousMethod	= 0x00000800,
		BlockKind_Constructor		= 0x00000900,
		BlockKind_Destructor		= 0x00000A00,
		BlockKind_Operator			= 0x00000B00,
		BlockKind_Conditional		= 0x00000C00,
		BlockKind_Loop				= 0x00000D00,
		BlockKind_Property			= 0x00000E00,
		BlockKind_Event				= 0x00000F00,
		BlockKind_Try				= 0x00001000,
		BlockKind_Catch				= 0x00001100,
		BlockKind_Filter			= 0x00001200,
		BlockKind_Finally			= 0x00001300,
		BlockKind_Fault				= 0x00001400,
		BlockKind_Lock				= 0x00001500,
		BlockKind_Using				= 0x00001600,
		BlockKind_Fixed				= 0x00001700,
		BlockKind_Switch			= 0x00001800,
		BlockKind_Case				= 0x00001900,
		BlockKind_LocalFunction		= 0x00001A00,
		BlockKind_Other				= 0x00001B00,
		BlockKind_Xml				= 0x00001C00,
		BlockKind_Xaml				= 0x00001D00,

		NamespaceBraces				= BraceKind_CurlyBraces | BlockKind_Namespace,
		TypeBraces					= BraceKind_CurlyBraces | BlockKind_Type,
		ModuleBraces				= BraceKind_CurlyBraces | BlockKind_Module,
		ValueTypeBraces				= BraceKind_CurlyBraces | BlockKind_ValueType,
		InterfaceBraces				= BraceKind_CurlyBraces | BlockKind_Interface,
		MethodBraces				= BraceKind_CurlyBraces | BlockKind_Method,
		AccessorBraces				= BraceKind_CurlyBraces | BlockKind_Accessor,
		AnonymousMethodBraces		= BraceKind_CurlyBraces | BlockKind_AnonymousMethod,
		ConstructorBraces			= BraceKind_CurlyBraces | BlockKind_Constructor,
		DestructorBraces			= BraceKind_CurlyBraces | BlockKind_Destructor,
		OperatorBraces				= BraceKind_CurlyBraces | BlockKind_Operator,
		ConditionalBraces			= BraceKind_CurlyBraces | BlockKind_Conditional,
		LoopBraces					= BraceKind_CurlyBraces | BlockKind_Loop,
		PropertyBraces				= BraceKind_CurlyBraces | BlockKind_Property,
		EventBraces					= BraceKind_CurlyBraces | BlockKind_Event,
		TryBraces					= BraceKind_CurlyBraces | BlockKind_Try,
		CatchBraces					= BraceKind_CurlyBraces | BlockKind_Catch,
		FilterBraces				= BraceKind_CurlyBraces | BlockKind_Filter,
		FinallyBraces				= BraceKind_CurlyBraces | BlockKind_Finally,
		FaultBraces					= BraceKind_CurlyBraces | BlockKind_Fault,
		LockBraces					= BraceKind_CurlyBraces | BlockKind_Lock,
		UsingBraces					= BraceKind_CurlyBraces | BlockKind_Using,
		FixedBraces					= BraceKind_CurlyBraces | BlockKind_Fixed,
		SwitchBraces				= BraceKind_CurlyBraces | BlockKind_Switch,
		CaseBraces					= BraceKind_CurlyBraces | BlockKind_Case,
		LocalFunctionBraces			= BraceKind_CurlyBraces | BlockKind_LocalFunction,
		OtherBlockBraces			= BraceKind_CurlyBraces | BlockKind_Other,
		XmlBlockBraces				= BraceKind_OtherBraces | BlockKind_Xml,
		XamlBlockBraces				= BraceKind_OtherBraces | BlockKind_Xaml,

		SingleQuotes				= BraceKind_SingleQuotes | BlockKind_None,
		DoubleQuotes				= BraceKind_DoubleQuotes | BlockKind_None,
		Parentheses					= BraceKind_Parentheses | BlockKind_None,
		AngleBrackets				= BraceKind_AngleBrackets | BlockKind_None,
		SquareBrackets				= BraceKind_SquareBrackets | BlockKind_None,
		CurlyBraces					= BraceKind_CurlyBraces | BlockKind_None,
		OtherBraces					= BraceKind_OtherBraces | BlockKind_None,
	}
}
