using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Navigation;
using iText.Kernel.Utils;
// using Org.BouncyCastle.Bcpg.OpenPgp;
using static InsertAndAdaptOutlines.InsertAndAdaptOutlines;



namespace InsertAndAdaptOutlines
{
	class InsertAndAdaptOutlines
	{
		private int tabs = 0;
		private static  int depth = 0;

		private string margin;

		private const string ROOTPATH = @"C:\Users\Jeffs\Documents\Programming\VisualStudioProjects\PDF SOLUTIONS\InsertAndAdaptOutlines\InsertAndAdaptOutlines\";
		private const string TESTROOTPATH = ROOTPATH + "PDF Test Folder\\";
		private const string FILEROOTPATH = TESTROOTPATH;

		private const string FILEEXT = ".pdf";
		private const string SRC_FILENAME = "Source";
		private const string INS1_FILENAME = "Bookmarks_a1";
		private const string INS2_FILENAME = "Bookmarks_b";
		private const string BOGUS_FILENAME = "Bogus1";

		private const string DEST_FILENAME = "output";

		private const string DIRECTORY_1 = "Directory 1.x\\";
		private const string DIRECTORY_2 = "Directory 2\\";
		private const string DIRECTORY_3 = DIRECTORY_2 + "Directory 3\\";

		private const string SAMPLE1 = TESTROOTPATH + SRC_FILENAME + FILEEXT;
		private const string SAMPLE2 = TESTROOTPATH + INS1_FILENAME + FILEEXT;
		private const string SAMPLE3 = TESTROOTPATH + INS2_FILENAME + FILEEXT;


		// some bad names
		// a directory only
		private const string SAMPLE_x1 = TESTROOTPATH + DIRECTORY_1;
		// non-existent file
		private const string SAMPLE_x2 = TESTROOTPATH + DIRECTORY_1 + "bogus.pdf";
		// bad path
		private const string SAMPLE_x3 = "..\\" + "Source.pdf";

		internal const string START_HEADER = "\u2401";

		const string DEST = ROOTPATH + DEST_FILENAME + FILEEXT;

		private bool collapseHeadings = true;
		private bool addPageToEmptyBookmarks = true;
		private bool ignoreMissingFiles = true;
		private bool overwiteExistingFile = true;

		private bool addHeadingForEachFile = true;
		private bool addImportBookmarksUnderHeading = true;
		private bool addImportBookmarks = true;

		public class Item
		{
			public string path;
			public List<Item> items = new List<Item>(0);

			internal Item(string path, 
				List<Item> items)
			{
				this.path = path;
				this.items = items;
			}
		}


		static void Main(string[] args)
		{
			InsertAndAdaptOutlines I = new InsertAndAdaptOutlines();

			if (I.manipulatePdf2(DEST))
			{
				LogMsgln("\nworked...");
			}
			else
			{
				LogMsgln("\nfailed...");
			}
			Console.WriteLine("press enter to continue: ");
			Console.Read();

		}

		public bool manipulatePdf(string destPdf)
		{
			List<string> files = new List<string>() { SRC_FILENAME,
				INS2_FILENAME, INS1_FILENAME };

			if (!merge(files, destPdf)) { return false; }

			return true;
		}

		public bool manipulatePdf2(string destPdf)
		{
			List<string> files = new List<string>() { SRC_FILENAME,
				INS2_FILENAME, INS1_FILENAME };

			merge(destPdf, files);

			return true;
		}

		public bool manipulatePdf3(string destPdf)
		{

			string[] files = Directory.GetFiles(TESTROOTPATH, "*.*", SearchOption.AllDirectories);

			LogMsgln("   start| ");
			LogMsgln("  output| " + destPdf);

			FileList fileList = new FileList(TESTROOTPATH);

			if (!fileList.Add(files))
			{
				return false;
			}

			fileList.Add(SAMPLE_x1);
			fileList.Add(SAMPLE_x2);
			fileList.Add(SAMPLE_x3);

//			listDirItems(fileList);

			PdfMergeTree merger = new PdfMergeTree(collapseHeadings,
				addHeadingForEachFile, addImportBookmarksUnderHeading,
				addImportBookmarks, addPageToEmptyBookmarks,
				ignoreMissingFiles, overwiteExistingFile);

			PdfDocument doc = merger.merge2(fileList, destPdf);

			if (doc != null)
			{
				LogMsgln("\nworked... pages| " + doc.GetNumberOfPages());
				
				doc.Close();
				

				LogMsgln("exists| " + File.Exists(destPdf));
			}
			else
			{
				LogMsgln("\n*** fail: documents not merged ***");
			}

			return true;
		}

		void listDirItems(FileList fileList)
		{
			LogMsgln("gross count| " + fileList.GrossCount);
			LogMsgln("  net count| " + fileList.NetCount);
			LogMsgln("   rootpath| " + fileList.RootPath);
			LogMsgln("");

			foreach (FileList.FileItem item in fileList)
			{
				LogMsgln("     path| " + item.path);
				LogMsgln("     type| " + item.ItemType);
				if (item.ItemType == FileList.FileItem.FileItemType.FILE)
				{
					LogMsgln("    depth| " + item.Depth);
					LogMsgln("      dir| " + item.getDirectory());
					LogMsgln("  heading| " + item.getHeading());
					LogMsgln("     name| " + item.getName());
					LogMsgln("full path| " + item.getFullPath());
				}
				else
				{
					LogMsgln("         | does not exist");
				}

				LogMsgln("");
			}
		}

		void listDirItems2(FileList fileList)
		{
			LogMsgln("gross count| " + fileList.GrossCount);
			LogMsgln("  net count| " + fileList.NetCount);
			LogMsgln("   rootpath| " + fileList.RootPath);
			LogMsgln("");

			foreach (FileList.FileItem item in fileList)
			{
				if (item.ItemType == FileList.FileItem.FileItemType.FILE)
				{
					LogMsgln("     path| " + item.path);
				}
			}

		}
//
//		public void manipulatePdf2(string destPdf)
//		{
//			List<Item> files1 =
//				new List<Item>() {
//					new Item(file(SRC_FILENAME), null),
//					new Item(file(INS1_FILENAME), null),
//					new Item(file(INS2_FILENAME), null),
//					new Item(file(BOGUS_FILENAME), null)
//				};
//
//			List<Item> files3 =
//				new List<Item>() {
//					new Item(file(SRC_FILENAME), null),
//					new Item(file(INS1_FILENAME), null),
//					new Item(file(INS2_FILENAME), null)
//				};
//
//			List<Item> files2 = 
//				new List<Item>() {
//					new Item(file(SRC_FILENAME), null),
//					new Item(file(INS1_FILENAME), null),
//					new Item(file(INS2_FILENAME), null),
//					new Item(folder(DIRECTORY_3), files3)
//					 };
//
//			List<Item> Items = 
//				new List<Item>()
//				{
//					new Item(folder(DIRECTORY_1), files1), 
//					new Item(folder(DIRECTORY_2), files2)
//				};
//
//			PdfMergeTree merger = new PdfMergeTree(collapseHeadings,
//				addHeadingForEachFile, addImportBookmarksUnderHeading,
//				addImportBookmarks, addPageToEmptyBookmarks,
//				ignoreMissingFiles);
//
//			PdfDocument doc = merger.merge(Items, FILEROOTPATH, destPdf);
//
//			if (doc != null)
//			{
//				doc.Close();
//			}
//			else
//			{
//				LogMsgln("\n*** fail: documents not merged ***");
//			}
//		}

		string file(string name)
		{
			return folder(name) + FILEEXT;
		}

		string folder(string name)
		{
			return FILEROOTPATH + name;
		}


		public bool merge(List<string> files, string destPdf)
		{
			PdfMergeTree merger = new PdfMergeTree(collapseHeadings,
				addHeadingForEachFile, addImportBookmarksUnderHeading,
				addImportBookmarks, addPageToEmptyBookmarks);

			PdfDocument doc = merger.merge(files, FILEROOTPATH, destPdf);

			if (doc == null) { return false; }

			doc.Close();

			return true;
		}


		public void merge(string destPdf, List<string> files)
		{
			BookmarkTree bookmarkTree = new BookmarkTree();
		
			int pageCount = 1;
		
			PdfWriter writer = new PdfWriter(destPdf);
			writer.SetSmartMode(true);
		
			PdfDocument outPdf = new PdfDocument(new PdfReader(file(files[0])), writer);
			// get the current outline tree
			PdfOutline outPdfOutlines = outPdf.GetOutlines(true);
		
//			PdfDocument PdfDocx = outPdf;

			PdfPage page = outPdf.GetPage(pageCount);
		
			// add a marker bookmark + the bookmark title
			PdfOutline ol = outPdfOutlines.AddOutline(START_HEADER + files[0], 0);
			ol.AddDestination(PdfExplicitDestination.CreateFit(page));
		
			pageCount += outPdf.GetNumberOfPages();
		
			for (int i = 1; i < files.Count; i++)
			{
				page = outPdf.GetPage(pageCount);
				
				ol = outPdfOutlines.AddOutline(START_HEADER + files[i]);
				ol.AddDestination(PdfExplicitDestination.CreateFit(page));
		
				// read in the pdf doc to insert
				PdfDocument insPdf = new PdfDocument(new PdfReader(file(files[i])));
				pageCount += insPdf.GetNumberOfPages();
		
				// copy pages from the insert document into the 
				insPdf.CopyPagesTo(1, insPdf.GetNumberOfPages(), outPdf);
		
				insPdf.Close();
		
			}
		
			LogMsgln("\nfinal bookmark tree before being update");
			listOutline(outPdf, outPdfOutlines);
		
			bookmarkTree.AddRootOutlines(outPdf, outPdfOutlines); //, addHeadingForEachFile);
		
			LogMsgln("\nsaved bookmark tree");
			LogMsgln(bookmarkTree.ToString());
		
			outPdfOutlines = clearOutline(outPdf);
			LogMsgln("\nbookmarks after being initalized");
			listOutline(outPdf, outPdfOutlines);
		
		
			bookmarkTree.CleanupPageNumbers();
		
			LogMsgln("\nbookmark tree after bein cleaned");
			LogMsgln(bookmarkTree.ToString());
		
		
			LogMsgln("\nupdating bookmarks");
		
			addBookmarks(outPdf, outPdfOutlines, bookmarkTree, 0, 1);
		
			depth = 0;
			LogMsgln("\nbookmark tree after being updated");
			listOutline(outPdf, outPdfOutlines);
		
			outPdf.Close();
		}
		
		PdfOutline clearOutline(PdfDocument doc)
		{
			// clear the current outline tree
			doc.GetOutlines(false).GetContent().Clear();
		
			// initalize a new outline tree
			doc.InitializeOutlines();
		
			return doc.GetOutlines(true);
		}

		int addBookmarks(PdfDocument doc, PdfOutline child, BookmarkTree bookmarkTree, 
			int currItem, int currDepth)
		{
			int pageNum;
		
			while (currItem < bookmarkTree.count())
			{
				PdfOutline grandChild = child.AddOutline(bookmarkTree[currItem].title);
		
				if (currDepth == 1 && collapseHeadings)
				{
					grandChild.SetOpen(false);
				}
		
				pageNum = bookmarkTree[currItem].page;
		
				LogMsgln(bookmarkTree[currItem].title + "  depth| " + currDepth);

				PdfPage page;
		
				if (pageNum >= 0)
				{
					page = doc.GetPage(pageNum);
					grandChild.AddDestination(PdfExplicitDestination.CreateFit(page));
				}
		
				if (currItem + 1 >= bookmarkTree.count()) return currItem;
		
				if (bookmarkTree[currItem + 1].depth > bookmarkTree[currItem].depth)
				{
					currItem = addBookmarks(doc, grandChild, bookmarkTree, 
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

		public static void listOutline(PdfDocument pdfDoc, PdfOutline outline)
		{
			LogMsg(formatBookmark(outline.GetTitle(), depth, outline.GetPageNumber(pdfDoc)));

			LogMsg(Environment.NewLine);

			IList<PdfOutline> kids = outline.GetAllChildren();

			if (kids.Count != 0)
			{
				depth++;

				for (int i = 0; i < kids.Count; i++)
				{
					listOutline(pdfDoc, kids[i]);
				}

				depth--;
			}
		}

		public static string formatBookmark(string title, int depth, int page)
		{
			return String.Format("bookmark|  depth| {1,3} | page| {0,3} | {2}{3}",
				page, depth, " ".Repeat(depth * 2), title);
		}

		public static void LogMsgln(string msg)
		{
			Console.WriteLine(msg);
		}

		public static void LogMsg(string msg)
		{
			Console.Write(msg);
		}

	}
}
