#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using ExcelTest.SheetSchedule;
using UtilityLibrary;

#endregion

// username: jeffs
// created:  11/7/2023 7:25:23 PM

namespace ExcelTest.Excel
{
	public class ExcelSupport
	{
	#region private fields

	#endregion

	#region ctor

		public ExcelSupport() { }

	#endregion

	#region public properties

	#endregion

	#region private properties

	#endregion

	#region public methods

		public DataSet ReadSchedule(FilePath<FileNameSimple> file)
		{
			DataSet schedule = null;

			using ( FileStream stream = File.Open(file.FullFilePath, 
						FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
				)
			{
				using (IExcelDataReader reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream) )
				{
					schedule = reader.AsDataSet();
				}
			}

			return schedule;
		}




	#endregion

	#region private methods

	#endregion

	#region event consuming

	#endregion

	#region event publishing

	#endregion

	#region system overrides

		public override string ToString()
		{
			return $"this is {nameof(ExcelSupport)}";
		}

	#endregion
	}
}