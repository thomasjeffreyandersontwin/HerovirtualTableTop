using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared.Events;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.OptionGroups
{
    public interface IOptionGroupViewModel
    {
        IOptionGroup OptionGroup { get; }
        event EventHandler EditModeEnter;
        event EventHandler EditModeLeave;
    }

    public class OptionGroupViewModel<T> : BaseViewModel, IOptionGroupViewModel where T : ICharacterOption
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private OptionGroup<T> optionGroup;
        
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

        public OptionGroup<T> OptionGroup
        {
            get
            {
                return optionGroup;
            }
            private set
            {
                optionGroup = value;
                OnPropertyChanged("OptionGroup");
            }
        }


        IOptionGroup IOptionGroupViewModel.OptionGroup
        {
            get
            {
                return OptionGroup as IOptionGroup;
            }
        }

        private T selectedOption;
        public T SelectedOption
        {
            get
            {
                return GetSelectedOption();
            }
            set
            {
                SetSelectedOption(value);
                OnPropertyChanged("SelectedOption");
                this.PlayOptionCommand.RaiseCanExecuteChanged();
                this.StopOptionCommand.RaiseCanExecuteChanged();
            }
        }

        public T DefaultOption
        {
            get
            {
                return GetDefaultOption();
            }
            set
            {
                SetDefaultOption(value);
                OnPropertyChanged("DefaultOption");
            }
        }

        public T ActiveOption
        {
            get
            {
                return GetActiveOption();
            }
            set
            {
                SetActiveOption(value);
                OnPropertyChanged("ActiveOption");
            }
        }

        private Character owner;
        public Character Owner
        {
            get { return owner; }
            private set { owner = value; }
        }

        private Visibility addOrRemoveIsVisible = Visibility.Visible;
        private IMessageBoxService messageBoxService;

        public Visibility AddOrRemoveIsVisible
        {
            get
            {
                return addOrRemoveIsVisible;
            }
            set
            {
                addOrRemoveIsVisible = value;
                OnPropertyChanged("AddOrRemoveIsVisible");
            }
        }
        
        public string OriginalName { get; set; }

        #endregion

        #region Commands

        public DelegateCommand<object> AddOptionCommand { get; private set; }
        public DelegateCommand<object> RemoveOptionCommand { get; private set; }

        public DelegateCommand<object> SetDefaultOptionCommand { get; private set; }

        public DelegateCommand<object> EditOptionCommand { get; private set; }

        public DelegateCommand<object> PlayOptionCommand { get; private set; }
        public DelegateCommand<object> StopOptionCommand { get; private set; }

        public DelegateCommand<object> TogglePlayOptionCommand { get; private set; }

        public ICommand SetActiveOptionCommand { get; private set; }

        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }

        #endregion

        #region Constructor

        public OptionGroupViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator, OptionGroup<T> optionGroup, Character owner)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.Owner = owner;
            this.Owner.PropertyChanged += Owner_PropertyChanged;
            this.OptionGroup = optionGroup;
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.StopAttack);
            InitializeAttackEventHandlers();
            InitializeCommands();
        }

        private void Owner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (typeof(T) == typeof(Identity))
            {
                switch (e.PropertyName)
                {
                    case "ActiveIdentity":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                    case "DefaultIdentity":
                        OnPropertyChanged("DefaultOption");
                        break;
                }
            }
        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.AddOptionCommand = new DelegateCommand<object>(this.AddOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });
            this.RemoveOptionCommand = new DelegateCommand<object>(this.RemoveOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });

            this.SetDefaultOptionCommand = new DelegateCommand<object>(this.SetDefaultOption);
            this.EditOptionCommand = new DelegateCommand<object>(this.EditOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });
            this.PlayOptionCommand = new DelegateCommand<object>(this.PlayOption, this.CanPlayOption);
            this.StopOptionCommand = new DelegateCommand<object>(this.StopOption, this.CanStopOption);
            this.TogglePlayOptionCommand = new DelegateCommand<object>(this.TogglePlayOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });

            this.EnterEditModeCommand = new DelegateCommand<object>(this.EnterEditMode, this.CanEnterEditMode);
            this.SubmitRenameCommand = new DelegateCommand<object>(this.SubmitRename);
            this.CancelEditModeCommand = new DelegateCommand<object>(this.CancelEditMode);
        }

        private bool CanEnterEditMode(object arg)
        {
            return OptionGroup.Name != "Available Identities" && OptionGroup.Name != "Powers";
        }

        #endregion

        #region Methods

        private void UpdateCommands()
        {
            this.AddOptionCommand.RaiseCanExecuteChanged();
            this.RemoveOptionCommand.RaiseCanExecuteChanged();
            this.EditOptionCommand.RaiseCanExecuteChanged();
            this.TogglePlayOptionCommand.RaiseCanExecuteChanged();
            this.PlayOptionCommand.RaiseCanExecuteChanged();
            this.StopOptionCommand.RaiseCanExecuteChanged();
        }

        private void AddOption(object state)
        {
            if (typeof(T) == typeof(Identity))
            {
                AddIdentity(state);
            }
            else if (typeof(T) == typeof(AnimatedAbility))
            {
                AddAbility(state);
            }
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Subscribe(this.SaveOptionGroupCompletedCallback);
        }
        
        private void RemoveOption(object state)
        {
            if (typeof(T) == typeof(Identity))
            {
                RemoveIdentity(state);
            }
            else
            {
                optionGroup.Remove(SelectedOption);
            }
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Subscribe(this.SaveOptionGroupCompletedCallback);
        }

        private void SaveOptionGroupCompletedCallback(object state)
        {
            this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Unsubscribe(this.SaveOptionGroupCompletedCallback);
        }
        
        private T GetDefaultOption()
        {
            if (typeof(T) == typeof(Identity))
            {
                return (T)Convert.ChangeType(owner.DefaultIdentity, typeof(T));
            }

            return default(T);
        }

        private void SetDefaultOption(T value)
        {
            if (typeof(T) == typeof(Identity))
            {
                owner.DefaultIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
        }

        private void SetDefaultOption(object state)
        {
            DefaultOption = SelectedOption;
        }

        private T GetActiveOption()
        {
            if (typeof(T) == typeof(Identity))
            {
                return (T)Convert.ChangeType(owner.ActiveIdentity, typeof(T));
            }

            return default(T);
        }

        private void SetActiveOption(T value)
        {
            if (typeof(T) == typeof(Identity))
            {
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
        }

        private T GetSelectedOption()
        {
            if (typeof(T) == typeof(Identity))
            {
                return (T)Convert.ChangeType(owner.ActiveIdentity, typeof(T));
            }
            else
            {
                return selectedOption;
            }
        }

        private void SetSelectedOption(T value)
        {
            if (typeof(T) == typeof(Identity))
            {
                if (!this.Owner.HasBeenSpawned)
                    this.SpawnAndTargetOwnerCharacter();
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            else
            {
                if (selectedOption != null && selectedOption is AnimatedAbility)
                {
                    if (selectedOption as AnimatedAbility != value as AnimatedAbility)
                    {
                        AnimatedAbility ability = selectedOption as AnimatedAbility;
                        if (ability.IsActive && !ability.Persistent)
                            ability.Stop();
                    }
                }
                selectedOption = value;
            }
            //if (typeof(T) == typeof(AnimatedAbility))
            //{
            //    PlayOption(null);
            //}
        }

        private bool CanPlayOption(object arg)
        {
            return (selectedOption is AnimatedAbility && !Helper.GlobalVariables_IsPlayingAttack);
        }

        private void PlayOption(object state)
        {
            AnimatedAbility ability = selectedOption as AnimatedAbility;
            if (ability != null)
                PlayAnimatedAbility(ability);
        }

        private void PlayAnimatedAbility(AnimatedAbility ability)
        {
            Character currentTarget = null;
            if (!ability.PlayOnTargeted)
            {
                this.SpawnAndTargetOwnerCharacter();
                currentTarget = this.Owner;
            }
            else
            {
                Roster.RosterExplorerViewModel rostExpVM = this.Container.Resolve<Roster.RosterExplorerViewModel>();
                currentTarget = rostExpVM.GetCurrentTarget() as Character;
                if (currentTarget == null)
                {
                    this.SpawnAndTargetOwnerCharacter();
                    currentTarget = this.Owner;
                }
            }
            ability.Play(Target: currentTarget);
        }

        private bool CanStopOption(object arg)
        {
            return CanPlayOption(arg);// && (selectedOption as AnimatedAbility).IsActive;
        }

        private void StopOption(object state)
        {
            AnimatedAbility ability = selectedOption as AnimatedAbility;
            if (ability != null)
                StopAnimatedAbility(ability);
        }

        private void StopAnimatedAbility(AnimatedAbility ability)
        {
            Character currentTarget = null;
            if (!ability.PlayOnTargeted)
            {
                this.SpawnAndTargetOwnerCharacter();
            }
            else
            {
                Roster.RosterExplorerViewModel rostExpVM = this.Container.Resolve<Roster.RosterExplorerViewModel>();
                currentTarget = rostExpVM.GetCurrentTarget() as Character;
                if (currentTarget == null)
                {
                    this.SpawnAndTargetOwnerCharacter();
                    currentTarget = this.Owner;
                }
            }
            ability.Stop(Target: currentTarget);
        }
        
        private void TogglePlayOption(object obj)
        {
            SelectedOption = (T)obj;
            if (SelectedOption is AnimatedAbility)
            {
                AnimatedAbility ability = obj as AnimatedAbility;
                if (!ability.IsActive)
                {
                    PlayOption(obj);
                }
                else
                {
                    StopOption(obj);
                }
            }
        }
        private void StopAttack(object state)
        {
            if (state != null && state is AnimatedAbility)
            {
                StopOption(state);
                Helper.GlobalVariables_IsPlayingAttack = false;
                this.UpdateCommands();
            }
        }
        private void SpawnAndTargetOwnerCharacter()
        {
            if (!this.Owner.HasBeenSpawned)
            {
                Crowds.CrowdMemberModel member = this.Owner as Crowds.CrowdMemberModel;
                if (member.RosterCrowd == null)
                    this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(new Tuple<Crowds.CrowdMemberModel, Crowds.CrowdModel>(member, member.RosterCrowd as Crowds.CrowdModel));
                member.Spawn(false);
            }
            this.Owner.Target();
        }

        private void AddIdentity(object state)
        {
            (optionGroup as OptionGroup<Identity>).Add(GetNewIdentity());
        }

        private void RemoveIdentity(object state)
        {
            if (SelectedOption == null)
                return;
            if (OptionGroup.Count == 1)
            {
                MessageBox.Show("Every character must have at least 1 Identity");
                return;
            }
            optionGroup.Remove(SelectedOption);
        }

        private Identity GetNewIdentity()
        {
            return new Identity("Model_Statesman", IdentityType.Model, optionGroup.NewValidOptionName("Identity"));
        }
        
        private void AddAbility(object state)
        {
            Attack attack = GetNewAttackAbility();
            (optionGroup as OptionGroup<AnimatedAbility>).Add(attack);

            this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            InitializeAttackEventHandlers(attack);
        }
        
        private Attack GetNewAttackAbility()
        {
            return new Attack(optionGroup.NewValidOptionName("Ability"), owner: this.Owner);
        }

        private void EditOption(object obj)
        {
            if (typeof(T) == typeof(Identity))
            {
                Identity identity = (Identity)Convert.ChangeType(SelectedOption, typeof(Identity));
                eventAggregator.GetEvent<EditIdentityEvent>().Publish(new Tuple<Identity, Character>(identity, Owner));
            }
            else if (typeof(T) == typeof(AnimatedAbility))
            {
                Attack attack = (Attack)Convert.ChangeType(SelectedOption, typeof(Attack));
                InitializeAttackEventHandlers(attack);
                eventAggregator.GetEvent<EditAbilityEvent>().Publish(new Tuple<AnimatedAbility, Character>(attack, Owner));
            }
        }

        private void InitializeAttackEventHandlers(Attack attack)
        {
            attack.AttackInitiated -= this.Ability_AttackInitiated;
            attack.AttackInitiated += Ability_AttackInitiated;
        }

        private void InitializeAttackEventHandlers()
        {
            if (typeof(T) == typeof(AnimatedAbility))
            {
                foreach(var option in this.OptionGroup)
                {
                    Attack attack = (Attack)Convert.ChangeType(option, typeof(Attack));
                    InitializeAttackEventHandlers(attack);
                }
            }
        }

        private void Ability_AttackInitiated(object sender, EventArgs e)
        {
            Character targetCharacter = sender as Character;
            CustomEventArgs<Attack> customEventArgs = e as CustomEventArgs<Attack>;
            if(targetCharacter != null && customEventArgs != null)
            {
                Helper.GlobalVariables_IsPlayingAttack = true;
                this.UpdateCommands();
                // Change mouse pointer to bulls eye
                Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                Mouse.OverrideCursor = cursor;
                // Inform Roster to update attacker
                this.eventAggregator.GetEvent<AttackInitiatedEvent>().Publish(new Tuple<Character, Attack>(targetCharacter, customEventArgs.Value));
            }
        }

        private void Ability_AttackTargetSelected(Tuple<Character, Attack> targetSelectedEventTuple)
        {

        }
        
        private void EnterEditMode(object state)
        {
            this.OriginalName = OptionGroup.Name;
            OnEditModeEnter(state, null);
        }
        
        private void CancelEditMode(object state)
        {
            OptionGroup.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);
                bool duplicateName = CheckDuplicateName(updatedName);
                if (!duplicateName)
                {
                    RenameOptionGroup(updatedName);
                    OnEditModeLeave(state, null);
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, Messages.DUPLICATE_NAME_CAPTION, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    this.CancelEditMode(state);
                }
            }
        }

        private void RenameOptionGroup(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            this.OptionGroup.Name = updatedName;
            this.Owner.OptionGroups.UpdateKey(this.OriginalName, updatedName);
            this.OriginalName = null;
        }

        private bool CheckDuplicateName(string updatedName)
        {
            return this.Owner.OptionGroups.ContainsKey(updatedName);
        }

        #endregion
    }
}
