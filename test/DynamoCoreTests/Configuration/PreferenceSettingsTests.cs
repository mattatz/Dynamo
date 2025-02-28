﻿using System.IO;
using Dynamo.Configuration;
using Dynamo.Models;
using NUnit.Framework;

namespace Dynamo.Tests.Configuration
{
    [TestFixture]
    class PreferenceSettingsTests : UnitTestBase
    {
        [Test]
        [Category("UnitTests")]
        public void TestLoad()
        {
            string settingDirectory = Path.Combine(TestDirectory, "settings");
            string settingsFilePath = Path.Combine(settingDirectory, "DynamoSettings-PythonTemplate-initial.xml");
            string initialPyFilePath = Path.Combine(settingDirectory, @"PythonTemplate-initial.py");

            // Assert files required for test exist
            Assert.IsTrue(File.Exists(settingsFilePath));

            var settings = PreferenceSettings.Load(settingsFilePath);

            Assert.IsNotNull(settings);
        }

        [Test]
        [Category("UnitTests")]
        public void TestGetPythonTemplateFilePath()
        {
            string settingDirectory = Path.Combine(TestDirectory, "settings");
            string settingsFilePath = Path.Combine(settingDirectory, "DynamoSettings-PythonTemplate-initial.xml");
            string initialPyFilePath = Path.Combine(settingDirectory, @"PythonTemplate-initial.py");

            // Assert files required for test exist
            Assert.IsTrue(File.Exists(settingsFilePath));
            Assert.IsTrue(File.Exists(initialPyFilePath));

            PreferenceSettings.Load(settingsFilePath);

            string pythonTemplateFilePath = Path.Combine(settingDirectory, PreferenceSettings.GetPythonTemplateFilePath());

            Assert.AreEqual(pythonTemplateFilePath, initialPyFilePath);
        }

        [Test]
        [Category("UnitTests")]
        public void TestSettingsSerialization()
        {
            string tempPath = System.IO.Path.GetTempPath();
            tempPath = Path.Combine(tempPath, "userPreference.xml");

            PreferenceSettings settings = new PreferenceSettings();

            // Assert defaults
            Assert.AreEqual(settings.GetIsBackgroundPreviewActive("MyBackgroundPreview"), true);
            Assert.AreEqual(settings.ShowCodeBlockLineNumber, true);
            Assert.AreEqual(settings.IsIronPythonDialogDisabled, false);
            Assert.AreEqual(settings.ShowTabsAndSpacesInScriptEditor, false);
            Assert.AreEqual(settings.EnableNodeAutoComplete, true);
            Assert.AreEqual(settings.DefaultPythonEngine, string.Empty);
            Assert.AreEqual(settings.MaxNumRecentFiles, PreferenceSettings.DefaultMaxNumRecentFiles);
            Assert.AreEqual(settings.ViewExtensionSettings.Count, 0);
            Assert.AreEqual(settings.DefaultRunType, RunType.Automatic);

            // Save
            settings.Save(tempPath);
            settings = PreferenceSettings.Load(tempPath);

            // Assert deserialized values are same when saved with defaults
            Assert.AreEqual(settings.GetIsBackgroundPreviewActive("MyBackgroundPreview"), true);
            Assert.AreEqual(settings.ShowCodeBlockLineNumber, true);
            Assert.AreEqual(settings.IsIronPythonDialogDisabled, false);
            Assert.AreEqual(settings.ShowTabsAndSpacesInScriptEditor, false);
            Assert.AreEqual(settings.EnableNodeAutoComplete, true);
            Assert.AreEqual(settings.DefaultPythonEngine, string.Empty);
            Assert.AreEqual(settings.MaxNumRecentFiles, PreferenceSettings.DefaultMaxNumRecentFiles);
            Assert.AreEqual(settings.ViewExtensionSettings.Count, 0);
            Assert.AreEqual(settings.DefaultRunType, RunType.Automatic);

            // Change setting values
            settings.SetIsBackgroundPreviewActive("MyBackgroundPreview", false);
            settings.ShowCodeBlockLineNumber = false;
            settings.IsIronPythonDialogDisabled = true;
            settings.ShowTabsAndSpacesInScriptEditor = true;
            settings.DefaultPythonEngine = "CP3";
            settings.MaxNumRecentFiles = 24;
            settings.EnableNodeAutoComplete = false;
            settings.DefaultRunType = RunType.Manual;
            settings.ViewExtensionSettings.Add(new ViewExtensionSettings()
            {
                Name = "MyExtension",
                UniqueId = "1234",
                DisplayMode = ViewExtensionDisplayMode.FloatingWindow,
                WindowSettings = new WindowSettings()
                {
                    Left = 123,
                    Top = 456,
                    Height = 321,
                    Width = 654,
                    Status = WindowStatus.Maximized
                }
            });
            settings.GroupStyleItemsList.Add(new GroupStyleItem 
            {
                Name = "TestGroup", 
                HexColorString = "000000" 
            });

            // Save
            settings.Save(tempPath);
            settings = PreferenceSettings.Load(tempPath);

            // Assert deserialized values are same as last changed
            Assert.AreEqual(settings.GetIsBackgroundPreviewActive("MyBackgroundPreview"), false);
            Assert.AreEqual(settings.ShowCodeBlockLineNumber, false);
            Assert.AreEqual(settings.IsIronPythonDialogDisabled, true);
            Assert.AreEqual(settings.ShowTabsAndSpacesInScriptEditor, true);
            Assert.AreEqual(settings.DefaultPythonEngine, "CP3");
            Assert.AreEqual(settings.MaxNumRecentFiles, 24);
            Assert.AreEqual(settings.EnableNodeAutoComplete, false);
            Assert.AreEqual(settings.ViewExtensionSettings.Count, 1);
            var extensionSettings = settings.ViewExtensionSettings[0];
            Assert.AreEqual(settings.DefaultRunType, RunType.Manual);
            Assert.AreEqual(extensionSettings.Name, "MyExtension");
            Assert.AreEqual(extensionSettings.UniqueId, "1234");
            Assert.AreEqual(extensionSettings.DisplayMode, ViewExtensionDisplayMode.FloatingWindow);
            Assert.IsNotNull(extensionSettings.WindowSettings);
            var windowSettings = extensionSettings.WindowSettings;
            Assert.AreEqual(windowSettings.Left, 123);
            Assert.AreEqual(windowSettings.Top, 456);
            Assert.AreEqual(windowSettings.Height, 321);
            Assert.AreEqual(windowSettings.Width, 654);
            Assert.AreEqual(windowSettings.Status, WindowStatus.Maximized);
            // Load function will only deserialize the customized style
            Assert.AreEqual(settings.GroupStyleItemsList.Count, 1);
            var styleItemsList = settings.GroupStyleItemsList[0];
            Assert.AreEqual(styleItemsList.Name, "TestGroup");
            Assert.AreEqual(styleItemsList.HexColorString, "000000");
        }

        [Test]
        [Category("UnitTests")]
        public void TestMigrateStdLibTokenToBuiltInToken()
        {
            string settingDirectory = Path.Combine(TestDirectory, "settings");
            string settingsFilePath = Path.Combine(settingDirectory, "DynamoSettings-stdlibtoken.xml");
            Assert.IsTrue(File.ReadAllText(settingsFilePath).Contains(DynamoModel.StandardLibraryToken));
            // Assert files required for test exist
            Assert.IsTrue(File.Exists(settingsFilePath));
            var settings = PreferenceSettings.Load(settingsFilePath);

            var token = settings.CustomPackageFolders[1];

            Assert.AreEqual(DynamoModel.BuiltInPackagesToken,token);
        }
        [Test]
        [Category("UnitTests")]
        public void TestSerializationDisableTrustWarnings()
        {
            //create new prefs
            var prefs = new PreferenceSettings();
            //assert default.
            Assert.IsFalse(prefs.DisableTrustWarnings);
            prefs.SetTrustWarningsDisabled(true);
            Assert.True(prefs.DisableTrustWarnings);
            //save
            var tempPath = GetNewFileNameOnTempPath(".xml");
            prefs.Save(tempPath);

            //load
            var settingsLoaded = PreferenceSettings.Load(tempPath);
            Assert.IsTrue(settingsLoaded.DisableTrustWarnings);

        }
    }
}
