﻿using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SDL.TridionVSRazorExtension
{
    internal sealed class ItemContextDeleteCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0140;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("0afcfbb1-8ba4-419e-bcef-61acd1516245");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContextDeleteCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ItemContextDeleteCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this._package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(MenuItemCallback, null, BeforeQueryStatus, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ItemContextDeleteCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ItemContextDeleteCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            TridionVSRazorExtensionPackage package = ((TridionVSRazorExtensionPackage)this.ServiceProvider);
            package.InitApplication();

            DTE applicationObject = package.ApplicationObject;
            Solution solution = package.Solution;
            Project project = package.Project;

            if (solution != null && project != null && applicationObject.SelectedItems != null)
            {
                foreach (SelectedItem item in applicationObject.SelectedItems)
                {
                    if (!item.Name.EndsWith(".cshtml") && !Functions.IsAllowedMimeType(item.Name))
                    {
                        MessageBox.Show("Item '" + item.ProjectItem.FileNames[0] + "' is not supported.", "Wrong Operation", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                var files = applicationObject.SelectedItems.Cast<SelectedItem>().Where(item => item.Name.EndsWith(".cshtml") || Functions.IsAllowedMimeType(item.Name)).Select(item => item.ProjectItem.FileNames[0]);
                Functions.DeleteFiles(files.ToArray(), project);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            TridionVSRazorExtensionPackage package = ((TridionVSRazorExtensionPackage)this.ServiceProvider);
            DTE applicationObject = package.ApplicationObject;

            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                menuCommand.Visible = true;
                if (applicationObject.SelectedItems != null)
                {
                    foreach (SelectedItem item in applicationObject.SelectedItems)
                    {
                        if (!item.Name.EndsWith(".cshtml") && !Functions.IsAllowedMimeType(item.Name))
                        {
                            menuCommand.Visible = false;
                        }
                    }
                }
            }
        }
    }
}