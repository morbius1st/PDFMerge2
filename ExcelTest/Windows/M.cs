#region + Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

// user name: jeffs
// created:   11/9/2023 7:04:02 PM

namespace ExcelTest.Windows
{
	public static class Mx
	{
		public static MainWindow mw { get; set; }

		public static void WriteLine(string text)
		{
			Write(text + "\n");
		}

		public static void Write(string text)
		{
			mw.Messages += text;
		}


	}
}
