﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Dynamo.ViewModels;

namespace Dynamo.Controls
{
    //http://blogs.msdn.com/b/chkoenig/archive/2008/05/24/hierarchical-databinding-in-wpf.aspx

    /// <summary>
    /// Interaction logic for WatchTree.xaml
    /// </summary>
    public partial class WatchTree : UserControl
    {
        private WatchViewModel _vm;
        private WatchViewModel prevWatchViewModel;
        private readonly int defaultWidthSize = 200;
        private readonly int defaultHeightSize = 200;
        private readonly int minWidthSize = 100;
        private readonly int minHeightSize = 38;

        public WatchTree()
        {
            InitializeComponent();
            this.Loaded += WatchTree_Loaded;
            this.Unloaded += WatchTree_Unloaded;
        }

        private void WatchTree_Unloaded(object sender, RoutedEventArgs e)
        {
            _vm.PropertyChanged -= _vm_PropertyChanged;
        }

        void WatchTree_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = this.DataContext as WatchViewModel;
            _vm.PropertyChanged += _vm_PropertyChanged;            
        }

        private void _vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsCollection")
            {
                if (_vm.IsCollection)
                {
                    this.Height = defaultHeightSize;
                }
                else
                {
                    this.Height = minHeightSize;
                }
                // When it doesn't have any element, it should be set back the width to the default.
                if (_vm.Children != null && _vm.Children.Count == 0)
                {
                    this.Width = defaultWidthSize;
                }
            }
            else if (e.PropertyName == "Children")
            {
                if (_vm.Children != null)
                {
                    if (!_vm.Children[0].IsCollection)
                    {
                        // We will use 7.5 as width factor for each character.

                        double requiredWidth = (_vm.Children[0].NodeLabel.Length * 7.5);
                        if (requiredWidth > (defaultWidthSize * 2))
                        {
                            requiredWidth = defaultWidthSize * 2;
                        }
                        requiredWidth += 20;
                        this.Width = requiredWidth;
                    }
                    else
                    {
                        this.Width = defaultWidthSize;
                    }
                }
            }            
        }

        internal void SetWatchNodeProperties() 
        {
            resizeThumb.Visibility = Visibility.Visible;
            this.Width = defaultWidthSize;
            this.Height = minHeightSize;
            inputGrid.MinHeight = minHeightSize;
            inputGrid.MinWidth = minWidthSize;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //find the element which was clicked
            //and implement it's method for jumping to stuff
            var fe = sender as FrameworkElement;

            if (fe == null)
                return;

            var node = (WatchViewModel)fe.DataContext;

            if (node != null)
                node.Click();
        }

        private void treeviewItem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TreeViewItem tvi = (TreeViewItem)sender;
            var node = tvi.DataContext as WatchViewModel;

            HandleItemChanged(tvi, node);

            e.Handled = true; 
        }

        private void treeviewItem_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (prevWatchViewModel != null)
            {
                if (e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Down)
                {
                    TreeViewItem tvi = sender as TreeViewItem;
                    var node = tvi.DataContext as WatchViewModel;
                    
                    // checks to see if the currently selected WatchViewModel is the top most item in the tree
                    // if so, prevent the user from using the up arrow.

                    // also if the current selected node is equal to the previous selected node, do not execute the change.

                    if (!(e.Key == System.Windows.Input.Key.Up && prevWatchViewModel.IsTopLevel) && node != prevWatchViewModel)
                    {
                        HandleItemChanged(tvi, node);
                    }
                }
            }

            e.Handled = true;
        }

        private void HandleItemChanged (TreeViewItem tvi, WatchViewModel node)
        {
            if (tvi == null || node == null)
                return;

            // checks to see if the node to be selected is the same as the currently selected node
            // if so, then de-select the currently selected node.

            if (node == prevWatchViewModel)
            {
                this.prevWatchViewModel = null;
                if (tvi.IsSelected)
                {
                    tvi.IsSelected = false;
                    tvi.Focus();
                }
            }
            else
            {
                this.prevWatchViewModel = node;
            }

            if (_vm.FindNodeForPathCommand.CanExecute(node.Path))
            {
                _vm.FindNodeForPathCommand.Execute(node.Path);
            }
        }
        private void ThumbResizeThumbOnDragDeltaHandler(object sender, DragDeltaEventArgs e)
        {
            var yAdjust = ActualHeight + e.VerticalChange;
            var xAdjust = ActualWidth + e.HorizontalChange;

            if (xAdjust >= inputGrid.MinWidth)
            {
                Width = xAdjust;
            }

            if (yAdjust >= inputGrid.MinHeight)
            {
                Height = yAdjust;
            }
        }
    }
}
