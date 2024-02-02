using System.Runtime.Serialization;

// App settings (per user)
//	- applies to a specific app in the suite
//	- holds information specific to the app
//	- located in the user's app data folder / app name / AppSettings


// ReSharper disable once CheckNamespace

namespace SettingsManager
{
#region app data class

	// this is the actual data set saved to the user's configuration file
	// this is unique for each program
	[DataContract(Namespace = "")]
	public class AppSettingDataFile : IDataFile
	{
		[IgnoreDataMember]
		public string DataFileVersion => "app 7.4a";

		[IgnoreDataMember]
		public string DataFileDescription => "app setting file for SettingsManager v7.4";

		[IgnoreDataMember]
		public string DataFileNotes => "app / any notes go here";

		[DataMember(Order = 1)]
		public int AppSettingsValue { get; set; } = 7;
	}

#endregion
}