#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UtilityLibrary;
using static SharedPdfCode.ShCode.TreeItemType;

#endregion

// username: jeffs
// created:  11/23/2023 11:43:14 AM

namespace SharedPdfCode.ShCode
{
	public enum TreeItemType
	{
		TI_LEAF,
		TI_FILE,
		TI_FILE_LEAF,
		TI_BRANCH
	}

	public interface ITreeItem
	{
		public TreeItemType ItemType { get; }
		public string Bookmark { get; }
		public string? Value { get; }
		public int PageNumber { get; }
	}

	public class PfTreeFile : PfTreeLeaf
	{
		public PfTreeFile(FilePath<FileNameSimple> file,
			string bookmark, int pageCount, bool keepBookmarks)
			: base(file, bookmark, pageCount, keepBookmarks)
		{
			itemType = TI_FILE;
		}
	}

	public class PfTreeFileLeaf : PfTreeLeaf
	{
		public PfTreeFileLeaf(FilePath<FileNameSimple> file,
			string bookmark, int pageCount, bool keepBookmarks)
			: base(file, bookmark, pageCount, keepBookmarks)
		{
			itemType = TI_FILE_LEAF;
		}
	}

	public class PfTreeLeaf : ITreeItem
	{
		protected TreeItemType itemType = TI_LEAF;

		public TreeItemType ItemType
		{
			get => itemType;
			private set => itemType = value;
		}

		public PfTreeLeaf(FilePath<FileNameSimple> file,
			string bookmark, int pageCount, bool keepBookmarks)
		{
			File = file;
			PageCount = pageCount;
			KeepBookmarks = keepBookmarks;
			Bookmark = bookmark;
		}

		public string? Value => Bookmark;

		public FilePath<FileNameSimple> File { get; }
		public string? FilePath => File.FullFilePath;
		public string Bookmark { get; private set; }
		public bool KeepBookmarks { get; }
		public int PageCount { get; }
		public int PageNumber { get; set; }
	}

	public class PfTreeBranch : ITreeItem
	{
		public TreeItemType ItemType { get; private set; }

		public string Bookmark { get; private set; }

		public int PageNumber { get; set; }

		public string? Value => Bookmark;

		public PfTreeBranch(string bookmark)
		{
			ItemType = TI_BRANCH;
			ItemList = new Dictionary<string, ITreeItem>();

			Bookmark = bookmark;
		}

		private string BranchName(string? name) => $"{name}-{TI_BRANCH}";
		private string LeafName(string? name) => $"{name}-{TI_LEAF}";

		// key == bookmark for branch
		// == filename for leaf
		public Dictionary<string, ITreeItem> ItemList { get; set; }

		public bool ContainsBranch(string bookmark, out PfTreeBranch? branch)
		{
			ITreeItem? item;

			branch = null;

			if (!ItemList.TryGetValue(BranchName(bookmark), out item)) return false;

			branch = (PfTreeBranch?) item;

			return true;
		}

		public bool ContainsLeaf(PfTreeLeaf? leaf, out PfTreeLeaf? leafItem)
		{
			ITreeItem? item;
			bool result =  ItemList.TryGetValue(LeafName(leaf.Value), out item);

			leafItem = (PfTreeLeaf?) item;

			return result;
		}

		public bool AddLeaf(PfTreeLeaf leaf)
		{
			string key = LeafName(leaf.Value);

			if (ItemList.ContainsKey(key)) return false;

			ItemList.Add(key, leaf);

			return true;
		}

		public bool AddBranch(string bookmark, out PfTreeBranch? branch)
		{
			branch = null;

			string key = BranchName(bookmark);

			if (ItemList.ContainsKey(key)) return false;

			ItemList.Add(key, new PfTreeBranch(bookmark));

			branch = (PfTreeBranch) ItemList[key];

			return true;
		}
	}


	// structure:
	// dir<root, ListA>>
	// ListA can contain branches and leaves  (e.g.: leaf, leaf, branch, branch, leaf, leaf)

	public class PdfFileTree
	{
		public const string ROOTNAME = "root";


	#region private fields

		private PfTreeBranch root;

		private int currPageNum = 1;

		private int currPdfFilePageNum = 0;
	#endregion

	#region ctor

		public PdfFileTree()
		{
			root = new PfTreeBranch(ROOTNAME);
		}

	#endregion

	#region public properties

		public PfTreeBranch Root => root;

		public int CurrPageNum => currPageNum;


	#endregion

	#region public methods

		public void SetCurrPdfFilePageNum()
		{
			currPdfFilePageNum = currPageNum;
		}

		public void AddToCurrentPageNumber(int amount)
		{
			currPageNum += amount;
		}

		public void AddDuplicateBranch(List<string?>? bookmarkList)
		{

		}

		public void GetOrAddBranch(List<string?>? bookmarkList, out PfTreeBranch? branch)
		{
			PfTreeBranch? tempBranch;
			branch = null;

			int level =	getBranch(bookmarkList, out tempBranch);

			if (level > 0)
			{
				branch = tempBranch;
				return;
			}

			for (int i = (level + 1) * -1; i < bookmarkList.Count - 1; i++)
			{
				tempBranch!.AddBranch(bookmarkList[i], out tempBranch);

				tempBranch.PageNumber = currPageNum;
			}

			tempBranch!.AddBranch(bookmarkList[^1], out branch);

			branch.PageNumber = currPageNum;
		}

		public void AddLeaf(PfTreeLeaf leaf, List<string?>? bookmarkList)
		{
			PfTreeBranch? branch;

			if (leaf.PageCount < 0)
			{
				leaf.PageNumber = currPdfFilePageNum + leaf.PageNumber - 1;
				currPageNum = leaf.PageNumber;
			}

			GetOrAddBranch(bookmarkList, out branch);

			if (leaf.PageCount > 0)
			{
				leaf.PageNumber = currPageNum;
				currPageNum += leaf.PageCount;
			}

			branch.AddLeaf(leaf);
		}

	#endregion

	#region private methods

		// intent: find the branch at the end of the list
		// outcomes
		// 1. found: branch is the found branch; result = the level found+1 (ie. 1 indexed)
		// 2. not found: a non-matching branch was found (or no more branches below exist)
		//    return: branch is the last matching branch; level is the last level * -1

		private int getBranch(List<string> bookmarkList, out PfTreeBranch? branch)
		{
			int level = 1;
			branch = null;

			PfTreeBranch? temp;

			bool result = true;
			PfTreeBranch? currentBranch = root;

			do
			{
				if (level == bookmarkList.Count + 1)
				{
					branch = currentBranch;
					return level;
				}

				if (currentBranch.ContainsBranch(bookmarkList[level - 1], out temp))
				{
					currentBranch = temp;
					level++;
				}
				else
				{
					// a non matching bookmark found
					branch = currentBranch;
					level = 0 - level;
					result = false;
				}
			}
			while (result);

			return level;
		}

	#endregion

	#region private properties

	#endregion

	#region event consuming

	#endregion

	#region event publishing

	#endregion

	#region system overrides

		public override string ToString()
		{
			return $"this is {nameof(PdfFileTree)}";
		}

	#endregion
	}
}