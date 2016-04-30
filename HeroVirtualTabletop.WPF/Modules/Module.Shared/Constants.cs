using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared
{
    public sealed class Constants
    {
        public const string APPLICATION_NAME = "Hero Virtual";

        public const string MAIN_REGION = "MainRegion";
        public const string BUSY_REGION = "BusyRegion";
        public const string NAVIGATION_BAR_REGION = "NavigationBarRegion";
        public const string HEROVIRTUALTABLETOP_REGION = "HeroVirtualTabletopRegion";

        public const string HEROVIRTUALTABLETOP_MODULENAME = "HeroVirtualTabletopModule";

        public const string LOG_CONFIGURATION_FILENAME = "log4net.config";
        public const string LOG_FOLDERNAME = "..\\..\\Log";

        public const string RESOURCE_DICTIONARY_PATH = "/Module.Shared;Component/Resources/ResourceDictionary/GeneralResources.xaml";
        public const string CUSTOM_MODELESS_TRANSPARENT_WINDOW_STYLENAME = "CustomModelessTransparentWindow";

        public const string INVALID_GAME_DIRECTORY_MESSAGE = "Invalid Game Directory! Please provide proper Game Directory for City of Heroes.";
        public const string INVALID_DIRECTORY_CAPTION = "Invalid Directory";

        public const string GAME_EXE_FILENAME = "cityofheroes.exe";
        public const string GAME_ICON_EXE_FILENAME = "icon.exe";
        public const string GAME_PROCESSNAME = "cityofheroes";
        public const string GAME_DATA_FOLDERNAME = "data";
        public const string GAME_CROWD_REPOSITORY_FILENAME = "CrowdRepoTest.data";
    }
}
