#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ExcelTest.Excel;
using ExcelTest.Windows;
using iText.Commons.Utils;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using static ExcelTest.SheetSchedule.ColumnType;
using static ExcelTest.SheetSchedule.ColumnFormat;
using static ExcelTest.SheetSchedule.ColumnSubject;
using static ExcelTest.SheetSchedule.RowType;
using iText.Kernel.Pdf.Tagging;
using CommonCode.ShCode;
using CommonPdfCodePdfLibrary;
using CommonPdfCodeShCode;
using UtilityLibrary;
using static CommonCode.ShCode.M;
using static CommonCode.ShCode.Status.StatusData;
using System.Windows.Markup;
using System.Xml;
using SettingsManager;
using DataSet = System.Data.DataSet;

#endregion

// username: jeffs
// created:  11/7/2023 7:28:26 PM

namespace ExcelTest.SheetSchedule
{
	public class SheetScheduleSupport
	{
	#region private fields

		// general

		public const string COMMENT = "<<";
		public const string COL_TITLE_PREFACE = "[";

		private const string PRIME_SCHEDULE = "[primary schedule]";
		private const string SHEET_SCHEDULE = "[sheet schedule]";

		// private readonly string XLSX = MainWindow.EXT_XLSX;
		// private readonly string PDF = MainWindow.EXT_PDF;

		// the column data object
		/// <summary>
		/// The column data object
		/// </summary>
		private ColumnData colData;

		// the actual list of column titles
		// based on what was read from the xlsx file
		// this gets validated against the coldata titles
		// on a per xlsx file basis - however this list
		// will only contain columns based on the row types
		// in the file
		/// <summary>
		/// The list of column titles as read from the xlsx file
		/// </summary>
		private List<ColumnTitle> schColTitleData;

		// the actual data as parsed from the
		// sheet schedule xlsx file
		/// <summary>
		/// The actual data as parsed from a sheet xlsx file
		/// </summary>
		private Dictionary<int, colValue>? colInfo;

		private Dictionary<int, string?>? headerInfo;

		private int colValueIdx = 0;

		// private string? priorHeader = null;
		// private string? priorSubPath = null;

		private PdfNodeTreeSupport ts;
		private PdfNodeTree tree;
		private int nodeTreeCount;

		private struct colValue
		{
			public ColumnSubject Subject { get; }
			public string? Value { get; }

			public colValue(ColumnSubject subject, string? value)
			{
				Subject = subject;
				Value = value;
			}
		}

		private FilePath<FileNameSimple> destFilePath;

		private string rootPath;
		private string schTypeName;
		private PdfSchData parentSch;

		private ExcelSupport xls;
		// private string xlsxFileName;

		private string fileBeingParsed;

		private bool parsingPrimary = false;

		// properties

		private List<IPdfData?>? schedules;
		private List<IPdfDataEx?>? sheets;

		private int pageCount = 1;

		// private PdfFileTree pdfTree;

	#endregion

	#region ctor

		public SheetScheduleSupport()
		{
			xls = new ExcelSupport();
			colData = new ColumnData();
		}

	#endregion

	#region public properties

		public List<IPdfData?> Schedules
		{
			get => schedules;
			set => schedules = value;
		}

		public List<IPdfDataEx?> Sheets
		{
			get => sheets;
			set => sheets = value;
		}

		// public PdfFileTree PdfTree
		// {
		// 	get => pdfTree;
		// 	private set => pdfTree = value;
		// }

		// public bool Status { get; private set; }

		public int PageCount
		{
			get => pageCount;
			private set { pageCount = value; }
		}

	#endregion

	#region public methods

		public void Reset()
		{
			colData = new ColumnData();
			// pdfTree = new PdfFileTree();

			tree = new PdfNodeTree();
			ts = new PdfNodeTreeSupport(tree);

			schColTitleData = new List<ColumnTitle>();
			colInfo = new Dictionary<int, colValue>();
			headerInfo = new Dictionary<int, string?>();

			Schedules = new List<IPdfData?>();
			Sheets = new List<IPdfDataEx?>();

			PageCount = 0;

			W.PbarStatReset();
			W.PbarPhaseReset();

		}

	#endregion

	#region 1 - get destination file

// STEP 1
// phase VD - validate desitnation
		public bool ValidateDestFile(FilePath<FileNameSimple>? dest, bool overwriteOk)
		{
			Status.Phase = Progress.PS_PH_VD;
			W.PbStatValue = (int) Progress.PS_PH_VD;

			destFilePath = dest;

			bool status = true;

			if (dest == null || dest.IsFolderPath)
			{
				Status.SetStatus(ErrorCodes.EC_GD_DEST_INVALID,
					Overall.OS_FAIL,
					$"File| {(dest == null ? "None specified" : dest.FullFilePath)}" );
			}
			else
			{
				if (dest.Exists)
				{
					if (overwriteOk)
					{
						try
						{
							File.Delete(dest.FullFilePath);
						}
						catch
						{
							Status.SetStatus(ErrorCodes.EC_VD_CANNOT_DELETE_FILE,
								Overall.OS_FAIL,
								$"File| {dest.FullFilePath}");
							
							status = false;
						}
					}
					else
					{
						Status.SetStatus(ErrorCodes.EC_DEST_EXISTS,
							Overall.OS_FAIL, "Allow overwrite or provide a unique file name");
						
						status = false;
					}
				}
			}

			if (status)
			{
				Status.SetStatus(ErrorCodes.EC_NO_ERROR,
					Overall.OS_WORKED);
			}

			return status;
		}

	#endregion

	#region 2 - PrimaryScheduleData

// STEP 2
// phase PP - read the primary schedule
		public bool GetSchedule(
			FilePath<FileNameSimple> file)
		{
			Status.Phase = Progress.PS_PH_PP;

			W.PbStatValue = (int) Progress.PS_PH_PP;

			W.PbarPhaseReset();

			bool status = true;
			parsingPrimary = true;

			fileBeingParsed = file.FileName;

			if (file == null || !file.Exists
				|| !file.FileExtensionNoSep.Equals(MainWindow.EXT_XLSX))
			{
				Status.SetStatus(
					ErrorCodes.EC_PP_FILE_MISSING_PRIME,
					Overall.OS_FAIL, file?.FullFilePath ?? "No file specified");
				status = false;
			}
			else
			{
				rootPath = file.FolderPath;

				schedules = new List<IPdfData?>();

				// read schedule to a dataset
				DataSet schedule = xls.ReadSchedule(file);

				// parse the dataset into the List<>
				if (!parseSchData(schedule))
				{
					Status.SetStatus(
						ErrorCodes.EC_PP_SCHED_WRONG_FORMAT,
						Overall.OS_FAIL);
					status = false;
				}
				else
				{
					Status.SetStatus(ErrorCodes.EC_NO_ERROR, Overall.OS_WORKED);
					W.PbPhaseValue++;
				}
			}

			// M.WriteLine($"file provided| {file.FullFilePath}");
			//
			// showPrimeSchedule();
			// status = false;

			return status;
		}

		// process primary schedule

		private bool parseSchData(DataSet xlSchedule)
		{
			// Status.IsGoodLocalSet = true;
			bool status = true;

			DataRowCollection rows = xlSchedule.Tables[0].Rows;

			W.PbPhaseMax = rows.Count + 1;
			W.PbPhaseValue = 1;

			int row = validateSchType(rows, PRIME_SCHEDULE);

			if (row == -1)
			{
				Status.SetStatus(ErrorCodes.EC_PP_PS_SCHED_WRONG_TYPE,
					Overall.OS_FAIL, $"{fileBeingParsed}");

				return false;
			}

			row = getSchTitles(rows, row);

			if (row == -1)
			{
				return false;
			}

			parentSch = new PdfSchData();

			if (parseSchDataRows(row, rows) == 0)
			{
				Status.SetStatus(ErrorCodes.EC_PP_PS_SCHED_EMPTY,
					Overall.OS_FAIL, $"{fileBeingParsed}");
				return false;
			}

			return status;
		}

		private int getSchTitles(DataRowCollection rows, int startRow)
		{
			string? field0;

			for (int i = startRow; i < rows.Count; i++)
			{
				DataRow r = rows[i];

				W.PbPhaseValue++;

				field0 = (r.Field<string>(0) ?? "");

				if (field0.IsVoid() || field0.Trim().StartsWith(COMMENT)) continue;

				// if (!parseSchTitles(r)) break;
				parseSchTitles(r);

				if (validateSchTitles())
				{
					return i;
				}

				break;
			}

			return -1;
		}

		private bool validateSchTitles()
		{
			string? search = null;
			string? reqd = null;
			string? found = null;

			int len;
			int i = 1;

			// Status.IsGoodLocalSet = true;
			bool status = true;

			if (schColTitleData.Count == 0) return false;

			if (parsingPrimary && schColTitleData[0].ColumnSubject != CS_TYPE) return false;

			foreach (ColumnTitle ct in schColTitleData)
			{
				len = ct.Heading.Length;
				len = len > ColumnData.MAX_TITLE_LEN ? ColumnData.MAX_TITLE_LEN : len;
				search = ct.Heading.Substring(0, len);
				found = ct.Heading;
				reqd = colData.ColumnTitles2[search].Heading;

				if (ct.IsVariableQty)
				{
					reqd = string.Format(reqd, i++);
				}
				else
				{
					i = 1;
				}

				if (reqd.Equals(found)) continue;

				// heading does not match

				Status.SetStatus(ErrorCodes.EC_PP_PS_SCHED_WRONG_TITLES,
					Overall.OS_WARNING, $"{fileBeingParsed}| found| {found}");

				status = false;
			}

			return status;
		}

		// parse each data row
		private int parseSchDataRows(int startRow, DataRowCollection rows)
		{
			int r = 0; // row index
			int i = 0;

			IPdfData? item;

			colInfo = new Dictionary<int, colValue>();
			colValueIdx = 0;

			// process each data row
			for (r = startRow; r < rows.Count; r++)
			{
				DataRow d = rows[r];

				// increment the progress bar
				W.PbPhaseValue++;

				item = parseDataRow(d);

				if (item == null) continue;

				item.Sequence = i++;

				if (item.RowType != RT_LIST) addHeadings(item);

				schedules.Add(item);

				colInfo = new Dictionary<int, colValue>();
				colValueIdx = 0;
			}

			return schedules!.Count;
		}

		// debug routines

		/*
		private void testScheduleTitles(DataRow r)
		{

			M.WriteLine("Original process method\n");
			getScheduleTitles(r);

			showSchColData();

			schColData = new List<ColumnTitle>();

			M.WriteLine("\n\nNew process method\n");
			getScheduleTitles2(r);

			showSchColData();
		}
		*/

		private void showPrimeSchedule()
		{
			// M.WriteLine("this is a test");

			foreach (IPdfData sch in schedules)
			{
				if (sch.RowType == RowType.RT_LIST)
				{
					showPrimarySch((PdfSchData) sch);
				}
				else if (sch.RowType == RowType.RT_SHEET)
				{
					showSheetData((PdfShtData) sch);
				}
				else if (sch.RowType == RowType.RT_PDF)
				{
					showPdfData((PdfDocData) sch);
				}
			}
		}

		/*
		public bool GetSchedule()
		{
			string? SheetSchedule =
				DATA_PATH + "\\" + SHEET_SCHEDULE;

			FilePath<FileNameSimple> file =
				new FilePath<FileNameSimple>(SheetSchedule);

			return GetSchedule(file);
		}
		*/

	#endregion

	#region 3 - SheetSchedule

// STEP 3
// phase PS - parse sheet schedule
// traverse "schedules" and parse each schedule list to
// populate "sheets"
		public bool GetSheetSchedule()
		{
			// Status.IsGoodLocalSet = true;
			bool status = true;
			parsingPrimary = false;

			Status.Phase = Progress.PS_PH_PS;
			W.PbStatValue = (int) Progress.PS_PH_PS;

			W.PbarPhaseReset();

			bool result = true;
			Status.StatusData.ErrorCodes error;

			if (schedules == null || schedules.Count == 0)
			{
				Status.SetStatus(
					ErrorCodes.EC_PP_FILE_MISSING_PRIME,
					Overall.OS_FAIL);
				status = false;
			}
			else
			{
				// showPrimeSchedule();
				// return false;

				W.PbPhaseMax = schedules.Count + 1;

				sheets = new List<IPdfDataEx?>();


				foreach (IPdfData sch in schedules)
				{
					W.PbPhaseValue++;

					error = ErrorCodes.EC_NO_ERROR;
					colData.ResetColTitleOk();

					if (sch.RowType == RT_LIST)
					{
						fileBeingParsed = sch.File.FileName;

						// todo have sub-method set status
						if (!addPdfSch((PdfSchData) sch))
						{
							error = ErrorCodes.EC_PS_LIST_INVALID;
						}
					}
					else if (sch.RowType == RT_SHEET)
					{
						// todo have sub-method set status
						if (!addPdfSht((PdfShtData) sch))
						{
							error = ErrorCodes.EC_PS_SHEET_INVALID;
						}
					}
					else if (sch.RowType == RT_PDF)
					{
						// todo have sub-method set status
						if (!addPdfDoc((PdfDocData) sch))
						{
							error = ErrorCodes.EC_PS_PDF_INVALID;
						}
					}

					if (error != ErrorCodes.EC_NO_ERROR)
					{
						Status.SetStatus(error, Overall.OS_FAIL, 
							$"{fileBeingParsed}");
						status = false;
						// break;
					}
				}

				if (status)
				{
					W.PbPhaseValue++;

					Status.SetStatus(ErrorCodes.EC_NO_ERROR, Overall.OS_WORKED);
					// Task.Factory.StartNew(() => showSheets());
					// showSheets();

				}
			}

			showSheets();
			// return false;

			return status;
		}

		private bool addPdfSht(PdfShtData pdfShtData)
		{
			PdfSchData sch = new PdfSchData();

			sch.TypeName = PRIME_SCHEDULE;

			pdfShtData.SetValue(CS_PARENT, sch);

			pdfShtData.SetFileName(rootPath);

			pdfShtData.SetValue(CS_PDFFILE, PdfSupport.GetPdfFile(pdfShtData.File));

			fileBeingParsed = pdfShtData.File.FileName;

			sheets.Add(pdfShtData);

			return true;
		}

		private bool addPdfDoc(PdfDocData? pdfDoc)
		{
			if (pdfDoc == null) return false;
			
			if (pdfDoc.FilePath.IsVoid())
			{
				Status.SetStatus(ErrorCodes.EC_FILE_MISSING, 
					Overall.OS_WARNING);
				return false;
			}

			fileBeingParsed = pdfDoc.FilePath;

			if (!pdfDoc.SetValue(CS_PDFFILE, getPdfFile(pdfDoc)!))
			{
				Status.SetStatus(pdfDoc.Status, Overall.OS_WARNING,
					$"{fileBeingParsed}");

				return false;
			}

			sheets.Add(pdfDoc);

			return true;
		}

		private bool addPdfSch(PdfSchData sch)
		{
			bool status = true;

			// root path is the folder that the primary schedule resides
			// expectation is that all other paths exist below this point

			if (sch.FilePath.IsVoid())
			{
				Status.SetStatus(ErrorCodes.EC_FILENAME_INVALID,
					Overall.OS_FAIL, $"{sch.File.FileName}");
				return false;
			}

			FilePath<FileNameSimple>? xlsxFile =
				getSubFile(sch.XlsxPath!, sch.FilePath, MainWindow.EXT_XLSX);

			// fileBeingParsed = xlsxFile.FileName;

			if (xlsxFile == null || !xlsxFile.IsValid)
			{
				// Status.SetStatus(ErrorCodes.EC_FILE_MISSING,
				// 	Overall.OS_FAIL, $"{xlsxFile.FileName}");
				return false;
			}

			DataSet ds = xls.ReadSchedule(xlsxFile);

			parentSch = sch;

			headerInfo = new Dictionary<int, string?>();

			if (!parseSheetSchedule(ds)) status = false;

			return status;
		}

		private FilePath<FileNameSimple>? getSubFile(string? relPath, string? file, string type)
		{
			FilePath<FileNameSimple>? result = getFilePath(relPath, file);

			if (result.IsFolderPath)
			{
				Status.SetStatus(ErrorCodes.EC_FILENAME_INVALID, Overall.OS_FAIL,
					$"provided| {file}");
				return null;
			}

			if (!result.FileExtensionNoSep.Equals(type))
			{
				Status.SetStatus(ErrorCodes.EC_FILENAME_INVALID, Overall.OS_FAIL,
					$"Incorrect file extension| provided| {file}");
				return null;
			}

			if (!result.Exists) result = FilePath<FileNameSimple>.Invalid;

			return result;
		}

		private FilePath<FileNameSimple>? getFilePath(string subPath, string file)
		{
			if (file.IsVoid()) return null;

			string filePath;

			if (subPath.IsVoid())
			{
				filePath = rootPath + "\\" + file;
			}
			else
			{
				filePath = rootPath + subPath + "\\" + file;
			}

			return new FilePath<FileNameSimple>(filePath);
		}

		// pdf file

		//*
		private PdfFile? getPdfFile(PdfDocData? pdfDoc)
		{
			if (pdfDoc == null) return null;

			FilePath<FileNameSimple>? file =
				getSubFile(pdfDoc.PdfPath, pdfDoc.FilePath, MainWindow.EXT_PDF);

			if (file == null || !file.IsValid) return null;

			return PdfSupport.GetPdfFile(file);
		}
		//*/

		private bool getPdfDocData(PdfDocData? pdfDoc)
		{
			// need to read each sheet and
			// get the sheet information:
			// read through file - it must have a bookmark
			// for each page - which equals the sheet number + sheet name

			// M.WriteLine($"file name| {pdfDoc.FilePath}");

			// showPdfFileData(pdfDoc.PdfFile);

			return true;
		}

		// sheet schedule

		private bool parseSheetSchedule(DataSet sheetSchedule)
		{
			DataRowCollection rows = sheetSchedule.Tables[0].Rows;

			bool result;

			// status set in sub-method
			int row = validateIsSheetSchedule(rows);

			if (row == -1) return false;

			// status set in sub-method
			row = getSheetSchTitles(rows, row);

			if (row == -1) return false;

			parseSheetDataRows(rows, row);

			return true;
		}

		private int validateIsSheetSchedule(DataRowCollection rows)
		{
			int row = validateSchType(rows, SHEET_SCHEDULE);
			if (row == -1)
			{
				Status.SetStatus(ErrorCodes.EC_PP_PS_SCHED_WRONG_TYPE, Overall.OS_FAIL);

				return -1;
			}

			return row;
		}

		private int getSheetSchTitles(DataRowCollection rows, int startRow)
		{
			string? field0;

			for (int i = startRow; i < rows.Count; i++)
			{
				DataRow r = rows[i];

				field0 = (r.Field<string>(0) ?? "");

				if (field0.IsVoid() || field0.Trim().StartsWith(COMMENT)) continue;

				// if (!parseSchTitles(r)) break;
				parseSchTitles(r);

				if (validateSchTitles())
				{ 
					return i;
				}

				break;
			}

			return -1;
		}

		private void parseSheetDataRows(DataRowCollection rows, int startRow)
		{
			int r = 0; // row index
			int i = 0;
			int strLen = schTypeName.Length;

			PdfShtData? sheet;

			colInfo = new Dictionary<int, colValue>();
			colValueIdx = 0;

			parentSch.TypeName = schTypeName;
			parentSch.Headings.Add(schTypeName.Substring(1, strLen - 2));

			for (r = startRow; r < rows.Count; r++)
			{
				DataRow d = rows[r];

				sheet = (PdfShtData?) parseDataRow(d);

				if (sheet == null) continue;

				sheet.Sequence = i++;

				addParentHeadings(sheet);
				addHeadings(sheet);

				sheet.SetFileName(rootPath);
				sheet.SetValue(CS_PDFFILE, PdfSupport.GetPdfFile(sheet.File));

				sheets.Add(sheet);

				colInfo = new Dictionary<int, colValue>();
				colValueIdx = 0;
			}
		}

		private void addParentHeadings(PdfShtData? sheet)
		{
			int i = 0;

			foreach (string? hdg in sheet.Parent.Headings)
			{
				sheet.Headings.Insert(i++, hdg);
			}
		}


		// debug routines

		public bool GetSheetSchedule(PdfSchData sch, string rootPath)
		{
			bool result;

			this.rootPath = rootPath;

			schedules = new List<IPdfData?>();
			schedules.Add(sch);

			result =  GetSheetSchedule();

			if (result)
			{
				showSheets();
			}

			return result;
		}

		private void showSheets()
		{
			WriteLine("\n*** showing sheets ***");


			pageCount = 0;

			foreach (IPdfData id in sheets)
			{
				if (id.RowType == RT_SHEET)
				{
					showSheet((PdfShtData) id);
				}
				else if (id.RowType == RT_PDF)
				{
					showPdfDoc((PdfDocData) id);
				}
			}

			M.WriteLine($"page count| {pageCount}");
			WriteLine("*** showing sheets done ***\n");
		}

		private void showSheet(PdfShtData sd)
		{
			string hx = null;

			if (sd.Headings != null)
			{
				sd.Headings.ForEach(s => { hx += $"\\{s} "; });
			}
			else
			{
				hx = "no headings";
			}

			int strLen = sd.Parent.TypeName.Length;
			strLen = strLen > 19 ? 19 : strLen;

			// string a = $"seq-> {sd.Sequence,3}";
			// string b = $"rt-> {sd.RowType,-8}";

			string z = $"{((sd.File?.Exists ?? false) ? "Y" : "N" )}";
			string y = $"{sd.PageCount,-2}";

			string c = $"  num-> {sd.SheetNumber,-8}";
			string d = $"name-> {sd.SheetName,-30}";

			string s = $"status-> {sd.Status.ToString(),-12}";

			string x;
			if (sd.Status == ErrorCodes.EC_NO_ERROR)
			{
				x = $"exist-> {sd.FileExists,-6}";
			}
			else
			{
				x = $"exist-> {"nope",-6}";
			}

			string t = $"type-> {sd.RowType,-12}";


			string f = $"file-> {(sd.File?.FileName ?? "null"),-45}";

			// string p = $"parent-> {sd.Parent.TypeName.Substring(0, strLen), -20}";
			string h = $"hdg-> {hx}";

			M.WriteLine($"{y}|{s}| {t}| {c}| {d}| {x}| {f}| {h}");
			// M.WriteLine($"\t{sd.File.FullFilePath}");

			pageCount += sd.PageCount;
		}

		private void showPdfDoc(PdfDocData dd)
		{
			string h = null;

			if (dd.Headings != null)
			{
				dd.Headings.ForEach(s => { h += $"\\{s} "; });
			}
			else
			{
				h = "no headings";
			}

			// string a = $"seq-> {dd.Sequence,3}";
			// string b = $"rt-> {dd.RowType,-8}";
			string z = $"{((dd.File?.Exists ?? false) ? "Y" : "N" )}";
			string y = $"{dd.PageCount,-2}";

			string s = $"status-> {dd.Status.ToString(),-12}";
			
			string p = $"pages-> {dd.PageCount,-11}";
			string m = $"keep-> {dd.KeepBookmarks,-48}";
			string e = $"exist-> {dd.File.Exists,-6}";
			string f = $"file-> {dd.File.FileName,-45}";
			string hx = $"hdg-> {h}";

			M.WriteLine($"{y}|{s}| {p}| {m}| {e}| {f}| {hx}");

			pageCount += dd.PageCount;

			// if (dd.PdfFile.PageLNameist.Count > 0)
			// {
			// 	showPageNameList(dd.PdfFile);
			// }
		}


		/* removed show routines
		private void showPageNameList(PdfFile pdfFile)
		{
			string a = "  | pdf file page";


			foreach (string s in pdfFile.PageLNameist)
			{
				string b = $"name-> {s}";

				WriteLine($"{a,-63}| {b}");
			}
		}

		private void showPdfFileData(PdfFile pdfFile)
		{
			foreach (var kvp in pdfFile.OutlineList)
			{
				M.WriteLine($"\tbookmark| {kvp.Key}| page| {kvp.Value}");
			}
		}
		*/

	#endregion

	#region 4 - validate methods

// STEP 4
// phase VS - validate sheet list
		public bool ValidateSheetList()
		{
			Status.Phase = Progress.PS_PH_VS;
			W.PbStatValue = (int) Progress.PS_PH_VS;

			W.PbarPhaseReset();

			bool status = true;

			pageCount = 0;

			if (sheets == null || sheets.Count  == 0)
			{
				Status.SetStatus(
					ErrorCodes.EC_SHEET_LIST_INVALID,
					Overall.OS_FAIL);
				status = false;
			}
			else
			{
				W.PbPhaseMax = sheets.Count + 1;

				foreach (IPdfDataEx pd in sheets)
				{
					W.PbPhaseValue++;

					if (pd.Status != ErrorCodes.EC_NO_ERROR)
					{
						Status.SetStatus(pd.Status,
							Overall.OS_WARNING,
							$"failed| {pd.FilePath}");
						status = false;
					}

					if (pd.RowType == RT_SHEET)
					{
						pageCount += ((PdfShtData)pd).PageCount;
					}
					else if (pd.RowType == RT_PDF)
					{
						pageCount += ((PdfDocData)pd).PageCount;
					}
				}

				if (status)
				{
					W.PbPhaseValue++;

					Status.SetStatus(ErrorCodes.EC_NO_ERROR,
						Overall.OS_WORKED);
				}
			}

			return status;
		}

	#endregion

	#region 5 - create pdf tree

// STEP 5
// phase CT - create pdf tree
		public bool CreatePdfTree()
		{
			Status.Phase = Progress.PS_PH_CT;
			W.PbStatValue = (int) Progress.PS_PH_CT;

			W.PbarPhaseReset();

			bool status = true;

			if (sheets == null || sheets.Count == 0)
			{
				Status.SetStatus(
					ErrorCodes.EC_SHEET_LIST_INVALID,
					Overall.OS_FAIL);
			}
			else
			{
				W.PbPhaseMax = sheets.Count;

				if (!ts.CreatePdfTree(sheets))
				{
					Status.SetStatus(ErrorCodes.EC_CT_CREATE_TREE_FAILED);
				}
				else
				{
					W.PbPhaseValue++;

					Status.SetStatus(ErrorCodes.EC_NO_ERROR, Overall.OS_WORKED);
					// Task.Factory.StartNew(() => showPdfNodeTree());
					showPdfNodeTree();
				}
			}


			return Status.IsGoodOverall;
			// return false;
		}

		/*
		private void addPdfToTree(IPdfDataEx data)
		{
			FilePath<FileNameSimple> file = 
				new FilePath<FileNameSimple>(data.File.FullFilePath);

			PfTreeLeaf leaf;
			PfTreeBranch branch;

			int pagenum = 0;
			int priorpagenum = 0;
			int priorlevelnum = 0;
			int levelnum = 0;
			int numHeadings = data.Headings.Count;

			if (data.PageCount == 1)
			{
				leaf = new PfTreeLeaf(file, data.Bookmark, data.PageCount, data.KeepBookmarks);

				pdfTree.AddLeaf(leaf, data.Headings);
			}
			else
			{
				// the item provided describes the whole collection of pages
				// provide this entry for the pdf merge portion

				// leaf = new PfTreeFile(file, data.Bookmark, 0, data.KeepBookmarks);
				// data.Headings = new List<string?>();
				// pdfTree.AddLeaf(leaf, data.Headings);

				processOutlines(data, file);

				// pdfTree.AddToCurrentPageNumber(data.PageCount);

				
				// process the outline list in preparation of
				// creating the bookmark list
				if ((data.PdfFile?.HasOutlines ?? false) &&
					(data.PdfFile?.OutlineList?.Count ?? 0) > 0)
				{
					foreach (KeyValuePair<string, int[]> kvp in data.PdfFile!.OutlineList!)
					{
						levelnum = kvp.Value[0];

						if (kvp.Value[2] == 0)
						{
							// got leaf
							leaf = new PfTreeFileLeaf(file, kvp.Key, 1, false);
							pdfTree.AddLeaf(leaf, data.Headings);
						}
						else
						{
							// got branch
							if (levelnum == priorlevelnum)
							{
								// going down a level
								data.Headings.Add(kvp.Key);
							}
							else
							{
								// going up some levels
								int lvlDiff = priorlevelnum - levelnum;
								int toRemove = lvlDiff - numHeadings;
								data.Headings.RemoveRange(toRemove, lvlDiff);
								data.Headings.Add(kvp.Key);
							}
						}

						priorlevelnum = levelnum;
					}
				}
			}
		}
		

		private void processOutlines(IPdfDataEx data, FilePath<FileNameSimple> file)
		{
			PfTreeLeaf leaf;

			// data.Headings = new List<string?>();

			int levelnum = 0;
			int priorlevelnum = 0;
			int numHeadings = data.Headings.Count;

			pdfTree.SetCurrPdfFilePageNum();

			// process the outline list in preparation of
			// creating the bookmark list
			// int[] [0] == curr level  / [1] == page num / [2] == is branch
			if ((data.PdfFile?.HasOutlines ?? false) &&
				(data.PdfFile?.OutlineList?.Count ?? 0) > 0)
			{
				foreach (KeyValuePair<string, int[]> kvp in data.PdfFile!.OutlineList!)
				{
					levelnum = kvp.Value[0];

					M.Write($"process| {kvp.Key, -35} | ");

					if (kvp.Value[2] == 0)
					{
						M.WriteLine("add as leaf");
						// got leaf
						leaf = new PfTreeFileLeaf(file, kvp.Key, -1, false);
						leaf.PageNumber = kvp.Value[1];
						pdfTree.AddLeaf(leaf, data.Headings);
					}
					else
					{
						M.Write("add as branch| ");
						// got branch
						if (levelnum == priorlevelnum || levelnum-priorlevelnum > 0)
						{
							M.WriteLine("go down");
							// going down a level
							data.Headings.Add(kvp.Key);
						}
						else
						{
							M.WriteLine("go up");
							int toRemove = kvp.Value[0];
							int lvlDiff = data.Headings.Count - toRemove;
							data.Headings.RemoveRange(toRemove, lvlDiff);
							data.Headings.Add(kvp.Key);
						}
					}

					priorlevelnum = levelnum;
				}
			}
		}

		private void processOutlines2(IPdfDataEx data, FilePath<FileNameSimple> file)
		{
			if (!(data.PdfFile?.HasOutlines ?? false) ||
				(data.PdfFile?.OutlineList?.Count ?? 0) == 0) return;

			PfTreeLeaf leaf;
			KeyValuePair<string, int[]> priorKvp;
			int levelnum = 1;
			int priorlevelnum = 1;
			int numHeadings = data.Headings.Count;
			string priorHeading = "";

			string b;
			int idx = 0;

			List<Tuple<string, int, int, string, string?[]>> x = new List<Tuple<string, int, int, string, string?[]>>();


			foreach (KeyValuePair<string, int[]> kvp in data.PdfFile!.OutlineList!)
			{
				levelnum = kvp.Value[0];

				M.Write($"{kvp.Key,-30}| lev#| {levelnum,-3}| prior#| {priorlevelnum,-3}");

				if (priorlevelnum < levelnum)
				{ 
					M.Write($"{"| add heading",-30}| ");
					data.Headings.Add(priorHeading);
					b = "B";
				} 
				else if (priorlevelnum > levelnum)
				{

					// going up some levels
					int toRemove = kvp.Value[0];
					int lvlDiff = data.Headings.Count - toRemove;
					data.Headings.RemoveRange(toRemove, lvlDiff);
					data.Headings.Add(kvp.Key);

					M.Write($"{$"| remove| dif| {lvlDiff,-3}| to rem| {toRemove}",-30}| ");
					b = "B";
				}
				else
				{
					M.Write($"{$"| none", -30}| ");

					b = "L";
				}

				foreach (string? h in data.Headings)
				{
					M.Write($"{h} \\ ");
				}
				M.WriteLine("");

				leaf = new PfTreeFileLeaf(file, kvp.Key, 0, false);
				pdfTree.AddLeaf(leaf, data.Headings);

				x.Add(new Tuple<string, int, int, string, string?[]>(kvp.Key, levelnum, priorlevelnum, b, data.Headings.ToArray()));

				// priorKvp = kvp;
				priorlevelnum = levelnum;
				priorHeading = kvp.Key;
				idx++;
			}

			M.WriteLine("\npdf list\n");


			for (var i = 0; i < x.Count; i++)
			{
				Tuple<string, int, int, string, string?[]> t = x[i];

				if (i+1<x.Count)
				{
					if (t.Item2 != x[i + 1].Item2)
					{
						b = "B";
					}
					else
					{
						b = "L";
					}
				}
				else
				{
					b = "x";
				}

				M.Write($"{t.Item4,-4}| {t.Item1, -32}| {t.Item2, -3} | {t.Item3,-3}| ");

				foreach (string? h in t.Item5)
				{
					M.Write($"{h} \\");
				}
				M.WriteLine("");
			}

		}

		struct zed
		{
			public string title { get; set; }
			public string tx { get; set; }
			public int levelnum { get; set; }
			public int priorlevelnum { get; set; }
			public string[] headings { get; set; }
			public int pageRef { get; set; }
			public int pageNum { get; set; }

			public zed(         string title, string tx, int pageNum, int pageRef, int levelnum, int priorlevelnum, string[] headings)
			{
				this.title = title;
				this.tx = tx;
				this.pageRef = pageRef;
				this.pageNum = pageNum;
				this.levelnum = levelnum;
				this.priorlevelnum = priorlevelnum;
				this.headings = headings;
			}

			public void setType(string t)
			{
				tx = t;
			}

			public override string ToString()
			{
				return $"{tx,-3}| {title, -32}| {pageNum,-3}| {pageRef,-3} {levelnum, -3}| {priorlevelnum, -3}";
			}
		}

		private void processOutlines3(IPdfDataEx data, FilePath<FileNameSimple> file)
		{			
			if (!(data.PdfFile?.HasOutlines ?? false) ||
						(data.PdfFile?.OutlineList?.Count ?? 0) == 0) return;

			PfTreeLeaf leaf;
			KeyValuePair<string, int[]> priorKvp;
			int levelnum = 1;
			int priorlevelnum = 1;
			int numHeadings = data.Headings.Count;
			string priorHeading = "";

			int idx = 0;
			string t;

			zed zz;

			List<zed> z = new List<zed>();

			foreach (KeyValuePair<string, int[]> kvp in data.PdfFile!.OutlineList!)
			{
				levelnum = kvp.Value[0];
				t = "L";

				if (priorlevelnum < levelnum)
				{
					data.Headings.Add(priorHeading);
					zz = z[idx - 1];
					zz.tx = "B";
					z[idx - 1] = zz;
				} 
				else if (priorlevelnum > levelnum)
				{

					// going up some levels
					int toRemove = kvp.Value[0];
					int lvlDiff = data.Headings.Count - toRemove;
					data.Headings.RemoveRange(toRemove, lvlDiff);
					data.Headings.Add(kvp.Key);
					t = "B";
				}

				z.Add(new zed(kvp.Key, t, pdfTree.CurrPageNum, kvp.Value[1], 
					levelnum, priorlevelnum, data.Headings.ToArray()));

				leaf = new PfTreeFileLeaf(file, kvp.Key, 0, false);
				pdfTree.AddLeaf(leaf, data.Headings);

				// priorKvp = kvp;
				priorlevelnum = levelnum;
				priorHeading = kvp.Key;
				idx++;
			}

			M.Write("\n");
			
			foreach (zed zx in z)
			{
				string h = "";

				foreach (string zs in zx.headings)
				{
					h += $"\\ {zs}";
				}

				M.WriteLine($"{zx.tx, -3}| {zx.title, -32}| {zx.pageNum,-3}| {zx.pageRef,-3} | {h}");
			}
		}

		private void processOutlines4(IPdfDataEx data, FilePath<FileNameSimple> file)
		{
			if (!(data.PdfFile?.HasOutlines ?? false) ||
				(data.PdfFile?.OutlineList?.Count ?? 0) == 0) return;

			PfTreeLeaf leaf;
			KeyValuePair<string, int[]> priorKvp;
			int levelnum = 1;
			int priorlevelnum = 1;
			int numHeadings = data.Headings.Count;
			string priorHeading = "";
			string startBookmark = data.Bookmark;

			foreach (KeyValuePair<string, int[]> kvp in data.PdfFile!.OutlineList!)
			{
				levelnum = kvp.Value[0];

				if (priorlevelnum < levelnum)
				{
					data.Headings.Add(priorHeading);
				} 
				else if (priorlevelnum > levelnum)
				{
					// going up some levels
					int toRemove = kvp.Value[0];
					int lvlDiff = data.Headings.Count - toRemove;
					data.Headings.RemoveRange(toRemove, lvlDiff);
					data.Headings.Add(kvp.Key);
				}

				leaf = new PfTreeFileLeaf(file, kvp.Key, 0, false);
				pdfTree.AddLeaf(leaf, data.Headings);

				// priorKvp = kvp;
				priorlevelnum = levelnum;
				priorHeading = kvp.Key;
			}

			PfTreeBranch b;

			bool result = pdfTree.Root.ContainsBranch(startBookmark, out b);

			if (!result) return;

			M.WriteLine("\nclean tree");
			cleanPdfTree(b);
			M.WriteLine("");

			M.WriteLine("\nto remove");
			foreach (string s in toRemove)
			{
				M.WriteLine($"{s}");
			}
		}

		private List<string> toRemove = new List<string>();
		private string priorBookmark;
		private string priorKey;

		private void cleanPdfTree(PfTreeBranch b)
		{

			foreach (KeyValuePair<string, ITreeItem> kvp in b.ItemList)
			{
				// if (kvp.Value.Bookmark.Equals(priorBookmark)) {toRemove.Add(priorBookmark);}
				//
				// priorBookmark = kvp.Value.Bookmark;
				// priorKey = kvp.Key;

				if (kvp.Value.ItemType == TreeItemType.TI_BRANCH)
				{
					if (kvp.Value.Bookmark.Equals(priorBookmark)) {toRemove.Add($"B |{kvp.Key,-30} ({priorKey})");}

					priorBookmark = kvp.Value.Bookmark;
					priorKey = kvp.Key;

					M.WriteLine($"got branch| {kvp.Value.Bookmark}");
					M.WriteLine("next");

					cleanPdfTree((PfTreeBranch) kvp.Value);
				}
				else
				{
					if (kvp.Value.Bookmark.Equals(priorBookmark)) {toRemove.Add($"L |{kvp.Key, -30} ({priorKey})");}

					priorBookmark = kvp.Value.Bookmark;
					priorKey = kvp.Key;

					M.WriteLine($"got leaf  | {kvp.Value.Bookmark}");
				}
			}
		}
		*/

		// show the tree

		private int level = 0;
		private int margMulti = 2;

		/*
		private void showPdfTree()
		{
			showPdfTree(pdfTree.Root);
		}

		private void showPdfTree(PfTreeBranch branch)
		{
			string p = "B  ";
			string preface = $"level| {level,-3}| pg| {branch.PageNumber,-3}{" ".Repeat(level * margMulti)}";

			M.WriteLine($"{p}{preface}{branch.Bookmark}");

			foreach (KeyValuePair<string, ITreeItem> kvp in branch.ItemList)
			{
				if (kvp.Value.ItemType == TreeItemType.TI_BRANCH)
				{
					level ++;

					showPdfTree((PfTreeBranch) kvp.Value);

					level--;
				}
				else
				{
					showPdfLeaf((PfTreeLeaf) kvp.Value);

					// Thread.Sleep(250);
				}
			}
		}

		private void showPdfLeaf(PfTreeLeaf leaf)
		{
			string h;

			string p = "L  ";
			string preface = $"     |    | pg| {leaf.PageNumber,-3} {" ".Repeat((level + 2) * margMulti)}{leaf.Bookmark}";

			if (leaf.ItemType == TreeItemType.TI_LEAF)
			{
				p = "L  ";
			}
			else if (leaf.ItemType == TreeItemType.TI_FILE)
			{
				p = "F  ";
			}
			else if (leaf.ItemType == TreeItemType.TI_FILE_LEAF)
			{
				p = "FL ";
			}
			else
			{
				p = "?  ";
			}

			M.WriteLine($"{p}{preface,-65}| pg cnt| {leaf.PageCount,-3}| file| {leaf.File.FileNameNoExt}");
		}
		*/

		private void showPdfNodeTree()
		{
			sb.Append("\n\n*** show pdf node tree ***\n");

			showPdfTreeNodes(tree.Root);

			sb.Append("*** show pdf node tree done ***\n");

			WriteLine(sb.ToString());
		}

		private StringBuilder sb = new StringBuilder();

		// when showing typical nodes
		private void showPdfTreeNodes(APdfTreeNode node)
		{
			if (!node.Bookmark.Equals(PdfNodeTree.ROOTNAME))
			{
				showPdfTreeBranch(node);
			}

			foreach (KeyValuePair<string, IPdfTreeItem> kvp in node.ItemList)
			{
				if (kvp.Value.ItemType == PdfTreeItemType.PT_BRANCH)
				{
					level++;

					showPdfTreeNodes((APdfTreeNode) kvp.Value);

					level--;
				} 
				else 
				if (kvp.Value.ItemType == PdfTreeItemType.PT_NODE)
				{
					showPdfNodeLeaf((PdfTreeNode) kvp.Value);
				} 
				else 
				if (kvp.Value.ItemType == PdfTreeItemType.PT_NODE_FILE)
				{
					showPdfNodes2((PdfTreeNode) kvp.Value, 1);
				} 
				else 
				{
					showPdfNodeLeaf((PdfTreeLeaf) kvp.Value);
				}
			}
		}

		private void showPdfTreeItem(APdfTreeNode node)
		{
			string n1 = node.ItemList.ToString();
			string n2 = node.Bookmark.ToString();
			string n3 = node.PageCount.ToString();
			string n4 = node.ItemType.ToString();
			string n6 = node.PageNumber.ToString();

		}


		private void showPdfTreeBranch(APdfTreeNode node)
		{
			string t = "B";

			string preface =
				$"level| {level,-3}| pg| {node.PageNumber,-3}{"  ".Repeat((level - 1) * margMulti)}{node.Bookmark}";

			WriteLine($"{t,-3}{preface}");

			sb.Append($"A type| {node.ItemType,-14}| pg| {node.PageNumber,3}|                | list count| {node.ItemList.Count,2}|               bkmrk| {node.Bookmark}\n");
		}

		private void showPdfNodeLeaf(PdfTreeLeaf node)
		{
			string s;
			string t = "?";
			string f = String.Empty;
			string preface = $"       |    | pg| {node.PageNumber,-3} {" ".Repeat((level + 2) * margMulti)}{node.Bookmark}";

			if (node.ItemType == PdfTreeItemType.PT_LEAF)
			{
				t = "L";
				f = $"| file| {node.File.FileName}";
			} 
			else if (node.ItemType == PdfTreeItemType.PT_NODE || node.ItemType == PdfTreeItemType.PT_NODE_FILE)
			{
				t = "N";
			}

			s = $"{(node.SheetNumber ?? "null"),-6}";
			

			M.WriteLine($"{t}{preface,-65}| pg cnt| {node.PageCount,-3}{f}");

			sb.Append($"B type| {node.ItemType,-14}| pg| {node.PageNumber,3}| sht num| {s}| list count| {node.ItemList.Count,2}| pg count| {node.PageCount,2}| bkmrk| {node.Bookmark}\n");

		}


		// specific when showing bookmarks from a compiled pdf file
		// shows the whole "branch"


		/* alternate showing pdf nodes
		private void showPdfNodes(PdfTreeNode node)
		{
			showPdfNode(node);

			foreach (KeyValuePair<string, IPdfTreeItem> kvp in node.ItemList)
			{
				showPdfNodes((PdfTreeNode) kvp.Value);
			}
		}

		private void showPdfNode(PdfTreeNode node)
		{
			if (node.ItemType == PdfTreeItemType.PT_NODE)
			{
				showPdfNodeItem(node);
			} 
			else 
			if (node.ItemType == PdfTreeItemType.PT_NODE_FILE)
			{
				showPdfNodeFile(node);
			}
		}

		private void showPdfNodeFile(PdfTreeNode node)
		{
			string t = "F";
			string f = node.File.FileName;
			
			string preface =
				$"level| {node.Level,-3}| pg| {node.PageNumber,-3}{"  ".Repeat((node.Level-1) * margMulti)}{node.Bookmark}";

			WriteLine($"{t,-3}{preface,-75} | file| {f}");

			// below - not used?
			sb.Append($"C type| {node.ItemType,-14}| pg| {node.PageNumber,3}| list count| {node.ItemList.Count,2}| pg count  | {node.PageCount,2}| bkmrk| {node.Bookmark}\n");
		}

		private void showPdfNodeItem(PdfTreeNode node)
		{
			string t = "N";
			string f = node.File.FileName;
			
			string preface =
				$"level| {node.Level,-3}| pg| {node.PageNumber,-3}{"  ".Repeat((node.Level-1) * margMulti)}{node.Bookmark}";

			WriteLine($"{t,-3}{preface}");

			// below - not used?
			sb.Append($"D type| {node.ItemType,-14}| pg| {node.PageNumber,3}| list count| {node.ItemList.Count,2}| pg count  | {node.PageCount,2}| bkmrk| {node.Bookmark}\n");
		}
		*/

		private void showPdfNodes2(PdfTreeNode node, int level)
		{
			showPdfNode2(node, level);

			foreach (KeyValuePair<string, IPdfTreeItem> kvp in node.ItemList)
			{
				showPdfNodes2((PdfTreeNode) kvp.Value, level + 1);
			}
		}

		private void showPdfNode2(PdfTreeNode node, int level)
		{
			if (node.ItemType == PdfTreeItemType.PT_NODE)
			{
				showPdfNodeItem2(node, level);
			} 
			else 
			if (node.ItemType == PdfTreeItemType.PT_NODE_FILE)
			{
				showPdfNodeFile2(node, level);
			}

		}

		private void showPdfNodeItem2(PdfTreeNode node, int level)
		{
			string t = "N";
			string f = node.File.FileName;
			string b = node.Bookmark;
			string b1 = "bkmk only";
			string sht = "";

			if (node.EstIsSheet)
			{
				b1 = "* est pg bkmk";
				sht = $"({node.SheetNumber} - {node.SheetName})";
			}
			
			string preface =
				$"level| {node.Level,-3}| pg| {node.PageNumber,-3}{"  ".Repeat((level-1) * margMulti)}{node.Bookmark}";

			WriteLine($"{t,-3}{preface,-63}| pg cnt| {node.PageCount,-3}");

			sb.Append($"E type| {node.ItemType,-14}| pg| {node.PageNumber,3}| {b1, -15}| list count| {node.ItemList.Count,2}| pg count| {node.PageCount,2}| bkmrk| {b}  {sht}\n");


		}

		private void showPdfNodeFile2(PdfTreeNode node, int level)
		{
			string t = "F";
			string f = node.File.FileName;
			
			string preface =
				$"level| {node.Level,-3}| pg| {node.PageNumber,-3}{"  ".Repeat((level-1) * margMulti)}{node.Bookmark}";

			WriteLine($"{t,-3}{preface,-63}| pg cnt| {node.PageCount,-3}| file| {f}");

			sb.Append($"F type| {node.ItemType,-14}| pg| {node.PageNumber,3}|                | list count| {node.ItemList.Count,2}| pg count| {node.PageCount,2}| bkmrk| {node.Bookmark}\n");

		}


	#endregion

	#region 6 - merge pdf file

// STEP 6
// phase MP - merge pdf tree
		public bool MergePdfTree()
		{
			// Status.IsGoodLocalSet = true;
			bool status = true;


			/*
			Type[] knowTypes = new [] {typeof(PdfTreeLeaf), typeof(APdfTreeNode), 
				typeof(FileNameAsSheetFile), typeof(FileNameSimple), typeof(AFileName),
				typeof(PdfTreeNode), typeof(PdfTreeNodeFile),
				typeof(FilePathInfo<FileNameAsSheetFile>)};

			DataContractSerializer ds = new DataContractSerializer(typeof(PdfTreeBranch), knowTypes);

			string filePath = @"C:\Users\jeffs\Documents\Programming\VisualStudioProjects\PDF SOLUTIONS\_Samples\output.xml";

			XmlWriterSettings xmlSettings = new XmlWriterSettings() {Indent = true};

			using (XmlWriter w = XmlWriter.Create(filePath, xmlSettings))
			{
				ds.WriteObject(w, tree.Root);
			}
			*/

			



			Status.Phase = Progress.PS_PH_MP;
			W.PbStatValue = (int) Progress.PS_PH_MP;

			W.PbarPhaseReset();

			if (destFilePath == null || destFilePath.IsFolderPath)
			{
				Status.SetStatus(ErrorCodes.EC_GD_DEST_INVALID,
					Overall.OS_FAIL,
					$"File| {(destFilePath == null ? "None specified" : destFilePath.FullFilePath)}" );
				status = false;
			}
			else if (destFilePath.Exists)
			{
				Status.SetStatus(ErrorCodes.EC_DEST_EXISTS,
					Overall.OS_FAIL,
					$"File| {(destFilePath == null ? "None specified" : destFilePath.FullFilePath)}" );
				status=false;
			}
			else
			{
				// M.WriteLine($"Saving to| {destFilePath.FullFilePath}");

				nodeTreeCount = tree.CountElements();

				W.PbPhaseMax = nodeTreeCount;

				// todo move status of sub-method
				if (PdfSupport.MergePdfTree(destFilePath, tree))
				{
					Status.SetStatus(ErrorCodes.EC_NO_ERROR,
						Overall.OS_WORKED);
				}
				else
				{
					Status.SetStatus(ErrorCodes.EC_MP_MERGE_FAIL,
						Overall.OS_FAIL);
					status= false;
				}
			}

			return status;
		}

	#endregion

	#region 7 - create bookmarks

// STEP 7
// phase CB - create bookmarks
		public bool CreatePdfOutlineTree()
		{
			// Status.IsGoodLocalSet = true;
			bool status = true;

			Status.Phase = Progress.PS_PH_CB;
			W.PbStatValue = (int) Progress.PS_PH_CB;

			W.PbarPhaseReset();

			W.PbPhaseMax = nodeTreeCount;

			// todo have sub-method set status
			if (PdfSupport.CreateOutlineTree(tree))
			{
				Status.SetStatus(ErrorCodes.EC_NO_ERROR,
					Overall.OS_WORKED);
			}

			return status;
		}
		
	#endregion

	#region 8 - files in folder

		// validate the "sheets" list of files versus the list of files in the folder
		// for extra / not accounted files
// STEP 8 - validate files in folder versus excel
		public bool ValidateFilesInFolder(FilePath<FileNameSimple> primeFile)
		{
			Status.Phase = Progress.PS_PH_VF;
			W.PbStatValue = (int) Progress.PS_PH_VF;
			W.PbarPhaseReset();

			W.PbPhaseMax = schedules.Count;
			W.PbPhaseValue = 1;

			string baseFolderPath = primeFile.FolderPath;
			string pdfFolderPath;

			List<string> files = new List<string>();
			List<string> temp;
			List<string> foldersSearched = new List<string>();

			bool result;


			foreach (IPdfData pd in schedules)
			{
				W.PbPhaseValue++;

				if (pd.RowType!= RT_LIST) continue;

				pdfFolderPath = baseFolderPath  + ((PdfSchData) pd).PdfPath;

				if (foldersSearched.Contains(pdfFolderPath)) continue;

				foldersSearched.Add(pdfFolderPath);

				if (validateFilesInFolder(pdfFolderPath, out temp)) continue;

				foreach (string s in temp)
				{
					files.Add(s);
				}
			}

			if (files.Count == 0)
			{
				Status.SetStatus(ErrorCodes.EC_NO_ERROR, Overall.OS_WORKED);
			}
			else
			{
				string answer = $"quantity| {files.Count}\n";

				foreach (string s in files)
				{
					FilePath<FileNameSimple> file = new FilePath<FileNameSimple>(s);

					answer += $"{file[-1.1]}{file[0.1]}\n";
				}

				Status.SetStatus(ErrorCodes.EC_VF_EXTRA_FILES_FOUND, 
					Overall.OS_WARNING, answer);
			}


			return files.Count == 0;
		}
		
		private bool validateFilesInFolder(string folder, out List<string> files)
		{
			// file is the folder with the 
			string[] filesInFolder = Directory.GetFiles(folder);
			files = new List<string>();

			bool result;
			string fileName;

			foreach (string s in filesInFolder)
			{
				files.Add(s.ToLower());
			}

			foreach (IPdfDataEx px in sheets)
			{
				fileName = px.File.FullFilePath.ToLower();

				if (files.Contains(fileName))
				{
					files.Remove(fileName);
				}
			}

			return files.Count == 0;
		}

	#endregion

	#region common methods

		private int validateSchType(DataRowCollection rows, string scheduleType)
		{
			for (int i = 0; i < rows.Count; i++)
			{
				DataRow r = rows[i];

				W.PbPhaseValue++;

				if (!validDataRow(r, true)) continue;

				string s = (r.Field<string>(0) ?? "");

				if (s.Trim().ToLower().Equals(scheduleType))
				{
					schTypeName = (r.Field<string>(1) ?? "").Trim();
					return ++i;
				}
			}

			return -1;
		}

		/*
		// this is based on the column titles being in the same order as the columns in the xls file
		private void getScheduleTitles(DataRow r)
		{
			schColData = new List<ColumnTitle>();

			List<ColumnTitle> ct = colData.ColumnTitles;

			string item;
			string? field;
			string test;

			int t = 0; // col title index
			int f = 0; // xlsx field idx
			int i = 1; // item index (when variable items)
			int m = r.ItemArray.Length > ct.Count ? ct.Count : r.ItemArray.Length;

			for (f = 0; f < m; )
			{
				field = (r.Field<string>(f) ?? "").Trim().ToLower();

				if (t >= m) break;

				if (ct[t].IsVariableQty)
				{
					// special processing for variable column

					test = string.Format(ct[t].Heading, i++);

					if (field.Equals(test))
					{
						ColumnTitle cx = ct[t];
						cx.Heading = field;
						cx.Found = true;
						schColData.Add(cx);
						f++;
						continue;
					}

					// does not match - past last variable column
					i = 1; // reset item index
					t++;   // move to next column title
				}

				test = ct[t].Heading;

				if (field.Equals((test)))
				{
					ColumnTitle cx = ct[t];
					cx.Found = true;
					schColData.Add(cx);
				}
				else
				{
					t++;
					continue;
				}

				f++;
				t++;
			}
		}
		*/

		private void parseSchTitles(DataRow r)
		{
			schColTitleData = new List<ColumnTitle>();

			Dictionary<string, ColumnTitle> ct = colData.ColumnTitles2;

			string item;
			string? field;
			string test;

			int len;
			bool result;
			ColumnTitle cx;

			int t = 0;                  // col title index
			int f = 0;                  // xlsx field idx
			int i = 1;                  // item index (when variable items)
			int m = r.ItemArray.Length; // > ct.Count-colData.NumVariableCols ? ct.Count-colData.NumVariableCols : r.ItemArray.Length;

			for (f = 0; f < m; )
			{
				field = (r.Field<string>(f) ?? "").Trim().ToLower();

				len = field.Length <
					ColumnData.MAX_TITLE_LEN
						? field.Length
						: ColumnData.MAX_TITLE_LEN;

				test = field.Substring(0, len);

				result = ct.TryGetValue(test, out cx);

				// matching column found?
				if (result)
				{
					// found a matching column
					if (cx.IsVariableQty)
					{
						// test variable
						test = string.Format(cx.Heading, i++);

						if (field.Equals(test))
						{
							cx.Found = true;
							cx.Heading = field;
							schColTitleData.Add(cx);
						}
						// else
						// {
						// 	Status.SetStatus(ErrorCodes.EC_PH_PP_PS_SCHED_WRONG_TITLES,
						// 		Overall.OS_PARTIAL_FAIL,
						// 		$"{fileBeingParsed} | found| {field}");
						// }
					}
					else
					{
						if (field.Equals(cx.Heading))
						{
							cx.Found = true;
							schColTitleData.Add(cx);
						}
						else
						// {
						// 	Status.SetStatus(ErrorCodes.EC_PH_PP_PS_SCHED_WRONG_TITLES,
						// 		Overall.OS_PARTIAL_FAIL,
						// 		$"{fileBeingParsed} | found| {field}");
						// }

						i = 1;
					}
				}

				// get next column
				f++;
			}

			// return Status.IsGood;
		}

		// is the row ok to use
		private bool validDataRow(DataRow r, bool allowColTitle = false)
		{
			string? field0 = r.Field<string>(0);

			if (!field0.IsVoid())
			{
				// if the row is a comment?
				if (field0.Trim().StartsWith(COMMENT)) return false;
				// is the first character the column title marker
				if (!allowColTitle && field0.Trim().StartsWith(COL_TITLE_PREFACE)) return false;
			}
			else
			{
				// string was empty
				return false;
			}

			return true;
		}


		// used by both primary and sheet schedules
		private IPdfData? parseDataRow(DataRow r)
		{
			int idx = 0; // row index

			RowType rt;
			IPdfData? item;
			bool? result;

			// determine if row is ok to use - not comment, empty, or column title
			if (!validDataRow(r)) return null;

			rt = getRowType(r);

			// if row is a heading, process headings and return
			if (rt == RT_SHEET || rt== RT_PDF)
			{
				saveHeadings(r);
			}

			result = validateDataRowTitles(rt);

			if (result.HasValue && result == false) return null;

			parseRowData(r);

			item = categorizePerRowType(rt);

			if (item == null )
			{
				Status.SetStatus(ErrorCodes.EC_ROW_HAS_ERRORS, Overall.OS_FAIL, 
					$"{fileBeingParsed}| row| {idx}");
			}

			return item;
		}

		// parse an individual data row into the dictionary collection
		private void parseRowData(DataRow? d)
		{
			if (d == null) return;

			int c = 0;
			string? value = null;
			ColumnSubject cs;

			for (c = 1; c < schColTitleData.Count; c++)
			{
				// get the column type
				cs = schColTitleData[c].ColumnSubject;

				// for this process, ignore heading columns
				if (cs == CS_HEADING) continue;

				// get the string value of the column
				value = d.Field<string>(c);

				// ignore empty columns
				if (value.IsVoid()) continue;

				// add the data to the collection
				colInfo.Add(colValueIdx++, new colValue(cs, value));
			}

			if (colInfo.ContainsKey(0)) return;

			colInfo.Add(0, new colValue(CS_TYPE, ColumnData.RT_SHEET_S));

		}

		private RowType getRowType(DataRow d)
		{
			if (!parsingPrimary) return RT_SHEET;

			string? value = (d?.Field<string>(0) ?? "").Trim().ToLower();

			if (value.IsVoid()) return RT_ERROR;

			RowType rt;

			if (!ColumnData.RowTypeList.TryGetValue(value, out rt))
			{
				return RT_ERROR;
			}

			return rt;
		}

		private IPdfData? categorizePerRowType(RowType rt)
		{
			IPdfData? result = null;

			switch (rt)
			{
			case RT_LIST:
				{
					// result = parseScheduleRow(colInfo);
					result = getPdfDataObj<PdfSchData>();
					break;
				}
			case RT_SHEET:
				{
					// result = parseSheetDataRow(colInfo);
					result = getPdfDataObj<PdfShtData>();
					parentSch.TypeName = schTypeName;
					((PdfShtData) result!).SetValue(CS_PARENT, parentSch);
					break;
				}
			case RT_PDF:
				{
					// result = parsePdfDataRow(colInfo);
					result = getPdfDataObj<PdfDocData>();

					// PdfSchData a = parentSch;
					break;
				}
			}

			return result;
		}

		private IPdfData? getPdfDataObj<T>()
			where T : class, IPdfData, new()
		{
			// if (colInfo == null) return null;

			List<ColumnTitle> cts = schColTitleData;

			IPdfData ps = new T();

			ColumnSubject cs;
			colValue cv;
			string? value;

			foreach (KeyValuePair<int, colValue> kvp in colInfo)
			{
				cv = kvp.Value;
				value = cv.Value ?? "null";
				cs = cv.Subject;

				ps.SetValue(cs, value);
			}

			return ps;
		}

		// validate actual column titles versus the planned column titles 
		// for what is optional and required
		private bool? validateDataRowTitles(RowType rt)
		{
			if ((int)rt < 0 || colData.ColumnTitlesOk[(int) rt].HasValue) return null;

			// Status.IsGoodLocalSet = true;
			bool status = true;

			bool result = true;
			ColumnTitle actCt;

			int i = 1;
			int j = 0;

			// verify schColData (schActColData) versus colData.ColumnTitles2 - per row type

			Dictionary<ColumnSubject, ColumnTitle>
				schActColData = getActColData(rt);

			foreach (KeyValuePair<string, ColumnTitle> kvp in colData.ColumnTitles2)
			{
				ColumnTitle ct = kvp.Value;

				result = schActColData.TryGetValue(ct.ColumnSubject, out actCt);

				if ( (!result && ct.ColumnInfo[rt] != CT_REQD) ||
					(!result && ct.ColumnInfo[rt] == CT_OPTIONAL) ||
					(ct.ColumnInfo[rt] == CT_IGNORE )) continue;
				
				// either
				// case 1: found and required - or -  => validate title
				// case 2: found and optional - or -  => validate title
				// case 3: not found but required - or - => error

				// case 3
				if (!result)
				{
					Status.SetStatus(
						ErrorCodes.EC_PP_PS_SCHED_WRONG_TITLES,
						Overall.OS_WARNING,
						$"{fileBeingParsed}| row type| {rt} | missing| {ct.ColumnSubject}" );
					status = false;

					continue;
				}

				// case 1 & 2
				j = validateTitle(ct, actCt, i);

				if (j < 0)
				{
					Status.SetStatus(
						ErrorCodes.EC_PP_PS_SCHED_WRONG_TITLES,
						Overall.OS_WARNING,
						$"{fileBeingParsed}| row type| {rt} | required| {ct.Heading} | found| {actCt.Heading}"
						);
					status = false;
				} 
				else if (j == 0)
				{
					i = 1;
				}
				else
				{
					i++;
				}

			}

			colData.ColumnTitlesOk[(int) rt] = status;

			return status;
		}

		// + int == good and set =+ i
		// 0 int == good and set i = 1;
		// - int == fail and set i = 1;
		private int validateTitle(ColumnTitle ct, ColumnTitle actCt, int i)
		{
			int result = 0;
			string test;

			if (ct.IsVariableQty)
			{
				test = string.Format(ct.Heading, i);

				result = test.Equals(actCt.Heading) ? 1 : -1;
			}
			else
			{
				result = ct.Heading.Equals(actCt.Heading) ? 0 : -1;
			}

			return result;
		}

		private Dictionary<ColumnSubject, ColumnTitle> getActColData(RowType rt)
		{
			Dictionary<ColumnSubject, ColumnTitle>
				schActColData = new Dictionary<ColumnSubject, ColumnTitle>();

			foreach (ColumnTitle ct in schColTitleData)
			{
				if (schActColData.ContainsKey(ct.ColumnSubject)) continue;

				schActColData.Add(ct.ColumnSubject, ct);
			}

			return schActColData;
		}

		// header

		private string getHeadingValue(DataRow d, out int depth)
		{
			int c = -1;
			depth = 0;
			string? value = null;

			foreach (ColumnTitle ct in schColTitleData)
			{
				c++;

				if (ct.ColumnSubject == CS_HEADING)
				{
					depth++;

					value = (d?.Field<string>(c) ?? "");

					if (value.IsVoid()) continue;

					break;
				}
			}
			return value;
		}


		// add the headers to the headers collection (dictionary)
		// the index is the header depth
		public void saveHeadings(DataRow d)
		{
			headerInfo = new Dictionary<int, string?>();

			headerInfo.Add(0, string.Empty);

			int c = -1;
			string value = null;
			int depth = 0;

			foreach (ColumnTitle ct in schColTitleData)
			{
				c++;

				if ( ct.ColumnSubject == CS_HEADING)
				{
					depth++;

					value = (d?.Field<string>(c) ?? "");

					// if empty, done
					if (value.IsVoid()) break;

					// got a heading value
					if (!addHeadingValue(depth, value)) break;
				}
			}
		}

		private bool addHeadingValue(int depth, string hdr)
		{
			if (!headerInfo.ContainsKey(depth - 1)) return false;

			headerInfo.Add(depth, hdr);

			return true;
		}


		/*
		// add the header information to the collection
		// this is a dictionary with the index equal to the header depth
		private void getHeading(DataRow d)
		{
			string value;
			int depth;

			value = getHeadingValue(d, out depth);
			if (value.IsVoid()) return;

			if (depth == 1 || headerInfo == null)
			{
				headerInfo = new Dictionary<int, string?>();
			}

			if (depth != 1 && headerInfo.ContainsKey(depth))
			{
				headerInfo[depth] = value;
				return;
			}

			headerInfo.Add(depth, value);
		}
		*/

		// add the headings to the sheet data
		private void addHeadings(IPdfData? item)
		{
			if (headerInfo == null || headerInfo.Count == 1) return;

			KeyValuePair<int, string?> kvp;

			for (int i = 1; i < headerInfo.Count; i++)
			{
				item.SetValue(CS_HEADING, headerInfo[i]);
			}


			// foreach (KeyValuePair<int, string?> kvp in headerInfo)
			// {
			// 	item.SetValue(CS_HEADING, kvp.Value);
			// 	// colInfo.Add(colValueIdx++, new colValue(CS_HEADING, kvp.Value));
			// }
		}



		// prime schedule

		private void showPrimarySch(PdfSchData? pdfSch)
		{
			if (pdfSch == null) return;

			string h = null;
			pdfSch.Headings.ForEach(s => { h += $"\\{s} "; } );

			string a = $"{(pdfSch.Sequence),3} ";
			string c = $"{(pdfSch.FilePath ?? "no file"),-30}";
			string f = $"{(pdfSch.PdfPath ?? "no path"),-31}";

			M.WriteLine($"XLSX| seq->{a}| xls filename-> {c}| pdf path-> {f}| hdr-> {h}");
		}

		// pdf data

		private void showPdfData(PdfDocData pdfDocData)
		{
			string h = null;
			pdfDocData.Headings.ForEach(s => { h += $"\\{s} "; } );

			string a = $"{(pdfDocData.Sequence),3} ";
			string c = $"{(pdfDocData.FilePath ?? "no file"),-78}";

			M.WriteLine($"PDF | seq->{a}| pdf file-> {c}| hdr-> {h}");
		}

		// sheet data

		private void showSheetData(PdfShtData pdfShtData)
		{
			// string a = $"{(sheetData.Sequence.ToString("D3")),-5}";

			string h = null;
			pdfShtData.Headings.ForEach(s => { h += $"\\{s} "; } );

			string a = $"{(pdfShtData.Sequence),3} ";

			// string b = $"{h ?? (priorHeader ?? "none")}";
			string c = $"{(pdfShtData.SheetNumber ?? "no sht num"),-7}";
			string d = $"{(pdfShtData.SheetName ?? "no sht name"),-68}";

			M.WriteLine($"SHT | seq->{a}| num-> {c}| name> {d}| hdr-> {h}");
		}

		// show

		public void showColTitles()
		{
			ColumnTitle ct;

			M.WriteLine("col titles| colData.ColumnTitles2|\n");

			foreach (KeyValuePair<string, ColumnTitle> kvp in colData.ColumnTitles2)
			{
				ct = kvp.Value;

				M.WriteLine(
					$"key| {kvp.Key,-10}| {((int) ct.ColumnSubject),-2} | {ct.ColumnSubject,-18}| {ct.Heading}"
					);
			}
		}

		public void showSchColData()
		{
			int i = 0;

			M.WriteLine("col data| schColData|\n");

			foreach (ColumnTitle ct in schColTitleData)
			{
				M.WriteLine($"({i++:D2}) | [{((int) ct.ColumnSubject),2}] | {ct.ColumnSubject,-18}| {ct.Heading}");
			}

			M.WriteLine("");
		}

		public void showColInfo()
		{
			int i = 0;

			M.WriteLine("col info");

			string value;

			foreach (KeyValuePair<int, colValue> kvp in colInfo)
			{
				value = $"[{kvp.Value.Subject}]";

				M.WriteLine($"{value,-16} | {kvp.Value.Value}");
			}

			M.WriteLine("");
		}

		private void showHeadings()
		{
			M.WriteLine("headings");

			int i = 0;

			foreach (KeyValuePair<int, string?> kvp in headerInfo)
			{
				M.WriteLine($"[{i++}] | key| {kvp.Key}| value| {kvp.Value}");
			}

			M.WriteLine("");
		}

	#endregion

	#region event consuming

	#endregion

	#region event publishing

	#endregion

	#region system overrides

		public override string ToString()
		{
			return $"this is {nameof(SheetScheduleSupport)}";
		}

	#endregion
	}
}