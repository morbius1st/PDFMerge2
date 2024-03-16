#region using

using System;
using System.Collections.Generic;
using SharedCode.ShCode;
using SharedPdfCode.ShCode;
using UtilityLibrary;
using static SharedCode.ShCode.M;
using static SharedCode.ShCode.Status.StatusData;

#endregion

// username: jeffs
// created:  12/9/2023 6:02:59 PM

namespace ExcelTest.SheetSchedule
{
	public class PdfNodeTreeSupport
	{
	#region private fields

		private PdfNodeTree tree;

	#endregion

	#region ctor

		public PdfNodeTreeSupport(PdfNodeTree tree)
		{
			this.tree= tree;
		}

	#endregion

	#region public properties

	#endregion

	#region private properties

	#endregion

	#region public methods

		public bool CreatePdfTree(List<IPdfDataEx> items)
		{
			Status.Phase = Status.StatusData.Progress.PS_PH_CT;

			if (tree == null || items == null || items.Count == 0)
			{
				Status.SetStatus(
					ErrorCodes.EC_SHEET_LIST_INVALID,
					Overall.OS_FAIL); ;
			}
			else
			{
				foreach (IPdfDataEx pd in items)
				{
					W.PbPhaseValue++;

					addToPdfTree(pd);
				}

				if (Status.IsGoodOverall)
				{
					Status.SetStatus(ErrorCodes.EC_NO_ERROR, Overall.OS_GOOD);
				}
			}

			return Status.IsGoodOverall;
		}

	#endregion

	#region private methods

		private void addToPdfTree(IPdfDataEx data)
		{
			APdfTreeNode node;

			if (data.RowType == RowType.RT_SHEET)
			{
				// got a single page pdf / sheet
				node = new PdfTreeLeaf(data.Bookmark,
					data.File, 
					data.PageCount);

				tree.AddNode(data.Headings!, node);
			} 
			else 
			if (data.RowType == RowType.RT_PDF)
			{
				if (data.KeepBookmarks)
				{
					// two parts - the file and the bookmarks
					// for the file, it has do not increase the page count
					// as this must be done while processing the
					// outlines
					node = new PdfTreeNodeFile(data.Bookmark,
						(FilePath<FileNameSimple>) data.File, data.PageCount);

					((PdfTreeNodeFile) node).Level = 1;


					tree.SetPdfNodePageNumber();

					// this will also save the current node
					// in the tree for future reference
					// for this operation, cannot modify the
					// page count in the tree
					tree.AddOutlineNode(new List<string>(), (PdfTreeNode) node, 0);

					processOutlines(data);

					tree.AddToPageNumber(data.PageCount);
					tree.UpdateCurrentNode(node);
				}
				else
				{
					node = new PdfTreeLeaf(data.Bookmark,
						(FilePath<FileNameSimple>) data.File, 
						data.PageCount);

					tree.AddNode(data.Headings, node);
				}

			}
			else
			{
				Status.SetStatus(ErrorCodes.EC_CT_INVALID_ITEM_FOUND,
					Overall.OS_PARTIAL_FAIL);
			}
		}

		private void processOutlines(IPdfDataEx data)
		{
			if (data.PdfFile.HasOutlineError)
			{
				Status.SetStatus(ErrorCodes.EC_CT_OUTLINES_HAS_ERRORS,
					Overall.OS_PARTIAL_FAIL, $"File| {data.File.FileName} | {data.Bookmark}");
			}

			PdfTreeNode node;

			bool result;

			int depthChange = 4; // 2 == up / 3 == down / 4 == level
			int levelnum = 0;
			int priorlevel = 1;

			// data.Headings is just a node list

			// kvp.Value[] ==
			// [0] = level / [1] == (relative) pageNumber / [2] = isBranch (0==no / 1 == yes)

			foreach (KeyValuePair<string, int[]> kvp in data.PdfFile.OutlineList)
			{
				// three options - up, down, or level
				// 2 == up / 3 == down / 4 == level
				levelnum = kvp.Value[0];

				// int lvldif = priorlevel - levelnum;
				// string h = String.Empty;
				// foreach (string? s in data.Headings)
				// {
				// 	h += $"\\{s}";
				// }
				// M.WriteLine($"bkmk-> {kvp.Key,-32}| lvl-> {kvp.Value[0],-3}| ({kvp.Value[2]})| dif-> {lvldif,-3}| hdg-> {h}");


				depthChange = kvp.Value[2];


				node = new PdfTreeNode(kvp.Key, 
					(FilePath<FileNameSimple>) data.File, 0);

				node.Level = levelnum+1;
				
				if (!tree.AddOutlineNode(data.Headings, node, kvp.Value[1] - 1)) return;

				if (depthChange == 2)
				{
					// 2 == up
					// the next node needs to up up versus the prior
					int toRemove = levelnum-1;
					int lvlDiff = data.Headings.Count - toRemove;

					// M.WriteLine($"\tup| rmv| {toRemove}| dif| {lvlDiff}");

					data.Headings.RemoveRange(toRemove, lvlDiff);
				} 


				if (depthChange == 3)
				{
					// M.WriteLine($"\tdn| add| {kvp.Key}");
					// 3 = down
					data.Headings.Add(kvp.Key);
				}

				priorlevel = kvp.Value[0];
			}

		}

	#endregion

	#region event consuming

	#endregion

	#region event publishing

	#endregion

	#region system overrides

		public override string ToString()
		{
			return $"this is {nameof(PdfNodeTreeSupport)}";
		}

	#endregion
	}
}