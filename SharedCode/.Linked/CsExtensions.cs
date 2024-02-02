using System;
using System.Diagnostics;
using System.Text;

namespace UtilityLibrary
{

	public static class CsExtensions
	{
		public static int AsInt<T>(this T e) where T : Enum, IConvertible //enum
		{
			return (int) (IConvertible) e;
		}

		public static bool IsUpperAlpha(this char c)
		{
			return (c >= 'A' && c <= 'Z');
		}
		
		public static bool IsLowerAlpha(this char c)
		{
			return (c >= 'a' && c <= 'z');
		}
				
		public static bool IsAlpha(this char c)
		{
			return IsUpperAlpha(c) || IsLowerAlpha(c);
		}

		public static bool IsNumber(this char c)
		{
			return (c >= '0' && c <= '9');
		}

		public static string IfNullOrWhiteSpace(this string s, string alternate)
		{
			if (string.IsNullOrWhiteSpace(s))
			{
				return alternate;
			}

			return s;
		}
		
		/// <summary>
		/// true if string is null or whitespace false otherwise
		/// </summary>
		[DebuggerStepThrough]
		public static bool IsVoid(this string s)
		{
			return string.IsNullOrWhiteSpace(s);
		}

		[DebuggerStepThrough]
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
			while ((i = s.IndexOf(substring, i, StringComparison.Ordinal)) != -1)
			{
				i += substring.Length;
				count++;
			}

			return count;
		}

		public static int IndexOfToOccurance(this string s, string substring,
			int occurance, int start = 0
			)
		{
			if (s.Trim().Length == 0) { return -1; }

			if (occurance < 0) { return -1; }

			int pos = start;

			for (int count = 0; count < occurance; count++)
			{
				pos = s.IndexOf(substring, pos, StringComparison.Ordinal);

				if (pos == -1) { return pos; }

				pos += substring.Length;
			}

			return pos - substring.Length;
		}

		public static string GetSubDirectory(this string path, int requestedDepth)
		{
			requestedDepth++;
			if (requestedDepth == 0) { return "\\"; }

			path = path.TrimEnd('\\');

			int depth = CountSubstring(path, "\\");

			if (requestedDepth > depth) { requestedDepth = depth; }

			int pos = IndexOfToOccurance(path, "\\", requestedDepth);

			if (pos < 0) { return ""; }

			pos = IndexOfToOccurance(path, "\\", requestedDepth + 1);

			if (pos < 0) { pos = path.Length; }

			return path.Substring(0, pos);
		}

		public static string GetSubDirectoryName(this string path, int requestedDepth)
		{
			requestedDepth++;

			path = path.TrimEnd('\\');

			if (requestedDepth > CountSubstring(path, "\\")) { return ""; }

			string result = GetSubDirectory(path, requestedDepth - 1);

			if (result.Length == 0) { return ""; }

			int pos = IndexOfToOccurance(path, "\\", requestedDepth) + 1;

			return result.Substring(pos);
		}

		public static string PadCenter(this string str, int totalLength, char padChar = '\u00A0')
		{
			if (str == null) str = "";

			int padAmount = totalLength - str.Length;

			if (padAmount <= 1)
			{
				if (padAmount == 1)
				{
					return str.PadRight(totalLength);
				}

				return str;
			}

			int padLeft = padAmount / 2 + str.Length;

			return str.PadLeft(padLeft).PadRight(totalLength);
		}

		public static string ToList(this string[] str, string divider = " ")
		{
			if (str == null) return "";

			string s = "";

			foreach (string s1 in str)
			{
				s += s1 + divider;
			}

			return s;

		}
	}
}