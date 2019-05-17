// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaBrowser.ViewModels
{
    public class DataTypeDescriptionViewModel : ReferenceDescriptionViewModel
    {
        private ExpandedNodeId binaryEncodingId;
        private XElement element;
        private string targetNamespace;
        private string baseType;

        public DataTypeDescriptionViewModel(ReferenceDescription description, ExpandedNodeId binaryEncodingId, XElement element, string targetNamespace, ReferenceDescriptionViewModel parent, Func<ReferenceDescriptionViewModel, Task> loadChildren)
            : base(description, parent, loadChildren)
        {
            this.binaryEncodingId = binaryEncodingId;
            this.element = element;
            this.targetNamespace = targetNamespace;
            this.baseType = parent.DisplayName;
        }

        public override string GetSnippet(string snippet, string language)
        {
            var s = new StringBuilder(snippet);
            s.Replace("$element$", this.element.ToString());
            s.Replace("$targetNamespace$", this.targetNamespace);
            s.Replace("$binaryEncodingId$", this.binaryEncodingId.ToString());
            s.Replace("$nodeId$", this.NodeId.ToString());
            s.Replace("$dataType$", this.DisplayName);
            s.Replace("$baseType$", this.baseType);
            s.AppendLine();
            return s.ToString();
        }

    }
}