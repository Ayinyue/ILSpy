using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL
{
	/// <summary>
	/// Receives the source statements that make up an ILAst method body while it is being
	/// written. Consumers (eg. dnSpy's ILAst view) use this to build debug information
	/// mapping IL ranges to text spans.
	/// </summary>
	public interface ILAstDebugInfoWriter
	{
		/// <summary>
		/// Called once per instruction written by <see cref="Block.WriteTo"/>.
		/// </summary>
		/// <param name="ilRange">IL range covered by the instruction.</param>
		/// <param name="textStart">Text offset of the first character of the instruction.</param>
		/// <param name="textEnd">Text offset one past the last character of the instruction.</param>
		void WriteStatement(Interval ilRange, int textStart, int textEnd);
	}
}
