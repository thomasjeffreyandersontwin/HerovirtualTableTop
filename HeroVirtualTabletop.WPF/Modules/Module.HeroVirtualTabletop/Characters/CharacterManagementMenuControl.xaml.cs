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

namespace Module.HeroVirtualTabletop.Characters
{
    /// <summary>
    /// Interaction logic for CharacterManagementMenuControl.xaml
    /// </summary>
    public partial class CharacterManagementMenuControl : UserControl
    {
        public CharacterManagementMenuControl()
        {
            InitializeComponent();
        }

        public ICommand SavePositionCommand
        {
            get
            {
                return (ICommand)GetValue(SavePositionCommandProperty);
            }
            set { SetValue(SavePositionCommandProperty, value); }
        }

        public static readonly DependencyProperty
            SavePositionCommandProperty =
            DependencyProperty.Register("SavePositionCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

        public ICommand SpawnCommand
        {
            get
            {
                return (ICommand)GetValue(SpawnCommandProperty);
            }
            set { SetValue(SpawnCommandProperty, value); }
        }

        public static readonly DependencyProperty
            SpawnCommandProperty =
            DependencyProperty.Register("SpawnCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

        public ICommand PlaceCommand
        {
            get
            {
                return (ICommand)GetValue(PlaceCommandProperty);
            }
            set { SetValue(PlaceCommandProperty, value); }
        }

        public static readonly DependencyProperty
            PlaceCommandProperty =
            DependencyProperty.Register("PlaceCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

        public ICommand ToggleTargetedCommand
        {
            get
            {
                return (ICommand)GetValue(ToggleTargetedCommandProperty);
            }
            set { SetValue(ToggleTargetedCommandProperty, value); }
        }

        public static readonly DependencyProperty
            ToggleTargetedCommandProperty =
            DependencyProperty.Register("ToggleTargetedCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

        public ICommand TargetAndFollowCommand
        {
            get
            {
                return (ICommand)GetValue(TargetAndFollowCommandProperty);
            }
            set { SetValue(TargetAndFollowCommandProperty, value); }
        }

        public static readonly DependencyProperty
            TargetAndFollowCommandProperty =
            DependencyProperty.Register("TargetAndFollowCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

        public ICommand MoveTargetToCameraCommand
        {
            get
            {
                return (ICommand)GetValue(MoveTargetToCameraCommandProperty);
            }
            set { SetValue(MoveTargetToCameraCommandProperty, value); }
        }

        public static readonly DependencyProperty
            MoveTargetToCameraCommandProperty =
            DependencyProperty.Register("MoveTargetToCameraCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

        public ICommand ManeuverWithCameraCommand
        {
            get
            {
                return (ICommand)GetValue(ManeuverWithCameraCommandProperty);
            }
            set { SetValue(ManeuverWithCameraCommandProperty, value); }
        }

        public static readonly DependencyProperty
            ManeuverWithCameraCommandProperty =
            DependencyProperty.Register("ManeuverWithCameraCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

        public ICommand ClearFromDesktopCommand
        {
            get
            {
                return (ICommand)GetValue(ClearFromDesktopCommandProperty);
            }
            set { SetValue(ClearFromDesktopCommandProperty, value); }
        }

        public static readonly DependencyProperty
            ClearFromDesktopCommandProperty =
            DependencyProperty.Register("ClearFromDesktopCommand",
            typeof(ICommand), typeof(CharacterManagementMenuControl),
            new PropertyMetadata(null));

    }
}
