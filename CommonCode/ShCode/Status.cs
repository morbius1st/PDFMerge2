#region + Using Directives

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityLibrary;
using static CommonCode.ShCode.Status.StatusData;

#endregion

// user name: jeffs
// created:   11/28/2023 10:48:22 PM

namespace CommonCode.ShCode
{
	public static class Status
	{
		// private static int isGoodLocalDepth = 0;
		// private static List<bool> isGoodLocal;

		private static StatusData.ErrorCodes lastError;
		private static Overall overall = Overall.OS_GOOD;

		static Status()
		{
			Reset();
		}

		public static bool SetStatus(StatusData.ErrorCodes err,
			StatusData.Overall oa = StatusData.Overall.OS_NULL,
			string errDetail = null)
		{
			if (oa != StatusData.Overall.OS_NULL) Overall = oa;
			
			if (err > ErrorCodes.EC_NULL)
			{
				lastError = err;

				ErrorInfo ei = new (err, errDetail);

				Errors.Add(ei);
			}

			return IsGoodOverall;
		}

		// public static bool IsGoodLocalSet { get; set; }
		/*
		public static bool IsGoodLocalGet => isGoodLocal[isGoodLocalDepth];

		public static bool IsGoodLocalGetUp
		{
			get
			{
				bool result = isGoodLocal[isGoodLocalDepth];

				if (isGoodLocalDepth > 0) isGoodLocalDepth--;

				return result;
			}
		}

		public static bool IsGoodLocalSet
		{
			set => isGoodLocal[isGoodLocalDepth++] = value;
		}

		public static void IsGoodLocalPop()
		{
			if (isGoodLocalDepth > 0) isGoodLocalDepth--;
		}
		*/


		public static bool IsGoodOverall
		{
			get
			{
				return Overall > Overall.OS_NULL &&
					Errors.Count == 0;
			}
		}

		public static StatusData.Progress Phase { get; set; }

		public static StatusData.Overall Overall
		{
			get => overall;
			set => overall = value;
		}

		public static List<ErrorInfo> Errors { get; private set; }

		public static void Reset()
		{
			Phase = StatusData.Progress.PS_PH_ST;
			Overall = StatusData.Overall.OS_STARTED;
			Errors = new List<ErrorInfo>();
			// isGoodLocal = new List<bool>();
			// isGoodLocalDepth = 0;
		}

		public static string GetPhaseDesc()
		{
			return StatusData.PhaseStatusDesc[Phase];
		}

		public static string GetOaStatusDesc()
		{
			return StatusData.OverallStatusDesc[Overall];
		}

		public static string GetErrors()
		{
			StringBuilder sb = new ();

			foreach (ErrorInfo er in Errors)
			{
				sb.AppendLine($"{er.GetErrorDesc()}{er.GetDetailedMsg("| ")}");
			}

			return sb.ToString();
		}

		public new static string ToString()
		{
			return $"Ph| {Phase}| OA| {Overall}| Er| {(Errors.Count > 0 ? Errors[^1] : "no Errors")}| Er Count| {Errors.Count}";
		}

		public struct ErrorInfo
		{
			public StatusData.ErrorCodes Error { get; private set; }
			public string DetaiInfo { get; private set; }

			public ErrorInfo(StatusData.ErrorCodes error, string detaiInfo)
			{
				Error = error;
				DetaiInfo = detaiInfo;
			}

			public string GetErrorDesc()
			{
				return StatusData.ErrorStatusDesc[Error];
			}

			public string GetDetailedMsg(string divider)
			{
				if (DetaiInfo.IsVoid()) return String.Empty;

				return $"{divider}{DetaiInfo}";
			}
		}

		public class StatusData
		{
			public enum Progress
			{
				PS_PH_ST = 0,
				PS_PH_VD = 1,
				PS_PH_PP = 2,
				PS_PH_PS = 3,
				PS_PH_VS = 4,
				PS_PH_CT = 5,
				PS_PH_MP = 6,
				PS_PH_CB = 7,
				PS_PH_VF = 8,

			}

			public const Progress PS_PH_XX = Progress.PS_PH_VF + 1;
			public const Progress PS_PH_ER = Progress.PS_PH_VF + 2;

			// value > 0 - ok to proceed
			// value < 0 - cannot proceed
			public enum Overall
			{
				OS_FAIL = -101,
				OS_WARNING = -1,
				// < os_null - failures
				OS_NULL = 0,
				// > os_null - ok / good
				OS_STARTED = 1,
				OS_WORKED = 2,
				OS_GOOD = 3,
			}

			public static Dictionary<Progress, string> PhaseStatusDesc = new ()
			{
				{ Progress.PS_PH_ST, "Phase 0 (Started)" },                      // _ST (start)
				{ Progress.PS_PH_VD, "Phase 1 (Validate Destination File)" },    // ** _VD (validate dest)
				{ Progress.PS_PH_PP, "Phase 2 (Process Primary Schedule)" },     // ** _PP (proc prime)
				{ Progress.PS_PH_PS, "Phase 3 (Process Schedules and Files)" },  // ** _PS (proc sch)
				{ Progress.PS_PH_VS, "Phase 4 (Validate Sheet List)" },          // ** _VS (validate sheet)
				{ Progress.PS_PH_CT, "Phase 5 (Create Pdf Tree)" },              // ** _CS (create sheetlist)
				{ Progress.PS_PH_MP, "Phase 6 (Merge Pdf Files)" },              // ** _MP (merge pdf)
				{ Progress.PS_PH_CB, "Phase 7 (Create Bookmarks)" },             // ** _CB (create bookmarks)
				{ Progress.PS_PH_VF, "Phase 8 (Check for Skipped Files)" },      // ** _VF (validate files)
				{ (Progress) PS_PH_XX, "All Phases Completed" },                 // ** _XX (Completed)
				{ (Progress) PS_PH_ER, "Process Incomplete" },                  // ** _XX (Completed)
			};

			public static Dictionary<Overall, string> OverallStatusDesc = new ()
			{
				{ Overall.OS_FAIL, "Fail" },          // oa stat, failed - do not proceed
				{ Overall.OS_GOOD, "Good" },          // oa stat is good - ok to proceed
				{ Overall.OS_WORKED, "Worked" },      // oa stat is good - ok to proceed
				{ Overall.OS_NULL, "Null" },          // no value - ignore
				{ Overall.OS_WARNING, "Warning" },    // oa stat, non-fatal failure - proceed with caution
				{ Overall.OS_STARTED, "Started" },    // oa stat - just started, nothing to report
			};


			public enum ErrorCodes
			{
				EC_RESET = -2,
				EC_UNSET = -1,
				// < ec_null mgmt codes
				EC_NO_ERROR = 0,
				EC_NULL = 0,
				// > ec_null are errors
				// general errors
				EC_SHEET_LIST_INVALID = 11,
				EC_DEST_EXISTS,
				EC_FILENAME_INVALID,
				EC_ROW_HAS_ERRORS,
				EC_EXCEPTION,
				
				// PDF processing >100
				EC_FILE_MISSING = 101,
				EC_CANNOT_READ_PDF,

				// pdf processing errors
				// > 100

				// > 1000 - phase specific errors
				EC_PP_FILE_MISSING_PRIME = 1001,
				EC_PP_SCHED_WRONG_FORMAT,

				EC_PP_PS_SCHED_WRONG_TYPE,
				EC_PP_PS_SCHED_WRONG_TITLES,
				EC_PP_PS_SCHED_EMPTY,

				EC_GD_DEST_INVALID,

				EC_MP_MERGE_FAIL,

				EC_PS_LIST_INVALID,
				EC_PS_SHEET_INVALID,
				EC_PS_PDF_INVALID,

				EC_CT_INVALID_ITEM_FOUND,
				EC_CT_OUTLINES_HAS_ERRORS,
				EC_CT_CREATE_TREE_FAILED,

				EC_VD_CANNOT_DELETE_FILE,

				EC_VF_EXTRA_FILES_FOUND,
			}

			public static Dictionary<ErrorCodes, string> ErrorStatusDesc = new ()
			{
				{ ErrorCodes.EC_UNSET, "Correct error needs to be determined" },
				{ ErrorCodes.EC_SHEET_LIST_INVALID, "Sheet Schedule is missing or invalid" },
				{ ErrorCodes.EC_DEST_EXISTS, "Destination file exists" },
				{ ErrorCodes.EC_FILENAME_INVALID, "PDF file name is not valid" },
				{ ErrorCodes.EC_ROW_HAS_ERRORS, "Data in a row has errors" },
				{ ErrorCodes.EC_EXCEPTION, "Exception" },

				// pdf processing errors > 100
				{ ErrorCodes.EC_FILE_MISSING, "PDF file not found" },
				{ ErrorCodes.EC_CANNOT_READ_PDF, "Cannot read the PDF file (invalid?)" },
				

				// phase specific errors > 1000
				// ph PP
				{ ErrorCodes.EC_PP_FILE_MISSING_PRIME, "Primary Schedule could not be located" },
				{ ErrorCodes.EC_PP_SCHED_WRONG_FORMAT, "Primary Schedule has an incorrect format" },

				// ph PP & PS
				{ ErrorCodes.EC_PP_PS_SCHED_WRONG_TYPE, "Schedule is the wrong type" },
				{ ErrorCodes.EC_PP_PS_SCHED_WRONG_TITLES, "Schedule has incorrect title(s)" },
				{ ErrorCodes.EC_PP_PS_SCHED_EMPTY, "Schedule has no valid rows" },

				// ph GD
				{ ErrorCodes.EC_GD_DEST_INVALID, "Destination file is invalid" },

				// ph MP
				{ ErrorCodes.EC_MP_MERGE_FAIL, "Merge PDF files failed" },

				// ph PS
				{ ErrorCodes.EC_PS_LIST_INVALID, "Sheet schedule is not valid" },
				{ ErrorCodes.EC_PS_SHEET_INVALID, "PDF Sheet information is not valid" },
				{ ErrorCodes.EC_PS_PDF_INVALID, "PDF file is not valid" },
				
				// ph VD
				{ ErrorCodes.EC_VD_CANNOT_DELETE_FILE, "Cannot delete the existing destination file" },

				// PH ct
				{ ErrorCodes.EC_CT_INVALID_ITEM_FOUND, "Item, with an invalid row type, found" },
				{ ErrorCodes.EC_CT_OUTLINES_HAS_ERRORS, "Pdf File has outline errors" },
				{ ErrorCodes.EC_CT_CREATE_TREE_FAILED, "Could not create the Pdf Tree" },

				// PH vf
				{ ErrorCodes.EC_VF_EXTRA_FILES_FOUND, "Folder(s) may have unincorporated file(s)" },
			};
		}
	}
}