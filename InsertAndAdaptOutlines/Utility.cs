using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;


using static InsertAndAdaptOutlines.InsertAndAdaptOutlines;

namespace InsertAndAdaptOutlines
{
	internal class Utility
	{
		internal static void pathTest(string sample)
		{
			string[] paths = new[]
			{
				sample,
				@"c:\dir\dir\file.txt",
				@"c:\dir\dir\",
				@"c:\dir\dir\dir",
				@"c:\dir\dir\dir.xxx"
			};

			foreach (string path in paths)
			{
				listPath(path, "");
				LogMsgln("");
			}
		}

		private static void listPath(string path, string offset)
		{
			LogMsgln(offset + "            path| " + path);
			LogMsgln(offset + "       file name| " + Path.GetFileName(path));
			LogMsgln(offset + "file name no ext| " + Path.GetFileNameWithoutExtension(path));
			LogMsgln(offset + "       extension| " + Path.GetExtension(path));
			LogMsgln(offset + "        dir name| " + Path.GetDirectoryName(path));
			LogMsgln(offset + "       path root| " + Path.GetPathRoot(path));

			string fullPath = Path.GetFullPath(path);

			if (fullPath.Length > 0)
			{
				LogMsgln(offset + "       full path| " + Path.GetFullPath(path));
				if (offset.Length == 0)
				{
					listPath(fullPath, offset + "  ");
				}
			}
			else
			{
				LogMsgln(offset + "       full path| none");
			}
		}
	}

	public static class StringExtensions
	{
		public static string Repeat(this string s, int quantity)
		{
			if (quantity <= 0) return "";

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < quantity; i++)
			{
				sb.Append(s);
			}

			return sb.ToString();
		}

		public static int CountSubstring(this string s, string substring)
		{
			int count = 0;
			int i = 0;
			while ((i = s.IndexOf(substring, i)) != -1)
			{
				i += substring.Length;
				count++;
			}

			return count;
		}
	}

	public static class OutlineExtension
	{
		// extension method to the outlines 
		// in the itext package
		public static int GetPageNumber(this PdfOutline outline, PdfDocument doc)
		{
			try
			{
				PdfDictionary dict = outline.GetContent().GetAsDictionary(PdfName.A);

				if (dict == null)
				{
					PdfArray array = outline.GetContent().GetAsArray(PdfName.Dest);

					if (array != null)
					{
						PdfObject obj = array.SubList(0, 1)[0];

						if (obj is PdfNumber)
						{
							return ((PdfNumber)obj).IntValue() + 1;
						}
						else if (obj is PdfDictionary)
						{
							return doc.GetPageNumber((PdfDictionary)obj);
						}
						else
						{
							return -3;
						}
					}
					else
					{
						return -1;
					}
				}

				dict = dict.GetAsArray(PdfName.D).GetAsDictionary(0);

				return doc.GetPageNumber(dict);

			}
			catch (Exception e)
			{
				return -2;
			}
		}
	}

}
