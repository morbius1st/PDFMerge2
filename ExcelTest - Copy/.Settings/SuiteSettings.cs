using System.Runtime.Serialization;

// Suite settings (per user)
//	- applies to a specific app suite (multiple programs)
//	- holds information needed by all programs in the suite, but not all users of the suite
//	- provides the pointer to the site settings file (why here: allows each user to be associated with different site files)
//	- located in the user's app data folder

// ReSharper disable once CheckNamespace

namespace SettingsManager
{
#region suite data class

	// this is the actual data set saved to the user's configuration file
	// this is unique for each program
	[DataContract(Namespace = "")]
	public class SuiteSettingDataFile : IDataFile
	{
		[IgnoreDataMember]
		public string DataFileVersion => "suite 7.4su";

		[IgnoreDataMember]
		public string DataFileDescription => "suite setting file for SettingsManager v7.4";

		[IgnoreDataMember]
		public string DataFileNotes => "suite / any notes go here";

		[DataMember(Order = 1)]
		public int SuiteSettingsValue { get; set; } = 7;

		[DataMember(Order = 2)]
		public string SiteRootPath { get; set; }
			= @"C:\Users\jeffs\AppData\Roaming\CyberStudio\SettingsManager\SettingsManagerv74\SiteSettings" ;
	}

#endregion
}