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
using Microsoft.Practices.Prism.Commands;
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
using Module.HeroVirtualTabletop.Desktop;
using Module.Shared;

namespace Module.HeroVirtualTabletop.Roster
{
    public class ActiveCharacterWidgetViewModel : BaseViewModel
    {

        #region Private Fields and Commands
        private EventAggregator eventAggregator;
        private System.Timers.Timer clickTimer_AbilityPlay = new System.Timers.Timer();
        private AnimatedAbility activeAbility;
        public DelegateCommand<object> PlayActiveAbilityCommand { get; private set; }
        public DelegateCommand<object> ToggleMovementCommand { get; private set; }
        public DelegateCommand<string> ActivatePanelCommand { get; private set; }
        public DelegateCommand<string> DeactivatePanelCommand { get; private set; }
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

            clickTimer_AbilityPlay.AutoReset = false;
            clickTimer_AbilityPlay.Interval = 2000;
            clickTimer_AbilityPlay.Elapsed +=
                new System.Timers.ElapsedEventHandler(clickTimer_AbilityPlay_Elapsed);

            this.PlayActiveAbilityCommand = new DelegateCommand<object>(delegate (object state) { this.PlayActiveAbility(); }, this.CanPlayActiveAbility);
            this.ToggleMovementCommand = new DelegateCommand<object>(delegate (object state) { this.ToggleMovement(); }, this.CanToggleMovement);
            this.ActivatePanelCommand = new DelegateCommand<string>(this.ActivatePanel);
            this.DeactivatePanelCommand = new DelegateCommand<string>(this.DeactivatePanel);

            //// Shortcut keys would work from anywhere, so no key handling needed here
           // DesktopKeyEventHandler keyHandler = new DesktopKeyEventHandler(RetrieveEventFromKeyInput);
        }
        #endregion


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
                    bool showOptionsInGroup = false;
                    if (character.OptionGroupExpansionStates.ContainsKey(group.Name))
                        showOptionsInGroup = character.OptionGroupExpansionStates[group.Name];
                    switch (group.Type)
                    {
                        case OptionType.Ability:

                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.Identity:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<Identity>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.CharacterMovement:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterMovement>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.Mixed:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterOption>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                    }
                }

                character.Activate();
            }

        }
        private void UnloadCharacter()
        {
            if (ActiveCharacter != null)
            {
                ActiveCharacter.Deactivate();
            }

            ActiveCharacter = null;
        }


        private void clickTimer_AbilityPlay_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            clickTimer_AbilityPlay.Stop();
            Action d = delegate ()
            {
                if (activeAbility != null && !activeAbility.Persistent && !activeAbility.IsAttack)
                {
                    activeAbility.DeActivate(ActiveCharacter);
                    //activeAbility.IsActive = false;
                    //OnPropertyChanged("IsActive");
                }
            };
            System.Windows.Application.Current.Dispatcher.BeginInvoke(d);
        }
        System.Windows.Forms.Keys vkCode;

        internal DesktopKeyEventHandler.EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == Constants.ACTIVE_CHARACTER_WIDGET)
            {
                this.vkCode = vkCode;

                if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift) && ActiveCharacter.AnimatedAbilities.Any(ab => ab.ActivateOnKey == vkCode))
                {
                    return this.PlayActiveAbility;
                }
                else if (Keyboard.Modifiers == ModifierKeys.Alt && ActiveCharacter.Movements.Any(m => m.ActivationKey == vkCode))
                {
                    return this.ToggleMovement;
                } 
            }
            return null;
        }

        public bool CanToggleMovement(object state) { return true; }
        public void ToggleMovement()
        {
            Keys vkCode = this.vkCode;
            CharacterMovement cm = ActiveCharacter.Movements.First(m => m.ActivationKey == vkCode);
            if (!cm.IsActive)
                cm.ActivateMovement();
            else
                cm.DeactivateMovement();
        }

        public bool CanPlayActiveAbility(object state) { return true; }
        public void PlayActiveAbility()
        {
            Keys vkCode = this.vkCode;
            activeAbility = ActiveCharacter.AnimatedAbilities.First(ab => ab.ActivateOnKey == vkCode);
            ActiveCharacter.Target(false);
            ActiveCharacter.ActiveIdentity.RenderWithoutAnimation(target:ActiveCharacter);
            activeAbility.Play();
            clickTimer_AbilityPlay.Start();
        }

        private void ActivatePanel(string panelName)
        {
            Helper.GlobalVariables_CurrentActiveWindowName = panelName;
        }

        private void DeactivatePanel(string panelName)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == panelName)
                Helper.GlobalVariables_CurrentActiveWindowName = "";
        }

    }
}
