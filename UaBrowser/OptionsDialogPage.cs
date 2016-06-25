// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Workstation.UaBrowser.ViewModels;

namespace Workstation.UaBrowser
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid(GuidList.GuidOptionsDialogPersistanceString)]
    public class OptionsDialogPage : UIElementDialogPage
    {
        private OptionsDialogPageControl view;
        private UaBrowserViewModel viewmodel;

        public OptionsDialogPage()
        {
        }

        protected override UIElement Child
        {
            get
            {
                if (this.view == null)
                {
                    this.view = new OptionsDialogPageControl();
                    var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
                    this.viewmodel = componentModel.DefaultExportProvider.GetExportedValue<UaBrowserViewModel>();
                    this.view.DataContext = this.viewmodel;
                }

                return this.view;
            }
        }

        protected override void OnApply(DialogPage.PageApplyEventArgs e)
        {
            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                this.view.LayoutRoot.BindingGroup.CommitEdit();
                this.viewmodel.SaveSettingsCommand.Execute(null);
            }

            base.OnApply(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            this.view.LayoutRoot.BindingGroup.CancelEdit();
            base.OnClosed(e);
        }
    }
}
