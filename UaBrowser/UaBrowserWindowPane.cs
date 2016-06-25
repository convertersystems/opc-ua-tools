// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Workstation.UaBrowser.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Workstation.UaBrowser
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    [Guid(GuidList.GuidToolWindowPersistanceString)]
    public class UaBrowserWindowPane : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UaBrowserWindowPane"/> class.
        /// </summary>
        public UaBrowserWindowPane()
            : base(null)
        {
            this.Caption = "OPC UA Browser";

            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 300;
            this.BitmapIndex = 0;
            this.Content = new UaBrowserControl();
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
            var vm = componentModel.DefaultExportProvider.GetExportedValue<UaBrowserViewModel>();

            var fe = this.Content as FrameworkElement;
            if (fe != null)
            {
                fe.DataContext = vm;
            }
        }
    }
}