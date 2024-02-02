using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using static InsertAndAdaptOutlines.InsertAndAdaptOutlines;

namespace InsertAndAdaptOutlines
{
	public class FileList : IEnumerable<FileList.FileItem>
	{
		public class FileItem
		{
			private static string rootPath;
			internal static int notFound { get; private set; }

			internal static int RootPathLen { get; private set; }

			public enum FileItemType {MISSING, FILE }
			
			internal string path { get; set; }
			internal FileItemType ItemType { get; private set; }


			public FileItem(string path)
			{
				this.path = setPath(Path.GetFullPath(path));
				this.ItemType = DetermineItemType(path);
				
			}

			private string setPath(string testPath)
			{
				if (RootPath == null)
				{
					throw new DirectoryNotFoundException();
				}

				if (testPath.Length <= RootPathLen)
				{
					return "";
				}

				return testPath.Substring(RootPathLen);
			}

			public static FileItemType DetermineItemType(string path)
			{
				if (path != null)
				{
					if (File.Exists(path))
					{
						// found file
						return FileItemType.FILE;
					}
				}

				notFound++;
				return FileItemType.MISSING;
			}

			internal int Depth
			{
				get
				{
					if (ItemType == FileItemType.MISSING) { return -1; }
					return path.CountSubstring("\\");
				}
			}

			internal string getName()
			{
				if (ItemType == FileItemType.MISSING) { return ""; }

				return Path.GetFileNameWithoutExtension(path);
			}

			internal string getHeading()
			{
				if (ItemType == FileItemType.MISSING) { return ""; }

				string[] directories = Path.GetDirectoryName(path).Split('\\');

				return directories[directories.Length - 1];
			}

			internal string getDirectory()
			{
				if (ItemType == FileItemType.MISSING) { return ""; }

				return Path.GetDirectoryName(path);
			}

			internal string getFullPath()
			{
				if (ItemType == FileItemType.MISSING) { return ""; }

				return Path.GetFullPath(RootPath + "\\" + path);
			}

			public static string RootPath
			{
				get { return rootPath; }
				set
				{
					if (value == null || value.Length <= 3)
					{
						rootPath = null;
					}

					rootPath = Path.GetFullPath(value);
					RootPathLen = RootPath.Length;
				}
			}
		}

		private List<FileItem> FileItems = new List<FileItem>();
		
		public FileList(string rootPath)
		{
			if (!Directory.Exists(rootPath))
			{
				throw new DirectoryNotFoundException();
			}

			FileItem.RootPath = rootPath;
		}

		internal FileItem this[int index] => FileItems[index];

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<FileItem> GetEnumerator()
		{
			return FileItems.GetEnumerator();
		}

		internal int GrossCount => FileItems.Count;
		internal int NetCount => FileItems.Count - FileItem.notFound;

		internal string RootPath => FileItem.RootPath;

		internal bool Add(string dirEntry)
		{
			if (dirEntry == null) { return false; }

			FileItems.Add(new FileItem(dirEntry));

			return true;
		}

		internal bool Add(string[] files)
		{
			bool result = true;

			if (files == null || files.Length == 0) { return false; }

			foreach (string file in files)
			{
				result = Add(file) && result;
			}

			return result;
		}

		// move only allowed within the same directory depth
		internal bool Move(int from, int to)
		{
			if (from < 0 || to < 0
				|| from == to || !canMove(from, to)) { return false; }

			int remove = from;

			if (to < from) { remove++; }

			FileItems.Insert(to, FileItems[from]);
			FileItems.RemoveAt(remove);

			return true;
		}

		private bool canMove(int from, int to)
		{
			return FileItems[from].getDirectory().Equals(FileItems[to].getDirectory());
		}

	}
}
