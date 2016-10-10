// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Workstation.UaBrowser.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Workstation.UaBrowser
{
    /// <summary>
    /// Interaction logic for UaBrowserControl.xaml
    /// </summary>
    public partial class UaBrowserControl : UserControl, IDisposable
    {
        private TreeViewItem dragItem;
        private Point? dragPoint;

        public UaBrowserControl()
        {
            this.InitializeComponent();
        }

        // Called by tool window when vs closes
        public void Dispose()
        {
            var vm = this.DataContext as UaBrowserViewModel;
            if (vm != null)
            {
                vm.Dispose();
            }
        }

        private static T FindAncestor<T>(DependencyObject dependencyObject)
            where T : class
        {
            var target = dependencyObject;
            do
            {
                if (target is Visual)
                {
                    target = VisualTreeHelper.GetParent(target);
                }
                else
                {
                    target = LogicalTreeHelper.GetParent(target);
                }
            }
            while (target != null && !(target is T));
            return target as T;
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.dragPoint = new Point?(e.GetPosition(null));
            this.dragItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.dragPoint.HasValue && this.dragItem != null)
            {
                var vector = this.dragPoint.Value - e.GetPosition(null);
                if (Math.Abs(vector.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(vector.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var rdvm = this.dragItem.DataContext as ReferenceDescriptionViewModel;
                    if (rdvm != null)
                    {
                        var vm = this.DataContext as UaBrowserViewModel;
                        var text = vm.FormatProperty(rdvm);
                        if (!string.IsNullOrEmpty(text))
                        {
                            DataObject data = new DataObject(System.Windows.DataFormats.Text, text);
                            DragDrop.DoDragDrop(this.dragItem, data, DragDropEffects.Copy);
                            this.dragItem = null;
                            this.dragPoint = null;
                        }
                    }
                }
            }
        }

        private void MainGrid_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            var grid = (Grid)e.TargetObject;
            var b = (bool?)grid.Tag;
            if (b == true)
            {
                VisualStateManager.GoToElementState(grid, "IsLoading", true);
            }
            else
            {
                VisualStateManager.GoToElementState(grid, "Ready", true);
            }
        }

        private void Refresh_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var vm = (UaBrowserViewModel)this.DataContext;
            await vm.RefreshAsync(e.Parameter as string, this.UserNameBox.Text, this.PasswordBox.Password);

        }

    }
}