using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private bool isSingleClick = false;
        private bool isDoubleClick = false;
        private bool isTripleClick = false;
        private bool isQuadrupleClick = false;
        private int milliseconds = 0;
        private int maxClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime * 4;
        private System.Windows.Forms.Timer clickTimer = new System.Windows.Forms.Timer();
        public RosterExplorerView(RosterExplorerViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;

            this.RosterViewListBox.SelectAll();
            this.RosterViewListBox.UnselectAll();

            clickTimer.Interval = 50;
            clickTimer.Tick +=
                new EventHandler(clickTimer_Tick);

            this.viewModel.RosterMemberAdded += this.viewModel_RosterMemberAdded;
        }
        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.LeftButton == MouseButtonState.Pressed)
            {
                var groupbox = Helper.GetTemplateAncestorByType(e.OriginalSource as TextBlock, typeof(GroupBox));
                var itemsPres = Helper.GetDescendantByType(groupbox, typeof(ItemsPresenter)) as ItemsPresenter;
                var vStackPanel = VisualTreeHelper.GetChild(itemsPres as DependencyObject, 0) as VirtualizingStackPanel;
                foreach (ListBoxItem item in vStackPanel.Children)
                {
                    item.IsSelected = true;
                }
                e.Handled = true;
            }
        }
        
        private void ListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                isSingleClick = true;
                // Start the click timer.
                clickTimer.Start();
            }
            // This is the second mouse click.
            else if (e.ClickCount == 2)
            {
                isDoubleClick = true;
            }
            else if (e.ClickCount == 3)
            {
                isTripleClick = true;
            }
            else if (e.ClickCount == 4)
            {
                isQuadrupleClick = true;
            }
            
        }

        void clickTimer_Tick(object sender, EventArgs e)
        {
            milliseconds += 50;

            if (milliseconds >= maxClickTime)
            {
                clickTimer.Stop();

                if (isQuadrupleClick)
                {
                    this.viewModel.ActivateCharacterCommand.Execute(null);
                }
                else if (isTripleClick)
                {
                    this.viewModel.ToggleManeuverWithCamera();
                }
                else if (isDoubleClick)
                {
                    //this.viewModel.TargetAndFollow();
                    this.viewModel.RosterMouseDoubleClicked = true;
                    this.viewModel.PlayDefaultAbility();
                }
                else
                {
                    //this.viewModel.TargetOrFollow();
                }

                isSingleClick = isDoubleClick = isTripleClick = isQuadrupleClick = false;
                milliseconds = 0;
            }
        }

        private void RosterViewListBox_Drop(object sender, DragEventArgs e)
        {
            this.viewModel.RaiseEventToImportRosterMember();
        }

        private void RosterViewListBox_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            } 
        }

        
        private void viewModel_RosterMemberAdded(object sender, EventArgs e)
        {
            CollectionViewSource source = (CollectionViewSource)(this.Resources["ParticipantsView"]);
            ListCollectionView view = (ListCollectionView)source.View;
            if (view != null && view.Groups != null && view.Groups.Count > 1)
            {
                //view.Refresh();
            }
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
