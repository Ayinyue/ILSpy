using System;
using System.IO;

namespace ICSharpCode.Decompiler.IL.ControlFlow {
	/// <summary>
	/// Temporary diagnostics for the iterator/async rewrite gap: records why a state
	/// machine was left as a raw kickoff stub, so the dominant rejection causes can be
	/// measured. Gated by DNSPY_ITERATOR_DIAG; inert in normal runs.
	/// </summary>
	static class StateMachineDiagnostics {
		internal static Action<string> Sink;

		public static void Log(string reason) {
			if (Sink == null)
				return;
			try {
				Sink(reason);
			}
			catch {
			}
		}

		public static bool Enabled {
			get { return Sink != null; }
		}

		/// <summary>Diagnostic gate: logs the rejection point and returns false (as the
		/// caller's pattern-match would have).</summary>
		internal static bool Reject(string what) {
			Log(what);
			return false;
		}
	}
}
