using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CrowdFromModelsViewModel : BaseViewModel
    {
        #region Private Members
        private EventAggregator eventAggregator;
        CharacterExplorerViewModel charExpVM;
        private CrowdModel editedCrowd;
        private CrowdModel tmpCrowd;
        private ObservableCollection<string> models;
        private CollectionViewSource modelsCVS;
        private string filter;
        private IMessageBoxService messageBoxService;
        private Visibility visibility = Visibility.Collapsed;
        private IntPtr keyboardHookID;

        #endregion

        #region Events

        public event EventHandler EditModeEnter;
        public void OnEditModeEnter(object sender, EventArgs e)
        {
            if (EditModeEnter != null)
                EditModeEnter(sender, e);
        }

        public event EventHandler EditModeLeave;
        public void OnEditModeLeave(object sender, EventArgs e)
        {
            if (EditModeLeave != null)
                EditModeLeave(sender, e);
        }

        #endregion

        #region Public Properties

        public CrowdModel EditedCrowd
        {
            get
            {
                return editedCrowd;
            }
            private set
            {
                editedCrowd = value;
                OnPropertyChanged("EditedCrowd");
            }
        }

        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                ModelsCVS.View.Refresh();
                OnPropertyChanged("Filter");
            }
        }

        public ObservableCollection<string> Models
        {
            get
            {
                return models;
            }
            set
            {
                models = value;
                OnPropertyChanged("Models");
            }
        }

        public CollectionViewSource ModelsCVS
        {
            get
            {
                return modelsCVS;
            }
        }

        public Visibility Visibility
        {
            get
            {
                return visibility;
            }
            set
            {
                visibility = value;
                OnPropertyChanged("Visibility");
            }
        }

        private IList selectedModels;
        public IList SelectedModels
        {
            get
            {
                return selectedModels;
            }
            set
            {
                selectedModels = value;
                OnPropertyChanged("SelectedModels");
            }
        }

        public string OriginalName { get; set; }
        #endregion

        #region Commands

        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitCrowdRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }
        public DelegateCommand<object> SpawnModelsCommand { get; private set; }
        public DelegateCommand<object> SaveCrowdCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }

        #endregion

        #region Constructor
        public CrowdFromModelsViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator) 
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            CreateModelsViewSource();
            charExpVM = this.Container.Resolve<CharacterExplorerViewModel>();
            this.eventAggregator.GetEvent<CreateCrowdFromModelsEvent>().Subscribe(this.LoadCrowd);
            tmpCrowd = new CrowdModel();

            keyboardHookID = KeyBoardHook.SetHook(CrowdFromModelsKeyboardHook);
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadCrowd);
            this.SubmitCrowdRenameCommand = new DelegateCommand<object>(this.SubmitCrowdRename);
            this.EnterEditModeCommand = new DelegateCommand<object>(this.EnterEditMode);
            this.CancelEditModeCommand = new DelegateCommand<object>(this.CancelEditMode);
            this.SpawnModelsCommand = new DelegateCommand<object>(this.SpawnModels);
            this.SaveCrowdCommand = new DelegateCommand<object>(this.SaveCrowd);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(this.ClearFromDesktop);
        }
        
        #endregion

        #region Methods

        private void LoadCrowd(CrowdModel crowd)
        {
            Filter = null;
            this.EditedCrowd = crowd;
            this.Visibility = Visibility.Visible;
        }

        private void UnloadCrowd(object state)
        {
            this.EditedCrowd = null;
            this.Visibility = Visibility.Collapsed;
        }

        private void SubmitCrowdRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);
                

                bool duplicateName = false;
                if (updatedName != this.OriginalName)
                    duplicateName = charExpVM.CrowdCollection.ContainsKey(updatedName);

                if (!duplicateName)
                {
                    RenameCrowd(updatedName);
                    OnEditModeLeave(state, null);
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, Messages.DUPLICATE_NAME_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                    this.CancelEditMode(state);
                }
            }
        }

        private void RenameCrowd(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            EditedCrowd.Name = updatedName;
            charExpVM.CrowdCollection.UpdateKey(this.OriginalName, updatedName);
            OriginalName = null;
        }

        private void EnterEditMode(object state)
        {
            this.OriginalName = EditedCrowd.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelEditMode(object state)
        {
            EditedCrowd.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SpawnModels(object obj)
        {
            int x = 0;
            foreach (string model in SelectedModels)
            {
                if (tmpCrowd.CrowdMemberCollection.Any(m => m.Name == model))
                    continue;
                CrowdMemberModel tmpChar = charExpVM.GetNewCharacter(model, model, IdentityType.Model) as CrowdMemberModel;
                tmpCrowd.Add(tmpChar);
                tmpChar.Spawn();
                tmpChar.Position.X += x++*3;
            }
        }

        private void SaveCrowd(object obj)
        {
            ICrowdMemberModel[] tmp = tmpCrowd.CrowdMemberCollection.ToArray();
            foreach (CrowdMemberModel model in tmp)
            {
                model.Name = GetValidMemberName();
                EditedCrowd.Add(model);
                charExpVM.AllCharactersCrowd.Add(model);
            }
            //ClearFromDesktop(null);
        }

        private string GetValidMemberName()
        {
            return charExpVM.GetAppropriateCharacterName(EditedCrowd.Name + " Member");
        }

        private void ClearFromDesktop(object obj)
        {
            foreach (CrowdMemberModel member in tmpCrowd.CrowdMemberCollection)
            {
                member.ClearFromDesktop();
            }
            tmpCrowd.RemoveAll();
        }

        private void CreateModelsViewSource()
        {
            models = new ObservableCollection<string>(File.ReadAllLines(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_MODELS_FILENAME)).OrderBy(m => m, new StringValueComparer()));
            modelsCVS = new CollectionViewSource();
            modelsCVS.Source = Models;
            modelsCVS.View.Filter += stringsCVS_Filter;
        }
        
        private bool stringsCVS_Filter(object item)
        {
            if (string.IsNullOrWhiteSpace(Filter))
            {
                return true;
            }
            if (SelectedModels != null && SelectedModels.Contains(item))
            {
                return true;
            }

            string strItem = item as string;
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(strItem);
        }

        #region Keyboard Hook

        private IntPtr CrowdFromModelsKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && this.Visibility == System.Windows.Visibility.Visible && this.EditedCrowd != null)
            {
                KBDLLHOOKSTRUCT keyboardLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                System.Windows.Forms.Keys vkCode = (System.Windows.Forms.Keys)keyboardLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;
                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    var winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    var currentProcId = Process.GetCurrentProcess().Id;
                    if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null)
                        || currentProcId == wndProcId)
                    {
                        if ((inputKey == Key.S) && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                        {
                            if (this.SpawnModelsCommand.CanExecute(null))
                                this.SpawnModelsCommand.Execute(null);
                        }
                        else if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Windows))
                        {
                            if (this.ClearFromDesktopCommand.CanExecute(null))
                                this.ClearFromDesktopCommand.Execute(null);
                        }
                        else if (inputKey == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Windows))
                        {
                            if (this.SaveCrowdCommand.CanExecute(null))
                                this.SaveCrowdCommand.Execute(null);
                        }
                    }
                }
            }
            return KeyBoardHook.CallNextHookEx(keyboardHookID, nCode, wParam, lParam);
        }

        #endregion

        #endregion
    }
}
