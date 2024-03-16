#region + Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ExcelTest.SheetSchedule.ColumnType;
using static ExcelTest.SheetSchedule.ColumnFormat;
using static ExcelTest.SheetSchedule.ColumnSubject;
using static ExcelTest.SheetSchedule.RowType;

#endregion

// user name: jeffs
// created:   11/12/2023 7:11:26 AM

namespace ExcelTest.SheetSchedule
{
	// describs a column title only
	public struct ColumnTitle
	{

		public ColumnSubject ColumnSubject { get; set; }
		public string Heading { get; set; }
		public bool IsVariableQty { get; set; }
		public ColumnFormat ColumnFormat { get; set; }

		public Dictionary<RowType, ColumnType> ColumnInfo { get; }

		public bool Found { get; set; }

		public ColumnTitle(
			ColumnSubject columnSubject,
			ColumnFormat columnFormat,
			string heading,
			ColumnType xlsx_Type,
			ColumnType pdfI_Type,
			ColumnType pdfC_Type,
			bool isVariable = false)

		{
			ColumnSubject = columnSubject;
			Heading = heading;
			ColumnFormat = columnFormat;
			IsVariableQty = isVariable;

			ColumnInfo = new Dictionary<RowType, ColumnType>();
			ColumnInfo.Add(RT_LIST, xlsx_Type);
			ColumnInfo.Add(RT_SHEET, pdfI_Type);
			ColumnInfo.Add(RT_PDF, pdfC_Type);

			Found = false;
		}

		public override string ToString()
		{
			return $"{ColumnSubject} | {Heading} | {Found}";
		}
	}


	public class ColumnData
	{

		public const int MAX_TITLE_LEN = 9;

		public static readonly string RT_HEADING_S = RowType.RT_HEADING.ToString().Trim().Substring(3).ToLower();
		public static readonly string RT_LIST_S = RowType.RT_LIST.ToString().Trim().Substring(3).ToLower();
		public static readonly string RT_SHEET_S = RowType.RT_SHEET.ToString().Trim().Substring(3).ToLower();
		public static readonly string RT_PDF_S = RowType.RT_PDF.ToString().Trim().Substring(3).ToLower();

		public static readonly Dictionary<string, RowType> RowTypeList = new ()
		{
			{ RT_HEADING_S, RT_HEADING },
			{ RT_LIST_S, RT_LIST },
			{ RT_SHEET_S, RT_SHEET },
			{ RT_PDF_S, RT_PDF }
		};

		public static int ColumnIdx { get; set; } = 0;
		public int NumVariableCols { get; set; } = 0;

		public bool Init { get; }

		public List<ColumnTitle>  ColumnTitles { get; private set; }

		public Dictionary<string, ColumnTitle> ColumnTitles2 { get; private set; }

		public bool?[] ColumnTitlesOk { get; private set; }

		public void ResetColTitleOk()
		{
			ColumnTitlesOk = new bool?[] { null, null, null };
		}
	
		public ColumnData()
		{
			init();
			Init = true;

			ColumnIdx = 0;
			// MinColumns = ColumnTitles!.Count;
		}

		private void init()
		{
			ResetColTitleOk();

			ColumnTitles = new List<ColumnTitle>(10);
			//                                                                                (0)  list    (1) sheet      (2) pdf      var
			ColumnTitles.Add(new ColumnTitle(CS_TYPE,       CF_STRING, "[type]"              , CT_REQD,     CT_REQD,      CT_REQD,     false));
			ColumnTitles.Add(new ColumnTitle(CS_HEADING,    CF_STRING, "[heading-{0}]"       , CT_REQD,     CT_REQD,      CT_REQD,     true));
			ColumnTitles.Add(new ColumnTitle(CS_X_REL_PATH, CF_STRING, "[xlsx relative path]", CT_OPTIONAL, CT_IGNORE,    CT_OPTIONAL, false));
			ColumnTitles.Add(new ColumnTitle(CS_FILE_NAME,  CF_STRING, "[file name]"         , CT_REQD,     CT_IGNORE,    CT_REQD,     false));
			ColumnTitles.Add(new ColumnTitle(CS_P_REL_PATH, CF_STRING, "[pdf relative path]" , CT_OPTIONAL, CT_OPTIONAL,  CT_OPTIONAL, false));
			ColumnTitles.Add(new ColumnTitle(CS_SHT_NUM,    CF_STRING, "[sheet number]"      , CT_IGNORE,   CT_REQD,      CT_IGNORE,   false));
			ColumnTitles.Add(new ColumnTitle(CS_SHT_NAME,   CF_STRING, "[sheet name]"        , CT_IGNORE,   CT_REQD,      CT_IGNORE,   false));
			ColumnTitles.Add(new ColumnTitle(CS_KEEP,       CF_BOOL,   "[keep]"              , CT_IGNORE,   CT_OPTIONAL,  CT_REQD,     false));

			init2();
		}

		private void init2()
		{
			ColumnTitles2 = new Dictionary<string, ColumnTitle>();

			foreach (ColumnTitle ct in ColumnTitles)
			{
				int len = ct.Heading.Length < MAX_TITLE_LEN ? ct.Heading.Length : MAX_TITLE_LEN;
				ColumnTitles2.Add(ct.Heading.Substring(0,len), ct);

				if (ct.IsVariableQty) NumVariableCols++;
			}
		}

		public override string ToString()
		{
			return $"this is {nameof(ColumnData)}| init| {Init}| count| {ColumnTitles.Count}";
		}
	}
}