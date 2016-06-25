// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Workstation.ServiceModel.Ua;
using System.Globalization;
using System.Windows.Data;

namespace Workstation.UaBrowser.Data
{
    [ValueConversion(typeof(AccessLevelFlags), typeof(string))]
    public class AccessLevelToStringConverter : ValueConverter<AccessLevelFlags, string>
    {
        protected override string Convert(AccessLevelFlags value, object parameter, CultureInfo culture)
        {
            return value.HasFlag(AccessLevelFlags.CurrentWrite) ? "{ get; set; }" : "{ get; }";
        }
    }
}