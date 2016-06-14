using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
using Prism.Events;
using System;
using System.Collections.Generic;
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
        private OptionGroupViewModel<Identity> identitiesViewModel;
        private OptionGroupViewModel<AnimatedAbility> animatedAbilitiesViewModel;
        private IntPtr hookID;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        private Visibility visibility = Visibility.Collapsed;
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

        public OptionGroupViewModel<Identity> IdentitiesViewModel
        {
            get
            {
                return identitiesViewModel;
            }
            set
            {
                identitiesViewModel = value;
                OnPropertyChanged("IdentitiesViewModel");
            }
        }

        public OptionGroupViewModel<AnimatedAbility> AnimatedAbilitiesViewModel
        {
            get
            {
                return animatedAbilitiesViewModel;
            }
            set
            {
                animatedAbilitiesViewModel = value;
                OnPropertyChanged("AnimatedAbilitiesViewModel");
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
        
        private void LoadCharacter(Character character)
        {
            this.UnloadCharacter();
            this.ActiveCharacter = character;
            if (character != null)
            {
                this.IdentitiesViewModel = this.Container.Resolve<OptionGroupViewModel<Identity>>(
                    new ParameterOverride("optionGroup", character.AvailableIdentities),
                    new ParameterOverride("owner", character)
                    );
                this.AnimatedAbilitiesViewModel = this.Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                    new ParameterOverride("optionGroup", character.AnimatedAbilities),
                    new ParameterOverride("owner", character)
                    );
                this.IdentitiesViewModel.AddOrRemoveIsVisible = Visibility.Collapsed;
                this.AnimatedAbilitiesViewModel.AddOrRemoveIsVisible = Visibility.Collapsed;

                //Setting hooks for PlayAbilityByKey
                hookID = KeyBoardHook.SetHook(this.PlayAbilityByKeyProc);
            }
        }
        
        private void UnloadCharacter()
        {
            if (ActiveCharacter != null)
                KeyBoardHook.UnsetHook(hookID);
            ActiveCharacter = null;
        }
                
        private IntPtr PlayAbilityByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyboardLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)keyboardLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;
                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN)
                    && Keyboard.IsKeyDown(Key.LeftAlt))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null)
                        || Process.GetCurrentProcess().Id == wndProcId)
                    {
                        if (ActiveCharacter.AnimatedAbilities.Any(ab => ab.ActivateOnKey == vkCode))
                        {
                            ActiveCharacter.AnimatedAbilities.First(ab => ab.ActivateOnKey == vkCode).Play();
                        }
                    }
                    WindowsUtilities.SetForegroundWindow(foregroundWindow);
                }
            }
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        #endregion
    }
}
