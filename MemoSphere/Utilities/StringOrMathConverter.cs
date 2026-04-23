using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WpfMath.Controls;

namespace WPF.Utilities
{
    public class StringOrMathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as string;
            if (string.IsNullOrWhiteSpace(text)) return null;

            if (text.Contains("$") || text.Contains(@"\"))
            {
                string latex = text.Replace("$", "").Trim();

                return new FormulaControl
                {
                    Formula = latex,
                    Scale = 20,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}