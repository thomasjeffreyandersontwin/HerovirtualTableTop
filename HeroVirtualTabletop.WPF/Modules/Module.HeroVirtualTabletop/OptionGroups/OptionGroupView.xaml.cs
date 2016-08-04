using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
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
        private IOptionGroupViewModel viewModel;

        public OptionGroupView()
        {
            InitializeComponent();

            this.DataContextChanged += (sender, e) =>
            {
                this.viewModel = this.DataContext as IOptionGroupViewModel;
                this.viewModel.EditModeEnter += viewModel_EditModeEnter;
                this.viewModel.EditModeLeave += viewModel_EditModeLeave;
            };
        }

        private void viewModel_EditModeEnter(object sender, EventArgs e)
        {
            Border headborder = (Border)this.grpBoxOptionGroup.Template.FindName("Header", this.grpBoxOptionGroup);

            ContentPresenter headContentPresenter = (ContentPresenter)headborder.Child;

            var dataTemplate = this.grpBoxOptionGroup.HeaderTemplate;
            TextBlock headerTextBlock = dataTemplate.FindName("textBlockName", headContentPresenter) as TextBlock;
            TextBox headerTextBox = dataTemplate.FindName("textBoxName", headContentPresenter) as TextBox;
            headerTextBox.Text = headerTextBlock.Text;
            headerTextBox.Visibility = Visibility.Visible;
            headerTextBox.Focus();
            headerTextBox.SelectAll();
        }

        private void viewModel_EditModeLeave(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            BindingExpression expression = txtBox.GetBindingExpression(TextBox.TextProperty);
            expression.UpdateSource();
            txtBox.Visibility = Visibility.Collapsed;
        }
    }
}
