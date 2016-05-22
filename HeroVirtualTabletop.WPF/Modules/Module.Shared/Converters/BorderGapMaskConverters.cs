using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Module.Shared.Converters
{
    public class LeftBorderGapMaskConverter : IMultiValueConverter
    {
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public LeftBorderGapMaskConverter()
        {
            //      base.ctor();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Type type1 = typeof(double);
            if (parameter == null
                || values == null
                || (values.Length != 3 || values[0] == null)
                || (values[1] == null
                    || values[2] == null
                    || (!type1.IsAssignableFrom(values[0].GetType())
                        || !type1.IsAssignableFrom(values[1].GetType())))
                || !type1.IsAssignableFrom(values[2].GetType()))
                return DependencyProperty.UnsetValue;

            Type type2 = parameter.GetType();
            if (!type1.IsAssignableFrom(type2)
                && !typeof(string).IsAssignableFrom(type2))
                return DependencyProperty.UnsetValue;

            double pixels1 = (double)values[0];
            double num1 = (double)values[1];
            double num2 = (double)values[2];
            if (num1 == 0.0 || num2 == 0.0)
                return (object)null;

            double pixels2 = !(parameter is string)
                ? (double)parameter
                : double.Parse((string)parameter, (IFormatProvider)NumberFormatInfo.InvariantInfo);

            Grid grid = new Grid();
            grid.Width = num1;
            grid.Height = num2;
            RowDefinition RowDefinition1 = new RowDefinition();
            RowDefinition RowDefinition2 = new RowDefinition();
            RowDefinition RowDefinition3 = new RowDefinition();
            RowDefinition1.Height = new GridLength(pixels2);
            RowDefinition2.Height = new GridLength(pixels1);
            RowDefinition3.Height = new GridLength(1.0, GridUnitType.Star);
            grid.RowDefinitions.Add(RowDefinition1);
            grid.RowDefinitions.Add(RowDefinition2);
            grid.RowDefinitions.Add(RowDefinition3);
            ColumnDefinition ColumnDefinition1 = new ColumnDefinition();
            ColumnDefinition ColumnDefinition2 = new ColumnDefinition();
            ColumnDefinition1.Width = new GridLength(num2 / 2.0);
            ColumnDefinition2.Width = new GridLength(1.0, GridUnitType.Star);
            grid.ColumnDefinitions.Add(ColumnDefinition1);
            grid.ColumnDefinitions.Add(ColumnDefinition2);
            Rectangle rectangle1 = new Rectangle();
            Rectangle rectangle2 = new Rectangle();
            Rectangle rectangle3 = new Rectangle();
            rectangle1.Fill = (Brush)Brushes.Black;
            rectangle2.Fill = (Brush)Brushes.Black;
            rectangle3.Fill = (Brush)Brushes.Black;

            Grid.SetColumnSpan((UIElement)rectangle1, 2);
            Grid.SetColumn((UIElement)rectangle1, 0);
            Grid.SetRow((UIElement)rectangle1, 0);
            Grid.SetColumn((UIElement)rectangle2, 1);
            Grid.SetRow((UIElement)rectangle2, 1);
            Grid.SetColumnSpan((UIElement)rectangle3, 2);
            Grid.SetColumn((UIElement)rectangle3, 0);
            Grid.SetRow((UIElement)rectangle3, 2);
            grid.Children.Add((UIElement)rectangle1);
            grid.Children.Add((UIElement)rectangle2);
            grid.Children.Add((UIElement)rectangle3);
            return (object)new VisualBrush((Visual)grid);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[1]
          {
            Binding.DoNothing
          };
        }
    }
}
