using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Navigation;
using iText.Kernel.Utils;
// using Org.BouncyCastle.Security;
using static InsertAndAdaptOutlines.InsertAndAdaptOutlines;

namespace InsertAndAdaptOutlines
{
	class PdfMergeTree
	{
		// 1. keep bookmarks from merged files
		// 2. provide a branch for each merged file
		// 3. adjust bookmarks to be children of the merged file branch
		// 4. adjust each page's lable to match its bookmark


		internal const string START_HEADER = "\u2401";
		private const string FILEEXT = ".pdf";

		private BookmarkTree bookmarkTree;

		public PdfMergeTree(bool collapseHeadings = true, 
			bool addHeadingForEachFile = true, 
			bool addImportBookmarksUnderHeading = true, 
			bool addImportBookmarks = false, 
			bool addPageToEmptyBookmarks = true,
			bool ignoreMissingFiles = true,
			bool overwiteExistingFile = false
			)
		{
			bookmarkTree = new BookmarkTree();

			this.CollapseHeadings = collapseHeadings;
			this.AddPageToEmptyBookmarks = addPageToEmptyBookmarks;
			this.IgnoreMissingFiles = ignoreMissingFiles;
			this.OverwiteExistingFile = overwiteExistingFile;

			bookmarkTree.AddHeadingForEachFile = addHeadingForEachFile;
			bookmarkTree.AddImportBookmarksUnderHeading = addImportBookmarksUnderHeading;
			bookmarkTree.AddImportBookmarks = addImportBookmarks;
		}

		private bool CollapseHeadings { get; set; } = true;
		private bool AddPageToEmptyBookmarks { get; set; } = true;
		private bool IgnoreMissingFiles { get; set; } = true;
		private bool OverwiteExistingFile { get; set; } = true;

		internal bool AddHeadingForEachFile
		{
			get { return bookmarkTree.AddHeadingForEachFile; }
			set { bookmarkTree.AddHeadingForEachFile = value; }
		}

		internal bool AddImportBookmarksUnderHeading
		{
			get { return bookmarkTree.AddImportBookmarksUnderHeading; }
			set { bookmarkTree.AddImportBookmarksUnderHeading = value; }
		}

		internal bool AddImportBookmarks
		{
			get { return bookmarkTree.AddImportBookmarks; }
			set { bookmarkTree.AddImportBookmarks = value; }
		}

		private PdfDocument destPdf;

		
		public PdfDocument merge2(FileList fileList, string destinition)
		{
			int pageCount = 1;

			PdfDocument destPdf = null;

			string destFullPath = Path.GetFullPath(destinition);

			if (File.Exists(destFullPath) && !OverwiteExistingFile)
			{
				return null;
			}

			LogMsgln("dest filename| " + destFullPath);

			PdfWriter writer = new PdfWriter(destFullPath);
			writer.SetSmartMode(true);

			destPdf = new PdfDocument(new PdfReader(fileList[0].getFullPath()), writer);

			// get the current outline tree
			PdfOutline destPdfOutline = destPdf.GetOutlines(true);

			// add a marker bookmark + the bookmark title0
			PdfOutline ol = destPdfOutline.AddOutline(START_HEADER + fileList[0].getName(), 0);
			
			PdfPage page = destPdf.GetPage(pageCount);
			ol.AddDestination(PdfExplicitDestination.CreateFit(page));

			pageCount += destPdf.GetNumberOfPages();

			for (int i = 1; i < fileList.GrossCount; i++)
			{
				if (fileList[i].ItemType.Equals(FileList.FileItem.FileItemType.MISSING))
				{
					continue;
				}

				LogMsg("adding| exists| " + File.Exists(fileList[i].getFullPath()) + " " + fileList[i].getFullPath());

				ol = destPdfOutline.AddOutline(START_HEADER + fileList[i].getName());

				PdfDocument insPdf = new PdfDocument(new PdfReader(fileList[i].getFullPath()));

				PdfPage page2 = insPdf.GetPage(pageCount);

				ol.AddDestination(PdfExplicitDestination.CreateFit(page));

				pageCount += insPdf.GetNumberOfPages();

				LogMsgln(" number of pages| " + insPdf.GetNumberOfPages());

				// copy pages from the inserted document to the destinition document
				insPdf.CopyPagesTo(1, insPdf.GetNumberOfPages(), destPdf);
				insPdf.Close();
			}


			return destPdf;
		}

		public PdfDocument merge(List<string> files, string destinitionPath, string destinitionFileName)
		{
			int pageCount = 1;

			LogMsgln("dest filename| " + destinitionFileName);

			PdfWriter writer = new PdfWriter(destinitionFileName);
//			writer.SetSmartMode(true);
			writer.SetSmartMode(false);

			PdfDocument destPdf = new PdfDocument(new PdfReader(file(destinitionPath, files[0])), writer);

			// get the current outline tree
			PdfOutline destPdfOutline = destPdf.GetOutlines(false);

			// add a marker bookmark + the bookmark title
//			PdfOutline ol = destPdfOutline.AddOutline(START_HEADER + files[0], 0);
//			ol.AddDestination(PdfExplicitDestination.CreateFit(pageCount));

			pageCount += destPdf.GetNumberOfPages();

			for (int i = 1; i < files.Count; i++)
			{
//				ol = destPdfOutline.AddOutline(START_HEADER + files[i]);

				string readFile = file(destinitionPath, files[i]);
				LogMsgln("reading| " + readFile);

				// read in the pdf destPdf to insert
				PdfDocument insPdf = new PdfDocument(new PdfReader(readFile));
//				ol.AddDestination(PdfExplicitDestination.CreateFit(pageCount));
				pageCount += insPdf.GetNumberOfPages();

				// copy pages from the insert document into the 
				insPdf.CopyPagesTo(1, insPdf.GetNumberOfPages(), destPdf);
				insPdf.Close();

			}
//			bookmarkTree.AddRootOutlines(destPdf, destPdfOutline);
//
//			destPdfOutline = clearOutline(destPdf);
//
//			if (AddPageToEmptyBookmarks) bookmarkTree.CleanupPageNumbers();
//
//			addBookmarks(bookmarkTree, destPdfOutline, 0, 1);
//
//			LogMsgln("\nbookmark tree after being updated");
//			listOutline(destPdf, destPdfOutline);

			return destPdf;
		}

		private string file(string destPath, string destFile)
		{
			return destPath + destFile + FILEEXT;
		}

		private PdfOutline clearOutline(PdfDocument doc)
		{
			// clear the current outline tree
			doc.GetOutlines(false).GetContent().Clear();

			// initalize a new outline tree
			doc.InitializeOutlines();

			return doc.GetOutlines(true);
		}

		int addBookmarks(PdfDocument doc, BookmarkTree bookmarkTree, PdfOutline child,
			int currItem, int currDepth)
		{
			int pageNum;

			while (currItem < bookmarkTree.count())
			{

				PdfOutline grandChild = child.AddOutline(bookmarkTree[currItem].title);

				if (CollapseHeadings)
				{
					grandChild.SetOpen(false);
				}

				pageNum = bookmarkTree[currItem].page;

				PdfPage page = doc.GetPage(pageNum);

				if (pageNum >= 0)
				{
					grandChild.AddDestination(PdfExplicitDestination.CreateFit(page));
				}

				if (currItem + 1 >= bookmarkTree.count()) return currItem;

				if (bookmarkTree[currItem + 1].depth > bookmarkTree[currItem].depth)
				{
					currItem = addBookmarks(doc, bookmarkTree, grandChild, 
						currItem + 1, currDepth + 1);
				}

				if (currItem + 1 >= bookmarkTree.count() ||
					(bookmarkTree[currItem + 1].depth < bookmarkTree[currItem].depth
						&& bookmarkTree[currItem + 1].depth != currDepth))
				{
					return currItem;
				}
				currItem++;
			}

			return currItem;
		}

	}
}
