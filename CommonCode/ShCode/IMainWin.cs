#region + Using Directives

#endregion

// user name: jeffs
// created:   11/26/2023 10:28:44 AM

namespace CommonCode.ShCode
{
	public interface IMainWin
	{
		public string Messages { get; set; }

		public string PrimeFolderPath { get; set; }
		public string PrimeFileNameNoExt { get; set; }
		public string DestFolderPath { get; set; }
		public string DestFileNameNoExt { get; set; }

		public void PbarStatReset();

		public double PbarStatValue { get; set; }
		public double PbarStatMin { get; set; }
		public double PbarStatMax { get; set; }

		public void PbarPhaseReset();

		public double PbarPhaseValue { get; set; }
		public double PbarPhaseMin { get; set; }
		public double PbarPhaseMax { get; set; }
	}
}
