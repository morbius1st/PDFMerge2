#region + Using Directives
using CommonCode.ShCode;

using CommonPdfCodePdfLibrary;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityLibrary;
using static ExcelTest.SheetSchedule.ColumnSubject;
using static CommonCode.ShCode.Status.StatusData;

#endregion

// user name: jeffs
// created:   11/12/2023 5:13:20 PM

namespace ExcelTest.SheetSchedule
{
	public class PdfDocData : IPdfDataEx
	{
		public PdfFile PdfFile { get; set; }

		public int                          Sequence { get; set; }
		public RowType                      RowType { get; } = RowType.RT_PDF;
		public List<string?>?               Headings { get; set;}
		public bool                         KeepBookmarks { get; private set;}

		// public                           PdfDataStatus Status { get; private set; }
		public Status.StatusData.ErrorCodes Status { get; private set; }

		public string?                      PdfPath { get; private set; }

		public IFilePath                    File => PdfFile.File;
		public string?                      FilePath { get; private set;}

		public int                          PageCount => (PdfFile?.File?.Exists ?? false) ? PdfFile.PageCount : 0; 

		public string                       Bookmark => Headings?[^1] ?? "null";

		public PdfDocData()
		{
			Sequence = -1;
			Headings = new List<string?>();
			FilePath = null;

			PdfPath = null;
			KeepBookmarks = false;
		}


		public PdfDocData(
			int sequence, 
			List<string?>? headings,
			string? fileAndPath,
			string? pPath,
			bool keepBookmarks) 
		{
			Sequence = sequence;
			Headings = headings;
			FilePath = fileAndPath;

			PdfPath = pPath;

			KeepBookmarks = keepBookmarks;
		}

		public bool SetValue(ColumnSubject cs, PdfFile value)
		{
			if (value == null) return false;

			PdfFile = value;

			if (!PdfFile.File.Exists)
			{
				Status =  ErrorCodes.EC_FILE_MISSING;
				return false;
			}

			Status = ErrorCodes.EC_NO_ERROR;

			if (PdfFile.HasOutlineError)
			{
				Headings[0] = "** Error ** " + Headings[0];
			}

			return true;
		}

		public bool SetValue(ColumnSubject cs, string value)
		{
			value = value.Trim();

			switch (cs)
			{
			case CS_HEADING:
				{
					Headings.Add(value);
					break;
				}
			case CS_FILE_NAME:
				{
					FilePath = value;
					break;
				}
			case CS_P_REL_PATH:
				{
					PdfPath = value;
					break;
				}
			case CS_KEEP:
				{
					value = value.ToLower();
					if (value.Equals("yes") || value.Equals("true"))
					{
						KeepBookmarks = true;
					}
					else
					{
						KeepBookmarks = false;
					}

					break;
				}
			default:
				{
					return false;
					break;
				}
			}

			return true;
		}

		public override string ToString()
		{
			return $"{RowType}| {Headings?[0] ?? "none"}| {FilePath ?? "none"}";
		}
	}
}
