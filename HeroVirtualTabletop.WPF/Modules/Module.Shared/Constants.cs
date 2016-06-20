using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared
{
    public sealed class Constants
    {
        #region Application
        public const string APPLICATION_NAME = "Hero Virtual";
        #endregion

        #region Regions
        public const string MAIN_REGION = "MainRegion";
        public const string BUSY_REGION = "BusyRegion";
        public const string NAVIGATION_BAR_REGION = "NavigationBarRegion";
        public const string HEROVIRTUALTABLETOP_REGION = "HeroVirtualTabletopRegion";
        #endregion

        #region Modules
        public const string HEROVIRTUALTABLETOP_MODULENAME = "HeroVirtualTabletopModule";
        #endregion

        #region Log
        public const string LOG_CONFIGURATION_FILENAME = "log4net.config";
        public const string LOG_FOLDERNAME = "..\\..\\Log";
        #endregion

        #region Resource Dictionary and Styles
        public const string RESOURCE_DICTIONARY_PATH = "/Module.Shared;Component/Resources/ResourceDictionary/GeneralResources.xaml";
        public const string CUSTOM_MODELESS_TRANSPARENT_WINDOW_STYLENAME = "CustomModelessTransparentWindow";
        #endregion

        #region Game Files and Directories
        public const string INVALID_GAME_DIRECTORY_MESSAGE = "Invalid Game Directory! Please provide proper Game Directory for City of Heroes.";
        public const string INVALID_DIRECTORY_CAPTION = "Invalid Directory";

        public const string GAME_EXE_FILENAME = "cityofheroes.exe";
        public const string GAME_ICON_EXE_FILENAME = "icon.exe";
        public const string GAME_PROCESSNAME = "cityofheroes";
        public const string GAME_DATA_FOLDERNAME = "data";
        public const string GAME_COSTUMES_FOLDERNAME = "costumes";
        public const string GAME_COSTUMES_EXT = ".costume";
        public const string GAME_CROWD_REPOSITORY_FILENAME = "CrowdRepo.data";
        public const string GAME_KEYBINDS_FILENAME = "required_keybinds.txt";
        public const string GAME_MODELS_FILENAME = "Models.txt";
        public const string GAME_DATA_BACKUP_FOLDERNAME = "Backup";
        public const string GAME_SOUND_FOLDERNAME = "sound";
        #endregion

        #region Character Explorer
        public const string DEFAULT_CHARACTER_NAME = "DEFAULT";
        public const string COMBAT_EFFECTS_CHARACTER_NAME = "COMBAT EFFECTS";
        public const string ALL_CHARACTER_CROWD_NAME = "All Characters";
        #endregion

        #region Roster Explorer
        public const string NO_CROWD_CROWD_NAME = "No Crowd";
        #endregion

        #region Default Abilities and Animations

        public const string STANDUP_ABILITY_NAME = "Stand Up";
        public const string HIT_ABITIY_NAME = "Hit";
        public const string DODGE_ABILITY_NAME = "Dodge";
        public const string STUNNED_ABITIY_NAME = "Stunned";
        public const string UNCONCIOUS_ABITIY_NAME = "Unconcious";
        public const string DYING_ABILITY_NAME = "Dying";
        public const string DEAD_ABITIY_NAME = "Dead";
        public const string MISS_ABITIY_NAME = "Miss";

        #endregion
    }
}
