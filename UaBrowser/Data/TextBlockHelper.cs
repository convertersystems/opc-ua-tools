// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;

namespace Workstation.UaBrowser.Data
{
    public static class TextBlockHelper
    {
        public static readonly DependencyProperty MarkupTextProperty = DependencyProperty.RegisterAttached(
            "MarkupText",
            typeof(string),
            typeof(TextBlockHelper),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure, MarkupTextPropertyChanged));

        public static void SetMarkupText(DependencyObject textBlock, string value)
        {
            textBlock.SetValue(MarkupTextProperty, value);
        }

        public static string GetMarkupText(DependencyObject textBlock)
        {
            return (string)textBlock.GetValue(MarkupTextProperty);
        }

        private static void MarkupTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as TextBlock;
            if (textBlock == null)
            {
                return;
            }

            var formattedText = (string)e.NewValue ?? string.Empty;
            formattedText = string.Format("<Span xml:space=\"preserve\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{0}</Span>", formattedText);

            textBlock.Inlines.Clear();
            using (var xmlReader = XmlReader.Create(new StringReader(formattedText)))
            {
                var result = (Span)XamlReader.Load(xmlReader);
                textBlock.Inlines.Add(result);
            }
        }
    }
}
