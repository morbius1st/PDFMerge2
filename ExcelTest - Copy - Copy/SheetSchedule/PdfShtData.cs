using System;
using System.Collections.Generic;
using ExcelTest.Windows;
using SharedCode.ShCode;

using SharedPdfCode.PdfLibrary;
using UtilityLibrary;
using static ExcelTest.SheetSchedule.ColumnSubject;

using static SharedCode.ShCode.Status.StatusData;

// using static ExcelTest.SheetSchedule.PdfDataStatus;

// Solution:     InsertAndAdaptOutlines
// Project:       ExcelTest
// File:             PdfSetData.cs
// Created:      2023-11-07 (7:29 PM)

namespace ExcelTest.SheetSchedule;


public class PdfShtData : IPdfDataEx
{
	private string bookmarkFormat = "{0} - {1}";

	public int Sequence { get; set; }
	public RowType RowType { get; } = RowType.RT_SHEET;
	public List<string?>? Headings { get; set; }
	public bool KeepBookmarks => false;
	public string? PdfPath { get; private set; }
	public string? SheetNumber { get; private set; }
	public string? SheetName { get; private set; }

	// public PdfDataStatus Status { get; private set; }
	public ErrorCodes Status { get; private set; }

	public IFilePath File { get; private set; }
	public bool FileExists => File.Exists;

	public PdfSchData Parent { get; private set; }

	public string? FilePath => File.FullFilePath;
	public PdfFile PdfFile { get; private set; }

	public int PageCount => (PdfFile?.File?.Exists ?? false) ? PdfFile.PageCount : 0; 

	public string Bookmark => string.Format(bookmarkFormat, SheetNumber, SheetName);
	public string BookmarkFormat
	{
		get => bookmarkFormat;
		private set => bookmarkFormat = value;
	}

	public PdfShtData()
	{
		Sequence = -1;
		Headings = new List<string?>();
		SheetNumber = null;
		SheetName = null;

		PdfPath = null;
		Status = ErrorCodes.EC_NO_ERROR;
	}

	public PdfShtData(int sequence,
		List<string?>? headings,
		string? pPath,
		string? sheetNumber,
		string? sheetName
		)
	{
		Sequence = sequence;
		Headings = headings;
		SheetNumber = sheetNumber;
		SheetName = sheetName;

		PdfPath = pPath;
	}

	public void SetFileName(string rootPath)
	{
		string filePath = String.Format("{0}{1}", rootPath, PdfPath);

		try
		{
			FilePath<FileNameAsSheetFile> f = new (filePath);

			f.FileNameObject.InvalidNameReplacementChars = new [] { " - " };

			string fileName = $"{SheetNumber} - {SheetName}";

			f.ChangeFileName(fileName, "pdf");

			File = f;

			if (!File.Exists)
			{
				Status = ErrorCodes.EC_FILE_MISSING;
			}
		}
		catch
		{
			Status = ErrorCodes.EC_FILE_MISSING;
		}

	}

	public bool SetValue(ColumnSubject cs, PdfFile? value)
	{
		if (value == null) return false;

		PdfFile = value;

		if (!PdfFile.File.Exists)
		{
			Status = ErrorCodes.EC_FILE_MISSING;
			return false;
		}

		if (PdfFile.HasOutlineError)
		{
			Headings[0] = "** Error ** " + Headings[0];
		}

		Status = ErrorCodes.EC_NO_ERROR;

		return true;
	}

	public bool SetValue(ColumnSubject cs, PdfSchData? value)
	{
		if (value == null) return false;

		Parent = value;

		// update pdf path based on parent information
		PdfPath = Parent.PdfPath + PdfPath;

		return true;
	}

	public bool SetValue(ColumnSubject cs, string? value)
	{
		value = value.Trim();

		switch (cs)
		{
		case CS_HEADING:
			{
				Headings.Add(value);
				break;
			}
		case CS_P_REL_PATH:
			{
				PdfPath = value;
				break;
			}
		case CS_SHT_NUM:
			{
				SheetNumber = value;
				break;
			}
		case CS_SHT_NAME:
			{
				SheetName = value;
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