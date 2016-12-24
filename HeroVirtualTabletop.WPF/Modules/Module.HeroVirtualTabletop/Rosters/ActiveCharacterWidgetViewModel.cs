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
    public class ActiveCharacterWidgetViewModel : Hooker
    {
        #region Private Fields
        private EventAggregator eventAggregator;
        private System.Timers.Timer clickTimer_AbilityPlay = new System.Timers.Timer();
        private AnimatedAbility activeAbility;
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

        internal override void ExecuteMouseEventRelatedLogic(DesktopMouseState mouseState) { }
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

                //Setting hooks for PlayAbilityByKey

                // hookID = KeyBoardHook.SetHook(this.PlayAbilityByKeyProc);
                this.ActivateKeyboardHook();
            }

        }
        private void UnloadCharacter()
        {
            if (ActiveCharacter != null)
            {
                ActiveCharacter.Deactivate();
                //KeyBoardHook.UnsetHook(hookID);
                DeactivateKeyboardHook();
            }
                
            ActiveCharacter = null;
        }

        
        private void clickTimer_AbilityPlay_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            clickTimer_AbilityPlay.Stop();
            Action d = delegate()
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

        #region event logic
        internal override void ExecuteKeyBoardEventRelatedLogic(Keys vkCode) {

            if (Keyboard.Modifiers == ModifierKeys.Alt && ActiveCharacter.AnimatedAbilities.Any(ab => ab.ActivateOnKey == vkCode))
            {
                playActiveAbility(vkCode);
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift) && ActiveCharacter.Movements.Any(m => m.ActivationKey == vkCode))
            {
                toggleMovement(vkCode);
            }
        }
        private void toggleMovement(Keys vkCode)
        {
            CharacterMovement cm = ActiveCharacter.Movements.First(m => m.ActivationKey == vkCode);
            if (!cm.IsActive)
                cm.ActivateMovement();
            else
                cm.DeactivateMovement();
        }
        private void playActiveAbility(Keys vkCode)
        {
            activeAbility = ActiveCharacter.AnimatedAbilities.First(ab => ab.ActivateOnKey == vkCode);
            activeAbility.Play();
            clickTimer_AbilityPlay.Start();

            
        }
        #endregion

    }
}
