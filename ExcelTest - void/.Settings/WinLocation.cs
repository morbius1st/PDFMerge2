// Solution:     TopoEdit
// Project:       Jack
// File:             WinLocation.cs
// Created:      2023-12-22 (7:36 AM)

using System.Runtime.Serialization;

namespace SettingsManager
{
	public enum WinId
	{
		WINMAIN
	}


	[DataContract(Namespace = "")]
	public struct WinLocation
	{
		[DataMember]
		public double Top { get; set; }

		[DataMember]
		public double Left { get; set; }

		[DataMember]
		public double Height { get; set; }

		[DataMember]
		public double Width { get; set; }

		[IgnoreDataMember]
		public double CenterOffsetX => Width / 2 * -1;

		[IgnoreDataMember]
		public double CenterOffsetY => Height / 2 * -1;

		public WinLocation(double top, double left, double height, double width)
		{
			Top = top;
			Left = left;
			Height = height;
			Width = width;
		}

		public double TopCentered(double screenLocationX)
		{
			return screenLocationX + CenterOffsetX;
		}

		public double LeftCentered(double screenLocationY)
		{
			return screenLocationY + CenterOffsetY;
		}
	}
}