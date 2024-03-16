#region + Using Directives
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

#endregion

// user name: jeffs
// created:   12/12/2023 11:20:08 PM

namespace SharedCode.ShCode
{
	public static class W
	{
		public static IMainWin mw { get; set; }

		public static string PrimeFolder
		{
			get => mw.PrimeFolderPath;
			set => mw.PrimeFolderPath = value;
		}

		public static string PrimeFile
		{
			get => mw.PrimeFileNameNoExt;
			set => mw.PrimeFileNameNoExt = value;
		}

		public static string DestFolder
		{
			get => mw.DestFolderPath;
			set => mw.DestFolderPath = value;
		}

		public static string DestFile
		{
			get => mw.DestFileNameNoExt;
			set => mw.DestFileNameNoExt = value;
		}

		public static void PbarStatReset()
		{
			mw.PbarStatReset();
		}

		public static double PbStatValue
		{
			get => mw.PbarStatValue;
			set => mw.PbarStatValue = value;
		}

		public static double PbStatMin
		{
			get => mw.PbarStatMin;
			set => mw.PbarStatMin = value;
		}

		public static double PbStatMax
		{
			get => mw.PbarStatMax;
			set => mw.PbarStatMax = value;
		}
		
		public static void PbarPhaseReset()
		{
			mw.PbarPhaseReset();
		}

		public static double PbPhaseValue
		{
			get => mw.PbarPhaseValue;
			set => mw.PbarPhaseValue = value;
		}

		public static double PbPhaseMin
		{
			get => mw.PbarPhaseMin;
			set => mw.PbarPhaseMin = value;
		}

		public static double PbPhaseMax
		{
			get => mw.PbarPhaseMax;
			set => mw.PbarPhaseMax = value;
		}
	}
}
