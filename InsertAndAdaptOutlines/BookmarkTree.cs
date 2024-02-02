using System;
using System.Collections.Generic;
using System.Text;
using iText.Kernel.Pdf;


using static InsertAndAdaptOutlines.InsertAndAdaptOutlines;

namespace InsertAndAdaptOutlines
{

	class BookmarkTree
	{

		internal bool AddHeadingForEachFile { get; set; } = true;

		internal bool AddImportBookmarksUnderHeading { get; set; } = true;

		internal bool AddImportBookmarks { get; set; } = true;

		public class Bookmark
		{
			public string title;
			public int depth;
			public int page;

			public Bookmark(string title, int page, int depth)
			{
				this.title = title;
				this.page = page;
				this.depth = depth;
			}

		}

		private int tabDepth = 0;

		private IList<Bookmark> bookmarks = new List<Bookmark>(5);

		public int count()
		{
			return bookmarks.Count;
		}

		public Bookmark this[int index] => bookmarks[index];

		public void AddRootOutlines(PdfDocument doc, 
			PdfOutline rootOutline) //  , bool AddBookmarksUnderHeading)
		{
			PdfOutline child;

			const int HEADER_START_DEPTH = 1;
			int childStartDepth = HEADER_START_DEPTH + 1;

			if (!AddImportBookmarksUnderHeading || !AddHeadingForEachFile)
			{
				childStartDepth = HEADER_START_DEPTH;
			}

			IList<PdfOutline> children = rootOutline.GetAllChildren();

			if (rootOutline == null || doc == null ||
				children.Count == 0) return;

			for (int i = 0; i < children.Count; i++)
			{
				child = children[i];

				if (child.GetTitle().Substring(0, 1).Equals(START_HEADER))
				{
					if (AddHeadingForEachFile)
					{
						bookmarks.Add(new Bookmark(child.GetTitle().Substring(1),
							child.GetPageNumber(doc), HEADER_START_DEPTH));
					}
				}
				else
				{
					if (AddImportBookmarks)
					{
						bookmarks.Add(new Bookmark(child.GetTitle(),
							child.GetPageNumber(doc), childStartDepth));

						AddChildren(child.GetAllChildren(), doc, childStartDepth);
					}
				}
			}
		}

		void AddChildren(IList<PdfOutline> children, PdfDocument doc, int depth)
		{
			if (children.Count == 0) return;

			depth++;
			foreach (PdfOutline child in children)
			{
				bookmarks.Add(new Bookmark(child.GetTitle(),
					child.GetPageNumber(doc), depth));

				AddChildren(child.GetAllChildren(), doc, depth);
			}
		}

		public void CleanupPageNumbers()
		{
			int lastPage = -1;

			if (bookmarks == null || bookmarks.Count == 0) return;

			for (int i = bookmarks.Count - 1; i >= 0; i--)
			{
				if (bookmarks[i].page > -1)
				{
					lastPage = bookmarks[i].page;
				}
				else
				{
					bookmarks[i].page = lastPage;
				}
			}
		}

		override public string ToString()
		{
			if (bookmarks.Count == 0) return null;
			LogMsgln(" ");
			StringBuilder sb = new StringBuilder();
			
			foreach (Bookmark bm in bookmarks)
			{
				sb.Append(formatBookmark(bm.title, bm.depth, bm.page));
				sb.Append(Environment.NewLine);
			}

			return sb.ToString();
		}
	}
}