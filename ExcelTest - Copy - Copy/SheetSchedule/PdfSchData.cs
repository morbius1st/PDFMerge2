using SharedPdfCode.PdfLibrary;
using System.Collections.Generic;
using System.Windows.Documents;
using SharedCode.ShCode;
using UtilityLibrary;
using static ExcelTest.SheetSchedule.ColumnSubject;

// Solution:     InsertAndAdaptOutlines
// Project:       ExcelTest
// File:             PdfPrimary.cs
// Created:      2023-11-07 (7:29 PM)

namespace ExcelTest.SheetSchedule;

/*
public enum PdfDataStatus
{
	PDS_GOOD,
	PDS_FILENAME_INVALID
}
*/

public interface IPdfData
{
	public int Sequence { get; set; }
	public RowType RowType { get; }
	public List<string?>? Headings { get; set; }
	public string? FilePath { get; }
	public bool SetValue(ColumnSubject cs, string? value);

	// public PdfDataStatus Status { get; }
	public Status.StatusData.ErrorCodes Status { get; }

	public IFilePath File { get; }
}

public interface IPdfDataEx : IPdfData
{
	public PdfFile PdfFile { get; }

	public string Bookmark { get; }

	public bool KeepBookmarks { get; }

	public int PageCount { get; }

}

public class PdfSchData : IPdfData
{
	public int Sequence { get; set; }
	public RowType RowType { get; } = RowType.RT_LIST;
	public List<string?>? Headings { get; set; }

	public IFilePath File => new FilePath<FileNameSimple>(FilePath);

	public string? FilePath { get; private set; }

	public string? XlsxPath { get; private set; }
	public string? PdfPath { get; private set; }

	public string? TypeName { get; set; }

	// public PdfDataStatus Status { get; private set; }
	public Status.StatusData.ErrorCodes Status { get; private set; }

	public PdfSchData()
	{
		Headings = new List<string?>();
	}

	public PdfSchData(
		int sequence,
		List<string?> headings,
		string? xPath,
		string? fileAndPath,
		string? pPath)
	{
		Sequence = sequence;
		Headings = headings;

		XlsxPath = xPath;
		PdfPath = pPath;

		FilePath = fileAndPath;
	}

	public bool SetValue(ColumnSubject cs, string value)
	{
		switch (cs)
		{
		case CS_HEADING:
			{
				Headings.Add(value);
				break;
			}
		case CS_X_REL_PATH:
			{
				XlsxPath = value;
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