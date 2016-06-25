// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Workstation.UaBrowser
{
    /// <summary>
    /// Interaction logic for OptionsDialogPageControl.xaml
    /// </summary>
    public partial class OptionsDialogPageControl : UserControl
    {
        static OptionsDialogPageControl()
        {
            EventManager.RegisterClassHandler(typeof(TextBox), UIElementDialogPage.DialogKeyPendingEvent, new EventHandler<DialogKeyEventArgs>(OptionsDialogPageControl.HandleTextBoxDialogKey));
        }

        private static void HandleTextBoxDialogKey(object sender, DialogKeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Tab)
            {
                e.Handled = true;
            }
        }

        public OptionsDialogPageControl()
        {
            this.InitializeComponent();
        }
    }
}
