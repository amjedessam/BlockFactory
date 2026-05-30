// Clean converters file for BlockFactory.Desktop

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BlockFactory.Desktop.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : (object)true;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class LoadingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLoading = value is bool b && b;
            string[] texts = parameter?.ToString()?.Split('|') ?? new[] { "تنفيذ", "جاري..." };
            return isLoading ? texts[1] : texts[0];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class CountToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int count && count > 0;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count) return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (int.TryParse(value?.ToString() ?? string.Empty, out int parsed)) return parsed > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DecimalToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d) return d > 0;
            if (value is double dbl) return dbl > 0;
            if (value is int i) return i > 0;
            if (decimal.TryParse(value?.ToString(), out decimal parsed)) return parsed > 0;
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            if (parts?.Length == 2)
            {
                bool isTrue = value is bool b && b;
                string hex = isTrue ? parts[0] : parts[1];
                return (Color)ColorConverter.ConvertFromString(hex)!;
            }
            return Colors.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            if (parts?.Length == 2) return value is true ? parts[0] : parts[1];
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class EnumBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true && parameter != null ? Enum.Parse(targetType, parameter.ToString()!) : (object)Binding.DoNothing;
    }

    public class BoolToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool expanded = value is bool b && b;
            if (parameter is string p)
            {
                var parts = p.Split('|');
                if (parts.Length == 2 && double.TryParse(parts[0], out double w1) && double.TryParse(parts[1], out double w2))
                    return new GridLength(expanded ? w1 : w2);
            }
            return new GridLength(expanded ? 220.0 : 60.0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = value != null && value != DependencyProperty.UnsetValue;
            if (string.Equals(parameter?.ToString(), "Invert", StringComparison.OrdinalIgnoreCase)) hasValue = !hasValue;
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ProfitColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return (Color)ColorConverter.ConvertFromString("#F8F9FA")!;
            decimal d = ToDecimal(value);
            string hex = d >= 0 ? "#EAFAF1" : "#FDEDEC";
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
        internal static decimal ToDecimal(object value) => value switch { decimal x => x, double x => (decimal)x, float x => (decimal)x, int x => x, long x => x, _ => 0m };
    }

    public class ProfitTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return (Color)ColorConverter.ConvertFromString("#7F8C8D")!;
            decimal d = ProfitColorConverter.ToDecimal(value);
            string hex = d >= 0 ? "#27AE60" : "#E74C3C";
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ProfitLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return "—";
            decimal d = ProfitColorConverter.ToDecimal(value);
            if (d > 0) return "ربح صافي";
            if (d < 0) return "خسارة";
            return "توازن";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            if (parts?.Length == 2)
            {
                bool isTrue = value is bool b && b;
                string hex = isTrue ? parts[0] : parts[1];
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            return new SolidColorBrush(Colors.Transparent);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(hex)!;
                }
                catch { }
            }
            return Colors.Transparent;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PositiveColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return (Color)ColorConverter.ConvertFromString("#7F8C8D")!;
            decimal d = ProfitColorConverter.ToDecimal(value);
            string hex = d > 0 ? "#27AE60" : "#E74C3C";
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

/*
// BlockFactory.Desktop/Converters/BoolToVisibilityConverter.cs

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BlockFactory.Desktop.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value is bool b ? !b : (object)true;
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class LoadingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool isLoading = value is bool b && b;
            string[] texts = parameter?.ToString()?.Split('|')
                ?? new[] { "?????", "????..." };
            return isLoading ? texts[1] : texts[0];
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public\ class\ CountToBoolConverter\ :\ IValueConverter\n\ \ \ \ \{\n\ \ \ \ \ \ \ \ public\ object\ Convert\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \ \ \ \ =>\ value\ is\ int\ count\ &&\ count\ >\ 0;\n\ \ \ \ \ \ \ \ public\ object\ ConvertBack\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \ \ \ \ =>\ Binding\.DoNothing;\n\ \ \ \ }\n\n\ \ \ \ public\ class\ CountToVisibilityConverter\ :\ IValueConverter\n\ \ \ \ \{\n\ \ \ \ \ \ \ \ public\ object\ Convert\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \{\n\ \ \ \ \ \ \ \ \ \ \ \ if\ \(value\ is\ int\ count\)\n\ \ \ \ \ \ \ \ \ \ \ \ \ \ \ \ return\ count\ >\ 0\ \?\ Visibility\.Visible\ :\ Visibility\.Collapsed;\n\ \ \ \ \ \ \ \ \ \ \ \ return\ Visibility\.Collapsed;\n\ \ \ \ \ \ \ \ }\n\n\ \ \ \ \ \ \ \ public\ object\ ConvertBack\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \ \ \ \ =>\ throw\ new\ NotImplementedException\(\);\n\ \ \ \ }\n
    public class DecimalToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is decimal d)
                return d > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is double dbl)
                return dbl > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            if (parts?.Length == 2)
            {
                bool isTrue = value is bool b && b;
                return new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        isTrue ? parts[0] : parts[1]));
            }
            return new SolidColorBrush(Colors.White);
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            if (parts?.Length == 2)
                return value is true ? parts[0] : parts[1];
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class EnumBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value is true && parameter != null
                ? Enum.Parse(targetType, parameter.ToString()!)
                : (object)Binding.DoNothing;
    }

    public class BoolToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool expanded = value is bool b && b;
            if (parameter is string p)
            {
                var parts = p.Split('|');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double w1) &&
                    double.TryParse(parts[1], out double w2))
                    return expanded ? w1 : w2;
            }
            return expanded ? 220.0 : 60.0;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
/*
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BlockFactory.Desktop.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value is bool b ? !b : true;
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class LoadingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool isLoading = value is bool b && b;
            string[] texts = parameter?.ToString()?.Split('|')
                ?? new[] { "?????", "????..." };
            return isLoading ? texts[1] : texts[0];
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public\ class\ CountToBoolConverter\ :\ IValueConverter\n\ \ \ \ \{\n\ \ \ \ \ \ \ \ public\ object\ Convert\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \ \ \ \ =>\ value\ is\ int\ count\ &&\ count\ >\ 0;\n\ \ \ \ \ \ \ \ public\ object\ ConvertBack\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \ \ \ \ =>\ Binding\.DoNothing;\n\ \ \ \ }\n\n\ \ \ \ public\ class\ CountToVisibilityConverter\ :\ IValueConverter\n\ \ \ \ \{\n\ \ \ \ \ \ \ \ public\ object\ Convert\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \{\n\ \ \ \ \ \ \ \ \ \ \ \ if\ \(value\ is\ int\ count\)\n\ \ \ \ \ \ \ \ \ \ \ \ \ \ \ \ return\ count\ >\ 0\ \?\ Visibility\.Visible\ :\ Visibility\.Collapsed;\n\ \ \ \ \ \ \ \ \ \ \ \ return\ Visibility\.Collapsed;\n\ \ \ \ \ \ \ \ }\n\n\ \ \ \ \ \ \ \ public\ object\ ConvertBack\(object\ value,\ Type\ targetType,\n\ \ \ \ \ \ \ \ \ \ \ \ object\ parameter,\ CultureInfo\ culture\)\n\ \ \ \ \ \ \ \ \ \ \ \ =>\ throw\ new\ NotImplementedException\(\);\n\ \ \ \ }\n
    public class DecimalToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is decimal d) return d > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is double dbl) return dbl > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            if (parts?.Length == 2)
            {
                bool isValid = value is bool b && b;
                return isValid
                    ? (Color)ColorConverter.ConvertFromString(parts[0])
                    : (Color)ColorConverter.ConvertFromString(parts[1]);
            }
            return Colors.White;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            if (parts?.Length == 2)
                return value is true ? parts[0] : parts[1];
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class EnumBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value is true ? parameter : Binding.DoNothing;
    }
    public class BoolToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool expanded = value is bool b && b;
            if (parameter is string p)
            {
                var parts = p.Split('|');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double w1) &&
                    double.TryParse(parts[1], out double w2))
                    return expanded ? w1 : w2;
            }
            return expanded ? 220.0 : 60.0;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}*/

/*
 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlockFactory.Desktop.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class LoadingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool isLoading = value is bool b && b;
            string[] texts = parameter?.ToString()?.Split('|')
                ?? new[] { "?????", "????..." };

            return isLoading ? texts[1] : texts[0];
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}*/

