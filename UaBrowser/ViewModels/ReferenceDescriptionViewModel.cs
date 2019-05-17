// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Workstation.ServiceModel.Ua;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;

namespace Workstation.UaBrowser.ViewModels
{
    public class ReferenceDescriptionViewModel : TreeViewItemViewModel
    {
        private static readonly Regex SafeCharsRegex = new Regex(@"[\W]");
        private static readonly ExpandedNodeId ObjectsFolder = ExpandedNodeId.Parse(ObjectIds.ObjectsFolder);
        private static readonly ExpandedNodeId ViewsFolder = ExpandedNodeId.Parse(ObjectIds.ViewsFolder);
        private static readonly ExpandedNodeId TypesFolder = ExpandedNodeId.Parse(ObjectIds.TypesFolder);

        private readonly ReferenceDescription description;
        private readonly Func<ReferenceDescriptionViewModel, Task> loadChildren;
        private string fullName;

        public ReferenceDescriptionViewModel(ReferenceDescription description, ReferenceDescriptionViewModel parent, Func<ReferenceDescriptionViewModel, Task> loadChildren)
            : base(parent, true)
        {
            this.description = description;
            this.loadChildren = loadChildren;
        }

        public LocalizedText DisplayName => this.description.DisplayName;

        public string FullName => this.fullName ?? (this.fullName = this.GetFullName());

        public ExpandedNodeId NodeId => this.description.NodeId;

        public QualifiedName BrowseName => this.description.BrowseName;

        public NodeClass NodeClass => this.description.NodeClass;

        public new ReferenceDescriptionViewModel Parent => base.Parent as ReferenceDescriptionViewModel;

        public virtual string GetSnippet(string snippet, string language)
        {
            return string.Empty;
        }

        private string GetFullName()
        {
            var name = new StringBuilder();
            for (ReferenceDescriptionViewModel current = this; current != null; current = current.Parent)
            {
                if (current.NodeId == ObjectsFolder || current.NodeId == TypesFolder || current.NodeId == ViewsFolder)
                {
                    break;
                }

                name.Insert(0, current.BrowseName.Name);
            }

            return SafeCharsRegex.Replace(name.ToString(), "_");
        }

        protected override async Task LoadChildrenAsync()
        {
            await this.loadChildren(this);
        }
    }
}