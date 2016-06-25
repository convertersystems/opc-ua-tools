// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Workstation.ServiceModel.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Workstation.UaBrowser.ViewModels
{
    public class ReferenceDescriptionViewModel : TreeViewItemViewModel
    {
        private readonly ReferenceDescription description;
        private readonly Type dataType;
        private readonly AccessLevelFlags accessLevel;
        private readonly EventNotifierFlags notifier;
        private readonly Func<ReferenceDescriptionViewModel, Task> loadChildren;

        public ReferenceDescriptionViewModel(ReferenceDescription description, Type dataType, AccessLevelFlags accessLevel, EventNotifierFlags eventNotifier, TreeViewItemViewModel parentViewModel, Func<ReferenceDescriptionViewModel, Task> loadChildren)
            : base(parentViewModel, loadChildren != null)
        {
            this.description = description;
            this.dataType = dataType;
            this.accessLevel = accessLevel;
            this.notifier = eventNotifier;
            this.loadChildren = loadChildren;
        }

        public Type DataType
        {
            get { return this.dataType; }
        }

        public AccessLevelFlags AccessLevel
        {
            get { return this.accessLevel; }
        }

        public bool IsObject
        {
            get { return this.description.NodeClass == NodeClass.Object; }
        }

        public bool IsMethod
        {
            get { return this.description.NodeClass == NodeClass.Method; }
        }

        public bool IsVariable
        {
            get { return this.description.NodeClass == NodeClass.Variable; }
        }

        public bool IsEventNotifier
        {
            get { return this.notifier.HasFlag(EventNotifierFlags.SubscribeToEvents); }
        }

        public new ReferenceDescriptionViewModel Parent
        {
            get { return base.Parent as ReferenceDescriptionViewModel; }
        }

        public string FullName
        {
            get
            {
                var list = new List<string>(4);
                ReferenceDescriptionViewModel current = this;
                list.Add(current.DisplayName);
                while (current.Parent != null && current.NodeClass != NodeClass.Object)
                {
                    current = current.Parent;
                    list.Add(current.DisplayName);
                }

                return string.Concat(list.ToArray().Reverse());
            }
        }

        public string DisplayName
        {
            get { return this.description.DisplayName.Text; }
        }

        public ExpandedNodeId NodeId
        {
            get { return this.description.NodeId; }
        }

        public QualifiedName BrowseName
        {
            get { return this.description.BrowseName; }
        }

        public NodeClass NodeClass
        {
            get { return this.description.NodeClass; }
        }

        protected override async Task LoadChildrenAsync()
        {
            await this.loadChildren(this);
        }
    }
}