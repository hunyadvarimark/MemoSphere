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

            // Ha a szöveg tartalmaz LaTeX jelölőt ($ vagy \), akkor FormulaControl-t adunk vissza
            if (text.Contains("$") || text.Contains(@"\"))
            {
                // A WpfMath nem szereti a $ jeleket, azokat le kell vágni
                string latex = text.Replace("$", "").Trim();

                return new FormulaControl
                {
                    Formula = latex,
                    Scale = 20, // Méret
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            // Ha sima szöveg, marad string (a ContentPresenter TextBlock-ként jeleníti meg)
            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}