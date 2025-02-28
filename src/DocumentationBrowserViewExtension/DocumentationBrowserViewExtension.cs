﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Dynamo.DocumentationBrowser.Properties;
using Dynamo.Logging;
using Dynamo.PackageManager;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace Dynamo.DocumentationBrowser
{
    /// <summary>
    /// The DocumentationBrowser view extension displays web or local html files on the Dynamo right panel.
    /// It reacts to documentation display request events in Dynamo to know what and when to display documentation.
    /// </summary>
    public class DocumentationBrowserViewExtension : ViewExtensionBase, IViewExtension, ILogSource
    {
        private ViewLoadedParams viewLoadedParamsReference;
        private MenuItem documentationBrowserMenuItem;
        private PackageManagerExtension pmExtension;

        internal DocumentationBrowserView BrowserView { get; private set; }
        internal DocumentationBrowserViewModel ViewModel { get; private set; }

        /// <summary>
        /// Extension Name
        /// </summary>
        public override string Name => Properties.Resources.ExtensionName;

        /// <summary>
        /// GUID of the extension
        /// </summary>
        public override string UniqueId => "68B45FC0-0BD1-435C-BF28-B97CB03C71C8";

        public DocumentationBrowserViewExtension()
        {
            // initialise the ViewModel and View for the window
            this.ViewModel = new DocumentationBrowserViewModel();
            this.BrowserView = new DocumentationBrowserView(this.ViewModel);
        }

        #region ILogSource

        public event Action<ILogMessage> MessageLogged;

        internal void OnMessageLogged(ILogMessage msg)
        {
            MessageLogged?.Invoke(msg);
        }
        #endregion

        #region IViewExtension lifecycle

        public override void Startup(ViewStartupParams viewStartupParams)
        {
            pmExtension = viewStartupParams.ExtensionManager.Extensions.OfType<PackageManagerExtension>().FirstOrDefault();
            PackageDocumentationManager.Instance.AddDynamoPaths(viewStartupParams.PathManager);
        }

        public override void Loaded(ViewLoadedParams viewLoadedParams)
        {
            if (viewLoadedParams == null) throw new ArgumentNullException(nameof(viewLoadedParams));

            this.ViewModel.MessageLogged += OnViewModelMessageLogged;
            PackageDocumentationManager.Instance.MessageLogged += OnMessageLogged;

            this.viewLoadedParamsReference = viewLoadedParams; 

            // Add a button to Dynamo View menu to manually show the window
            this.documentationBrowserMenuItem = new MenuItem { Header = Resources.MenuItemText, IsCheckable = true };
            this.documentationBrowserMenuItem.Checked += MenuItemCheckHandler;
            this.documentationBrowserMenuItem.Unchecked += MenuItemUnCheckedHandler;
            this.viewLoadedParamsReference.AddExtensionMenuItem(this.documentationBrowserMenuItem);

            // subscribe to the documentation open request event from Dynamo
            this.viewLoadedParamsReference.RequestOpenDocumentationLink += HandleRequestOpenDocumentationLink;

            // subscribe to property changes of DynamoViewModel so we can show/hide the browser on StartPage display
            (viewLoadedParams.DynamoWindow.DataContext as DynamoViewModel).PropertyChanged += HandleStartPageVisibilityChange;

            // pmExtension could be null, if this is the case we bail before interacting with it.
            if (pmExtension is null)
                return;

            // subscribe to package loaded so we can add the package documentation 
            // to the Package documentation manager when a package is loaded
            pmExtension.PackageLoader.PackgeLoaded += OnPackageLoaded;

            // add packages already loaded to the PackageDocumentationManager
            foreach (var pkg in pmExtension.PackageLoader.LocalPackages)
            {
                OnPackageLoaded(pkg);
            }
        }

        private void OnPackageLoaded(Package pkg)
        {
            // Add documentation files from the package to the DocManager
            PackageDocumentationManager.Instance.AddPackageDocumentation(pkg.NodeDocumentaionDirectory, pkg.Name);
        }

        private void MenuItemUnCheckedHandler(object sender, RoutedEventArgs e)
        {
            viewLoadedParamsReference.CloseExtensioninInSideBar(this);
        }

        private void MenuItemCheckHandler(object sender, RoutedEventArgs e)
        {
            AddToSidebar(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
            if (this.viewLoadedParamsReference != null)
            {
                this.viewLoadedParamsReference.RequestOpenDocumentationLink -= HandleRequestOpenDocumentationLink;
            }

            if (this.ViewModel != null)
            {
                this.ViewModel.MessageLogged -= OnViewModelMessageLogged;
            }

            if (this.documentationBrowserMenuItem != null)
            {
                this.documentationBrowserMenuItem.Checked -= MenuItemCheckHandler;
                this.documentationBrowserMenuItem.Unchecked -= MenuItemUnCheckedHandler;
            }

            this.BrowserView?.Dispose();
            this.ViewModel?.Dispose();

            if (this.viewLoadedParamsReference != null)
            {
                (this.viewLoadedParamsReference.DynamoWindow.DataContext as DynamoViewModel).PropertyChanged -=
                    HandleStartPageVisibilityChange;
            }

            if (this.pmExtension != null)
            {
                this.pmExtension.PackageLoader.PackgeLoaded -= OnPackageLoaded;
            }
            PackageDocumentationManager.Instance.Dispose();
        }

        /// <summary>
        /// Dispose function after extension is closed
        /// </summary>
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// This method handles the documentation open requests coming from Dynamo.
        /// The incoming request is routed to the ViewModel for processing.
        /// </summary>
        /// <param name="args">The incoming event data.</param>
        public void HandleRequestOpenDocumentationLink(OpenDocumentationLinkEventArgs args)
        {
            if (args == null) return;

            // ignore events targeting remote resources so the sidebar is not displayed
            if (args.IsRemoteResource)
                return;

            // make sure the view is added to the Sidebar
            // this also forces the Sidebar to open
            AddToSidebar(false);

            // forward the event to the ViewModel to handle
            this.ViewModel?.HandleOpenDocumentationLinkEvent(args);

            // Check the menu item
            this.documentationBrowserMenuItem.IsChecked = true;
        }

        private void OnViewModelMessageLogged(ILogMessage msg)
        {
            OnMessageLogged(msg);
        }

        private void AddToSidebar(bool displayDefaultContent)
        {
            // verify the browser window has been initialized
            if (this.BrowserView == null)
            {
                OnMessageLogged(LogMessage.Error(Resources.BrowserViewCannotBeAddedToSidebar));
                return;
            }

            // make sure the documentation window is not empty before displaying it
            // we have to do this here because we cannot detect when the sidebar is displayed
            if (displayDefaultContent)
            {
                this.ViewModel?.EnsurePageHasContent();
            }

            this.viewLoadedParamsReference?.AddToExtensionsSideBar(this, this.BrowserView);
        }

        // hide browser directly when startpage is shown to deal with air space problem.
        // https://github.com/dotnet/wpf/issues/152
        private void HandleStartPageVisibilityChange(object sender, PropertyChangedEventArgs e)
        {
            DynamoViewModel dynamoViewModel = sender as DynamoViewModel;

            if (dynamoViewModel != null && e.PropertyName == nameof(DynamoViewModel.ShowStartPage))
            {
                ViewModel.ShowBrowser = !dynamoViewModel.ShowStartPage;
            }
        }

        public override void Closed()
        {
            if (this.documentationBrowserMenuItem != null)
            {
                this.documentationBrowserMenuItem.IsChecked = false;
            }
        }
    }
}
