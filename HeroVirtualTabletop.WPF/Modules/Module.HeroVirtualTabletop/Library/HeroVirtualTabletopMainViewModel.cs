using Framework.WPF.Library;
using Framework.WPF.Library.Enumerations;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Properties;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Resources;

namespace Module.HeroVirtualTabletop.Library
{
    public class HeroVirtualTabletopMainViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        Module.Shared.Logging.ILogManager logService = new Module.Shared.Logging.FileLogManager(typeof(HeroVirtualTabletopMainViewModel));

        #endregion

        #region Events

        #endregion

        #region Public Properties

        public IPopupService PopupService
        {
            get { return this.Container.Resolve<IPopupService>(); }
        }

        #endregion

        #region Commands

        #endregion

        #region Constructor

        public HeroVirtualTabletopMainViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator) 
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;

            LaunchGame();

            LoadModelsFile();
            //logService.Info("Models loaded");

            LoadCostumeFiles();
            //logService.Info("Costumes loaded");

            LoadSoundFiles();
            //logService.Info("Sounds loaded");

            ClearTempFilesFromDataFolder();

            //LoadSoundFiles();

            // Load camera on start
            new Camera().Render();
            //logService.Info("Camera rendered");

            LoadMainView();
            //logService.Info("MainView displayed");

            this.eventAggregator.GetEvent<ActivateCharacterEvent>().Subscribe(this.LoadActiveCharacterWidget);
            this.eventAggregator.GetEvent<DeactivateCharacterEvent>().Subscribe(this.CloseActiveCharacterWidget);
            this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Subscribe(this.ConfigureAttack);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.CloseActiveAttackWidget);
        }

        #endregion

        #region Methods

        private void LoadMainView()
        {
            System.Windows.Style style = Helper.GetCustomWindowStyle();
            double minheight = 400, minwidth = 80;
            style.Setters.Add(new Setter(Window.MinHeightProperty, minheight));
            style.Setters.Add(new Setter(Window.MinWidthProperty, minwidth));
            CharacterCrowdMainViewModel characterCrowdMainViewModel = this.Container.Resolve<CharacterCrowdMainViewModel>();
            PopupService.ShowDialog("CharacterCrowdMainView", characterCrowdMainViewModel, "", false, ReleaseAllSoundResource, new SolidColorBrush(Colors.Transparent), style);
        }

        private void ReleaseAllSoundResource(CancelEventArgs e)
        {
            this.eventAggregator.GetEvent<StopAllActiveAbilitiesEvent>().Publish(null);
        }
        
        private void LoadActiveCharacterWidget(Tuple<Character, string, string> tuple)
        {
            Character character = tuple.Item1;
            string optionGroupName = tuple.Item2;
            string optionName = tuple.Item3;
            if (character != null && PopupService.IsOpen("ActiveCharacterWidgetView") == false)
            {
                System.Windows.Style style = Helper.GetCustomWindowStyle();
                double minwidth = 80;
                style.Setters.Add(new Setter(Window.MinWidthProperty, minwidth));
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                double left = desktopWorkingArea.Right - 500;
                double top = desktopWorkingArea.Bottom - 80 * character.OptionGroups.Count;
                object savedPos = PopupService.GetPosition("ActiveCharacterWidgetView", character.Name);
                if(savedPos != null)
                {
                    double[] posArray = (double[])savedPos;
                    left = posArray[0];
                    top = posArray[1];
                }
                style.Setters.Add(new Setter(Window.LeftProperty, left));
                style.Setters.Add(new Setter(Window.TopProperty, top));
                ActiveCharacterWidgetViewModel viewModel = this.Container.Resolve<ActiveCharacterWidgetViewModel>();
                PopupService.ShowDialog("ActiveCharacterWidgetView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style, WindowStartupLocation.Manual);
                this.eventAggregator.GetEvent<ActivateCharacterEvent>().Publish(tuple);
            }
            else if (character == null && PopupService.IsOpen("ActiveCharacterWidgetView"))
            {
                this.CloseActiveCharacterWidget(null);
            }
        }

        private void ConfigureAttack(Tuple<List<Character>, Attack> tuple)
        {
            List<Character> defendingCharacters = tuple.Item1;
            if (defendingCharacters != null && defendingCharacters.Count > 0)
            {
                this.LoadActiveAttackWidget(tuple);
                IntPtr foregroundWindow = WindowsUtilities.FindWindow(null, "MainWindow");
                WindowsUtilities.SetForegroundWindow(foregroundWindow);
            }
            else // blank target
            {
                ActiveAttackConfiguration config = new ActiveAttackConfiguration();
                config.AttackMode = AttackMode.None;
                config.AttackResult = AttackResultOption.Miss;
                this.eventAggregator.GetEvent<SetActiveAttackEvent>().Publish(new Tuple<List<Character>, Attack>(tuple.Item1, tuple.Item2));
            }
        }
        private void LoadActiveAttackWidget(Tuple<List<Character>, Attack> tuple)
        {
            if (tuple.Item1 != null && tuple.Item2 != null && PopupService.IsOpen("ActiveAttackView") == false)
            {
                System.Windows.Style style = Helper.GetCustomWindowStyle();
                ActiveAttackViewModel viewModel = this.Container.Resolve<ActiveAttackViewModel>();
                var position = System.Windows.Forms.Cursor.Position;
                Mouse.OverrideCursor = Cursors.Arrow; 
                PopupService.ShowDialog("ActiveAttackView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style);
                this.eventAggregator.GetEvent<ConfigureActiveAttackEvent>().Publish(tuple);
            }
            else if ((tuple.Item1 == null || tuple.Item2 == null) && PopupService.IsOpen("ActiveAttackView"))
            {
                PopupService.CloseDialog("ActiveAttackView");
            }
        }

        private void CloseActiveAttackWidget(object state)
        {
            PopupService.CloseDialog("ActiveAttackView");
        }

        private void CloseActiveCharacterWidget(object state)
        {
            Character target = state as Character;
            PopupService.SavePosition("ActiveCharacterWidgetView", target != null ? target.Name : null);
            PopupService.CloseDialog("ActiveCharacterWidgetView");
        }
        
        private void LaunchGame()
        {
            bool directoryExists = CheckGameDirectory();
            if (!directoryExists)
                SetGameDirectory();
            //logService.Info("Launching game...");
            IconInteractionUtility.RunCOHAndLoadDLL(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
            //logService.Info("Game launched");
            LoadRequiredKeybinds();
            //logService.Info("Keybinds loaded");
            CreateCameraFilesIfNotExists();
            CreateAreaAttackPopupMenuIfNotExists();
        }

        private void LoadRequiredKeybinds()
        {
            CheckRequiredKeybindsFileExists();

            IconInteractionUtility.ExecuteCmd("bind_load_file required_keybinds.txt");
        }

        private bool CheckGameDirectory()
        {
            bool directoryExists = false;
            string gameDir = Module.Shared.Settings.Default.CityOfHeroesGameDirectory;
            if (!string.IsNullOrEmpty(gameDir) && Directory.Exists(gameDir) && File.Exists(Path.Combine(gameDir, Constants.GAME_EXE_FILENAME)))
            {
                directoryExists = true;
            }
            return directoryExists;
        }

        private void SetGameDirectory()
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowNewFolderButton = false;
            dialog.Description = Messages.SELECT_GAME_DIRECTORY_MESSAGE;
            while (true)
            {
                System.Windows.Forms.DialogResult dr = dialog.ShowDialog();
                if (dr == System.Windows.Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
                {
                    if (File.Exists(Path.Combine(dialog.SelectedPath, Constants.GAME_EXE_FILENAME)))
                    {
                        Module.Shared.Settings.Default.CityOfHeroesGameDirectory = dialog.SelectedPath;
                        Module.Shared.Settings.Default.Save();
                        break;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(Constants.INVALID_GAME_DIRECTORY_MESSAGE, Constants.INVALID_DIRECTORY_CAPTION, MessageBoxButton.OK);
                    }
                }
            }

        }

        private void CheckRequiredKeybindsFileExists()
        {
            bool directoryExists = CheckGameDirectory();
            if (!directoryExists)
                SetGameDirectory();

            string dataDir = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_KEYBINDS_FILENAME);
            if (!File.Exists(filePath))
            {
                ExtractRequiredKeybindsFile();
            }
        }

        private void ExtractRequiredKeybindsFile()
        {
            File.AppendAllText(
                Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_KEYBINDS_FILENAME),
                Resources.required_keybinds
                );
        }

        private void LoadModelsFile()
        {
            string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_MODELS_FILENAME);
            if (!File.Exists(filePath))
            {
                File.AppendAllText(
                filePath, Resources.Models
                );
            }
        }

        private void LoadSoundFiles()
        {
            string folderPath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_SOUND_FOLDERNAME);
            if (Directory.Exists(folderPath)) 
            {
                return;
            }
            else
            {
                //var resourceName = "Module.HeroVirtualTabletop.Resources.sound.zip";
                //var assembly = Assembly.GetExecutingAssembly();
                //using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                //using (StreamReader reader = new StreamReader(stream))
                //{
                //    ZipArchive archive = new ZipArchive(reader.BaseStream);
                //    archive.ExtractToDirectory(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
                //}
            }
        }

        private void LoadCostumeFiles()
        {
            string folderPath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            if (Directory.Exists(folderPath))
            {
                return;
            }
            else
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.costumes.zip";
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    ZipArchive archive = new ZipArchive(reader.BaseStream);
                    archive.ExtractToDirectory(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
                }

                
            }
        }

        private void CreateAreaAttackPopupMenuIfNotExists()
        {
            string dirTexts = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_TEXTS_FOLDERNAME);
            if (!Directory.Exists(dirTexts))
                Directory.CreateDirectory(dirTexts);
            string dirLanguage = Path.Combine(dirTexts, Constants.GAME_LANGUAGE_FOLDERNAME);
            if (!Directory.Exists(dirLanguage))
                Directory.CreateDirectory(dirLanguage);
            string dirMenus = Path.Combine(dirLanguage, Constants.GAME_MENUS_FOLDERNAME);
            if (!Directory.Exists(dirMenus))
                Directory.CreateDirectory(dirMenus);
            string fileAreaAttackMenu = Path.Combine(dirMenus, Constants.GAME_AREAATTACK_MENU_FILENAME);
            var assembly = Assembly.GetExecutingAssembly();
            
            if (!File.Exists(fileAreaAttackMenu))
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.areaattack.mnu";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.AppendAllText(
                        fileAreaAttackMenu, result
                        );
                }
            }
        }

        private void CreateCameraFilesIfNotExists()
        {
            string dirData = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);

            string enableCameraFile = Path.Combine(dirData, Constants.GAME_ENABLE_CAMERA_FILENAME);
            string disableCameraFile = Path.Combine(dirData, Constants.GAME_DISABLE_CAMERA_FILENAME);

            var assembly = Assembly.GetExecutingAssembly();

            if (!File.Exists(enableCameraFile))
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.enable_camera.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.AppendAllText(
                        enableCameraFile, result
                        );
                }
            }

            if (!File.Exists(disableCameraFile))
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.disable_camera.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.AppendAllText(
                        disableCameraFile, result
                        );
                }
            }
        }

        private void ClearTempFilesFromDataFolder()
        {
            string dirPath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);
            System.IO.DirectoryInfo di = new DirectoryInfo(dirPath);

            foreach (FileInfo file in di.GetFiles())
            {
                if(file.Name.Contains(Constants.DEFAULT_DELIMITING_CHARACTER_TRANSLATION) || file.Name.Contains(Constants.DEFAULT_DELIMITING_CHARACTER_TRANSLATION))
                {
                    file.Delete();
                }
            }
        }

        #endregion
    }
}
