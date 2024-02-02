using System;
using System.Collections.Generic;
using System.Text;
using iText.Kernel.Pdf;

using static InsertAndAdaptOutlines.InsertAndAdaptOutlines;

namespace InsertAndAdaptOutlines
{

	class BookmarkTree2
	{
		public class Bookmark2
		{
			public enum bookmarkType
			{
				file,
				folder
			}

			public string path;
			public bookmarkType type;
			public int pageNumber;
			public IList<Bookmark2> bookmarks2;

			public Bookmark2(string path, bookmarkType type, int pageNumber, IList<Bookmark2> bookmarks2)
			{
				this.path = path;
				this.type = type;
				this.pageNumber = pageNumber;
				this.bookmarks2 = bookmarks2;
			}

		}

		private int tabDepth = 0;

		private IList<Bookmark2> bookmarks2 = new List<Bookmark2>(5);

		private string rootPath { get; set; } = "";

		internal bool AddHeadingForEachFile { get; set; } = true;

		internal bool AddImportBookmarksUnderHeading { get; set; } = true;

		internal bool AddImportBookmarks { get; set; } = false;

		BookmarkTree2(string rootPath)
		{
			this.rootPath = rootPath;
		}


		public Bookmark2 this[int index] => bookmarks2[index];

		public int count() { return bookmarks2.Count; }
//
//		public void AddRootOutlines(PdfDocument doc, 
//			PdfOutline rootOutline) //  , bool AddBookmarksUnderHeading)
//		{
//			PdfOutline child;
//
//			const int HEADER_START_DEPTH = 1;
//			int childStartDepth = HEADER_START_DEPTH + 1;
//
//			if (!AddImportBookmarksUnderHeading || !AddHeadingForEachFile)
//			{
//				childStartDepth = HEADER_START_DEPTH;
//			}
//
//			IList<PdfOutline> children = rootOutline.GetAllChildren();
//
//			if (rootOutline == null || doc == null ||
//				children.Count == 0) return;
//
//			for (int i = 0; i < children.Count; i++)
//			{
//				child = children[i];
//
//				if (child.GetTitle().Substring(0, 1).Equals(START_HEADER))
//				{
//					if (AddHeadingForEachFile)
//					{
//						bookmarks2.Add(new Bookmark2(child.GetTitle().Substring(1),
//							child.GetPageNumber(doc), HEADER_START_DEPTH));
//					}
//				}
//				else
//				{
//					if (AddImportBookmarks)
//					{
//						bookmarks2.Add(new Bookmark2(child.GetTitle(),
//							child.GetPageNumber(doc), childStartDepth));
//
//						AddChildren(child.GetAllChildren(), doc, childStartDepth);
//					}
//				}
//			}
//		}
//
//		void AddChildren(IList<PdfOutline> children, PdfDocument doc, int depth)
//		{
//			if (children.Count == 0) return;
//
//			depth++;
//			foreach (PdfOutline child in children)
//			{
//				bookmarks2.Add(new Bookmark2(child.GetTitle(),
//					child.GetPageNumber(doc), depth));
//
//				AddChildren(child.GetAllChildren(), doc, depth);
//			}
//		}
//
//		public void CleanupPageNumbers()
//		{
//			int lastPage = -1;
//
//			if (bookmarks2 == null || bookmarks2.Count == 0) return;
//
//			for (int i = bookmarks2.Count - 1; i >= 0; i--)
//			{
//				if (bookmarks2[i].pageNumber > -1)
//				{
//					lastPage = bookmarks2[i].pageNumber;
//				}
//				else
//				{
//					bookmarks2[i].pageNumber = lastPage;
//				}
//			}
//		}
//
//		override public string ToString()
//		{
//			if (bookmarks2.Count == 0) return null;
//			LogMsgln(" ");
//			StringBuilder sb = new StringBuilder();
//			
//			foreach (Bookmark2 bm in bookmarks2)
//			{
//				sb.Append(formatBookmark(bm.path, bm.depth, bm.pageNumber));
//				sb.Append(Environment.NewLine);
//			}
//
//			return sb.ToString();
//		}
	}
}