// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.UaBrowser.ViewModels
{

    public class Parameter
    {
        public Parameter(string parameterType, string name, string description)
        {
            this.ParameterType = parameterType;
            this.Name = name;
            this.Description = description;
        }

        public string ParameterType { get; }

        public string Name { get; }

        public string Description { get; }
    }
}