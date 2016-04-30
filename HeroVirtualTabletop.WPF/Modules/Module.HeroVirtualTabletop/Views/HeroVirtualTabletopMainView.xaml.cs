using Module.HeroVirtualTabletop.ViewModels;
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

namespace Module.HeroVirtualTabletop.Views
{
    /// <summary>
    /// Interaction logic for HeroVirtualTabletopMainView.xaml
    /// </summary>
    public partial class HeroVirtualTabletopMainView : UserControl
    {
        private HeroVirtualTabletopMainViewModel viewModel;
        public HeroVirtualTabletopMainView(HeroVirtualTabletopMainViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
        }
    }
}
