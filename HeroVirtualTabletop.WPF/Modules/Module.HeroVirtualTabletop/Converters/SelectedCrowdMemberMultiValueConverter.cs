using Module.HeroVirtualTabletop.DomainModels;
using Module.HeroVirtualTabletop.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Converters
{
    public class SelectedCrowdMemberMultiValueConverter : IMultiValueConverter
    {
        /// <summary>   Converts boolean values. </summary>
        /// <param name="values">       The values. </param>
        /// <param name="targetType">   Type of the target. </param>
        /// <param name="parameter">    The parameter. </param>
        /// <param name="culture">      The culture. </param>
        /// <returns>   The converted object. </returns>
        public object Convert(object[] values, Type targetType, object parameter,
                              System.Globalization.CultureInfo culture)
        {
            if ((bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)
                return null;

            CrowdModel crowdModel = null;

            if (values.Count() == 2)
            {
                if (values[0] is CrowdModel)
                    crowdModel = values[0] as CrowdModel;
                else if(values[0] is Character && values[1] is CrowdModel)
                    crowdModel = values[1] as CrowdModel;
            }

            return crowdModel;
        }

        /// <summary>   Convert back. </summary>
        /// <exception cref="NotImplementedException">  Thrown when the requested operation is
        ///                                             unimplemented. </exception>
        /// <param name="value">        The value. </param>
        /// <param name="targetTypes">  List of types of the targets. </param>
        /// <param name="parameter">    The parameter. </param>
        /// <param name="culture">      The culture. </param>
        /// <returns>   The converted object. </returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
                                    System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
