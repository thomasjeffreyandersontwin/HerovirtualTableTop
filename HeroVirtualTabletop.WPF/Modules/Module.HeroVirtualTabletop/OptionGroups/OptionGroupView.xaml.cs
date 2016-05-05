using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Module.HeroVirtualTabletop.OptionGroups
{
    /// <summary>
    /// Interaction logic for OptionGroupView.xaml
    /// </summary>
    public partial class OptionGroupView : UserControl
    {

        public OptionGroupView()
        {
            InitializeComponent();
        }

    }

    public class OptionListBox : ListBox
    {
        public ICharacterOption DefaultOption
        {
            get { return (ICharacterOption)GetValue(DefaultOptionProperty); }
            set { SetValue(DefaultOptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultOption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultOptionProperty =
            DependencyProperty.Register("DefaultOption", typeof(ICharacterOption), typeof(OptionListBox), new PropertyMetadata(null));

        public ICharacterOption ActiveOption
        {
            get { return (ICharacterOption)GetValue(ActiveOptionProperty); }
            set { SetValue(ActiveOptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultOption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveOptionProperty =
            DependencyProperty.Register("ActiveOption", typeof(ICharacterOption), typeof(OptionListBox), new PropertyMetadata(null));
    }
}
