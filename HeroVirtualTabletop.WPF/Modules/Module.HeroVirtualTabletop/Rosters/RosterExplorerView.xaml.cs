using Module.HeroVirtualTabletop.Roster;
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

namespace Module.HeroVirtualTabletop.Roster
{
    /// <summary>
    /// Interaction logic for RosterExplorerView.xaml
    /// </summary>
    public partial class RosterExplorerView : UserControl
    {
        private RosterExplorerViewModel viewModel;
        public RosterExplorerView(RosterExplorerViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
        }

        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var ItemsPres = ((sender as TextBlock).Parent as Expander).Content as ItemsPresenter;
                var VStackPanel = VisualTreeHelper.GetChild(ItemsPres as DependencyObject, 0) as VirtualizingStackPanel;
                foreach (ListBoxItem item in VStackPanel.Children)
                {
                    item.IsSelected = true;
                }
                e.Handled = true;
            }
        }
    }
}
