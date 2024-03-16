using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExcelDataReader;
using ExcelTest.Annotations;
using ExcelTest.SheetSchedule;
using iText.Commons.Utils;
using SettingsManager;
using SharedCode.ShCode;
using SharedPdfCode.ShCode;
using UtilityLibrary;
using static SharedCode.ShCode.Status.StatusData;
using iText.StyledXmlParser.Jsoup.Internal;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace ExcelTest.Windows
{

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged, IMainWin
	{
		public const string ROOT_PATH = @"C:\Users\jeffs\Documents\Programming\VisualStudioProjects\PDF SOLUTIONS";
		public const string DATA_PATH = ROOT_PATH + @"\_Samples\Test";
		public const string PDF_PATH = ROOT_PATH + @"\_Samples\Test";
		public const string XLSX_PATH =  @"\Excel Files";

		public const string PRIME_SCHEDULE = "Primary-Sheet-Schedule.xlsx";
		public const string SHEET_SCHEDULE = "Sheet List-Architectural.xlsx";

		public const string DEST_PATH = ROOT_PATH + @"\_Samples\Test";
		public const string DEST_FILE = "combined.pdf";

		public static string EXT_PDF = "pdf";
		public static string EXT_XLSX = "xlsx";

		private SheetScheduleSupport schSupport;
		private string messages;

		private FilePath<FileNameSimple> dest;

		private FilePath<FileNameSimple> prime;

		private double pbarStatValue = 2;
		private double pbarStatMax = 0;
		private double pbarStatMin = 0;

		private double pbarFileValue = 20;
		private double pbarFileMax = 200;
		private double pbarFileMin = 0;

		private bool isEditing = false;
		private bool isEditingOverwrite = false;
		private string primeFileFound;

		private FilePath<FileNameSimple> primeFilePath;
		private FilePath<FileNameSimple> destFilePath;


		public MainWindow()
		{
			M.mw = this;
			W.mw = this;

			InitializeComponent();

			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

			init();
		}

		private void init()
		{
			schSupport = new SheetScheduleSupport();

			UserSettings.Admin.Read();

			getPrimeFilePath();
			getDestFilePath();

			// OnPropertyChanged(nameof(PrimeFolderPath));
			// OnPropertyChanged(nameof(PrimeFileName));
			// OnPropertyChanged(nameof(DestFolderPath));
			// OnPropertyChanged(nameof(DestFileNameNoExt));

			PbarPhaseReset();
			PbarStatReset();
		}

		private void quit()
		{
			UserSettings.Admin.Write();
		}

		// properties
		public string Messages
		{
			get => messages;
			set
			{ 
				messages = value;
				OnPropertyChanged();
			}
		}

		public string PrimeFolderPath
		{
			get
			{
				// M.WriteLine($"(get) is editing| {isEditing}");
				// return getPrimeFolderPath();

				return getEllipsisifiedPath(UserSettings.Data.PrimaryScheduleFolderPath);
			}
			set
			{
				// M.WriteLine($"(set) is editing| {isEditing}");
				isEditing = false;
				if (value == UserSettings.Data.PrimaryScheduleFolderPath) return;
				UserSettings.Data.PrimaryScheduleFolderPath = value;

				getPrimeFilePath();
			}
		}

		public string PrimeFileNameNoExt
		{
			get => UserSettings.Data.PrimaryScheduleFileNameNoExt;
			set
			{
				if (value == UserSettings.Data.PrimaryScheduleFileNameNoExt) return;
				
				getPrimeFilePath();
			}
		}

		public string PrimeFileName => $"{PrimeFileNameNoExt}{PrimeFileExt}";

		public string PrimeFileExt => $".{EXT_XLSX}";

		public string PrimeFileFound => (primeFilePath?.Exists ?? false) ? "Yes" : "No";

		public string DestFolderPath
		{
			get => getEllipsisifiedPath(UserSettings.Data.DestFolderPath);
			set
			{
				isEditing = false;
				if (value == UserSettings.Data.DestFolderPath) return;
				UserSettings.Data.DestFolderPath = value;

				getDestFilePath();
			}
		}

		public string DestFileNameNoExt
		{
			get => UserSettings.Data.DestFileName;
			set
			{
				if (value == UserSettings.Data.DestFileName) return;
				UserSettings.Data.DestFileName = value;

				getDestFilePath();
			}
		}

		public string DestFileName => $"{DestFileNameNoExt}{DestFileExt}";

		public string DestFileExt => $".{EXT_PDF}";

		public string CurrentPhase { get; private set; }

		public double PbarStatMin
		{
			get => pbarStatMin;
			set
			{
				if (value.Equals(pbarStatMin)) return;
				pbarStatMin = value;
				OnPropertyChanged();
			}
		}

		public double PbarStatMax
		{
			get => pbarStatMax;
			set
			{
				if (value.Equals(pbarStatMax)) return;
				pbarStatMax = value;
				OnPropertyChanged();
			}
		}

		public double PbarStatValue
		{
			get => pbarStatValue;
			set
			{
				if (value.Equals(pbarStatValue)) return;
				pbarStatValue = value;
				OnPropertyChanged();
			}
		}


		public double PbarPhaseMin
		{
			get => pbarFileMin;
			set
			{
				if (value.Equals(pbarFileMin)) return;
				pbarFileMin = value;
				OnPropertyChanged();
			}
		}

		public double PbarPhaseMax
		{
			get => pbarFileMax;
			set
			{
				if (value.Equals(pbarFileMax)) return;
				pbarFileMax = value;
				OnPropertyChanged();
			}
		}

		public double PbarPhaseValue
		{
			get => pbarFileValue;
			set
			{
				if (value.Equals(pbarFileValue)) return;
				pbarFileValue = value;
				OnPropertyChanged();
			}
		}


		// ui properties



		// overwrite system
		// overwrite tblk is visible
		// overwrite changes color on mouse over
		// overwrite tblk is mousedown
		// overwrite => collapsed
		// overwrite yes => visible (change color on mouse over)
		// overwrite no => visible (change color on mouse over)
		// one of the two above is mousedown
		// overwrite is set / on prop change
		// overwrite yes & down => collapsed
		// overwrite => visible
		public Visibility OverwriteDest
		{
			get
			{
				if (isEditingOverwrite) return Visibility.Collapsed;

				return Visibility.Visible;
			}
		}

		public Visibility OverwriteOptions
		{
			get
			{
				if (isEditingOverwrite) return Visibility.Visible;

				return Visibility.Collapsed;
			}
		}

		public string Overwrite
		{
			get
			{
				if (UserSettings.Data.OverwriteDestination) return "Yes";
				return "No";
			}
		}


		public event PropertyChangedEventHandler? PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// utility methods

		private string getEllipsisifiedPath(string path)
		{
			if (!isEditing)
			{
				return CsStringUtil.EllipsisifyString(path,
					CsStringUtil.JustifyHoriz.CENTER, 60);
			}

			return path;
		}

		// private string getPrimeFolderPath()
		// {
		// 	// M.WriteLine($"(sho) is editing| {isEditing}");
		// 	
		// 	if (!isEditing)
		// 	{
		// 		return CsStringUtil.EllipsisifyString(
		// 			UserSettings.Data.PrimaryScheduleFolderPath,
		// 			CsStringUtil.JustifyHoriz.CENTER, 70);
		// 	}
		//
		// 	return UserSettings.Data.PrimaryScheduleFolderPath;
		// }

		public void PbarPhaseReset()
		{
			PbarPhaseMin = 0;
			PbarPhaseMax = 100;
			PbarPhaseValue = 0;

			CurrentPhase = Status.GetPhaseDesc();
			OnPropertyChanged(nameof(CurrentPhase));
		}

		public void PbarStatReset()
		{
			PbarStatMin = 0;
			PbarStatMax = Enum.GetNames(typeof(Progress)).Length;
			PbarStatValue = 0;
		}

		private void getPrimeFilePath()
		{
			// string u = CsUtilities.UserName;
			// string m = CsUtilities.MachineName;
			// string c = CsUtilities.CompanyName;
			// string a = CsUtilities.AssemblyName;
			// string v = CsUtilities.AssemblyVersion;
			// string d = CsUtilities.AssemblyDirectory;
			//
			//
			// string x = UserSettings.Admin.Path.RootFolderPath;
			// string y = UserSettings.Admin.Path.SettingFilePath;

			string sFilePath =
				$"{UserSettings.Data.PrimaryScheduleFolderPath}\\{PrimeFileName}";

			primeFilePath =  new FilePath<FileNameSimple>(sFilePath);

			updatePrimeProperties();
		}

		private void updatePrimeProperties()
		{
			OnPropertyChanged(nameof(PrimeFolderPath));
			OnPropertyChanged(nameof(PrimeFileNameNoExt));
			OnPropertyChanged(nameof(PrimeFileFound));
		}

		private void setPrimFilePath(string path)
		{
			primeFilePath =  new FilePath<FileNameSimple>(path);

			UserSettings.Data.PrimaryScheduleFolderPath = primeFilePath.FolderPath;
			UserSettings.Data.PrimaryScheduleFileNameNoExt = primeFilePath.FileNameNoExt;

			updatePrimeProperties();
		}

		private void getDestFilePath()
		{
			string sFilePath =
				$"{UserSettings.Data.DestFolderPath}\\{DestFileName}";

			destFilePath =  new FilePath<FileNameSimple>(sFilePath);

			OnPropertyChanged(nameof(DestFolderPath));
			OnPropertyChanged(nameof(DestFileNameNoExt));
		}



		private void BtnExit_OnClick(object sender, RoutedEventArgs e)
		{
			quit();
			this.Close();
		}

		private void BtnClr_OnClick(object sender, RoutedEventArgs e)
		{
			Messages = "";
		}


		private void BtnGetPrimeFile_OnClick(object sender, RoutedEventArgs e)
		{

			//var d = new Microsoft.Win32.OpenFileDialog();
			//d.InitialDirectory = primeFilePath.FolderPath;
			//d.Multiselect = false;
			//d.Title = "Select Primary Schedule File";
			//// d.ShowHiddenItems = false;
			//d.Filter = $"Excel Schedule|*{PrimeFileExt}";

			//bool? result = d.ShowDialog() ;

			//if (result == true)
			//{
			//	setPrimFilePath( d.FileName);
			//}

			CommonOpenFileDialog d = new CommonOpenFileDialog();
			d.IsFolderPicker = false;
			d.InitialDirectory = primeFilePath.FolderPath;
			d.Multiselect = false;
			d.Title = "Select Primary Schedule File";
			d.ShowHiddenItems = false;
			d.Filters.Add(new CommonFileDialogFilter("Excel Schedule", $"{PrimeFileExt}"));

			if (d.ShowDialog(this) == CommonFileDialogResult.Ok)
			{
				setPrimFilePath( d.FileName);
			}

		}


		/*
		private void BtnGetPrimeFolder_OnClick(object sender, RoutedEventArgs e)
		{
			var d = new Microsoft.Win32.OpenFolderDialog();

			d.InitialDirectory = primeFilePath.FolderPath;
			d.Multiselect = false;
			d.Title = "Select Primary Schedule Folder";
			d.ShowHiddenItems = false;

			bool? result = d.ShowDialog() ;

			if (result == true)
			{
				W.PrimeFolder = d.FolderName;
			}

		}
		*/

		private void BtnGetDestFolder_OnClick(object sender, RoutedEventArgs e)
		{

			/*
			var d = new Microsoft.Win32.OpenFolderDialog();
			
			d.InitialDirectory = primeFilePath.FolderPath; // start at the same location
			d.Multiselect = false;
			d.Title = "Select Destination Folder";
			d.ShowHiddenItems = false;
			
			bool? result = d.ShowDialog() ;
			
			if (result == true)
			{
				W.DestFolder = d.FolderName;
			}
			*/

			CommonOpenFileDialog d = new CommonOpenFileDialog();
			d.IsFolderPicker = true;
			d.InitialDirectory = primeFilePath.FolderPath;
			d.Multiselect = false;
			d.Title = "Select Destination Folder";
			d.ShowHiddenItems = false;

			if (d.ShowDialog(this) == CommonFileDialogResult.Ok)
			{
				W.DestFolder = d.FileName;
			}

		}


		// complete process

		private void BtnProcessComplete_OnClick(object sender, RoutedEventArgs e)
		{
			/*
			 complete process
			phase 1: process primary schedule
			phase 2: process sheet schedule
			phase 3: validate sheet schedule
			phase 4: create pdf tree
			phase 5: merge pdf tiles
			phase 6: assign outlines
			*/

			bool result = true;

			Messages = String.Empty;

			Task[] tasks = new Task[1];


			tasks[0]= Task.Factory.StartNew(() => result = processComplete());

			Task.Factory.ContinueWhenAll(tasks, complete => showStatus(result));


			// showStatus(result);


			// if (!result)
			// {
			// 	M.WriteLine("\n*** FAILED ***");
			// 	M.WriteLine("Errors| ");
			//
			// 	if (Status.Errors.Count > 0)
			// 	{
			// 		M.Write(Status.GetErrors());
			// 	}
			//
			// 	return;
			// }
			//
			// M.WriteLine("\n*** PROCESS WORKED ***");
			// M.WriteLine($"Page Count| {schSupport.PageCount}");
		}


		private bool processComplete()
		{
			schSupport.Reset();

			Status.Reset();

			int idx = 1;
			int idxMax = (int) PbarStatMax - 1;
			bool result = true;

			while (result)
			{
				if (idx == 1) { result = ValidateDestFile(); }
				else if (idx == 2) { result = processPrimarySchedule(); }
				else if (idx == 3) { result = processSheetSchedule(); }
				else if (idx == 4) { result = validateSchedule(); }
				else if (idx == 5) { result = createPdfTree(); }
				else if (idx == 6) { result = mergePdfTree(); }
				else if (idx == 7) { result = createBookmarkTree(); }

				if (result) { showProgressStatus(); }

				if (++idx > idxMax) break;

				// if (idx > 2) break;
			}

			return result;
		}

		private void showProgressStatus()
		{
			M.WriteLine($"{Status.GetPhaseDesc(),-40} -> {Status.GetOaStatusDesc()}");
		}

		private void showStatus(bool result)
		{
			int idx = result ? 0 : 1;

			W.PbStatValue++;

			string[,] msgs = new string[,]
			{
				// [0,0] & [0,1] - good
				{ "\n*** PROCESS WORKED ***", 
					$"Page Count| {schSupport.PageCount}" },
				// [0,0] & [0,1] - fail
				{"\n*** FAILED ***", null }
			};
			
			// prefix msg - nothing when null
			M.WriteLine(msgs[idx,0]);

			if (Status.Errors.Count > 0)
			{
				M.WriteLine($"Phase    | {Status.GetPhaseDesc()}");
				M.WriteLine($"OA Status| {Status.GetOaStatusDesc()}");

				M.WriteLine($"Errors   | Exist");

				M.Write(Status.GetErrors());
			}

			// suffix message - nothing when null
			M.WriteLine(msgs[idx,1]);
		}

		// step 1
		private bool ValidateDestFile()
		{
			// bool overwriteOk = ;
			//
			// string destPath = DEST_PATH + "\\" + DEST_FILE;
			//
			// dest = new FilePath<FileNameSimple>(destPath);

			return schSupport.ValidateDestFile(destFilePath, UserSettings.Data.OverwriteDestination);
		}

		// step 2  [PP] - process primary schedule
		private bool processPrimarySchedule()
		{
			// FilePath<FileNameSimple> file = new FilePath<FileNameSimple>(DATA_PATH + "\\" + PRIME_SCHEDULE);

			if (!primeFilePath.Exists) return false;

			return schSupport.GetSchedule(primeFilePath);
		}

		// step 3 [ps] - process sheet schedule
		private bool processSheetSchedule()
		{
			return schSupport.GetSheetSchedule();
		}

		// step 4 [vs] - validate schedule
		private bool validateSchedule()
		{
			return schSupport.ValidateSheetList();
		}

		// step 5 [ct] - create pdf tree
		private bool createPdfTree()
		{
			return schSupport.CreatePdfTree();
		}

		// step 6 [mp] - merge the pdf files into a single pdf
		private bool mergePdfTree()
		{
			// string destPath = DEST_PATH + "\\" + DEST_FILE;
			//
			// FilePath<FileNameSimple> dest = new FilePath<FileNameSimple>(destPath);

			return schSupport.MergePdfTree();
		}

		// step 7 [cb] - create the outline tree / assign to the pages
		private bool createBookmarkTree()
		{
			return schSupport.CreatePdfOutlineTree();
		}

		
		// step 2
		private void BtnProcessPrimary_OnClick(object sender, RoutedEventArgs e)
		{
			Messages = String.Empty;

			schSupport.Reset();

			Status.Reset();

			FilePath<FileNameSimple> file = new FilePath<FileNameSimple>(DATA_PATH + "\\" + PRIME_SCHEDULE);

			bool result = schSupport.GetSchedule(file);

			M.WriteLine("col titles|\n");
			schSupport.showColTitles();

			M.WriteLine("\ncol data|\n");
			schSupport.showSchColData();

			showStatus(result);

			if (!result)
			{
				M.WriteLine("\n*** FAILED ***");
			}
			else
			{
				M.WriteLine("\n*** WORKED ***");
			}
		}
		// step 3
		private void BtnProcessSheetSchedule_OnClick(object sender, RoutedEventArgs e)
		{
			Messages = String.Empty;

			bool result = processSheetSchedule();

			showStatus(result);

			if (result)
			{
				M.WriteLine("\n*** WORKED ***");
			}
			else
			{
				M.WriteLine("\n*** FAILED ***");
			}
		}

		private void BtnMakeTree_OnClick(object sender, RoutedEventArgs e)
		{
			bool result = createPdfTree();

			if (result)
			{
				M.WriteLine("\n*** WORKED ***\n");
			}
			else
			{
				M.WriteLine("*** FAILED ***");
			}
		}

		private void BtnMakePdf_OnClick(object sender, RoutedEventArgs e)
		{
			bool result = mergePdfTree();

			if (result)
			{
				M.WriteLine("\n*** WORKED ***\n");
			}
			else
			{
				M.WriteLine("*** FAILED ***");
			}
		}


		// tests

		/*
		private void getFile()
		{
			
			var d = new Microsoft.Win32.OpenFileDialog();
			d.InitialDirectory = UserSettings.Data.PrimaryScheduleFileNameNoExt;
			d.Multiselect = false;
			d.FileName = "Combined";
			d.DefaultExt = ".pdf";
			d.Filter = "PDF Documents|*.pdf";

			bool? result = d.ShowDialog();

			if (result == true)
			{
				M.WriteLine($"file| {d.FileName}");
			}


			var x = new Microsoft.Win32.OpenFolderDialog();
			x.RootDirectory = UserSettings.Data.PrimaryScheduleFileNameNoExt;
			x.Multiselect = false;
			x.Title = "Select Initial Folder";
			x.ShowHiddenItems = false;
			
			result = x.ShowDialog();
			
			if (result == true)
			{
				M.WriteLine($"got folder| {x.FolderName}");
			}

		}
		*/

		private void BtnProcessXlsx_OnClick(object sender, RoutedEventArgs e)
		{
			Messages = String.Empty;

			bool result = false;
			string rootPath = DATA_PATH;
			string xPath = @"\Excel Files";
			string fPath = @"\Sheet Files";
			string sfile = "Sheet List-Architectural.xlsx";
			
			List<string> hdg = new() { "Architectural" };

			PdfSchData sch = new (1, hdg, xPath, sfile, fPath);
			sch.TypeName = "architectural";

			result = schSupport.GetSheetSchedule(sch, rootPath);

			if (result)
			{
				M.WriteLine("\n*** WORKED ***\n");
			}
			else
			{
				M.WriteLine("*** FAILED ***");
			}
		}

		private void BtnTestExists_OnClick(object sender, RoutedEventArgs e)
		{
			string filepath = DATA_PATH + @"\test1.txt";

			FilePath<FileNameSimple> file = new FilePath<FileNameSimple>(filepath);

			if (file.Exists)
			{
				M.WriteLine("overwrite existing file (Y/N)? Y");

				File.Delete(filepath);

				if (file.Exists)
				{
					int a = 1;
				}

			}



		}

		private void testAsync()
		{
			for (int i = 0; i < 10; i++)
			{
				M.WriteLine("Updating interface async");
				Thread.Sleep(1000);
			}
		}

		private void BtnTestAsyncA_OnClick(object sender, RoutedEventArgs e)
		{
			Task.Factory.StartNew(() => testAsync());

		}

		private void BtnTestAsyncB_OnClick(object sender, RoutedEventArgs e)
		{
			M.WriteLine("Updating interface not async");
		}

		/*
		private void BtnFileDialogs_OnClick(object sender, RoutedEventArgs e)
		{
			getFile();
		}
		*/

		private void Tbx_PreviewMouseUp(object sender, RoutedEventArgs e)
		{
			if (!isEditing) return;

			((TextBox) sender).TextAlignment = TextAlignment.Right;

			((TextBox) sender).Select(((TextBox) sender).Text.Length,0);

			e.Handled=true;
		}

		private void Tbx_LostFocus(object sender, RoutedEventArgs e)
		{
			isEditing = false;
		}

		private void Tbx_GotFocus(object sender, RoutedEventArgs e)
		{
			isEditing = true;
			// M.WriteLine("(got) is editing| true");

			// ((TextBox) sender).TextAlignment = TextAlignment.Right;

			OnPropertyChanged((string) ((TextBox) sender).Tag);

			e.Handled=true;

		}

		private void Tbx_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				((TextBox) sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			}
		}

		private void TblkOverwriteDest_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			isEditingOverwrite = true;
			OnPropertyChanged(nameof(OverwriteDest));
			OnPropertyChanged(nameof(OverwriteOptions));
		}


		private void TblkOverwriteNo_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			isEditingOverwrite= false;

			UserSettings.Data.OverwriteDestination = false;

			OnPropertyChanged(nameof(Overwrite));
			OnPropertyChanged(nameof(OverwriteDest));
			OnPropertyChanged(nameof(OverwriteOptions));
		}

		private void TblkOverwriteYes_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			isEditingOverwrite= false;

			UserSettings.Data.OverwriteDestination = true;

			OnPropertyChanged(nameof(Overwrite));
			OnPropertyChanged(nameof(OverwriteDest));
			OnPropertyChanged(nameof(OverwriteOptions));
		}
	}
}