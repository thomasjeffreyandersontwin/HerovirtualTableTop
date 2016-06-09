using AutoItX3Lib;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
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
using System.Windows.Media;
using System.Windows.Resources;

namespace Module.HeroVirtualTabletop.Library
{
    public class HeroVirtualTabletopMainViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;

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

            //LoadSoundFiles();

            // Load camera on start
            new Camera().Render();

            LoadMainView();

            this.eventAggregator.GetEvent<ActivateCharacterEvent>().Subscribe(this.LoadActiveCharacterWidget);
            
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
            this.eventAggregator.GetEvent<StopAllActiveAbilities>().Publish(null);
        }
        
        private void LoadActiveCharacterWidget(Character character)
        {
            if (character != null && PopupService.IsOpen("ActiveCharacterWidgetView") == false)
            {
                System.Windows.Style style = Helper.GetCustomWindowStyle();
                double minwidth = 80;
                style.Setters.Add(new Setter(Window.MinWidthProperty, minwidth));
                ActiveCharacterWidgetViewModel viewModel = this.Container.Resolve<ActiveCharacterWidgetViewModel>();
                PopupService.ShowDialog("ActiveCharacterWidgetView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style);
                this.eventAggregator.GetEvent<ActivateCharacterEvent>().Publish(character);
            }
            else if (character == null && PopupService.IsOpen("ActiveCharacterWidgetView"))
            {
                PopupService.CloseDialog("ActiveCharacterWidgetView");
            }
        }
        
        private void LaunchGame()
        {
            bool directoryExists = CheckGameDirectory();
            if (!directoryExists)
                SetGameDirectory();

            Process[] Processes = Process.GetProcessesByName(Constants.GAME_PROCESSNAME);
            if (Processes.Length == 0)
            {
                string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_ICON_EXE_FILENAME);
                if (File.Exists(filePath))
                {
                    Process.Start(filePath, "-r");
                    // Need to automate the following process
                    var x = MessageBox.Show("Please wait for COH to initialize and close this message");
                }
            }
            LoadRequiredKeybinds();
        }

        private void LoadRequiredKeybinds()
        {
            CheckRequiredKeybindsFileExists();
            IntPtr hWnd = WindowsUtilities.FindWindow("CrypticWindow", null);

            if (IntPtr.Zero == hWnd) //Game is not running
            {
                return;
            }
            WindowsUtilities.SetForegroundWindow(hWnd);
            WindowsUtilities.SetActiveWindow(hWnd);
            WindowsUtilities.ShowWindow(hWnd, 3); // 3 = SW_SHOWMAXIMIZED

            System.Threading.Thread.Sleep(250);

            AutoItX3 input = new AutoItX3();

            input.Send("{ENTER}");
            System.Threading.Thread.Sleep(250);
            input.Send("/bind_load_file required_keybinds.txt");
            System.Threading.Thread.Sleep(250);
            input.Send("{ENTER}");
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
            if (Directory.Exists(folderPath)) //&& Directory.GetFiles(folderPath, "*.ogg", SearchOption.AllDirectories).Count() >= 6218)
            {
                return;
            }
            else
            {
                Uri uri = new Uri("/Resources/sound.zip", UriKind.Relative);
                StreamResourceInfo data = Application.GetContentStream(uri);

                ZipArchive archive = new ZipArchive(data.Stream);
                archive.ExtractToDirectory(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
            }
        }

        #endregion
    }
}
