// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaBrowser.ViewModels
{
    public class EventDescriptionViewModel : ReferenceDescriptionViewModel
    {
        public EventDescriptionViewModel(ReferenceDescription description, ReferenceDescriptionViewModel parent, Func<ReferenceDescriptionViewModel, Task> loadChildren)
            : base(description, parent, loadChildren)
        {
        }

        public string PropertyType => "BaseEvent";

        public override string GetSnippet(string snippet, string language)
        {
            var s = new StringBuilder(snippet);
            s.Replace("$name$", this.DisplayName);
            s.Replace("$browseName$", this.BrowseName.ToString());
            s.Replace("$fullName$", this.FullName);
            s.Replace("$dataType$", this.PropertyType);
            s.Replace("$nodeId$", this.NodeId.ToString());
            s.Replace("$parentNodeId$", this.Parent.NodeId.ToString());
            s.AppendLine();
            return s.ToString();
        }
    }
}