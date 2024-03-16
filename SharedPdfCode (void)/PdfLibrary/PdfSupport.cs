#region + Using Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

using iText.IO.Source;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Navigation;
using iText.Kernel.Utils;
using iText.Layout.Element;
using SharedCode.ShCode;
using SharedPdfCode.ShCode;
using UtilityLibrary;
using static SharedCode.ShCode.Status.StatusData;

#endregion

// user name: jeffs
// created:   11/20/2023 12:11:37 AM

namespace SharedPdfCode.PdfLibrary
{
	public class PdfFile
	{
		// public PdfDocument pdfDocument { get; }
		// public PdfCatalog pdfCatalog { get; }
		// public PdfDictionary pdfDictionary { get; }

		public FilePath<FileNameSimple> File { get; private set; }

		// currLevel / pageNumber / isBranch (0==no / 1 == yess)
		public Dictionary<string, int[]>? OutlineList { get; private set; }

		// int = page number / int = number of usages of this page number - expected is 1
		public Dictionary<int, int> PageVsOutlineList { get; set; }

		public int PageCount { get; private set; }

		public bool HasOutlines { get; private set; }

		public bool HasOutlineError { get; private set; }

		// removed
		// public List<PdfPage> Pages { get; private set; }

		/*
		// removed
		public PdfFile(PdfDocument pdfDocument, FilePath<FileNameSimple> file)
		{
			if (pdfDocument == null || file == null) 
				throw new ArgumentNullException(nameof(pdfDocument), "pdf source cannot be null");

			File = file;

			this.pdfDocument = pdfDocument;
			pdfCatalog = pdfDocument.GetCatalog();
			pdfDictionary = pdfCatalog.GetPdfObject();

			init();
		}

		public PdfFile(PdfDocument pdfDocument, FilePath<FileNameAsSheetFile> file)
		{
			if (pdfDocument == null || file == null) 
				throw new ArgumentNullException(nameof(pdfDocument), "pdf source cannot be null");

			File = new FilePath<FileNameSimple>(file.FullFilePath);

			this.pdfDocument = pdfDocument;
			pdfCatalog = pdfDocument.GetCatalog();
			pdfDictionary = pdfCatalog.GetPdfObject();

			init();
		}
		*/

		public PdfFile(PdfDocument pdfDoc, IFilePath file)
		{
			init(pdfDoc, file);
		}

		private void init(PdfDocument pdfDoc, IFilePath file)
		{
			OutlineList = null;

			File = new FilePath<FileNameSimple>(file.FullFilePath);

			HasOutlineError = false;
			PageCount = pdfDoc.GetNumberOfPages();
			HasOutlines = pdfDoc.HasOutlines();

			if (PageCount > 1)
			{
				if (HasOutlines)
				{
					OutlineList = PdfSupport.GetOutlineList(pdfDoc);

					if (OutlineList!.Count == 0)
					{
						HasOutlineError = true;
						HasOutlines = false;
					}
					else
					{
						PageVsOutlineList = PdfSupport.GetPageVsOutlineList();

						PdfSupport.adjustPdfOutlineList();
					}
				}
				else
				{
					HasOutlineError = true;
					HasOutlines = false;
				}
			}
		}
	}

	public class PdfSupport
	{
	#region private fields

		private static readonly Lazy<PdfSupport> instance =
			new Lazy<PdfSupport>(() => new PdfSupport());

		private static Dictionary<string, int[]>? outlineList;

		private static IMainWin mw;

		private static PdfDocument pdfDoc;

	#endregion

	#region ctor

		private PdfSupport() { }

	#endregion

	#region public properties

		public static PdfSupport Instance => instance.Value;

		public static int PageCount { get; set; }

		// public static IMainWin M
		// {
		// 	get => mw; set => mw = value;
		// }

	#endregion

	#region private properties

	#endregion

	#region public methods

		/*
		public static PdfFile? GetPdfFile(FilePath<FileNameSimple>? file)
		{
			return getPdfFile(file);
		}

		public static PdfFile? GetPdfFile(FilePath<FileNameAsSheetFile> file)
		{
			return getPdfFile(file);
		}
		*/

		public static PdfFile? GetPdfFile(IFilePath file)
		{
			return getPdfFile(file);
		}

		public static Dictionary<string, int[]>? GetOutlineList(PdfDocument pdfDoc)
		{
			if (pdfDoc == null) return null;

			outlineList = new Dictionary<string, int[]>();

			PdfNameTree desTree = pdfDoc.GetCatalog().GetNameTree(PdfName.Dests);
			IDictionary<PdfString, PdfObject> pdfDict = desTree.GetNames();
			PdfOutline root = pdfDoc.GetOutlines(false);
			IPdfNameTreeAccess treeAccess = pdfDoc.GetCatalog().GetNameTree(PdfName.Dests);

			traverseOutlines(root, treeAccess, pdfDoc, 0);

			return outlineList;
		}

		public static List<PdfPage> GetPages(PdfDocument pdfDoc)
		{
			List<PdfPage> pages = new List<PdfPage>();

			int numPages = pdfDoc.GetNumberOfPages();

			for (int i = 1; i < numPages; i++)
			{
				pages.Add(pdfDoc.GetPage(i));
			}

			return pages;
		}

		// merge and add bookmarks
		// provide list of pdf files organized by bookmark
		// that is a "tree" list
		// directory<string, item>
		// item can be a pdf file or a directory<string, item>

// 		public static bool MergePdfTree(FilePath<FileNameSimple> dest, PdfFileTree pdfTree)
// 		{
// 			if (dest == null || dest.Exists || dest.IsFolderPath) return false;
//
// 			pdfDoc = new PdfDocument(new PdfWriter(dest.FullFilePath));
// 			PdfMergerProperties mp = new PdfMergerProperties();
// 			mp.SetCloseSrcDocuments(true);
// 			mp.SetMergeTags(true);
// 			mp.SetMergeOutlines(false);
//
// 			PdfMerger merger = new PdfMerger(pdfDoc, mp);
//
// 			merger.SetCloseSourceDocuments(true);
//
// 			try
// 			{
// 				mergeTreeItems(pdfTree.Root, merger);
// 			}
// 			catch
// 			{
// 				pdfDoc.Close();
// 				return false;
// 			}
//
// 			PageCount = pdfDoc.GetNumberOfPages();
//
//
// 			/*
// 			PdfOutline rootOutline = pdfDoc.GetOutlines(false);
//
// 			try
// 			{
// 				createOutlineTree(pdfTree.Root, pdfDoc, rootOutline);
// 			}
// 			catch 
// 			{
// 				pdfDoc.Close();
// 				return false;
// 			}
//
//
// 			pdfDoc.Close();
// 			*/
//
// 			return true;
// 		}

		public static void ClosePdf()
		{
			if (!pdfDoc.IsClosed()) pdfDoc.Close();
		}

		/*
		public static bool CreateOutlineTree(PdfFileTree pdfTree)
		{
			if (pdfDoc == null || pdfDoc.IsClosed()) return false;

			PdfOutline rootOutline = pdfDoc.GetOutlines(false);

			try
			{
				createOutlineTree(pdfTree.Root, pdfDoc, rootOutline);
			}
			catch
			{
				pdfDoc.Close();
				return false;
			}

			pdfDoc.Close();

			return true;
		}
		*/

		public static void adjustPdfOutlineList2()
		{
			int idx = 0;
			int tmp;

			KeyValuePair<string, int[]> priorkvp = new KeyValuePair<string, int[]>("", new []{1,0,0});

			foreach (KeyValuePair<string, int[]> kvp in outlineList!)
			{

				if (idx > 0)
				{
					// test levels
					// [0] = level / [1] = page number / [2] is branch
					// compare page numbers
					if (priorkvp.Value[1] > kvp.Value[1])
					{
						kvp.Value[2] = 2;
					} 

					else
					
					// compare levels
					if (priorkvp.Value[0] < kvp.Value[0])
					{
						priorkvp.Value[2] = 3;
					} 

					else 

					// compare page numbers
					if ( priorkvp.Value[1] == kvp.Value[1] )
					{
						priorkvp.Value[2] = 4;
					}
										
					// else 
					//
					// if (tmp < 0)
					// {
					// 	kvp.Value[2] = 5;
					// } 


					showOutlineItem(priorkvp);
				}


				priorkvp = kvp;
				idx++;
			}

			showOutlineItem(priorkvp);
		}

		public static void adjustPdfOutlineList()
		{
			int idx = 0;

			int prior = 1;
			int curr = 1;

			KeyValuePair<string, int[]> priorkvp = new KeyValuePair<string, int[]>("", new []{1,0,0});

			foreach (KeyValuePair<string, int[]> kvp in outlineList!)
			{
				curr = kvp.Value[0];

				// test levels
				// [0] = level / [1] = page number / [2] is branch

				
				if ( prior == curr )
				{
					priorkvp.Value[2] = 4;
				}

				else
				
				if (prior < curr)
				{
					priorkvp.Value[2] = 3;
				} 

				else 

				if (prior > curr)
				{
					priorkvp.Value[2] = 2;
				} 
				
				// showOutlineItem(priorkvp);

				prior = kvp.Value[0];
				priorkvp = kvp;
				idx++;
			}

			// showOutlineItem(priorkvp);
		}


		private static string[] outlineType = new []
		{
			"Leaf", "Branch", "up", "down", "level"
		};

		private static void showOutlineItem(KeyValuePair<string, int[]> kvp)
		{
			// int b = kvp.Value[2];
			// if (b < 2)
			// {
			// 	int x = 1;
			// }

			string b1 = outlineType[kvp.Value[2]];
			string a = $"bkmk-> {"   ".Repeat(kvp.Value[0]-1)}{kvp.Key}";
			string l = $"lvl-> {kvp.Value[0]}";
			string p = $"pg-> {kvp.Value[1]}";

			M.WriteLine($"{a,-50}| {l,-4}| {p}| ({kvp.Value[2]}) {b1}");

		}

		
		public static bool MergePdfTree(FilePath<FileNameSimple> dest, PdfNodeTree tree)
		{
			if (dest == null || dest.Exists || dest.IsFolderPath) return false;

			pdfDoc = new PdfDocument(new PdfWriter(dest.FullFilePath));
			PdfMergerProperties mp = new PdfMergerProperties();
			mp.SetCloseSrcDocuments(true);
			mp.SetMergeTags(true);
			mp.SetMergeOutlines(false);

			PdfMerger merger = new PdfMerger(pdfDoc, mp);

			merger.SetCloseSourceDocuments(true);

			try
			{
				mergeTreeItems(tree.Root, merger);
			}
			catch
			{
				pdfDoc.Close();
				return false;
			}

			PageCount = pdfDoc.GetNumberOfPages();

			return true;
		}


		public static bool CreateOutlineTree(PdfNodeTree tree)
		{
			if (pdfDoc == null || pdfDoc.IsClosed()) return false;

			PdfOutline rootOutline = pdfDoc.GetOutlines(false);

			try
			{
				createOutlines(tree.Root, pdfDoc, rootOutline, 1);
			}
			catch (Exception e)
			{
				Status.SetStatus(ErrorCodes.EC_EXCEPTION, Overall.OS_FAIL,
					$"{e.Message}");
				return false;
			}


			ClosePdf();
			return true;
		}

	#endregion

	#region private methods

		private static void mergeTreeItems(PdfTreeBranch branch, PdfMerger merger)
		{

			foreach (KeyValuePair<string, IPdfTreeItem> kvp in branch.ItemList)
			{
				W.PbPhaseValue++;

				if (kvp.Value.ItemType == PdfTreeItemType.PT_BRANCH)
				{
					mergeTreeItems((PdfTreeBranch) kvp.Value, merger);
				}
				else if (kvp.Value.ItemType == PdfTreeItemType.PT_LEAF)
				{
					PdfDocument src = 
						new PdfDocument(new PdfReader(((PdfTreeLeaf) kvp.Value).FilePath));
		
					merger.Merge(src, 1, ((PdfTreeLeaf) kvp.Value).PageCount);
				} 
				else if (kvp.Value.ItemType == PdfTreeItemType.PT_NODE_FILE)
				{
					PdfDocument src = 
						new PdfDocument(new PdfReader(((PdfTreeNodeFile) kvp.Value).FilePath));
		
					merger.Merge(src, 1, ((PdfTreeNodeFile) kvp.Value).PageCount);
				}
				else
				{
					M.WriteLine("invalid tree item found");
				}
			}
		}

		/// <summary>
		/// merge a list of PDF's into a combined pdf
		/// </summary>
		// private static void mergeTreeItems(PfTreeBranch branch, PdfMerger merger)
		// {
		// 	foreach (KeyValuePair<string, ITreeItem> kvp in branch.ItemList)
		// 	{
		// 		if (kvp.Value.ItemType == TreeItemType.TI_BRANCH)
		// 		{
		// 			mergeTreeItems((PfTreeBranch) kvp.Value, merger);
		// 		}
		// 		else if (kvp.Value.ItemType != TreeItemType.TI_FILE_LEAF)
		// 		{
		// 			PdfDocument src =
		// 				new PdfDocument(new PdfReader(((PfTreeLeaf) kvp.Value).FilePath));
		//
		// 			merger.Merge(src, 1, ((PfTreeLeaf) kvp.Value).PageCount);
		// 		}
		// 	}
		// }

		private static void createOutlines(PdfTreeBranch branch,
			PdfDocument pdfDoc, PdfOutline outline, int level)
		{
			PdfOutline ol = null;

			// M.WriteLine($"start  1| {branch.Bookmark,-32}| {branch.ItemType,-12}| level| {level}");

			foreach (KeyValuePair<string, IPdfTreeItem> kvp in branch.ItemList)
			{
				W.PbPhaseValue++;

				if (kvp.Value.ItemType == PdfTreeItemType.PT_NODE_FILE)
				{
					ol = outline.AddOutline(kvp.Value.Bookmark);
					ol.AddDestination(PdfExplicitDestination.CreateFit(
						pdfDoc.GetPage(kvp.Value.PageNumber)));

					// M.WriteLine($"before 2| {kvp.Key,-32}| {branch.ItemType,-12}| level| {level}");
					createOutlines((PdfTreeNodeFile) kvp.Value, pdfDoc, ol!, level);
					// M.WriteLine($"after  2| {kvp.Key,-32}| {branch.ItemType,-12}| level| {level}");

					continue;
				}

				ol = outline.AddOutline(kvp.Value.Bookmark);
				ol.AddDestination(PdfExplicitDestination.CreateFit(
					pdfDoc.GetPage(kvp.Value.PageNumber)));

				if (kvp.Value.ItemType == PdfTreeItemType.PT_BRANCH)
				{
					// M.WriteLine($"before 1| {kvp.Key,-32}| {branch.ItemType,-12}| level| {level}");

					createOutlines((PdfTreeBranch) kvp.Value, pdfDoc, ol, level+1);

					// M.WriteLine($"after  1| {kvp.Key,-32}| {branch.ItemType,-12}| level| {level}");
				} 
				// else //if (kvp.Value.ItemType == PdfTreeItemType.PT_NODE)
				// {
				// 	M.WriteLine($"{kvp.Key,-32}| {kvp.Value.ItemType,-12}| level| {level}");
				// }
			}
		}

		private static void createOutlines2(PdfTreeNode node,
			PdfDocument pdfDoc, PdfOutline outline, int level)
		{
			PdfOutline ol = null;

			foreach (KeyValuePair<string, IPdfTreeItem> kvp in node.ItemList)
			{
				M.WriteLine($"        | {kvp.Key,-32}| {kvp.Value.ItemType,-12}| node level| {((PdfTreeNode) kvp.Value).Level} level| {level}");

				ol = outline.AddOutline(kvp.Value.Bookmark);
				ol.AddDestination(PdfExplicitDestination.CreateFit(
					pdfDoc.GetPage(kvp.Value.PageNumber)));

				if (((PdfTreeNode) kvp.Value).Level > level)
				{
					createOutlines((PdfTreeNode) kvp.Value, pdfDoc, ol, 
						((PdfTreeNode) kvp.Value).Level);
				} 
				
			}
		}

		private static void createOutlines(PdfTreeNode node,
			PdfDocument pdfDoc, PdfOutline outline, int level)
		{
			PdfOutline ol = null;

			foreach (KeyValuePair<string, IPdfTreeItem> kvp in node.ItemList)
			{
				ol = outline.AddOutline(kvp.Value.Bookmark);
				ol.AddDestination(PdfExplicitDestination.CreateFit(
					pdfDoc.GetPage(kvp.Value.PageNumber)));

				if (((PdfTreeNode) kvp.Value).Level > level)
				{
					createOutlines((PdfTreeNode) kvp.Value, pdfDoc, ol, level + 1);
				}
			}
		}


		/*
		private static void createOutlineTree(PfTreeBranch branch, 
			PdfDocument pdfDoc, PdfOutline outline)
		{
			foreach (KeyValuePair<string, ITreeItem> kvp in branch.ItemList)
			{
				if (kvp.Value.ItemType == TreeItemType.TI_FILE) continue;

				PdfOutline ol = outline.AddOutline(kvp.Value.Bookmark);
				ol.AddDestination(PdfExplicitDestination.CreateFit(
					pdfDoc.GetPage(kvp.Value.PageNumber)));

				if (kvp.Value.ItemType == TreeItemType.TI_BRANCH)
				{
					createOutlineTree((PfTreeBranch) kvp.Value, pdfDoc, ol);
				}
			}
		}
		*/

		// private static void addChildOutlineTree(PdfDocument pdfDoc,
		// 	PdfOutline outline, int currPageNum, Dictionary<string, int>? childOutlines)
		// {
		// 	int pagenum;
		//
		// 	foreach (KeyValuePair<string, int> kvp in childOutlines)
		// 	{
		// 		pagenum = currPageNum + kvp.Value;
		//
		// 		PdfOutline ol = outline.AddOutline(kvp.Key);
		// 		ol.AddDestination(PdfExplicitDestination.CreateFit(pdfDoc.GetPage(pagenum)));
		//
		// 	}
		// }
		//

		private static void traverseOutlines(
			PdfOutline outline, IPdfNameTreeAccess names,
			PdfDocument pdfDocument, int currLevel)
		{
			string title;
			int pageNumber;
			int isBranch = 0;
			PdfObject pdfObject;
			PdfDestination destination;
			PdfDictionary pdfDict;
			

			if (outline.GetDestination() != null)
			{
				title = outline.GetTitle();
				// pageNumber = pdfDocument.GetPageNumber((PdfDictionary) outline.GetDestination().GetDestinationPage(names));
				destination = outline.GetDestination();
				pdfObject = destination.GetDestinationPage(names);
				pdfDict = (PdfDictionary) pdfObject;
				pageNumber = pdfDocument.GetPageNumber(pdfDict);

				PdfDictionary a = outline.GetContent();
				bool b1 = a.IsEmpty();
				ICollection<PdfObject> v1 = a.Values();

				PdfDestination d = outline.GetDestination();
				PdfObject p = d.GetDestinationPage(names);
				

				isBranch = 0;

				// if (outline.GetAllChildren().Count > 0)
				// {
				// 	isBranch = 1;
				// }

				outlineList!.Add(title, new int[] { currLevel, pageNumber, isBranch } );
			}

			foreach (PdfOutline child in outline.GetAllChildren())
			{
				traverseOutlines(child, names, pdfDocument, currLevel + 1);
			}
		}

		public static Dictionary<int, int> GetPageVsOutlineList()
		{

			Dictionary<int, int> pg = new Dictionary<int, int>();

			// int[] = curr level / pageNumber / isbranch
			foreach (KeyValuePair<string, int[]> kvp in outlineList)
			{
				if (pg.ContainsKey(kvp.Value[1]))
				{
					pg[kvp.Value[1]] += 1;
				}
				else
				{
					pg.Add(kvp.Value[1],1);
				}
			}

			return pg;
		}

		/*
		// already combined pdf - may have existing bookmarks
		private static PdfFile? getPdfFile(FilePath<FileNameSimple>? file)
		{
			if (file == null || !file.Exists || file.IsFolderPath) return null;

			PdfReader reader = new PdfReader(file.FullFilePath);

			PdfDocument pdf = new PdfDocument(reader);

			PdfFile pf = new PdfFile(pdf, file);

			reader.Close();

			return pf;
		}

		// sheet file - considered a single pdf in the file
		private static PdfFile? getPdfFile(FilePath<FileNameAsSheetFile>? file)
		{
			if (file == null || !file.Exists || file.IsFolderPath) return null;

			PdfReader reader = new PdfReader(file.FullFilePath);

			PdfDocument pdf = new PdfDocument(reader);

			PdfFile pf = new PdfFile(pdf, file);

			reader.Close();

			return pf;
		}
		*/

		private static PdfFile? getPdfFile(IFilePath? file)
		{
			if (file == null || !file.Exists || file.IsFolderPath) return null;

			PdfFile pf = null;

// *** exception
			try
			{
				PdfReader reader = new PdfReader(file.FullFilePath);

				PdfDocument pdf = new PdfDocument(reader);

				pf = new PdfFile(pdf, file);

				reader.Close();
			}
			catch
			{
				Status.SetStatus(ErrorCodes.EC_CANNOT_READ_PDF,
					Overall.OS_PARTIAL_FAIL,
					$"{file.FileName}");

				return null;
			}

			return pf;
		}

	#endregion

	#region event consuming

	#endregion

	#region event publishing

	#endregion

	#region system overrides

		public override string ToString()
		{
			return $"this is {nameof(PdfSupport)}";
		}

	#endregion
	}
}