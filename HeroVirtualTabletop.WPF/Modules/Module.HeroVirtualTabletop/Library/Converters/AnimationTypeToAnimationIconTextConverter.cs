using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    public class AnimationTypeToAnimationIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconText = null;
            AnimationType animationType = (AnimationType)value;
            switch(animationType)
            {
                case AnimationType.Movement:
                    iconText = "\uf008";
                    break;
                case AnimationType.FX:
                    iconText = "\uf0d0";
                    break;
                case AnimationType.Sound:
                    iconText = "\uf001";
                    break;
                case AnimationType.Pause:
                    iconText = "\uf04c";
                    break;
                case AnimationType.Sequence:
                    iconText = "\uf126";
                    break;
                case AnimationType.Reference:
                    iconText = "\uf08e";
                    break;
            }
            return iconText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
