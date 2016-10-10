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
using Module.HeroVirtualTabletop.Movements;
using Module.Shared;
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

        bool IsReadOnlyMode { get; set; }

        void RemoveOption(int index);
        void InsertOption(int index, ICharacterOption option);

        void SaveOptionGroup();
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

        private bool isReadOnlyMode;
        public bool IsReadOnlyMode
        {
            get
            {
                return isReadOnlyMode;
            }
            set
            {
                isReadOnlyMode = value;
                OnPropertyChanged("IsReadOnlyMode");
            }
        }

        private bool showOptions;
        public bool ShowOptions
        {
            get
            {
                return showOptions;
            }
            set
            {
                showOptions = value;
                OnPropertyChanged("ShowOptions");
            }
        }

        private string loadingOptionName;
        public string LoadingOptionName
        {
            get
            {
                return loadingOptionName;
            }
            set
            {
                loadingOptionName = value;
                if (!string.IsNullOrEmpty(value))
                {
                    var optionToLoad = OptionGroup.FirstOrDefault(o => o.Name == value);
                    if (optionToLoad != null)
                    {
                        ShowOptions = true;
                        this.SelectedOption = optionToLoad;
                        this.TogglePlayOption(optionToLoad);
                    }
                }
                else
                {
                    ShowOptions = false;
                }
            }
        }

        private Character owner;
        public Character Owner
        {
            get { return owner; }
            private set { owner = value; }
        }
        public bool IsStandardOptionGroup
        {
            get
            {
                return OptionGroup.Name == Constants.IDENTITY_OPTION_GROUP_NAME || OptionGroup.Name == Constants.ABILITY_OPTION_GROUP_NAME || OptionGroup.Name == Constants.MOVEMENT_OPTION_GROUP_NAME;
            }
        }

        private Visibility addOrRemoveIsVisible = Visibility.Visible;
        private IMessageBoxService messageBoxService;

        public bool NewOptionGroupAdded { get; set; }

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
        public DelegateCommand SetActiveOptionCommand { get; private set; }
        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitOptionGroupRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }
        public DelegateCommand<object> RenameNewOptionGroupCommand { get; private set; }

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
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.StopAttack);
            if (!this.IsStandardOptionGroup)
            {
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Subscribe(this.RemoveOption);
            }

            InitializeCommands();
        }

        private void Owner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.OptionGroup.Type == OptionType.Identity)
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
            else if (this.OptionGroup.Type == OptionType.Ability)
            {
                switch (e.PropertyName)
                {
                    case "ActiveAbility":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                }
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                switch (e.PropertyName)
                {
                    case "ActiveMovement":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                    case "DefaultMovement":
                        OnPropertyChanged("DefaultOption");
                        break;
                }
            }
            else if (this.OptionGroup.Type == OptionType.Mixed)
            {
                switch (e.PropertyName)
                {
                    case "ActiveIdentity":
                    case "ActiveMovement":
                    case "ActiveAbility":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                    case "DefaultIdentity":
                    case "DefaultMovement":
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

            this.SetDefaultOptionCommand = new DelegateCommand<object>(this.SetDefaultOption, this.CanSetDefaultOption);
            this.EditOptionCommand = new DelegateCommand<object>(this.EditOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });
            this.PlayOptionCommand = new DelegateCommand<object>(this.PlayOption, this.CanPlayOption);
            this.StopOptionCommand = new DelegateCommand<object>(this.StopOption, this.CanStopOption);
            this.TogglePlayOptionCommand = new DelegateCommand<object>(this.TogglePlayOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });

            this.EnterEditModeCommand = new DelegateCommand<object>(this.EnterEditMode, this.CanEnterEditMode);
            this.SubmitOptionGroupRenameCommand = new DelegateCommand<object>(this.SubmitRename);
            this.CancelEditModeCommand = new DelegateCommand<object>(this.CancelEditMode);
            this.RenameNewOptionGroupCommand = new DelegateCommand<object>(this.RenameOptionGroup);
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
            else if (typeof(T) == typeof(CharacterMovement))
            {
                AddCharacterMovement(state);
            }
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Subscribe(this.SaveOptionGroupCompletedCallback);
            this.SaveOptionGroup();
        }

        private void RemoveOption(object state)
        {
            var optionToRemove = SelectedOption;
            if (typeof(T) == typeof(Identity))
            {
                RemoveIdentity(state);
            }
            else if (typeof(T) == typeof(CharacterMovement))
            {
                RemoveCharacterMovement(state);
            }
            else
            {
                optionGroup.Remove(SelectedOption);
            }
            if (this.IsStandardOptionGroup)
            {
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Publish(optionToRemove);
            }
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Subscribe(this.SaveOptionGroupCompletedCallback);
            this.SaveOptionGroup();
        }

        private void RemoveOption(ICharacterOption option)
        {
            T optionToRemove = (T)option;
            if (optionGroup.Contains(optionToRemove))
            {
                optionGroup.Remove(optionToRemove);
            }
        }

        public void SaveOptionGroup()
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        private void SaveOptionGroupCompletedCallback(object state)
        {
            this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Unsubscribe(this.SaveOptionGroupCompletedCallback);
        }

        private T GetDefaultOption()
        {
            T defaultOption = default(T);
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                defaultOption = (T)Convert.ChangeType(owner.DefaultIdentity, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                defaultOption = (T)Convert.ChangeType(owner.DefaultMovement, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.Mixed)
            {
                // return the default option of the selected type
                if (SelectedOption is Identity)
                {
                    defaultOption = (T)Convert.ChangeType(owner.DefaultIdentity, typeof(Identity));
                }
                else if (SelectedOption is CharacterMovement)
                {
                    defaultOption = (T)Convert.ChangeType(owner.DefaultMovement, typeof(CharacterMovement));
                }
            }

            return defaultOption;
        }

        private void SetDefaultOption(T value)
        {
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                owner.DefaultIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                owner.DefaultMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            }
        }
        private bool CanSetDefaultOption(object state)
        {
            return typeof(T) == typeof(Identity) || typeof(T) == typeof(CharacterMovement);
        }

        private void SetDefaultOption(object state)
        {
            DefaultOption = SelectedOption;
        }

        private T GetActiveOption()
        {
            T activeOption = default(T);
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                activeOption = (T)Convert.ChangeType(owner.ActiveIdentity, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.Ability)
            {
                activeOption = (T)Convert.ChangeType(owner.ActiveAbility, typeof(Attack));
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                activeOption = (T)Convert.ChangeType(owner.ActiveCharacterMovement, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.Mixed)
            {
                if (SelectedOption is Identity)
                {
                    activeOption = (T)Convert.ChangeType(owner.ActiveIdentity, typeof(Identity));
                }
                else if (SelectedOption is CharacterMovement)
                {
                    activeOption = (T)Convert.ChangeType(owner.ActiveCharacterMovement, typeof(CharacterMovement));
                }
                else if (SelectedOption is AnimatedAbility)
                {
                    activeOption = (T)Convert.ChangeType(owner.ActiveAbility, typeof(Attack));
                }
            }

            return activeOption;
        }

        private void SetActiveOption(T value)
        {
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            //else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            //{
            //    owner.ActiveMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            //}
        }

        private T GetSelectedOption()
        {
            return selectedOption;
        }

        private void SetSelectedOption(T value)
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
            else if (selectedOption != null && selectedOption is CharacterMovement)
            {
                if (selectedOption as CharacterMovement != value as CharacterMovement)
                {
                    CharacterMovement characterMovement = selectedOption as CharacterMovement;
                    if (characterMovement.IsActive)
                        characterMovement.DeactivateMovement();
                }
            }
            selectedOption = value;
            if (value is Identity)
            {
                if (!this.Owner.HasBeenSpawned)
                    this.SpawnAndTargetOwnerCharacter();
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            //else if (value is CharacterMovement)
            //{
            //    if (!this.Owner.HasBeenSpawned)
            //        this.SpawnAndTargetOwnerCharacter();
            //    owner.ActiveMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            //}
        }

        private bool CanPlayOption(object arg)
        {
            return ((selectedOption is AnimatedAbility) || (selectedOption is CharacterMovement) && !Helper.GlobalVariables_IsPlayingAttack);
        }

        private void PlayOption(object state)
        {
            if(selectedOption is AnimatedAbility)
            {
                AnimatedAbility ability = selectedOption as AnimatedAbility;
                if (ability != null)
                    PlayAnimatedAbility(ability);
            }
            else
            {
                this.SpawnAndTargetOwnerCharacter();
                CharacterMovement characterMovement = selectedOption as CharacterMovement;
                if(characterMovement != null && characterMovement.Movement != null && !characterMovement.IsActive)
                {
                    characterMovement.ActivateMovement();
                }
                this.Owner.ActiveCharacterMovement = characterMovement;
            }
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
            owner.ActiveAbility = ability;
            ability.Play(Target: currentTarget);
        }

        private bool CanStopOption(object arg)
        {
            return CanPlayOption(arg);
        }

        private void StopOption(object state)
        {
            if(selectedOption is AnimatedAbility)
            {
                AnimatedAbility abilityToStop = state as AnimatedAbility;
                AnimatedAbility ability = selectedOption as AnimatedAbility;
                if (ability != null && abilityToStop != null && ability == abilityToStop)
                    StopAnimatedAbility(ability);
            }
            else
            {
                this.Owner.ActiveCharacterMovement = null;
                CharacterMovement characterMovement = selectedOption as CharacterMovement;
                if (characterMovement != null && characterMovement.Movement != null && characterMovement.IsActive)
                {
                    characterMovement.DeactivateMovement();
                }
            }
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
            owner.ActiveAbility = null;
            ability.Stop(Target: currentTarget);
        }

        private void TogglePlayOption(object obj)
        {
            if (SelectedOption != null && SelectedOption is AnimatedAbility && !(obj is AnimatedAbility))
            {
                StopOption(SelectedOption);
            }
            else if (SelectedOption != null && SelectedOption is CharacterMovement && !(obj is CharacterMovement))
            {
                StopOption(SelectedOption);
            }

            //SelectedOption = (T)obj;
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
            else if(SelectedOption is CharacterMovement)
            {
                CharacterMovement characterMovement = obj as CharacterMovement;
                if (!characterMovement.IsActive)
                {
                    PlayOption(obj);
                }
                else
                {
                    StopOption(obj);
                }
            }
        }
        private void AttackInitiated(object state)
        {
            this.UpdateCommands();
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
        private void RemoveCharacterMovement(object state)
        {
            if (SelectedOption == null)
                return;
            CharacterMovement characterMovement = SelectedOption as CharacterMovement;
            if (this.Owner.DefaultMovement == characterMovement)
                this.Owner.DefaultMovement = null;
            //if (this.Owner.ActiveMovement == characterMovement)
            //    this.Owner.ActiveMovement = null;
            optionGroup.Remove(SelectedOption);
        }
        private Identity GetNewIdentity()
        {
            return new Identity("Model_Statesman", IdentityType.Model, optionGroup.GetNewValidOptionName("Identity"));
        }

        private void AddAbility(object state)
        {
            Attack attack = GetNewAttackAbility();
            (optionGroup as OptionGroup<AnimatedAbility>).Add(attack);

            this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            this.eventAggregator.GetEvent<AddOptionEvent>().Publish(attack);
        }

        private Attack GetNewAttackAbility()
        {
            return new Attack(optionGroup.GetNewValidOptionName("Ability"), owner: this.Owner);
        }

        private void AddCharacterMovement(object state)
        {
            CharacterMovement characterMovement = GetNewCharacterMovement();
            (optionGroup as OptionGroup<CharacterMovement>).Add(characterMovement);
            this.SelectedOption = (T)Convert.ChangeType(characterMovement, typeof(CharacterMovement));
            eventAggregator.GetEvent<EditMovementEvent>().Publish((characterMovement));
        }

        private CharacterMovement GetNewCharacterMovement()
        {
            return new CharacterMovement(optionGroup.GetNewValidOptionName("Movement"), this.Owner);
        }

        private void EditOption(object obj)
        {
            if (SelectedOption is Identity)
            {
                Identity identity = (Identity)Convert.ChangeType(SelectedOption, typeof(Identity));
                eventAggregator.GetEvent<EditIdentityEvent>().Publish(new Tuple<Identity, Character>(identity, Owner));
            }
            else if (SelectedOption is AnimatedAbility)
            {
                Attack attack = (Attack)Convert.ChangeType(SelectedOption, typeof(Attack));
                eventAggregator.GetEvent<EditAbilityEvent>().Publish(new Tuple<AnimatedAbility, Character>(attack, Owner));
            }
            else if (SelectedOption is CharacterMovement)
            {
                CharacterMovement characterMovement = (CharacterMovement)Convert.ChangeType(SelectedOption, typeof(CharacterMovement));
                eventAggregator.GetEvent<EditMovementEvent>().Publish((characterMovement));
            }
        }

        #region Rename Option Group

        private bool CanEnterEditMode(object arg)
        {
            return !this.IsStandardOptionGroup;
        }
        private void EnterEditMode(object state)
        {
            this.OriginalName = OptionGroup.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelEditMode(object state)
        {
            OptionGroup.Name = this.OriginalName;
            this.OriginalName = null;
            OnEditModeLeave(state, null);
        }

        private void RenameOptionGroup(object state)
        {
            if (this.NewOptionGroupAdded)
            {
                this.NewOptionGroupAdded = false;
                this.EnterEditMode(null);
            }
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
                    this.SaveOptionGroup();
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
            return this.OriginalName != updatedName && this.Owner.OptionGroups.ContainsKey(updatedName);
        }

        #endregion

        #region ReOrder Option Group

        public void RemoveOption(int index)
        {
            (optionGroup as OptionGroup<T>).RemoveAt(index);
        }

        public void InsertOption(int index, ICharacterOption characterOption)
        {
            OptionGroup<T> group = (optionGroup as OptionGroup<T>);
            var existingIndex = group.IndexOf((T)characterOption);
            if (existingIndex >= 0)
            {
                group.RemoveAt(existingIndex);
                if (index > 0 && index >= group.Count)
                    index -= 1;
            }
            group.Insert(index, (T)characterOption);
        }

        #endregion

        #endregion
    }
}
