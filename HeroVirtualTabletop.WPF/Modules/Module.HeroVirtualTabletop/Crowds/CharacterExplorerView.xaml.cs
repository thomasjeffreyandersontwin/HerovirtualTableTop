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
using Module.Shared.Events;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.HeroVirtualTabletop.Crowds
{
    /// <summary>
    /// Interaction logic for CharacterExplorerView.xaml
    /// </summary>
    public partial class CharacterExplorerView : UserControl
    {
        private CharacterExplorerViewModel viewModel;
        private CrowdModel selectedCrowdRoot;
        public CharacterExplorerView(CharacterExplorerViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.EditModeEnter += viewModel_EditModeEnter;
            this.viewModel.EditModeLeave += viewModel_EditModeLeave;
            this.viewModel.EditNeeded += viewModel_EditNeeded;
            this.viewModel.ExpansionUpdateNeeded+=viewModel_ExpansionUpdateNeeded;
        }
        private void viewModel_EditNeeded(object sender, EventArgs e)
        {
            ICrowdMemberModel modelToSelect = sender as ICrowdMemberModel;
            if(sender == null) // need to unselect
            {
                DependencyObject dObject = treeViewCrowd.GetItemFromSelectedObject(treeViewCrowd.SelectedItem);
                TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    this.selectedCrowdRoot = null;
                }
            }
            else
            {
                bool itemFound = false;
                TextBox txtBox = null;
                treeViewCrowd.UpdateLayout();
                if (this.viewModel.SelectedCrowdModel == null)
                {
                    var allCharsCrowd = this.viewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
                    if(allCharsCrowd != null)
                    {
                        TreeViewItem firstItem = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(allCharsCrowd) as TreeViewItem;
                        if (firstItem != null)
                        {
                            firstItem.IsSelected = true;
                            this.viewModel.SelectedCrowdModel = firstItem.DataContext as CrowdModel;
                        }
                    }
                }
                if (sender is CrowdModel)
                {
                    if (this.viewModel.SelectedCrowdModel == null || this.viewModel.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME) // A new crowd has been added to the collection
                    {
                        for (int i = 0; i < treeViewCrowd.Items.Count; i++) 
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
                                    this.viewModel.SelectedCrowdModel = model as CrowdModel;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!itemFound)
                {
                    DependencyObject dObject = null;
                    if(this.selectedCrowdRoot != null && this.viewModel.SelectedCrowdModel != null)
                    {
                        TreeViewItem item = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(this.selectedCrowdRoot) as TreeViewItem;
                        dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, this.viewModel.SelectedCrowdModel.Name);
                    }
                    else
                        dObject = treeViewCrowd.GetItemFromSelectedObject(this.viewModel.SelectedCrowdModel);
                    TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                    if (tvi != null)
                    {
                        ICrowdMemberModel model = tvi.DataContext as ICrowdMemberModel;
                        if (tvi.Items != null)
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
                                        if(model is CrowdModel)
                                        {
                                            this.viewModel.SelectedCrowdModel = model as CrowdModel;
                                            this.viewModel.SelectedCrowdParent = tvi.DataContext as CrowdModel;
                                        }
                                        else
                                        {
                                            this.viewModel.SelectedCrowdMemberModel = model as CrowdMemberModel;
                                            this.viewModel.SelectedCrowdModel = tvi.DataContext as CrowdModel;
                                        }
                                        if(this.selectedCrowdRoot == null)
                                            this.selectedCrowdRoot = tvi.DataContext as CrowdModel;
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
                TreeViewItem item = GetRootTreeViewItemParent(treeViewItem);
                if (item != null)
                    this.selectedCrowdRoot = item.DataContext as CrowdModel;
                else
                    this.selectedCrowdRoot = null;
                if (treeViewItem.DataContext is CrowdModel)
                {
                    treeViewItem = GetImmediateTreeViewItemParent(treeViewItem);
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

        private TreeViewItem GetImmediateTreeViewItemParent(TreeViewItem treeViewItem)
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
            return treeViewItem;
        }

        private TreeViewItem GetRootTreeViewItemParent(TreeViewItem treeViewItem)
        {
            DependencyObject dObject = VisualTreeHelper.GetParent(treeViewItem); // got the immediate parent
            treeViewItem = dObject as TreeViewItem; // now get first treeview item parent
            while (true)
            {
                dObject = VisualTreeHelper.GetParent(dObject);
                if(dObject is TreeViewItem)
                    treeViewItem = dObject as TreeViewItem;
                else if (dObject is TreeView)
                    break;
            }
            return treeViewItem;
        }

        private void treeViewCrowd_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                TreeViewItem item = GetRootTreeViewItemParent(treeViewItem);
                if (item != null)
                    this.selectedCrowdRoot = item.DataContext as CrowdModel;
                else
                    this.selectedCrowdRoot = null;
                if (treeViewItem.DataContext is CrowdModel)
                {
                    treeViewItem = GetImmediateTreeViewItemParent(treeViewItem);
                    if (treeViewItem != null)
                        this.viewModel.SelectedCrowdParent = treeViewItem.DataContext as CrowdModel;
                    else
                        this.viewModel.SelectedCrowdParent = null;

                }
                else
                    this.viewModel.SelectedCrowdParent = null;
            }
        }

        private TreeViewItem FindTreeViewItemUnderTreeViewItemByModelName(TreeViewItem tvi, string modelName)
        {
            TreeViewItem treeViewItemRet = null;
            if (tvi != null)
            {
                ICrowdMemberModel model = tvi.DataContext as ICrowdMemberModel;
                if (model.Name == modelName)
                {
                    return tvi;
                }
                else if (tvi.Items != null)
                {
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        var treeViewItem = FindTreeViewItemUnderTreeViewItemByModelName(item, modelName);
                        if (treeViewItem != null)
                        {
                            treeViewItemRet = treeViewItem;
                            break;
                        }
                    }
                }
            }
            return treeViewItemRet;
        }
        #region TreeView Expansion Management

        private void viewModel_ExpansionUpdateNeeded(object sender, CustomEventArgs<ExpansionUpdateEvent> e)
        {
            CrowdModel crowdModel = sender as CrowdModel;
            DependencyObject dObject = null;
            ExpansionUpdateEvent updateEvent = e.Value;
            if(updateEvent == ExpansionUpdateEvent.Filter)
            {
                ExpandMatchedNode(sender);
            }
            else
            {
                if (this.selectedCrowdRoot != null && crowdModel != null)
                {
                    TreeViewItem item = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(this.selectedCrowdRoot) as TreeViewItem;
                    dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, crowdModel.Name);
                    if (dObject == null)
                        dObject = treeViewCrowd.GetItemFromSelectedObject(crowdModel);
                }
                else
                    dObject = treeViewCrowd.GetItemFromSelectedObject(crowdModel);
                TreeViewItem tvi = dObject as TreeViewItem; 
                if (tvi != null)
                {
                    ICrowdMemberModel model = tvi.DataContext as ICrowdMemberModel;
                    if (tvi.Items != null && tvi.Items.Count > 0)
                    {
                        if (updateEvent != ExpansionUpdateEvent.Delete)
                            tvi.IsExpanded = true;
                        else
                            UpdateExpansions(tvi);
                    }
                    else
                        tvi.IsExpanded = false;
                }
            }
        }
        /// <summary>
        /// This will recursively make the nodes unexpanded if there are no children in it. Otherwise it will hold the current state
        /// </summary>
        /// <param name="tvi"></param>
        private void UpdateExpansions(TreeViewItem tvi)
        {
            if(tvi != null)
            {
                if (tvi.Items != null && tvi.Items.Count > 0)
                {
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        UpdateExpansions(item);
                    }
                }
                else
                    tvi.IsExpanded = false;
            }
        }

        /// <summary>
        /// This will expand a matched item and its matched children
        /// </summary>
        /// <param name="sender"></param>
        private void ExpandMatchedNode(object sender)
        {
            CrowdModel crowdModel = sender as CrowdModel;
            if(crowdModel.IsMatched)
            {
                DependencyObject dObject = treeViewCrowd.GetItemFromSelectedObject(crowdModel);
                TreeViewItem tvi = dObject as TreeViewItem; 
                if (tvi != null)
                {
                    tvi.IsExpanded = true;
                    ExpandMatchedItems(tvi);
                }
            }
        }
        /// <summary>
        /// This will recursively expand a matched item and its matched children
        /// </summary>
        /// <param name="tvi"></param>
        private void ExpandMatchedItems(TreeViewItem tvi)
        {
            if (tvi != null)
            {
                tvi.UpdateLayout();
                if (tvi.Items != null && tvi.Items.Count > 0)
                {
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        if (item != null)
                        {
                            CrowdModel model = item.DataContext as CrowdModel;
                            if (model != null && model.IsMatched)
                            {
                                item.IsExpanded = true;
                                ExpandMatchedItems(item);
                            }
                            else
                                item.IsExpanded = false; 
                        }
                    }
                }
            }
        }
        #endregion
    }
}
