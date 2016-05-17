using ApplicationShell.Views;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Prism.Mvvm;
using Prism.Modularity;
using Prism.Events;
using Prism.Unity;
using System;
using Module.Shared.Error;
using Module.Shared.Enumerations;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.PopupService;
using Framework.WPF.Services.MessageBoxService;
using Module.HeroVirtualTabletop;
using System.Diagnostics;
using System.IO;
using ApplicationShell.Properties;
using ApplicationShell.Models.Navigation;
using Module.Shared;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Module.Shared.Messages;
using System.Reflection;
using System.Resources;
using Module.HeroVirtualTabletop.Library.Utility;
using AutoItX3Lib;

namespace ApplicationShell
{
    public class Bootstrapper : UnityBootstrapper
    {
        public IEventAggregator eventAggregator;

        Module.Shared.Logging.ILogManager logService = new Module.Shared.Logging.FileLogManager(typeof(Bootstrapper));

        /// <summary>
        /// Initialize the shell
        /// </summary>
        /// <returns> reference to shell</returns>

        protected override DependencyObject CreateShell()
        {
            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            System.AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            MainWindow shell = Container.Resolve<MainWindow>();
            Application.Current.MainWindow = shell;

            LaunchGame();

            shell.Dispatcher.BeginInvoke((Action)delegate   //This takes advantage of the fact that the Bootstrapper and ModuleLoader run on 
            {                                           //the UI thread and queues a delegate on the Dispatcher that will only 
                shell.Show();                           //get invoked when all the other stuff is done. 
                NavigationManager.Navigate(ModuleEnum.HeroVirtualTabletop);
            });

            return shell;
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
            input.Send("/bind_load_file required_keybinds.txt");
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
            while(true)
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

        void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {

        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            if (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            string message = ex.Message;
            ErrorItems errors = new ErrorItems();
            errors.Errors.Add(
                new ErrorItem
                {
                    ErrorDisplayType = ErrorDisplayType.Error,
                    FriendlyMessage = message,
                    Error = ex,
                });
            eventAggregator = Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<Module.Shared.Events.CommonEvents.ErrorOccuredEvent>().Publish(errors);
            Container.Resolve<IBusyService>().HideAllBusy();
            logService.Error(message);
            e.Handled = true;
        }

        protected override IModuleCatalog CreateModuleCatalog() 
        {
            ModuleCatalog moduleCatalog = new Prism.Modularity.ModuleCatalog();
            moduleCatalog.AddModule(typeof(HeroVirtualTabletopModule), InitializationMode.OnDemand);

            return moduleCatalog;
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            Uri iconUri = new Uri
           ("pack://application:,,,/Module.Shared;Component/Resources/Images/appIcon2.gif");// Not showing in the message. Need to investigate later
            //("Resources/Images/application.ico", UriKind.Relative);
            ImageSource icon = BitmapFrame.Create(iconUri);

            Container.RegisterType<HeroVirtualTabletopModule>(new ContainerControlledLifetimeManager());

            Container.RegisterType<IBusyService, BusyService>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IPopupService, PopupService>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IMessageBoxService, MessageBoxService>(new ContainerControlledLifetimeManager(), new InjectionConstructor(icon));
        }
    }
}
