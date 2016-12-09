using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Roster
{
    public class ActiveCharacterWidgetViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IntPtr hookID;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        private Character activeCharacter;
        public Character ActiveCharacter
        {
            get
            {
                return activeCharacter;
            }
            set
            {
                activeCharacter = value;
                OnPropertyChanged("ActiveCharacter");
            }
        }

        private ObservableCollection<IOptionGroupViewModel> optionGroups;
        public ObservableCollection<IOptionGroupViewModel> OptionGroups
        {
            get
            {
                return optionGroups;
            }
            private set
            {
                optionGroups = value;
                OnPropertyChanged("OptionGroups");
            }
        }

        #endregion

        #region Constructor

        public ActiveCharacterWidgetViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<ActivateCharacterEvent>().Subscribe(this.LoadCharacter);
            InitializeCommands();
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {

        }

        #endregion

        #region Private Methods

        private void LoadCharacter(Tuple<Character, string, string> tuple)
        {
            this.UnloadCharacter();

            Character character = tuple.Item1;
            string optionGroupName = tuple.Item2;
            string optionName = tuple.Item3;
            this.ActiveCharacter = character;
            if (character != null)
            {
                this.OptionGroups = new ObservableCollection<IOptionGroupViewModel>();
                foreach (IOptionGroup group in character.OptionGroups)
                {
                    bool loadedOptionExists = group.Name == optionGroupName;
                    switch (group.Type)
                    {
                        case OptionType.Ability:
                            
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.Identity:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<Identity>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.CharacterMovement:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterMovement>>(
                            new ParameterOverride("optionGroup", group), 
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.Mixed:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterOption>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                    }
                }

                character.Activate();

                //Setting hooks for PlayAbilityByKey
                hookID = KeyBoardHook.SetHook(this.PlayAbilityByKeyProc);
            }
        }
        
        private void UnloadCharacter()
        {
            if (ActiveCharacter != null)
            {
                ActiveCharacter.Deactivate();
                KeyBoardHook.UnsetHook(hookID);
            }
                
            ActiveCharacter = null;
        }
                
        private IntPtr PlayAbilityByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyboardLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)keyboardLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;
                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null)
                        || Process.GetCurrentProcess().Id == wndProcId)
                    {
                        if (Keyboard.IsKeyDown(Key.LeftAlt) && ActiveCharacter.AnimatedAbilities.Any(ab => ab.ActivateOnKey == vkCode))
                        {
                            ActiveCharacter.AnimatedAbilities.First(ab => ab.ActivateOnKey == vkCode).Play();
                        }
                        else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
                        {
                            if (ActiveCharacter.Movements.Any(m => m.ActivationKey == vkCode))
                            {
                                CharacterMovement cm = ActiveCharacter.Movements.First(m => m.ActivationKey == vkCode);
                                if (!cm.IsActive)
                                    cm.ActivateMovement();
                                else
                                    cm.DeactivateMovement();
                            }
                        }
                        //Jeff can now press control to toggle the default movement on and off
                        
                    }
                    WindowsUtilities.SetForegroundWindow(foregroundWindow);
                }
            }
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        #endregion
    }
}
