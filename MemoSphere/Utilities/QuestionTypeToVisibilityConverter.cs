using Core.Enums;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPF.Utilities
{
    public class QuestionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is QuestionType currentType && parameter is string target)
            {

                if (target == "MultipleChoice" && currentType == QuestionType.MultipleChoice) return Visibility.Visible;
                if (target == "TrueFalse" && currentType == QuestionType.TrueFalse) return Visibility.Visible;
                if (target == "NotTrueFalse" && currentType != QuestionType.TrueFalse) return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}