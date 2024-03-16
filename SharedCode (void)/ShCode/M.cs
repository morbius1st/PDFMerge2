#region + Using Directives

#endregion

// user name: jeffs
// created:   11/9/2023 7:04:02 PM

// namespace ExcelTest.Windows
using System.Diagnostics;

namespace SharedCode.ShCode
{
	public static class M
	{
		public static IMainWin mw { get; set; }

		[DebuggerStepThrough]
		public static void WriteLine(string? text)
		{
			if (text == null) return;

			Write(text + "\n");
		}

		[DebuggerStepThrough]
		public static void Write(string? text)
		{
			if (text == null) return;

			mw.Messages += text;
		}


	}
}
