// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace Workstation.UaBrowser
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]

    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.5.13", IconResourceID = 400)]

    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]

    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(UaBrowserWindowPane))]

    // This attribute registers the package directory for locating references for this package.
    [ProvideBindingPath]
    [Guid(GuidList.GuidUaBrowserPkgString)]
    [ProvideOptionPage(typeof(OptionsDialogPage), "OPC UA Browser", "General", 0, 0, true)]
    public sealed class UaBrowserPackage : Package
    {
        public UaBrowserPackage()
        {
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.GuidUaBrowserCmdSet, (int)PkgCmdIDList.CmdidUaBrowser);
                MenuCommand menuToolWin = new MenuCommand(this.ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the
        /// tool window. See the Initialize method to see how the menu item is associated to
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(UaBrowserWindowPane), 0, true);
            if ((window == null) || (window.Frame == null))
            {
                throw new NotSupportedException("Unable to create OPC UA Browser window.");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}