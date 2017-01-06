using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Xna.Framework;
using Module.Shared;
using Module.Shared.Enumerations;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.GameCommunicator;

namespace Module.HeroVirtualTabletop.Desktop
{

    class DesktopContextMenu
    {
        public static FileSystemWatcher ContextCommandFileWatcher;
        public CrowdMemberModel Character=null;
        public bool IsDisplayed=false;
        public bool IsPlayingAreaEffect=false;

        private RosterExplorerViewModel _viewModel;
        public DesktopContextMenu(RosterExplorerViewModelRF viewModel) {
            _viewModel = viewModel;
            if (ContextCommandFileWatcher == null)
            {
                ContextCommandFileWatcher = new FileSystemWatcher();
                ContextCommandFileWatcher.Path = string.Format("{0}\\", Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME));
                ContextCommandFileWatcher.IncludeSubdirectories = false;
                ContextCommandFileWatcher.Filter = "*.txt";
                ContextCommandFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                ContextCommandFileWatcher.Changed += LaunchMethodBasedOnFileName;
            }
            DesktopContextMenu.ContextCommandFileWatcher.EnableRaisingEvents = false;
            CreateBindSaveFilesForContextCommands();
            ContextCommandFileWatcher.EnableRaisingEvents = true;
        }
    
        public void GenerateAndDisplay(CrowdMemberModel character)
        {
            Character = character;
            GenerateAndDisplay();
            ContextCommandFileWatcher.EnableRaisingEvents = true;
        }

        public void GenerateMenu()
        {
            CrowdMemberModel character = Character;
            string fileCharacterMenu = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_TEXTS_FOLDERNAME, Constants.GAME_LANGUAGE_FOLDERNAME, Constants.GAME_MENUS_FOLDERNAME, Constants.GAME_CHARACTER_MENU_FILENAME);
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "Module.HeroVirtualTabletop.Resources.character.mnu";
            List<string> menuFileLines = new List<string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    menuFileLines.Add(line);
                }

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < menuFileLines.Count - 1; i++)
                {
                    sb.AppendLine(menuFileLines[i]);
                }
                if (character.OptionGroups != null && character.OptionGroups.Count > 0)
                {
                    foreach (var optionGroup in character.OptionGroups)
                    {
                        sb.AppendLine(string.Format("Menu \"{0}\"", optionGroup.Name));
                        sb.AppendLine("{");
                        foreach (ICharacterOption option in optionGroup.Options)
                        {
                            string whiteSpaceReplacedOptionGroupName = optionGroup.Name.Replace(" ", Constants.SPACE_REPLACEMENT_CHARACTER);
                            string whiteSpaceReplacedOptionName = option.Name.Replace(" ", Constants.SPACE_REPLACEMENT_CHARACTER);
                            sb.AppendLine(string.Format("Option \"{0}\" \"bind_save_file {1}{2}{3}.txt\"", option.Name, whiteSpaceReplacedOptionGroupName, Constants.DEFAULT_DELIMITING_CHARACTER, whiteSpaceReplacedOptionName));
                        }
                        sb.AppendLine("}");
                    }
                }
                sb.AppendLine(menuFileLines[menuFileLines.Count - 1]);

                File.WriteAllText(
                    fileCharacterMenu, sb.ToString()
                    );
            }
        }
        public void DisplayMenu()
        {
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.PopMenu, "character");
            keyBindsGenerator.CompleteEvent();

        }

        public void GenerateAndDisplay()
        {

            if (Character != null)
            {
                
                if (IsPlayingAreaEffect)
                {
                    if (_viewModel.AttackingCharacter != null && _viewModel.AttackingCharacter.Name != Character.Name)
                    {
                        _viewModel.AddDesktopTargetToRosterSelection(Character);
                        DisplayAreaEffectMenu();
                        IsDisplayed = true;
                    }
                }
                else
                {
                    _viewModel.AddDesktopTargetToRosterSelection(Character);
                    GenerateMenu();
                    DisplayMenu();
                    IsDisplayed = true;
                }
            }
        }

        private void DisplayAreaEffectMenu()
        {
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            Character.Target();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.PopMenu, "areaattack");  //refactor
            keyBindsGenerator.CompleteEvent();
        }

        private void LaunchMethodBasedOnFileName(object sender, FileSystemEventArgs e)
        {
            Action action = delegate ()
            {
                IsDisplayed = false;
                if (IsPlayingAreaEffect)
                {

                    if (e.Name == Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_FILENAME)
                    {
                        _viewModel.TargetCharacterForAreaAttack(null);
                    }
                    else if (e.Name == Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_EXECUTE_FILENAME)
                    {
                        ContextCommandFileWatcher.EnableRaisingEvents = false;
                        _viewModel.TargetAndExecuteAreaAttack(null);
                    }
                }
                else
                {
                    ContextCommandFileWatcher.EnableRaisingEvents = false;
                    switch (e.Name)
                    {
                        case Constants.GAME_CHARACTER_BINDSAVE_SPAWN_FILENAME:
                            _viewModel.Spawn();
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_PLACE_FILENAME:
                            _viewModel.Place();
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_SAVEPOSITION_FILENAME:
                            _viewModel.SavePosition();
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_MOVECAMERATOTARGET_FILENAME:
                            _viewModel.TargetAndFollow(true);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_MOVETARGETTOCAMERA_FILENAME:
                            _viewModel.MoveTargetToCamera();
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_RESETORIENTATION_FILENAME:
                            _viewModel.ResetOrientation();
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_MANUEVERWITHCAMERA_FILENAME:
                            _viewModel.ToggleManeuverWithCamera();
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_ACTIVATE_FILENAME:
                            _viewModel.ToggleActivateCharacter(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_CLEARFROMDESKTOP_FILENAME:
                            _viewModel.ClearFromDesktop();
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_CLONEANDLINK_FILENAME:
                            {
                                Character character = _viewModel.SelectedParticipants != null && _viewModel.SelectedParticipants.Count == 1 ? _viewModel.SelectedParticipants[0] as Character : null;
                                _viewModel.EventAggregator.GetEvent<CloneLinkCrowdMemberEvent>().Publish(character as CrowdMemberModel);
                                break;
                            }
                        default:
                            {
                                if (e.Name.StartsWith(Constants.GAME_CHARACTER_BINDSAVE_MOVETARGETTOCHARACTER_FILENAME))
                                {
                                    int index = e.Name.IndexOf(Constants.DEFAULT_DELIMITING_CHARACTER);
                                    if (index > 0)
                                    {
                                        string whiteSpceReplacedCharacterName = e.Name.Substring(index + 1, e.Name.Length - index - 5); // to get rid of the .txt part
                                        string characterName = whiteSpceReplacedCharacterName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                        Character character = _viewModel.Participants.FirstOrDefault(p => p.Name == characterName) as Character;
                                        if (character != null)
                                        {
                                            Vector3 destination = new Vector3(character.Position.X, character.Position.Y, character.Position.Z);
                                            foreach (Character c in _viewModel.SelectedParticipants)
                                            {
                                                c.MoveToLocation(destination);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Character character = _viewModel.SelectedParticipants != null && _viewModel.SelectedParticipants.Count == 1 ? _viewModel.SelectedParticipants[0] as Character : null;
                                    int index = e.Name.IndexOf(Constants.DEFAULT_DELIMITING_CHARACTER);
                                    if (index > 0 && character != null)
                                    {
                                        string whiteSpaceReplacedOptionGroupName = e.Name.Substring(0, index - 1); // The special characters are translated to two characters, so need to subtract one additional character
                                        string whiteSpceReplacedOptionName = e.Name.Substring(index + 1, e.Name.Length - index - 5); // to get rid of the .txt part
                                        string optionGroupName = whiteSpaceReplacedOptionGroupName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                        string optionName = whiteSpceReplacedOptionName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                        _viewModel.ToggleActivateCharacter(character, optionGroupName, optionName);
                                    }
                                }

                                break;
                            }
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
            IsDisplayed = false;
        }

        public void CreateBindSaveFilesForContextCommands()
        {
            string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_FILENAME);
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_EXECUTE_FILENAME);
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
        }
    }
}
