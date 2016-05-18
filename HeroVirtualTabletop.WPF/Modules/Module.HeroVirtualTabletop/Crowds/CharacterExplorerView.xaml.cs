using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Framework.WPF.Extensions;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;

namespace Module.HeroVirtualTabletop.Crowds
{
    /// <summary>
    /// Interaction logic for CharacterExplorerView.xaml
    /// </summary>
    public partial class CharacterExplorerView : UserControl
    {
        private CharacterExplorerViewModel viewModel;
        public CharacterExplorerView(CharacterExplorerViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.EditModeEnter += viewModel_EditModeEnter;
            this.viewModel.EditModeLeave += viewModel_EditModeLeave;
            this.viewModel.SelectionUpdated += viewModel_SelectionUpdated;
        }

        private void viewModel_SelectionUpdated(object sender, EventArgs e)
        {
            ICrowdMemberModel modelToSelect = sender as ICrowdMemberModel;
            //SelectTreeViewItem(toSelect);
            if(sender == null) // need to unselect
            {
                DependencyObject dObject = treeViewCrowd.GetItemFromSelectedObject(treeViewCrowd.SelectedItem);
                TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                if(tvi != null)
                    tvi.IsSelected = false;
            }
            else
            {
                bool itemFound = false;
                TextBox txtBox = null;
                if (this.viewModel.SelectedCrowdModel == null || this.viewModel.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME)
                {
                    treeViewCrowd.UpdateLayout();
                    //TreeViewItem firstItem = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(this.viewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]) as TreeViewItem;
                    //if (firstItem != null)
                    //    firstItem.IsSelected = true;
                    for (int i = 1; i < treeViewCrowd.Items.Count; i++) // A new crowd has been added to the collection
                    {
                        TreeViewItem item = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(treeViewCrowd.Items[i]) as TreeViewItem;
                        if (item != null)
                        {
                            var model = item.DataContext as ICrowdMemberModel;
                            if (model.Name == modelToSelect.Name)
                            {
                                item.IsSelected = true;
                                itemFound = true;
                                txtBox = FindTextBoxInTemplate(item);
                                break;
                            }
                        }
                    }
                }
                if (!itemFound)
                {
                    DependencyObject dObject = treeViewCrowd.GetItemFromSelectedObject(this.viewModel.SelectedCrowdModel);
                    TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                    if (tvi != null)
                    {
                        ICrowdMemberModel model = tvi.DataContext as ICrowdMemberModel;
                        if (model.Name == modelToSelect.Name) // A crowd has been added
                        {
                            tvi.IsSelected = true;
                            itemFound = true;
                            txtBox = FindTextBoxInTemplate(tvi);
                        }
                        else if (tvi.Items != null)
                        {
                            tvi.IsExpanded = true;
                            tvi.UpdateLayout();
                            for (int i = 0; i < tvi.Items.Count; i++)
                            {
                                TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                                if (item != null)
                                {
                                    model = item.DataContext as ICrowdMemberModel;
                                    if (model.Name == modelToSelect.Name)
                                    {
                                        item.IsSelected = true;
                                        itemFound = true;                                       
                                        item.UpdateLayout();
                                        txtBox = FindTextBoxInTemplate(item);
                                        break;
                                    }
                                }
                            }
                        }
                    } 
                }
                if (txtBox != null)
                    this.viewModel.EnterEditModeCommand.Execute(txtBox);
            }
        }

        private TextBox FindTextBoxInTemplate(TreeViewItem item)
        {
            TextBox textBox = Helper.GetDescendantByType(item, typeof(TextBox)) as TextBox;
            return textBox;
        }

        

        private void viewModel_EditModeEnter(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            Grid grid = txtBox.Parent as Grid;
            TextBox textBox = grid.Children[1] as TextBox;
            textBox.Visibility = Visibility.Visible;
            textBox.Focus();
            textBox.SelectAll();
        }

        private void viewModel_EditModeLeave(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            txtBox.Visibility = Visibility.Hidden;
            BindingExpression expression = txtBox.GetBindingExpression(TextBox.TextProperty);
            expression.UpdateSource(); 
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                if (treeViewItem.DataContext is CrowdModel)
                {
                    DependencyObject dObject = VisualTreeHelper.GetParent(treeViewItem); // got the immediate parent
                    treeViewItem = dObject as TreeViewItem; // now get first treeview item parent
                    while (treeViewItem == null)
                    {
                        dObject = VisualTreeHelper.GetParent(dObject);
                        treeViewItem = dObject as TreeViewItem;
                        if (dObject is TreeView)
                            break;
                    }
                    if(treeViewItem != null)
                        this.viewModel.SelectedCrowdParent = treeViewItem.DataContext as CrowdModel;
                    else
                        this.viewModel.SelectedCrowdParent = null;
                }
                else
                    this.viewModel.SelectedCrowdParent = null;
            }
        }
        private TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void SelectTreeViewItem(object item)
        {
            try
            {
                var tvi = GetContainerFromItem(this.treeViewCrowd, item);

                tvi.Focus();
                tvi.IsSelected = true;

                var selectMethod =
                    typeof(TreeViewItem).GetMethod("Select",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                selectMethod.Invoke(tvi, new object[] { true });
            }
            catch { }
        }

        private TreeViewItem GetContainerFromItem(ItemsControl parent, object item)
        {
            var found = parent.ItemContainerGenerator.ContainerFromItem(item);
            if (found == null)
            {
                for (int i = 0; i < parent.Items.Count; i++)
                {
                    var childContainer = parent.ItemContainerGenerator.ContainerFromIndex(i) as ItemsControl;
                    TreeViewItem childFound = null;
                    if (childContainer != null)
                    {
                        bool expanded = (childContainer as TreeViewItem).IsExpanded;
                        (childContainer as TreeViewItem).IsExpanded = true;
                        childFound = GetContainerFromItem(childContainer, item);
                        (childContainer as TreeViewItem).IsExpanded = childFound == null ? expanded : true;
                    }
                    if (childFound != null)
                    {
                        (childContainer as TreeViewItem).IsExpanded = true;
                        return childFound;
                    }
                        
                }
            }
            return found as TreeViewItem;
        }

        private void textBlockCharacter_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
