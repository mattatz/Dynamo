﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Dynamo.Core;
using Dynamo.Graph.Connectors;
using Dynamo.Interfaces;
using Dynamo.Models;
using Dynamo.Utilities;
using DynamoUtilities;

namespace Dynamo.Configuration
{
    /// <summary>
    /// PreferenceSettings is a class for GUI to persist certain settings.
    /// Upon running of the GUI, those settings that are persistent will be loaded
    /// from a XML file from DYNAMO_SETTINGS_FILE.
    /// When GUI is closed, the settings are saved back into the XML file.
    /// </summary>
    public class PreferenceSettings : NotificationObject, IPreferences, IRenderPrecisionPreference, IDisablePackageLoadingPreferences
    {
        private string numberFormat;
        private string lastUpdateDownloadPath;
        private int maxNumRecentFiles;
        private bool isBackgroundGridVisible;
        private bool disableTrustWarnings = false;
        #region Constants
        /// <summary>
        /// Indicates the maximum number of files shown in Recent Files
        /// </summary>
        internal const int DefaultMaxNumRecentFiles = 10;

        /// <summary>
        /// Indicates the default render precision, i.e. the maximum number of tessellation divisions
        /// </summary>
        internal const int DefaultRenderPrecision = 128;

        /// <summary>
        /// Temp PreferenceSetting Location for testing
        /// </summary>
        public static string DynamoTestPath = null;

        /// <summary>
        /// Default date format
        /// </summary>
        public const string DefaultDateFormat = "MMMM dd, yyyy h:mm tt";

        /// <summary>
        /// Default time
        /// </summary>
        public static readonly System.DateTime DynamoDefaultTime = new System.DateTime(1977, 4, 12, 12, 12, 0, 0);

        #endregion

        /// The following settings are persistent between Dynamo sessions and are user-controllable
        #region Collect Information settings

        /// <summary>
        /// Indicates first run
        /// </summary>
        public bool IsFirstRun { get; set; }

        /// <summary>
        /// Indicates whether usage reporting is approved or not.
        /// </summary>
        [Obsolete("Property will be deprecated in Dynamo 3.0")]
        public bool IsUsageReportingApproved { get { return false; } set { } }

        /// <summary>
        /// Indicates whether Google analytics reporting is approved or not.
        /// </summary>
        public bool IsAnalyticsReportingApproved { get; set; }

        /// <summary>
        /// Indicates whether ADP analytics reporting is approved or not.
        /// </summary>
        [XmlIgnore]
        [Obsolete("Setter is obsolete - ADP consent should not be set directly, it should be set using the consent dialog.")]
        public bool IsADPAnalyticsReportingApproved
        {
            get { return Logging.AnalyticsService.IsADPOptedIn; }
            set { throw new Exception("do not use"); }
        }
        #endregion

        #region UI & Graphics settings

        /// <summary>
        /// The width of the library pane.
        /// </summary>
        public int LibraryWidth { get; set; }

        /// <summary>
        /// The height of the console display.
        /// </summary>
        public int ConsoleHeight { get; set; }

        /// <summary>
        /// Indicates if preview bubbles should be displayed on nodes.
        /// </summary>
        public bool ShowPreviewBubbles { get; set; }

        /// <summary>
        /// Indicates if code block node line numbers should  be displayed.
        /// </summary>
        public bool ShowCodeBlockLineNumber { get; set; }

        /// <summary>
        /// Should connectors be visible?
        /// </summary>
        public bool ShowConnector { get; set; }

        /// <summary>
        /// Should connector tooltip be visible?
        /// </summary>
        public bool ShowConnectorToolTip { get; set; }

        /// <summary>
        /// The types of connector: Bezier or Polyline.
        /// </summary>
        public ConnectorType ConnectorType { get; set; }

        /// <summary>
        /// Collection of pairs [BackgroundPreviewName;isActive]
        /// </summary>
        public List<BackgroundPreviewActiveState> BackgroundPreviews { get; set; }

        /// <summary>
        /// Returns active state of specified background preview 
        /// </summary>
        /// <param name="name">Background preview name</param>
        /// <returns>The active state</returns>
        public bool GetIsBackgroundPreviewActive(string name)
        {
            var pair = GetBackgroundPreviewData(name);

            return pair.IsActive;
        }

        /// <summary>
        /// Sets active state of specified background preview 
        /// </summary>
        /// <param name="name">Background preview name</param>
        /// <param name="value">Active state</param>
        public void SetIsBackgroundPreviewActive(string name, bool value)
        {
            var pair = GetBackgroundPreviewData(name);

            pair.IsActive = value;
        }

        private BackgroundPreviewActiveState GetBackgroundPreviewData(string name)
        {
            // find or create BackgroundPreviewActiveState instance in list by name
            var pair = BackgroundPreviews.FirstOrDefault(p => p.Name == name)
                ?? new BackgroundPreviewActiveState { Name = name };
            if (!BackgroundPreviews.Contains(pair))
            {
                BackgroundPreviews.Add(pair);
            }

            return pair;
        }

        /// <summary>
        /// Should the background grid be shown?
        /// </summary>
        public bool IsBackgroundGridVisible
        {
            get
            {
                return isBackgroundGridVisible;
            }
            set
            {
                if (value == isBackgroundGridVisible) return;
                isBackgroundGridVisible = value;

                RaisePropertyChanged(nameof(IsBackgroundGridVisible));
            }
        }


        /// <summary>
        /// Indicates whether background preview is active or not.
        /// </summary>
        [Obsolete("Property will be deprecated in Dynamo 3.0, please use BackgroundPreviews")]
        public bool IsBackgroundPreviewActive
        {
            get
            {
                return GetIsBackgroundPreviewActive("IsBackgroundPreviewActive");
            }
            set
            {
                SetIsBackgroundPreviewActive("IsBackgroundPreviewActive", value);
            }
        }

        /// <summary>
        /// Indicate which render precision will be used
        /// </summary>
        public int RenderPrecision { get; set; }

        /// Indicates whether surface and solid edges will
        /// be rendered.
        /// </summary>
        public bool ShowEdges { get; set; }

        /// <summary>
        /// Indicates whether show detailed or compact layout during search.
        /// </summary>
        public bool ShowDetailedLayout { get; set; }


        /// <summary>
        /// The last X coordinate of the Dynamo window.
        /// </summary>
        public double WindowX { get; set; }

        /// <summary>
        /// The last Y coordinate of the Dynamo window.
        /// </summary>
        public double WindowY { get; set; }

        /// <summary>
        /// The last width of the Dynamo window.
        /// </summary>
        public double WindowW { get; set; }

        /// <summary>
        /// The last height of the Dynamo window.
        /// </summary>
        public double WindowH { get; set; }

        /// <summary>
        /// Should Dynamo use hardware acceleration if it is supported?
        /// </summary>
        public bool UseHardwareAcceleration { get; set; }

        #endregion

        #region Dynamo application settings

        /// <summary>
        /// The decimal precision used to display numbers.
        /// </summary>
        public string NumberFormat
        {
            get { return numberFormat; }
            set
            {
                numberFormat = value;
                RaisePropertyChanged("NumberFormat");
            }
        }

        /// <summary>
        /// The maximum number of recent file paths to be saved.
        /// </summary>
        public int MaxNumRecentFiles
        {
            get { return maxNumRecentFiles; }
            set
            {
                if (value > 0)
                {
                    maxNumRecentFiles = value;
                }
                else
                {
                    maxNumRecentFiles = DefaultMaxNumRecentFiles;
                }
                RaisePropertyChanged("MaxNumRecentFiles");
            }
        }

        /// <summary>
        /// A list of recently opened file paths.
        /// </summary>
        public List<string> RecentFiles { get; set; }

        /// <summary>
        /// A list of backup file paths.
        /// </summary>
        public List<string> BackupFiles { get; set; }

        /// <summary>
        /// A list of folders packages, custom nodes or direct paths to .dll and .ds files.
        /// </summary>
        public List<string> CustomPackageFolders { get; set; }

        /// <summary>
        /// If true, trust warnings for opening .dyn files from untrusted locations will not be shown.
        /// Do not use this property setter, it does nothing. Exists only to support serialization.
        /// </summary>
        public bool DisableTrustWarnings
        {
            get => disableTrustWarnings;
            //no-op
            set { }
        }

        /// <summary>
        /// Backing store for TrustedLocations
        /// </summary>
        private List<string> trustedLocations { get; set; } = new List<string>();

        /// <summary>
        /// Set trusted locations in the PreferenceSettings configuration.
        /// </summary>
        /// <param name="locs"></param>
        internal void SetTrustedLocations(IEnumerable<string> locs)
        {
            trustedLocations.Clear();
            foreach (var loc in locs) trustedLocations.Add(loc);
        }
        
        internal void SetTrustWarningsDisabled(bool disabled)
        {
            disableTrustWarnings = disabled;
        }

        // This function is used to deserialize the trusted locations manually
        // so that the TrustedLocation propertie's setter does not need to be public.
        private List<string> DeserializeTrustedLocations(XmlNode preferenceSettingsElement)
        {
            List<string> output = new List<string>();
            try
            {
                var parentNode = preferenceSettingsElement.SelectSingleNode($@"//{nameof(TrustedLocations)}");
                foreach (XmlNode value in parentNode.ChildNodes)
                {
                    output.Add(value.InnerText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return output;
        }

        private bool DeserializeDisableTrustWarnings(XmlNode preferenceSettingsElement)
        {
            try
            {
                return bool.Parse(preferenceSettingsElement.SelectSingleNode($@"//{nameof(DisableTrustWarnings)}").InnerText);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Manually deserialize some preferences from the PreferencesSettings file.
        /// This is done so that we can avoid exposing these property setters to the public API.
        /// </summary>
        /// <param name="prefsFilePath"></param>
        private void DeserializeInternalPrefs(string prefsFilePath)
        {
            try
            {
                //manually load some xml we don't want to create public setters for.
                var doc = new System.Xml.XmlDocument();
                doc.Load(prefsFilePath);
                var prefs = doc.SelectSingleNode($@"//{nameof(PreferenceSettings)}");

                var trustedLocations = DeserializeTrustedLocations(prefs);
                SetTrustedLocations(trustedLocations.Distinct());
                var trustWarningsDisabled = DeserializeDisableTrustWarnings(prefs);
                SetTrustWarningsDisabled(trustWarningsDisabled);
            }
            catch
            { }
        }

        /// <summary>
        /// A copy of the list of trusted locations (folders) as recorded in the preferences file.
        /// Do not directly use this property when trying to check for trusted locations.
        /// Instead use Dynamo.Core.TrustedLocationsManager to manage for trusted locations.
        /// </summary>
        public List<string> TrustedLocations
        {
            get => trustedLocations.ToList(); //Copy of the internal list
        }

        /// <summary>
        /// A list of packages used by the Package Manager to determine
        /// which packages are marked for deletion.
        /// </summary>
        public List<string> PackageDirectoriesToUninstall { get; set; }

        /// <summary>
        /// Path to the Python (.py) file to use as a starting template when creating a new PythonScript Node.
        /// </summary>
        public string PythonTemplateFilePath
        {
            get { return pythonTemplateFilePath; }
            set { pythonTemplateFilePath = value; }
        }

        /// <summary>
        /// The backing store for the Python template file path. Required as static property cannot implement an interface member.
        /// </summary>
        private static string pythonTemplateFilePath = "";

        /// <summary>
        /// This defines how long (in milliseconds) will the graph be automatically saved.
        /// </summary>
        public int BackupInterval { get; set; }

        /// <summary>
        /// This defines how many files will be backed up.
        /// </summary>
        public int BackupFilesCount { get; set; }

        /// <summary>
        /// Indicates if the user has accepted the terms of 
        /// use for downloading packages from package manager.
        /// </summary>
        public bool PackageDownloadTouAccepted { get; set; }

        /// <summary>
        /// Indicates the default state of the "Open in Manual Mode"
        /// checkbox in OpenFileDialog
        /// </summary>
        public bool OpenFileInManualExecutionMode { get; set; }

        /// <summary>
        /// This defines if user wants to see the Iron Python Extension Dialog box on every new session.
        /// </summary>
        public bool IsIronPythonDialogDisabled { get; set; }

        /// <summary>
        /// This defines if user wants to see the whitespaces and tabs in python script editor.
        /// </summary>
        public bool ShowTabsAndSpacesInScriptEditor { get; set; }

        /// <summary>
        /// This defines if user wants to see the enabled node Auto Complete feature for port interaction.
        /// </summary>
        public bool EnableNodeAutoComplete { get; set; }

        /// <summary>
        /// Engine used by default for new Python script and string nodes. If not empty, this takes precedence over any system settings.
        /// </summary>
        public string DefaultPythonEngine
        {
            get
            {
                return defaultPythonEngine;
            }
            set
            {
                defaultPythonEngine = value;
            }
        }

        /// <summary>
        /// Static field backing the DefaultPythonEngine setting property.
        /// </summary>
        private static string defaultPythonEngine;

        internal event Func<string> RequestUserDataFolder;
        internal string OnRequestUserDataFolder()
        {
            return RequestUserDataFolder?.Invoke();
        }

        private string selectedPackagePathForInstall;
        // TODO: Add this to IPreferences in Dynamo 3.0
        /// <summary>
        /// Currently selected package path where all packages downloaded from the Package Manager
        /// will be installed. The default package path for install is the user data directory
        /// currently used by the Dynamo environment.
        /// </summary>
        public string SelectedPackagePathForInstall
        {
            get
            {
                if (string.IsNullOrEmpty(selectedPackagePathForInstall))
                {
                    selectedPackagePathForInstall = OnRequestUserDataFolder();
                }
                return selectedPackagePathForInstall;
            }
            set
            {
                selectedPackagePathForInstall = value;
            }
        }

        /// <summary>
        /// Indicates (if any) which namespaces should not be displayed in the Dynamo node library.
        /// String format: "[library name]:[fully qualified namespace]"
        /// </summary>
        public List<string> NamespacesToExcludeFromLibrary { get; set; }

        /// <summary>
        /// True if the NamespacesToExcludeFromLibrary element is found in DynamoSettings.xml.
        /// </summary>
        [XmlIgnore]
        public bool NamespacesToExcludeFromLibrarySpecified { get; set; }

        /// <summary>
        /// Settings that apply to view extensions.
        /// </summary>
        public List<ViewExtensionSettings> ViewExtensionSettings { get; set; }

        private bool disableBuiltinPackages;
        /// <summary>
        /// If enabled Dynamo Built-In Packages will not be loaded.
        /// </summary>
        public bool DisableBuiltinPackages
        {
            get { return disableBuiltinPackages; }
            set
            {
                disableBuiltinPackages = value;
                RaisePropertyChanged(nameof(DisableBuiltinPackages));
            }
        }

        private bool disableCustomPackageLocations;
        /// <summary>
        /// If enabled user's custom package locations will not be loaded.
        /// </summary>
        public bool DisableCustomPackageLocations
        {
            get { return disableCustomPackageLocations; }
            set
            {
                disableCustomPackageLocations = value;
                RaisePropertyChanged(nameof(DisableCustomPackageLocations));
            }
        }
        /// <summary>
        /// Defines the default run type when opening a workspace
        /// </summary>
        public RunType DefaultRunType { get; set; }

        /// <summary>
        /// Show Run Preview flag.
        /// </summary>
        public bool ShowRunPreview { get; set; }


        /// <summary>
        /// Stores the group styles added in the preference settings
        /// </summary>
        public List<GroupStyleItem> GroupStyleItemsList { get; set; }

        /// <summary>
        /// Limits the size of the tags used by the SearchDictionary
        /// This static property is not serialized and is assigned NodeSearchTagSizeLimit's value 
        /// if found at deserialize time.
        /// </summary>
        internal static int NodeSearchTagSizeLimitValue = 300;

        /// <summary>
        /// Limits the size of the tags used by the SearchDictionary
        /// </summary>
        public int NodeSearchTagSizeLimit
        {
            get { return NodeSearchTagSizeLimitValue; }
            set { NodeSearchTagSizeLimitValue = value; }
        }

        /// <summary>
        /// The Version of the IronPython package that Dynamo will download when it is found as missing in graphs.
        /// This static property is not serialized and is assigned IronPythonResolveTargetVersion's value 
        /// if found at deserialize time.
        /// </summary>
        internal static Version IronPythonResolveVersion = new Version(2, 4, 0);

        /// <summary>
        /// The Version of the IronPython package that Dynamo will download when it is found as missing in graphs.
        /// </summary>
        public string IronPythonResolveTargetVersion
        {
            get { return IronPythonResolveVersion.ToString(); }
            set { IronPythonResolveVersion = Version.TryParse(value, out Version newVal) ? newVal : IronPythonResolveVersion; }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PreferenceSettings"/> class.
        /// </summary>
        public PreferenceSettings()
        {
            RecentFiles = new List<string>();
            WindowH = 768;
            WindowW = 1024;
            WindowY = 0.0;
            WindowX = 0.0;
            BackgroundPreviews = new List<BackgroundPreviewActiveState>();

            // Default Settings
            IsFirstRun = true;
            IsAnalyticsReportingApproved = true;
            LibraryWidth = 304;
            ConsoleHeight = 0;
            ShowPreviewBubbles = true;
            ShowCodeBlockLineNumber = true;
            ShowConnector = true;
            ShowConnectorToolTip = true;
            ConnectorType = ConnectorType.BEZIER;
            IsBackgroundGridVisible = true;
            PackageDirectoriesToUninstall = new List<string>();
            NumberFormat = "f3";
            UseHardwareAcceleration = true;
            PackageDownloadTouAccepted = false;
            maxNumRecentFiles = DefaultMaxNumRecentFiles;
            RenderPrecision = DefaultRenderPrecision;
            ShowEdges = false;
            OpenFileInManualExecutionMode = false;
            ShowDetailedLayout = true;
            NamespacesToExcludeFromLibrary = new List<string>();
            DefaultRunType = RunType.Automatic;

            BackupInterval = 60000; // 1 minute
            BackupFilesCount = 1;
            BackupFiles = new List<string>();

            CustomPackageFolders = new List<string>();

            PythonTemplateFilePath = "";
            IsIronPythonDialogDisabled = false;
            ShowTabsAndSpacesInScriptEditor = false;
            EnableNodeAutoComplete = true;
            DefaultPythonEngine = string.Empty;
            ViewExtensionSettings = new List<ViewExtensionSettings>();
            GroupStyleItemsList = new List<GroupStyleItem>();
        }

        /// <summary>
        /// Saves PreferenceSettings to XML, given a file path.
        /// </summary>
        /// <param name="filePath">Path of the XML File to save to.</param>
        /// <returns>True if file is saved successfully, false if an error occurred.</returns>
        public bool Save(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PreferenceSettings));
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(fs, this);
                    fs.Close(); // Release file lock
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return false;
        }

        /// <summary>
        /// Saves PreferenceSettings in a default directory when no path is 
        /// specified.
        /// </summary>
        /// <param name="preferenceFilePath">The file path to save preference
        /// settings to. If this parameter is null or empty string, preference 
        /// settings will be saved to the default path.</param>
        /// <returns>True if file is saved successfully, false if an error occurred.</returns>
        public bool SaveInternal(string preferenceFilePath)
        {
            if (!String.IsNullOrEmpty(DynamoTestPath))
            {
                preferenceFilePath = DynamoTestPath;
            }

            return Save(preferenceFilePath);
        }

        /// <summary>
        /// Loads PreferenceSettings from specified XML file if possible,
        /// else initializes PreferenceSettings with default values.
        /// </summary>
        /// <param name="filePath">Path of the XML File</param>
        /// <returns>
        /// Stored PreferenceSettings from xml file or
        /// Default PreferenceSettings if xml file is not found.
        /// </returns>
        public static PreferenceSettings Load(string filePath)
        {
            // Constructor will be called anyway in either condition below so no need to initialize now.
            PreferenceSettings settings = null;

            if (String.IsNullOrEmpty(filePath) || (!File.Exists(filePath)))
                return new PreferenceSettings();

            try
            {
                var serializer = new XmlSerializer(typeof(PreferenceSettings));
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    settings = serializer.Deserialize(fs) as PreferenceSettings;
                    fs.Close(); // Release file lock
                }
            }
            catch
            {
                if (settings == null)
                {
                    return new PreferenceSettings();
                }
            }

            settings.CustomPackageFolders = settings.CustomPackageFolders.Distinct().ToList();
            settings.GroupStyleItemsList = settings.GroupStyleItemsList.GroupBy(entry => entry.Name).Select(result => result.First()).ToList();
            MigrateStdLibTokenToBuiltInToken(settings);

            settings.DeserializeInternalPrefs(filePath);

            return settings;
        }

        /// <summary>
        /// Returns the static Python template file path.
        /// When the file exists and is not empty, its contents are used to populate new Python Script nodes added to the Dynamo workspace.
        /// </summary>
        /// <returns></returns>
        public static string GetPythonTemplateFilePath()
        {
            return pythonTemplateFilePath;
        }

        /// <summary>
        /// Provides access to the DefaultPythonEngine setting in a static context. Used from PythonNodeBase.
        /// </summary>
        /// <returns>DefaultPythonEngine setting value</returns>
        internal static string GetDefaultPythonEngine()
        {
            return defaultPythonEngine;
        }

        internal void InitializeNamespacesToExcludeFromLibrary()
        {
            if (!NamespacesToExcludeFromLibrarySpecified)
            {
                NamespacesToExcludeFromLibrary.Add(
                    "ProtoGeometry.dll:Autodesk.DesignScript.Geometry.TSpline"
                );
                NamespacesToExcludeFromLibrarySpecified = true;
            }
        }

        //migrate old path token to new path token
        private static void MigrateStdLibTokenToBuiltInToken(PreferenceSettings settings)
        {
            for (var i = 0; i < settings.CustomPackageFolders.Count; i++)
            {
                var path = settings.CustomPackageFolders[i];
                if (path == DynamoModel.StandardLibraryToken)
                {
                    settings.CustomPackageFolders[i] = DynamoModel.BuiltInPackagesToken;
                }
            }
        }
    }
}