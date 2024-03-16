// Solution:     InsertAndAdaptOutlines
// Project:       ExcelTest
// File:             SheetScheduleConfig.cs
// Created:      2023-11-07 (7:31 PM)

using System.Windows.Input;

namespace ExcelTest.SheetSchedule;

public enum RowType
{
	RT_ERROR = -3,
	RT_UNSET = -2,
	RT_HEADING=-1,
	// below are also list / array index numbers
	RT_LIST=0,
	RT_SHEET=1,
	RT_PDF=2,
}

public enum ColumnFormat
{
	CF_STRING = 0,
	CF_BOOL,
	CF_NUMBER,
}

public enum ColumnType
{
	CT_REQD = 0,
	CT_IGNORE,
	CT_OPTIONAL
}

public enum ColumnSubject 
{
	// variable
	CS_HEADING = -1,
	// non-variable
	CS_TYPE = 0,
	CS_X_REL_PATH = 1,
	CS_FILE_NAME,
	CS_P_REL_PATH,
	CS_SHT_NUM,
	CS_SHT_NAME,
	CS_KEEP,
	CS_PARENT,
	CS_PDFFILE,
}