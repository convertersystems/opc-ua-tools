// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Workstation.UaBrowser.Data
{
    [ValueConversion(typeof(Type), typeof(string))]
    public class TypeToStringConverter : ValueConverter<Type, string>
    {
        protected override string Convert(Type value, object parameter, CultureInfo culture)
        {
            return this.FormatTypeName(value);
        }

        private string FormatTypeName(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            if (type.IsNested && type.DeclaringType.IsGenericType)
            {
                throw new NotImplementedException();
            }

            StringBuilder txt = new StringBuilder();
            txt.Append(type.Name, 0, type.Name.IndexOf('`'));
            txt.Append("<");
            txt.Append(string.Join(", ", type.GetGenericArguments().Select(arg => this.FormatTypeName(arg))));
            txt.Append(">");
            return txt.ToString();
        }
    }
}