﻿using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Module.Shared.Events;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Movements
{
    public class MovementEditorViewModel: BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;
        private Character defaultCharacter;

        #endregion

        #region Events

        public event EventHandler<CustomEventArgs<bool>> MovementAdded;
        public void OnMovementAdded(object sender, CustomEventArgs<bool> e)
        {
            if (MovementAdded != null)
            {
                MovementAdded(sender, e);
            }
        }

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

        //private Character currentCharacter;

        //public Character CurrentCharacter
        //{
        //    get
        //    {
        //        return currentCharacter;
        //    }
        //    set
        //    {
        //        currentCharacter = value;
        //        OnPropertyChanged("CurrentCharacter");
        //    }
        //}

        private CharacterMovement currentCharacterMovement;
        public CharacterMovement CurrentCharacterMovement
        {
            get
            {
                return currentCharacterMovement;
            }
            set
            {
                currentCharacterMovement = value;
                if (currentCharacterMovement != null && this.currentCharacterMovement.Character != null && this.currentCharacterMovement.Character.DefaultMovement == currentCharacterMovement)
                    this.IsDefaultMovementLoaded = true;
                else
                    this.IsDefaultMovementLoaded = false;
                OnPropertyChanged("CurrentMovement");
                this.SaveMovementCommand.RaiseCanExecuteChanged();
                this.SetDefaultMovementCommand.RaiseCanExecuteChanged();
            }
        }

        private MovementMember selectedMovementMember;
        public MovementMember SelectedMovementMember
        {
            get
            {
                return selectedMovementMember;
            }
            set
            {
                selectedMovementMember = value;
                if(selectedMovementMember != null && selectedMovementMember.MemberAbility != null)
                {
                    selectedMovementMember.MemberAbility.PropertyChanged -= this.SelectedMemberAbility_PropertyChanged;
                    selectedMovementMember.MemberAbility.PropertyChanged += this.SelectedMemberAbility_PropertyChanged;
                }
                OnPropertyChanged("SelectedMovementMember");
            }
        }


        private ObservableCollection<Movement> availableMovements;
        public ObservableCollection<Movement> AvailableMovements
        {
            get
            {
                return availableMovements;
            }
            set
            {
                availableMovements = value;
                OnPropertyChanged("AvailableMovements");
            }
        }

        private Movement selectedMovement;
        public Movement SelectedMovement
        {
            get
            {
                return selectedMovement;
            }
            set
            {
                selectedMovement = value;
                if (selectedMovement != null && this.CurrentCharacterMovement != null)
                {
                    if (this.CurrentCharacterMovement.Character != this.defaultCharacter)
                    {
                        this.CurrentCharacterMovement.Movement = selectedMovement;
                        string prevName = this.CurrentCharacterMovement.Name;
                        this.CurrentCharacterMovement.Name = selectedMovement.Name;
                        this.CurrentCharacterMovement.Character.Movements.UpdateKey(prevName, selectedMovement.Name);
                    }
                }
                this.SaveMovement(null);
                OnPropertyChanged("SelectedMovement");
                this.RemoveMovementCommand.RaiseCanExecuteChanged();
            }
        }

        private bool isShowingMovementEditor;
        public bool IsShowingMovementEditor
        {
            get
            {
                return isShowingMovementEditor;
            }
            set
            {
                isShowingMovementEditor = value;
                OnPropertyChanged("IsShowingMovementEditor");
            }
        }

        private bool isDefaultMovementLoaded;
        public bool IsDefaultMovementLoaded
        {
            get
            {
                return isDefaultMovementLoaded;
            }
            set
            {
                isDefaultMovementLoaded = value;
                OnPropertyChanged("IsDefaultMovementLoaded");
            }
        }
        private CollectionViewSource referenceAbilitiesCVS;
        public CollectionViewSource ReferenceAbilitiesCVS
        {
            get
            {
                return referenceAbilitiesCVS;
            }
        }
        private string filter;
        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                if (referenceAbilitiesCVS != null)
                {
                    referenceAbilitiesCVS.View.Refresh();
                }
                OnPropertyChanged("Filter");
            }
        }

        public bool CanEditMovementOptions
        {
            get
            {
                return !Helper.GlobalVariables_IsPlayingAttack;
            }
        }

        public string OriginalName { get; set; }

        #endregion

        #region Commands

        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> EnterMovementEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitMovementRenameCommand { get; private set; }
        public DelegateCommand<object> CancelMovementEditModeCommand { get; private set; }
        public DelegateCommand<object> AddMovementCommand { get; private set; }
        public DelegateCommand<object> SaveMovementCommand { get; private set; }
        public DelegateCommand<object> RemoveMovementCommand { get; private set; }
        public DelegateCommand<object> LoadResourcesCommand { get; private set; }
        public DelegateCommand<object> SetDefaultMovementCommand { get; private set; }
        public DelegateCommand<object> DemoDirectionalMoveCommand { get; private set; }
        public DelegateCommand<object> LoadAbilityEditorCommand { get; private set; }

        #endregion

        #region Constructor

        public MovementEditorViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditMovementEvent>().Subscribe(this.LoadMovement);
            this.eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Subscribe(this.LoadReferenceResource);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.AttackEnded);
            // Unselect everything at the beginning
            this.InitializeMovementSelections();
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.CloseEditor);
            this.SubmitMovementRenameCommand = new DelegateCommand<object>(this.SubmitMovementRename);
            this.SaveMovementCommand = new DelegateCommand<object>(this.SaveMovement, this.CanSaveMovement);
            this.EnterMovementEditModeCommand = new DelegateCommand<object>(this.EnterMovementEditMode);
            this.CancelMovementEditModeCommand = new DelegateCommand<object>(this.CancelMovementEditMode);
            this.AddMovementCommand = new DelegateCommand<object>(this.AddMovement, this.CanAddMovement);
            this.RemoveMovementCommand = new DelegateCommand<object>(this.RemoveMovement, this.CanRemoveMovement);
            this.SubmitMovementRenameCommand = new DelegateCommand<object>(this.SubmitMovementRename);
            this.SetDefaultMovementCommand = new DelegateCommand<object>(this.SetDefaultMovement, this.CanSetDefaultMovement);
            this.LoadResourcesCommand = new DelegateCommand<object>(this.LoadResources);
            this.DemoDirectionalMoveCommand = new DelegateCommand<object>(this.DemoDirectionalMovement, this.CanDemoDirectionalMovement);
            this.LoadAbilityEditorCommand = new DelegateCommand<object>(this.LoadAbilityEditor, this.CanLoadAbilityEditor);
        }

        private void InitializeMovementSelections()
        {
            this.CurrentCharacterMovement = null;
        }

        #endregion

        #region Methods

        #region Load Movement

        private void LoadMovement(CharacterMovement characterMovement)
        {
            this.InitializeMovementSelections();
            this.IsShowingMovementEditor = true;
            this.CurrentCharacterMovement = characterMovement;
            this.eventAggregator.GetEvent<NeedDefaultCharacterRetrievalEvent>().Publish(this.LoadAvailableMovements);
            this.SelectedMovement = characterMovement.Movement;
        }

        private void LoadAvailableMovements(Character defaultCharacter)
        {
            this.defaultCharacter = defaultCharacter;
            string currentMovementName = this.CurrentCharacterMovement.Movement != null ? this.CurrentCharacterMovement.Movement.Name : "";
            var allMovements = defaultCharacter.Movements.Select((cm) => { return cm.Movement; }).Where(m => m != null).Distinct();
            var editingCharacterMovements = this.CurrentCharacterMovement.Character.Movements.Select((cm) => { return cm.Movement; }).Where(m => m != null && m.Name != currentMovementName).Distinct();
            this.AvailableMovements = new ObservableCollection<Movement>(allMovements.Except(editingCharacterMovements));
        }

        #endregion

        #region Rename Movement

        private void EnterMovementEditMode(object state)
        {
            if(SelectedMovement != null)
            {
                this.OriginalName = SelectedMovement.Name;
                OnEditModeEnter(state, null);
            }
        }

        private void CancelMovementEditMode(object state)
        {
            SelectedMovement.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitMovementRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);

                bool duplicateName = false;
                if (updatedName != this.OriginalName)
                    duplicateName = this.defaultCharacter.Movements.FirstOrDefault(m => m.Name == updatedName) != null; //this.CurrentCharacterMovement.Character.Movements.ContainsKey(updatedName);

                if (!duplicateName)
                {
                    RenameMovement(updatedName);
                    OnEditModeLeave(state, null);
                    this.SaveMovement(null);
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, "Rename Movement", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.CancelMovementEditMode(state);
                }
            }
        }

        private void RenameMovement(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            SelectedMovement.Name = updatedName;
            this.CurrentCharacterMovement.Movement = SelectedMovement;
            this.CurrentCharacterMovement.Name = updatedName;
            this.CurrentCharacterMovement.Character.Movements.UpdateKey(OriginalName, updatedName);
            // TODO: need to update each character that has this movement to use the updated name
            CharacterMovement cmDefault = this.defaultCharacter.Movements.FirstOrDefault(m => m.Name == OriginalName);
            if(cmDefault != null)
            {
                cmDefault.Name = updatedName;
                this.defaultCharacter.Movements.UpdateKey(OriginalName, updatedName);
            }
            
            
            OriginalName = null;
        }

        #endregion

        #region Add Movement

        private bool CanAddMovement(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void AddMovement(object state)
        {
            if (this.AvailableMovements == null)
                this.AvailableMovements = new ObservableCollection<Movement>();

            string validMovementName = GetNewValidMovementName();
            Movement movement = new Movement(validMovementName);

            if(this.CurrentCharacterMovement.Character != this.defaultCharacter || (this.CurrentCharacterMovement.Character == this.defaultCharacter && this.CurrentCharacterMovement.Movement != null))
            {
                CharacterMovement cmDefault = new CharacterMovement(movement.Name, this.defaultCharacter);
                cmDefault.Movement = movement;
                this.defaultCharacter.Movements.Add(cmDefault);

                if (this.CurrentCharacterMovement.Character == this.defaultCharacter)
                {
                    this.CurrentCharacterMovement = cmDefault;
                    this.AvailableMovements = new ObservableCollection<Movement> { movement };
                }
                else
                {
                    this.CurrentCharacterMovement.Movement = movement;
                    this.AvailableMovements.Add(movement);
                }
            }
            else
            {
                this.CurrentCharacterMovement.Movement = movement;
                this.AvailableMovements.Add(movement);
            }

            this.SelectedMovement = movement;

            OnMovementAdded(movement, null);

            this.SaveMovement(null);
        }

        public string GetNewValidMovementName(string name = "Movement")
        {
            string suffix = string.Empty;
            int i = 0;
            while ((this.defaultCharacter.Movements.Any((CharacterMovement cm) => { return cm.Movement != null && cm.Movement.Name == name + suffix; })))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return string.Format("{0}{1}", name, suffix).Trim();
        }

        #endregion

        #region Remove Movement

        private bool CanRemoveMovement(object state)
        {
            return this.SelectedMovement != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void RemoveMovement(object state)
        {
            this.eventAggregator.GetEvent<RemoveMovementEvent>().Publish(this.SelectedMovement.Name);
            //if (this.CurrentCharacterMovement.Character.Name != Constants.DEFAULT_CHARACTER_NAME)
            //{
            //    if (this.CurrentCharacterMovement.Character.DefaultMovement != null && this.CurrentCharacterMovement.Character.DefaultMovement.Name == this.SelectedMovement.Name)
            //        this.CurrentCharacterMovement.Character.DefaultMovement = null;
            //    if (this.CurrentCharacterMovement.Character.ActiveMovement != null && this.CurrentCharacterMovement.Character.ActiveMovement.Name == this.SelectedMovement.Name)
            //        this.CurrentCharacterMovement.Character.ActiveMovement = null;
            //}
            this.SaveMovement(null);
            this.CloseEditor();
        }

        #endregion

        #region Save Movement

        private bool CanSaveMovement(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SaveMovement(object state)
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(state);
        }

        #endregion

        #region Close Editor
        private void CloseEditor(object state = null)
        {
            this.CurrentCharacterMovement = null;
            this.IsShowingMovementEditor = false;
        }
        #endregion

        #region Demo Movement/Animations

        private bool CanDemoDirectionalMovement(object state)
        {
            MovementMember member = state as MovementMember;
            return member != null && member.MemberAbility != null && member.MemberAbility.Reference != null;
        }

        private void DemoDirectionalMovement(object state)
        {
            MovementMember member = state as MovementMember;
            member.MemberAbility.Reference.Play(false, this.CurrentCharacterMovement.Character);
        }

        private void GetDirectionalAbility(MovementDirection direction)
        {
            AnimatedAbility ability = null;
            MovementMember movementMember = null;
            switch(direction)
            {
                case MovementDirection.Left:
                   movementMember  = SelectedMovement.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Left);
                   ability = movementMember.MemberAbility.Reference;
                    break;
                case MovementDirection.Right:
                    break;
                case MovementDirection.Forward:
                    break;
                case MovementDirection.Backward:
                    break;
                case MovementDirection.Upward:
                    break;
                case MovementDirection.Downward:
                    break;
                case MovementDirection.Still:
                    break;
            }
        }

        #endregion

        #region Load Ability Editor

        private bool CanLoadAbilityEditor(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void LoadAbilityEditor(object state)
        {
            MovementMember member = state as MovementMember;
            eventAggregator.GetEvent<EditAbilityEvent>().Publish(new Tuple<AnimatedAbility, Character>(member.MemberAbility.Reference, this.defaultCharacter));
        }

        #endregion

        #region Attack Consistency
        private void AttackInitiated(Tuple<Character, Attack> tuple)
        {
            this.UpdateCommandsAndControls();
        }

        private void AttackEnded(object state)
        {
            if (state != null && state is AnimatedAbility)
            {
                this.UpdateCommandsAndControls();
            }
        }

        private void UpdateCommandsAndControls()
        {
            this.SaveMovementCommand.RaiseCanExecuteChanged();
            this.AddMovementCommand.RaiseCanExecuteChanged();
            this.RemoveMovementCommand.RaiseCanExecuteChanged();
            OnPropertyChanged("CanEditMovementOptions");
        }

        #endregion

        #region Set Default

        private bool CanSetDefaultMovement(object state)
        {
            return this.CurrentCharacterMovement != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SetDefaultMovement(object state)
        {
            if(this.IsDefaultMovementLoaded)
            {
                this.CurrentCharacterMovement.Character.DefaultMovement = this.CurrentCharacterMovement;
            }
            else
            {
                this.CurrentCharacterMovement.Character.DefaultMovement = null;
            }
            this.SaveMovement(null);
        }

        #endregion

        #region Animation Resources

        private void SelectedMemberAbility_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Resource")
            {
                (sender as AnimationElement).DisplayName = GetDisplayNameFromResourceName((sender as AnimationElement).Resource);
                SaveMovement(null);
                //DemoMovement(null);
            }
        }
        private void LoadResources(object state)
        {
            // publish a event to retrieve reference resources
            this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
        }

        private void LoadReferenceResource(ObservableCollection<AnimatedAbility> abilityCollection)
        {
            if (referenceAbilitiesCVS == null)
            {
                referenceAbilitiesCVS = new CollectionViewSource();
                referenceAbilitiesCVS.Source = new ObservableCollection<AnimationResource>(abilityCollection.Where(a => !a.IsAttack).Select((x) => { return new AnimationResource(x, x.Name); }));
                referenceAbilitiesCVS.View.Filter += ResourcesCVS_Filter;
            }
            else
            {
                var updatedAbilityResources = abilityCollection.Where(a => !a.IsAttack).Select((x) => { return new AnimationResource(x, x.Name); });
                var currentAbilityResources = referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>;
                var addedResources = updatedAbilityResources.Where(a => currentAbilityResources.Where(ca => ca.Name == a.Name && ca.Reference.Owner != null && a.Reference.Owner != null && ca.Reference.Owner.Name == a.Reference.Owner.Name).FirstOrDefault() == null);
                if (addedResources.Count() > 0)
                {
                    foreach (var addedResource in addedResources)
                    {
                        (referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>).Add(addedResource);
                    }
                }
                else
                {
                    var deletedResources = new List<AnimationResource>(currentAbilityResources.Where(ca => updatedAbilityResources.Where(a => a.Name == ca.Name && ca.Reference.Owner != null && a.Reference.Owner != null && ca.Reference.Owner.Name == a.Reference.Owner.Name).FirstOrDefault() == null));
                    if (deletedResources.Count() > 0)
                    {
                        foreach (var deletedResource in deletedResources)
                        {
                            var resourceToDelete = (referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>).First(ar => ar.Name == deletedResource.Name && ar.Reference.Owner != null && deletedResource.Reference.Owner != null && ar.Reference.Owner.Name == deletedResource.Reference.Owner.Name);
                            (referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>).Remove(resourceToDelete);
                        }
                    }
                }
            }
            OnPropertyChanged("ReferenceAbilitiesCVS");
        }

        private bool ResourcesCVS_Filter(object item)
        {
            AnimationResource animationRes = item as AnimationResource;
            
            if (string.IsNullOrWhiteSpace(Filter))
            {
                return true;
            }

            bool caseReferences = false;
            if (animationRes.Reference != null && animationRes.Reference.Owner != null)
            {
                caseReferences = new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Name) || new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Owner.Name);
            }
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.TagLine) || caseReferences;
        }

        #endregion

        #region Utility

        private string GetDisplayNameFromResourceName(string resourceName)
        {
            return Path.GetFileNameWithoutExtension(resourceName);
        }
        #endregion

        #endregion
    }
}
