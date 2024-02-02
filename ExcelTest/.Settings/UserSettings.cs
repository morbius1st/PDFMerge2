using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using SharedCode.ShCode;

// User settings (per user) 
//  - user's settings for a specific app
//	- located in the user's app data folder / app name


// ReSharper disable once CheckNamespace


namespace SettingsManager
{
#region user data class

	// this is the actual data set saved to the user's configuration file
	// this is unique for each program
	[DataContract(Namespace = "")]
	public class UserSettingDataFile : IDataFile
	{
		public UserSettingDataFile()
		{
			WinLocations = new Dictionary<WinId, WinLocation>();
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (WinLocations == null) WinLocations = new Dictionary<WinId, WinLocation>();
		}

		[IgnoreDataMember]
		public string DataFileVersion => "user 7.4u";

		[IgnoreDataMember]
		public string DataFileDescription => "user setting file for SettingsManager v7.4";

		[IgnoreDataMember]
		public string DataFileNotes => "user / any notes go here";

		[DataMember(Order = 1)]
		public int UserSettingsValue { get; set; } = 7;

		[DataMember(Order = 2)]
		public string PrimaryScheduleFileNameNoExt { get; set; } = "unselected";   //= "Primary-Sheet-Schedule";

		[DataMember(Order = 3)]
		public string PrimaryScheduleFolderPath { get; set;} = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

		[DataMember(Order = 4)]
		public string DestFileName { get; set; } = "Combined";

		[DataMember(Order = 5)]
		public string DestFolderPath { get; set;} = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

		[DataMember(Order = 6)]
		public bool OverwriteDestination { get; set; } = false;

		[DataMember]
		public Dictionary<WinId, WinLocation> WinLocations { get; set; }


		public WinLocation GetLocation(WinId id)
		{
			if (WinLocations.ContainsKey(id)) { return WinLocations[id]; }

			return new WinLocation(100.0, 100.0, -1.0, -1.0);
		}

		public void SaveLocation(WinId id, WinLocation location)
		{
			if (WinLocations.ContainsKey(id))
			{
				WinLocations[id] = location;
			}
			else
			{
				WinLocations.Add(id,location);
			}
		}


	}

#endregion
}


// , APP_SETTINGS, SUITE_SETTINGS, MACH_SETTINGS, SITE_SETTINGS