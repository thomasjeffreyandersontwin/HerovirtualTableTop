<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Module.Shared.Converters
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bRet = false;
            bool bParam = Boolean.Parse(parameter.ToString());
            if (value != null)
                bRet = ((bool)value ^ bParam);
            return bRet;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bParam = Boolean.Parse(parameter.ToString());
            return ((bool)value ^ bParam);
        }
    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Module.Shared.Converters
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bRet = false;
            bool bParam = Boolean.Parse(parameter.ToString());
            if (value != null)
                bRet = ((bool)value ^ bParam);
            return bRet;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bParam = Boolean.Parse(parameter.ToString());
            return ((bool)value ^ bParam);
        }
    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
