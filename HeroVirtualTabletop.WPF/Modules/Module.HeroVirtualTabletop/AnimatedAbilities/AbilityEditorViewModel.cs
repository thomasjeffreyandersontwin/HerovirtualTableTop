<<<<<<< HEAD
﻿using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared.Events;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using System.Collections.ObjectModel;
using Module.Shared;
using System.Windows.Data;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AbilityEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;
        private IResourceRepository resourceRepository;

        public bool isUpdatingCollection = false;
        public object lastAnimationElementsStateToUpdate = null;

        #endregion

        #region Events

        public event EventHandler<CustomEventArgs<bool>> AnimationAdded;
        public void OnAnimationAdded(object sender, CustomEventArgs<bool> e)
        {
            if (AnimationAdded != null)
            {
                AnimationAdded(sender, e);
            }
        }

        public event EventHandler AnimationElementDraggedFromGrid;
        public void OnAnimationElementDraggedFromGrid(object sender, EventArgs e)
        {
            if (AnimationElementDraggedFromGrid != null)
            {
                AnimationElementDraggedFromGrid(sender, e);
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

        public event EventHandler SelectionChanged;
        public void OnSelectionChanged(object sender, EventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(sender, e);
            }
        }

        public event EventHandler<CustomEventArgs<ExpansionUpdateEvent>> ExpansionUpdateNeeded;
        public void OnExpansionUpdateNeeded(object sender, CustomEventArgs<ExpansionUpdateEvent> e)
        {
            if (ExpansionUpdateNeeded != null)
                ExpansionUpdateNeeded(sender, e);
        }

        #endregion

        #region Public Properties
        private Character owner;
        public Character Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
                OnPropertyChanged("Owner");
            }
        }

        private Attack currentAttackAbility;
        public Attack CurrentAttackAbility
        {
            get
            {
                return currentAttackAbility;
            }
            set
            {
                currentAttackAbility = value;
                OnPropertyChanged("CurrentAttackAbility");
                this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
            }
        }
        private AnimatedAbility currentAbility;
        public AnimatedAbility CurrentAbility
        {
            get
            {
                return currentAbility;
            }
            set
            {
                currentAbility = value;
                OnPropertyChanged("CurrentAbility");
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
                this.PasteAnimationCommand.RaiseCanExecuteChanged();
            }
        }
        private bool isShowingAblityEditor;
        public bool IsShowingAbilityEditor
        {
            get
            {
                return isShowingAblityEditor;
            }
            set
            {
                isShowingAblityEditor = value;
                OnPropertyChanged("IsShowingAbilityEditor");
            }
        }

        private IAnimationElement selectedAnimationElement;
        public IAnimationElement SelectedAnimationElement
        {
            get
            {
                return selectedAnimationElement;
            }
            set
            {
                if (selectedAnimationElement != null)
                    (selectedAnimationElement as AnimationElement).PropertyChanged -= SelectedAnimationElement_PropertyChanged;
                if (value != null)
                    (value as AnimationElement).PropertyChanged += SelectedAnimationElement_PropertyChanged;
                selectedAnimationElement = value;
                OnPropertyChanged("SelectedAnimationElement");
                OnPropertyChanged("CanPlayWithNext");
                Filter = string.Empty;
                OnSelectionChanged(value, null);
                this.RemoveAnimationCommand.RaiseCanExecuteChanged();
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
                this.CutAnimationCommand.RaiseCanExecuteChanged();
                this.PasteAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        private void SelectedAnimationElement_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Resource")
            {
                (sender as AnimationElement).DisplayName = GetDisplayNameFromResourceName((sender as AnimationElement).Resource);
                SaveAbility(null);
                DemoAnimation(null);
                SaveResources();
                this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            }
        }

        private IAnimationElement selectedAnimationParent;
        public IAnimationElement SelectedAnimationParent
        {
            get
            {
                return selectedAnimationParent;
            }
            set
            {
                selectedAnimationParent = value;
                this.RemoveAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        private AnimationSequenceType sequenceType;

        public AnimationSequenceType SequenceType
        {
            get
            {
                return sequenceType;
            }
            set
            {
                sequenceType = value;
                OnPropertyChanged("SequenceType");
            }
        }


        private bool isSequenceAbilitySelected;
        public bool IsSequenceAbilitySelected
        {
            get
            {
                return isSequenceAbilitySelected;
            }
            set
            {
                isSequenceAbilitySelected = value;
                OnPropertyChanged("IsSequenceAbilitySelected");
            }
        }

        private bool isPauseElementSelected;
        public bool IsPauseElementSelected
        {
            get
            {
                return isPauseElementSelected;
            }
            set
            {
                isPauseElementSelected = value;
                OnPropertyChanged("IsPauseElementSelected");
            }
        }

        private SequenceElement currentSequenceElement;
        public SequenceElement CurrentSequenceElement
        {
            get
            {
                return currentSequenceElement;
            }
            set
            {
                currentSequenceElement = value;
                OnPropertyChanged("CurrentSequenceElement");
            }
        }

        private PauseElement currentPauseElement;
        public PauseElement CurrentPauseElement
        {
            get
            {
                return currentPauseElement;
            }
            set
            {
                currentPauseElement = value;
                OnPropertyChanged("CurrentPauseElement");
                this.ConfigureUnitPauseCommand.RaiseCanExecuteChanged();
            }
        }

        private bool isReferenceAbilitySelected;
        public bool IsReferenceAbilitySelected
        {
            get
            {
                return isReferenceAbilitySelected;
            }
            set
            {
                isReferenceAbilitySelected = value;
                OnPropertyChanged("IsReferenceAbilitySelected");
            }
        }

        private ReferenceAbility currentReferenceElement;
        public ReferenceAbility CurrentReferenceElement
        {
            get
            {
                return currentReferenceElement;
            }
            set
            {
                currentReferenceElement = value;
                OnPropertyChanged("CurrentReferenceElement");
                this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            }
        }

        private IAnimationElement selectedAnimationElementRoot;
        public IAnimationElement SelectedAnimationElementRoot
        {
            get
            {
                return selectedAnimationElementRoot;
            }
            set
            {
                selectedAnimationElementRoot = value;
            }
        }
        public string OriginalName { get; set; }
        public string OriginalAnimationDisplayName { get; set; }
        private string editableAnimationDisplayName;
        public string EditableAnimationDisplayName
        {
            get
            {
                return editableAnimationDisplayName;
            }
            set
            {
                editableAnimationDisplayName = value;
                OnPropertyChanged("EditableAnimationDisplayName");
            }
        }

        private ObservableCollection<AnimationResource> movResources;
        public ReadOnlyObservableCollection<AnimationResource> MOVResources { get; private set; }

        private CollectionViewSource movResourcesCVS;
        public CollectionViewSource MOVResourcesCVS
        {
            get
            {
                return movResourcesCVS;
            }
        }

        private ObservableCollection<AnimationResource> fxResources;
        public ReadOnlyObservableCollection<AnimationResource> FXResources { get; private set; }

        private CollectionViewSource fxResourcesCVS;
        public CollectionViewSource FXResourcesCVS
        {
            get
            {
                return fxResourcesCVS;
            }
        }

        private ObservableCollection<AnimationResource> soundResources;
        public ReadOnlyObservableCollection<AnimationResource> SoundResources { get; private set; }

        private CollectionViewSource soundResourcesCVS;
        public CollectionViewSource SoundResourcesCVS
        {
            get
            {
                return soundResourcesCVS;
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
                if (SelectedAnimationElement != null)
                    switch (SelectedAnimationElement.Type)
                    {
                        case AnimationType.Movement:
                            movResourcesCVS.View.Refresh();
                            break;
                        case AnimationType.FX:
                            fxResourcesCVS.View.Refresh();
                            break;
                        case AnimationType.Sound:
                            soundResourcesCVS.View.Refresh();
                            break;
                        case AnimationType.Reference:
                            referenceAbilitiesCVS.View.Refresh();
                            break;
                    }
                OnPropertyChanged("Filter");
            }
        }

        public bool CanPlayWithNext
        {
            get
            {
                bool canPlayWithNext = false;
                if (SelectedAnimationElement != null)
                {
                    if (SelectedAnimationElement.Type == AnimationType.FX || SelectedAnimationElement.Type == AnimationType.Movement)
                    {
                        IAnimationElement next = GetNextAnimationElement(SelectedAnimationElement);
                        if (next != null && (next.Type == AnimationType.FX || next.Type == AnimationType.Movement))
                            canPlayWithNext = true;
                    }
                }
                return canPlayWithNext;
            }
        }

        private bool isAttackSelected;
        public bool IsAttackSelected
        {
            get
            {
                return isAttackSelected;
            }
            set
            {
                isAttackSelected = value;
                OnPropertyChanged("IsAttackSelected");
            }
        }

        private bool isHitSelected;
        public bool IsHitSelected
        {
            get
            {
                return isHitSelected;
            }
            set
            {
                isHitSelected = value;
                OnPropertyChanged("IsHitSelected");
            }
        }
        public bool CanEditAbilityOptions
        {
            get
            {
                return !Helper.GlobalVariables_IsPlayingAttack;
            }
        }
        #endregion

        #region Commands


        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> LoadResourcesCommand { get; private set; }
        public DelegateCommand<object> EnterAbilityEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitAbilityRenameCommand { get; private set; }
        public DelegateCommand<object> CancelAbilityEditModeCommand { get; private set; }
        public DelegateCommand<object> AddAnimationElementCommand { get; private set; }
        public DelegateCommand<object> SaveAbilityCommand { get; private set; }
        public DelegateCommand<object> RemoveAnimationCommand { get; private set; }
        public DelegateCommand<object> SaveSequenceCommand { get; private set; }
        public DelegateCommand<object> UpdateSelectedAnimationCommand { get; private set; }
        public DelegateCommand<object> SubmitAnimationElementRenameCommand { get; private set; }
        public DelegateCommand<object> EnterAnimationElementEditModeCommand { get; private set; }
        public DelegateCommand<object> CancelAnimationElementEditModeCommand { get; private set; }
        public DelegateCommand<object> DemoAnimatedAbilityCommand { get; private set; }
        public DelegateCommand<object> DemoAnimationCommand { get; private set; }
        public DelegateCommand<object> CloneAnimationCommand { get; private set; }
        public DelegateCommand<object> CutAnimationCommand { get; private set; }
        public DelegateCommand<object> LinkAnimationCommand { get; private set; }
        public DelegateCommand<object> PasteAnimationCommand { get; private set; }
        public DelegateCommand<object> UpdateReferenceTypeCommand { get; private set; }
        public DelegateCommand<object> ConfigureAttackAnimationCommand { get; private set; }
        public DelegateCommand<object> ConfigureUnitPauseCommand { get; private set; }

        #endregion

        #region Constructor

        public AbilityEditorViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, IResourceRepository resourceRepository, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.resourceRepository = resourceRepository;
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe(this.LoadAnimatedAbility);
            this.eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Subscribe(this.LoadReferenceResource);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.AttackEnded);
            // Unselect everything at the beginning
            this.InitializeAnimationElementSelections();
        }

        #endregion

        #region Initialization

        private void InitializeAnimationElementSelections()
        {
            this.SelectedAnimationElement = null;
            this.SelectedAnimationParent = null;
            this.IsSequenceAbilitySelected = false;
            this.IsReferenceAbilitySelected = false;
            this.IsPauseElementSelected = false;
            this.CurrentSequenceElement = null;
            this.CurrentReferenceElement = null;
            this.CurrentPauseElement = null;
            this.CurrentAbility = null;
            this.CurrentAttackAbility = null;
            this.Owner = null;
        }

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadAbility);
            this.LoadResourcesCommand = new DelegateCommand<object>(this.LoadResources);
            this.SubmitAbilityRenameCommand = new DelegateCommand<object>(this.SubmitAbilityRename);
            this.SaveAbilityCommand = new DelegateCommand<object>(this.SaveAbility, this.CanSaveAbility);
            this.SaveSequenceCommand = new DelegateCommand<object>(this.SaveSequence, this.CanSaveSequence);
            this.EnterAbilityEditModeCommand = new DelegateCommand<object>(this.EnterAbilityEditMode);
            this.CancelAbilityEditModeCommand = new DelegateCommand<object>(this.CancelAbilityEditMode);
            this.AddAnimationElementCommand = new DelegateCommand<object>(this.AddAnimationElement, this.CanAddAnimationElement);
            this.RemoveAnimationCommand = new DelegateCommand<object>(this.RemoveAnimation, this.CanRemoveAnimation);
            this.UpdateSelectedAnimationCommand = new DelegateCommand<object>(this.UpdateSelectedAnimation);
            this.SubmitAnimationElementRenameCommand = new DelegateCommand<object>(this.SubmitAnimationElementRename);
            this.EnterAnimationElementEditModeCommand = new DelegateCommand<object>(this.EnterAnimationElementEditMode, this.CanEnterAnimationElementEditMode);
            this.CancelAnimationElementEditModeCommand = new DelegateCommand<object>(this.CancelAnimationElementEditMode);
            this.DemoAnimatedAbilityCommand = new DelegateCommand<object>(this.DemoAnimatedAbility, this.CanDemoAnimation);
            this.DemoAnimationCommand = new DelegateCommand<object>(this.DemoAnimation, this.CanDemoAnimation);
            this.CloneAnimationCommand = new DelegateCommand<object>(this.CloneAnimation, this.CanCloneAnimation);
            this.CutAnimationCommand = new DelegateCommand<object>(this.CutAnimation, this.CanCutAnimation);
            this.PasteAnimationCommand = new DelegateCommand<object>(this.PasteAnimation, this.CanPasteAnimation);
            this.UpdateReferenceTypeCommand = new DelegateCommand<object>(this.UpdateReferenceType, this.CanUpdateReferenceType);
            this.ConfigureAttackAnimationCommand = new DelegateCommand<object>(this.ConfigureAttackAnimation, this.CanConfigureAttackAnimation);
            this.ConfigureUnitPauseCommand = new DelegateCommand<object>(this.ConfigureUnitPause, this.CanConfigureUnitPause);
        }

        #endregion

        #region Methods

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
            this.SaveAbilityCommand.RaiseCanExecuteChanged();
            this.SaveSequenceCommand.RaiseCanExecuteChanged();
            this.AddAnimationElementCommand.RaiseCanExecuteChanged();
            this.RemoveAnimationCommand.RaiseCanExecuteChanged();
            this.DemoAnimatedAbilityCommand.RaiseCanExecuteChanged();
            this.DemoAnimationCommand.RaiseCanExecuteChanged();
            this.CloneAnimationCommand.RaiseCanExecuteChanged();
            this.CutAnimationCommand.RaiseCanExecuteChanged();
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
            this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
            this.ConfigureUnitPauseCommand.RaiseCanExecuteChanged();
            OnPropertyChanged("CanEditAbilityOptions");
        }

        #endregion

        #region Update Selected Animation

        private void UpdateSelectedAnimation(object state)
        {
            if (state != null) // Update selection
            {
                if (!isUpdatingCollection)
                {
                    IAnimationElement parentAnimationElement;
                    Object selectedAnimationElement = Helper.GetCurrentSelectedAnimationInAnimationCollection(state, out parentAnimationElement);
                    if (selectedAnimationElement != null && selectedAnimationElement is IAnimationElement) // Only update if something is selected
                    {
                        this.SelectedAnimationElement = selectedAnimationElement as IAnimationElement;
                        this.SelectedAnimationParent = parentAnimationElement;
                        this.SetCurrentSequenceAnimation();
                        this.SetCurrentReferenceAbility();
                        this.SetCurrentPauseElement();
                    }
                    else if (selectedAnimationElement == null && (this.CurrentAbility == null || this.CurrentAbility.AnimationElements.Count == 0))
                    {
                        this.SelectedAnimationElement = null;
                        this.SelectedAnimationParent = null;
                        this.IsSequenceAbilitySelected = false;
                        this.IsReferenceAbilitySelected = false;
                        this.IsPauseElementSelected = false;
                        this.CurrentSequenceElement = null;
                        this.CurrentReferenceElement = null;
                        this.CurrentPauseElement = null;
                    }
                }
                else
                    this.lastAnimationElementsStateToUpdate = state;
            }
            else // Unselect
            {
                this.SelectedAnimationElement = null;
                this.SelectedAnimationParent = null;
                this.IsSequenceAbilitySelected = false;
                this.IsReferenceAbilitySelected = false;
                this.IsPauseElementSelected = false;
                this.CurrentSequenceElement = null;
                this.CurrentReferenceElement = null;
                this.CurrentPauseElement = null;
                OnAnimationAdded(null, null);
            }
        }

        private void LockModelAndMemberUpdate(bool isLocked)
        {
            this.isUpdatingCollection = isLocked;
            if (!isLocked)
                this.UpdateAnimationElementTree();
        }

        private void UpdateAnimationElementTree()
        {
            // Update character crowd if necessary
            if (this.lastAnimationElementsStateToUpdate != null)
            {
                this.UpdateSelectedAnimation(lastAnimationElementsStateToUpdate);
                this.lastAnimationElementsStateToUpdate = null;
            }
        }

        private void SetCurrentSequenceAnimation()
        {
            if (this.SelectedAnimationElement is SequenceElement)
            {
                this.IsSequenceAbilitySelected = true;
                this.CurrentSequenceElement = this.SelectedAnimationElement as SequenceElement;
            }
            else if (this.SelectedAnimationParent is SequenceElement)
            {
                this.IsSequenceAbilitySelected = true;
                this.CurrentSequenceElement = this.SelectedAnimationParent as SequenceElement;
            }
            else
            {
                this.IsSequenceAbilitySelected = false;
                this.CurrentSequenceElement = null;
            }
        }

        private void SetCurrentReferenceAbility()
        {
            if (this.SelectedAnimationElement is ReferenceAbility)
            {
                this.CurrentReferenceElement = this.SelectedAnimationElement as ReferenceAbility;
                this.IsReferenceAbilitySelected = true;
                //this.LoadReferenceResource();
            }
            else
            {
                this.CurrentReferenceElement = null;
                this.IsReferenceAbilitySelected = false;
            }
        }

        private void SetCurrentPauseElement()
        {
            if(this.SelectedAnimationElement is PauseElement)
            {
                this.CurrentPauseElement = this.SelectedAnimationElement as PauseElement;
                this.IsPauseElementSelected = true;
            }
            else
            {
                this.CurrentPauseElement = null;
                this.IsPauseElementSelected = false;
            }
        }
        #endregion

        #region Load Animated Ability
        private void LoadAnimatedAbility(Tuple<AnimatedAbility, Character> tuple)
        {
            this.InitializeAnimationElementSelections();
            this.IsShowingAbilityEditor = true;
            this.CurrentAttackAbility = tuple.Item1 as Attack;
            this.CurrentAbility = this.CurrentAttackAbility as AnimatedAbility;
            this.Owner = tuple.Item2 as Character;
            if (this.CurrentAttackAbility.IsAttack)
            {
                this.IsAttackSelected = true;
                this.IsHitSelected = false;
            }
        }
        private void UnloadAbility(object state = null)
        {
            this.CurrentAttackAbility = null;
            //this.Owner.AvailableIdentities.CollectionChanged -= AvailableIdentities_CollectionChanged;
            this.Owner = null;
            this.IsShowingAbilityEditor = false;
        }

        #endregion

        #region Rename
        private void EnterAbilityEditMode(object state)
        {
            this.OriginalName = CurrentAttackAbility.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelAbilityEditMode(object state)
        {
            CurrentAttackAbility.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitAbilityRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);

                bool duplicateName = false;
                if (updatedName != this.OriginalName)
                    duplicateName = this.Owner.AnimatedAbilities.ContainsKey(updatedName);

                if (!duplicateName)
                {
                    RenameAbility(updatedName);
                    OnEditModeLeave(state, null);
                    this.SaveAbility(null);
                    this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, "Rename Ability", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.CancelAbilityEditMode(state);
                }
            }
        }

        private void RenameAbility(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            CurrentAttackAbility.Name = updatedName;
            Owner.AnimatedAbilities.UpdateKey(OriginalName, updatedName);
            OriginalName = null;
        }

        private bool CanEnterAnimationElementEditMode(object state)
        {
            return this.SelectedAnimationElement is PauseElement;
        }

        private void EnterAnimationElementEditMode(object state)
        {
            this.OriginalAnimationDisplayName = (this.SelectedAnimationElement as AnimationElement).DisplayName;
            if (this.SelectedAnimationElement is PauseElement)
                this.EditableAnimationDisplayName = (this.SelectedAnimationElement as PauseElement).Time.ToString();
            OnEditModeEnter(state, null);
        }

        private void CancelAnimationElementEditMode(object state)
        {
            (this.SelectedAnimationElement as AnimationElement).DisplayName = this.OriginalAnimationDisplayName;
            this.OriginalAnimationDisplayName = "";
            OnEditModeLeave(state, null);
        }
        private void SubmitAnimationElementRename(object state)
        {
            if (this.SelectedAnimationElement is PauseElement && this.OriginalAnimationDisplayName != "") // Original Display Name empty means we already cancelled the rename
            {
                string pausePeriod = Helper.GetTextFromControlObject(state);
                int period;
                if (!Int32.TryParse(pausePeriod, out period))
                    pausePeriod = "1";
                else
                    (this.SelectedAnimationElement as PauseElement).Time = period;

                (this.SelectedAnimationElement as PauseElement).DisplayName = "Pause " + pausePeriod.ToString();
                this.OriginalAnimationDisplayName = "";
                OnEditModeLeave(state, null);
                this.SaveAbility(null);
            }
        }
        #endregion

        #region Add Animation Element

        private bool CanAddAnimationElement(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack; 
        }

        private void AddAnimationElement(object state)
        {
            AnimationType animationType = (AnimationType)state;
            AnimationElement animationElement = this.GetAnimationElement(animationType);
            this.AddAnimationElement(animationElement);
            OnAnimationAdded(animationElement, null);
            this.SaveAbility(null);
        }

        private void AddAnimationElement(AnimationElement animationElement)
        {
            int order = GetAppropriateOrderForAnimationElement();
            if (!this.IsSequenceAbilitySelected)
                this.CurrentAbility.AddAnimationElement(animationElement, order);
            else
            {
                if (this.SelectedAnimationElement is SequenceElement)
                    (this.SelectedAnimationElement as SequenceElement).AddAnimationElement(animationElement, order);
                else
                    (this.SelectedAnimationParent as SequenceElement).AddAnimationElement(animationElement, order);
            }
            this.CloneAnimationCommand.RaiseCanExecuteChanged();
        }

        private AnimationElement GetAnimationElement(AnimationType abilityType)
        {
            AnimationElement animationElement = null;
            List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            string fullName = "";
            switch (abilityType)
            {
                case AnimationType.Movement:
                    animationElement = new MOVElement("", "");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Movement, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.FX:
                    animationElement = new FXEffectElement("", "");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.FX, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sound:
                    animationElement = new SoundElement("", "");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Sound, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sequence:
                    animationElement = new SequenceElement("");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Sequence, flattenedList);
                    animationElement.Name = fullName;
                    (animationElement as SequenceElement).SequenceType = AnimationSequenceType.And;
                    animationElement.DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                    break;
                case AnimationType.Pause:
                    animationElement = new PauseElement("", 1);
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Pause, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = "Pause " + (animationElement as PauseElement).Time.ToString();
                    break;
                case AnimationType.Reference:
                    animationElement = new ReferenceAbility("", null);
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Reference, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    //this.LoadReferenceResource();
                    break;
            }

            return animationElement;
        }
        private int GetAppropriateOrderForAnimationElement()
        {
            int order = 0;
            if (this.SelectedAnimationParent == null)
            {
                if (this.SelectedAnimationElement == null)
                {
                    order = this.CurrentAbility.LastOrder + 1;
                }
                else if (this.SelectedAnimationElement is SequenceElement)
                {
                    order = (this.SelectedAnimationElement as SequenceElement).LastOrder + 1;
                }
                else
                {
                    order = this.SelectedAnimationElement.Order + 1;
                    //order = prevOrder + 1;
                    //foreach (var element in this.CurrentAbility.AnimationElements.Where(e => e.Order > prevOrder))
                    //    element.Order += 1;
                }
            }
            else
            {
                if (this.SelectedAnimationParent is SequenceElement)
                {
                    order = this.SelectedAnimationElement.Order + 1;
                    //order = prevOrder + 1;
                    //foreach (var element in (this.SelectedAnimationParent as SequenceElement).AnimationElements.Where(e => e.Order > prevOrder))
                    //    element.Order += 1;
                }
            }
            return order;
        }
        

        #endregion

        #region Save Ability

        private bool CanSaveAbility(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SaveResources()
        {
            this.resourceRepository.SaveMoveResources(this.movResources.ToList());
            this.resourceRepository.SaveFXResources(this.fxResources.ToList());
            this.resourceRepository.SaveSoundResources(this.soundResources.ToList());
        }

        private void SaveAbility(object state)
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(state);
            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
        }

        #endregion

        #region Load Resources
        private void LoadResources(object state)
        {
            List<AnimationResource> moveResourceCollection = this.resourceRepository.GetMoveResources();
            movResources = new ObservableCollection<AnimationResource>(moveResourceCollection);
            MOVResources = new ReadOnlyObservableCollection<AnimationResource>(movResources);

            List<AnimationResource> fxResourceCollection = this.resourceRepository.GetFXResources();
            fxResources = new ObservableCollection<AnimationResource>(fxResourceCollection);
            FXResources = new ReadOnlyObservableCollection<AnimationResource>(fxResources);

            List<AnimationResource> soundResourceCollection = this.resourceRepository.GetSoundResources();
            soundResources = new ObservableCollection<AnimationResource>(soundResourceCollection);
            SoundResources = new ReadOnlyObservableCollection<AnimationResource>(soundResources);

            movResourcesCVS = new CollectionViewSource();
            movResourcesCVS.Source = MOVResources;
            MOVResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("MOVResourcesCVS");

            fxResourcesCVS = new CollectionViewSource();
            fxResourcesCVS.Source = FXResources;
            fxResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("FXResourcesCVS");

            soundResourcesCVS = new CollectionViewSource();
            soundResourcesCVS.Source = SoundResources;
            soundResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("SoundResourcesCVS");
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
            if (animationRes.Reference == this.CurrentAbility)
                return false;
            if (animationRes.Reference != null)
            {
                AnimatedAbility reference = animationRes.Reference;
                if (reference != null && reference.AnimationElements != null && reference.AnimationElements.Count > 0)
                {
                    if (reference.AnimationElements.Contains(this.CurrentAbility))
                        return false;
                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(reference.AnimationElements.ToList());
                    if (elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                        return false;
                }
                ////Check if the referenced ability contains the parent ability
                //if (reference.AnimationElements.Contains(this.CurrentAbility))
                //    return false;
                ////Check if inside the referenced ability there's any reference to the current ability
                //if (reference.AnimationElements.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                //    return false;
            }
            if (string.IsNullOrWhiteSpace(Filter))
            {
                return true;
            }


            if (SelectedAnimationElement != null && SelectedAnimationElement.Resource == animationRes)
            {
                return true;
            }
            bool caseReferences = false;
            if (animationRes.Reference != null && animationRes.Reference.Owner != null)
            {
                caseReferences = new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Name) || new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Owner.Name);
            }
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.TagLine) || new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Name) || caseReferences;
        }

        #endregion

        #region Save Sequence

        private bool CanSaveSequence(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SaveSequence(object state)
        {
            if (this.CurrentSequenceElement != null)
            {
                this.CurrentSequenceElement.DisplayName = "Sequence: " + this.CurrentSequenceElement.SequenceType.ToString();
            }
            this.SaveAbility(state);
        }
        #endregion

        #region Remove Animation

        private bool CanRemoveAnimation(object state)
        {
            return this.SelectedAnimationElement != null && !Helper.GlobalVariables_IsPlayingAttack; 
        }

        private void RemoveAnimation(object state)
        {
            this.LockModelAndMemberUpdate(true);
            if (this.SelectedAnimationParent != null)
            {
                this.DeleteAnimationElementFromParentElementByName(this.SelectedAnimationParent, this.SelectedAnimationElement.Name);
            }
            else
            {
                this.CurrentAbility.RemoveAnimationElement(this.SelectedAnimationElement.Name);
            }
            this.SaveAbility(null);
            this.LockModelAndMemberUpdate(false);
        }

        private void DeleteAnimationElementFromParentElementByName(IAnimationElement parent, string nameOfDeletingAnimation)
        {
            SequenceElement parentSequenceElement = parent as SequenceElement;
            if (parentSequenceElement != null && parentSequenceElement.AnimationElements.Count > 0)
            {
                //var anim = parentSequenceElement.AnimationElements.Where(a => a.Name == nameOfDeletingAnimation).FirstOrDefault();
                parentSequenceElement.RemoveAnimationElement(nameOfDeletingAnimation);
                if (parentSequenceElement.AnimationElements.Count == 0 && parentSequenceElement.Name == this.SelectedAnimationElementRoot.Name)
                {
                    this.SelectedAnimationElementRoot = null;
                }
            }
            OnExpansionUpdateNeeded(parent, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
        }

        #endregion

        #region Utility Methods

        private List<AnimationElement> GetFlattenedAnimationList(List<AnimationElement> animationElementList)
        {
            List<AnimationElement> _list = new List<AnimationElement>();
            foreach (AnimationElement animationElement in animationElementList)
            {
                if (animationElement is SequenceElement)
                {
                    SequenceElement sequenceElement = (animationElement as SequenceElement);
                    if (sequenceElement.AnimationElements != null && sequenceElement.AnimationElements.Count > 0)
                        _list.AddRange(GetFlattenedAnimationList(sequenceElement.AnimationElements.ToList()));
                }
                _list.Add(animationElement);
            }
            return _list;
        }

        private string GetDisplayNameFromResourceName(string resourceName)
        {
            return Path.GetFileNameWithoutExtension(resourceName);
        }

        private IAnimationElement GetNextAnimationElement(IAnimationElement selectedAnimationElement)
        {
            if (selectedAnimationElement.Order == currentAbility.LastOrder)
                return null;
            return currentAbility.AnimationElements.Where(x => { return x.Order == selectedAnimationElement.Order + 1; }).FirstOrDefault();
        }
        #endregion

        #region Demo Animation

        private bool CanDemoAnimation(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void DemoAnimatedAbility(object state)
        {
            Character currentTarget = GetCurrentTarget();
            this.CurrentAbility.Play(Target: currentTarget);
        }

        private void DemoAnimation(object state)
        {
            Character currentTarget = GetCurrentTarget();
            if (this.SelectedAnimationElement != null)
                this.SelectedAnimationElement.Play(Target: currentTarget, forcePlay: true);
        }

        private Character GetCurrentTarget()
        {
            Character currentTarget = null;
            if (!this.CurrentAbility.PlayOnTargeted)
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
            return currentTarget;
        }

        private void SpawnAndTargetOwnerCharacter()
        {
            if (this.Owner != null) // Will be null if the editor isn't loaded yet
            {
                if (!this.Owner.HasBeenSpawned)
                {
                    Crowds.CrowdMemberModel member = this.Owner as Crowds.CrowdMemberModel;
                    if (member.RosterCrowd == null)
                        this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(new Tuple<Crowds.CrowdMemberModel, Crowds.CrowdModel>(member, member.RosterCrowd as Crowds.CrowdModel));
                    member.Spawn(false);
                }
                this.Owner.Target(false);
            }
        }
        #endregion

        #region Clone Animation

        private bool CanCloneAnimation(object state)
        {
            return (this.SelectedAnimationElement != null || (this.SelectedAnimationElement == null && this.CurrentAbility != null && this.CurrentAbility.AnimationElements != null && this.CurrentAbility.AnimationElements.Count > 0)) && !Helper.GlobalVariables_IsPlayingAttack; 
        }
        private void CloneAnimation(object state)
        {
            if (this.SelectedAnimationElement != null)
                Helper.GlobalClipboardObject = this.SelectedAnimationElement; // any animation element
            else
                Helper.GlobalClipboardObject = this.CurrentAbility; // Sequence element
            Helper.GlobalClipboardAction = ClipboardAction.Clone;
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Cut Animation
        private bool CanCutAnimation(object state)
        {
            return (this.SelectedAnimationElement != null) && !Helper.GlobalVariables_IsPlayingAttack; 
        }
        private void CutAnimation(object state)
        {
            if (this.SelectedAnimationParent != null)
            {
                Helper.GlobalClipboardObject = this.SelectedAnimationElement;
                Helper.GlobalClipboardObjectParent = this.SelectedAnimationParent;
            }
            else
            {
                Helper.GlobalClipboardObject = this.SelectedAnimationElement;
                Helper.GlobalClipboardObjectParent = this.CurrentAbility;
            }
            Helper.GlobalClipboardAction = ClipboardAction.Cut;
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Paste Animation
        private bool CanPasteAnimation(object state)
        {
            bool canPaste = false;
            if(!Helper.GlobalVariables_IsPlayingAttack)
            {
                switch (Helper.GlobalClipboardAction)
                {
                    case ClipboardAction.Clone:
                        if (Helper.GlobalClipboardObject != null)
                        {
                            if (Helper.GlobalClipboardObject is SequenceElement)
                            {
                                SequenceElement seqElement = Helper.GlobalClipboardObject as SequenceElement;
                                List<AnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
                                if (!(elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; })))
                                    canPaste = true;
                            }
                            else if (Helper.GlobalClipboardObject is ReferenceAbility)
                            {
                                ReferenceAbility refAbility = Helper.GlobalClipboardObject as ReferenceAbility;
                                if (refAbility.Reference == this.CurrentAbility)
                                    canPaste = false;
                                else if (refAbility.Reference != null && refAbility.Reference.AnimationElements != null && refAbility.Reference.AnimationElements.Count > 0)
                                {
                                    bool refexists = false;
                                    if (refAbility.Reference.AnimationElements.Contains(this.CurrentAbility))
                                        refexists = true;
                                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
                                    if (elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                                        refexists = true;
                                    if (!refexists)
                                        canPaste = true;
                                }
                                else
                                    canPaste = true;
                            }
                            else
                                canPaste = true;
                        }
                        break;
                    case ClipboardAction.Cut:
                        if (Helper.GlobalClipboardObject != null && Helper.GlobalClipboardObjectParent != null)
                        {
                            if (Helper.GlobalClipboardObject is SequenceElement)
                            {
                                SequenceElement seqElement = Helper.GlobalClipboardObject as SequenceElement;
                                List<AnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
                                if (!(elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; })))
                                    canPaste = true;
                            }
                            else if (Helper.GlobalClipboardObject is ReferenceAbility)
                            {
                                ReferenceAbility refAbility = Helper.GlobalClipboardObject as ReferenceAbility;
                                if (refAbility.Reference == this.CurrentAbility)
                                    canPaste = false;
                                else if (refAbility.Reference != null && refAbility.Reference.AnimationElements != null && refAbility.Reference.AnimationElements.Count > 0)
                                {
                                    bool refexists = false;
                                    if (refAbility.Reference.AnimationElements.Contains(this.CurrentAbility))
                                        refexists = true;
                                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
                                    if (elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                                        refexists = true;
                                    if (!refexists)
                                        canPaste = true;
                                }
                                else
                                    canPaste = true;
                            }
                            else
                                canPaste = true;
                        }
                        break;
                }
            }
            return canPaste;
        }
        private void PasteAnimation(object state)
        {
            // Lock animation Tree from updating
            this.LockModelAndMemberUpdate(true);
            List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            switch (Helper.GlobalClipboardAction)
            {
                case ClipboardAction.Clone:
                    {
                        IAnimationElement animationElement = (Helper.GlobalClipboardObject as AnimationElement).Clone() as IAnimationElement;
                        animationElement.Name = AnimatedAbility.GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if (animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, null);
                        this.SaveAbility(null);
                        break;
                    }
                case ClipboardAction.Cut:
                    {
                        SequenceElement parentSequenceElement = Helper.GlobalClipboardObjectParent as SequenceElement;
                        AnimationElement animationElement = Helper.GlobalClipboardObject as AnimationElement;
                        parentSequenceElement.RemoveAnimationElement(animationElement);
                        animationElement.Name = AnimatedAbility.GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if (animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, new CustomEventArgs<bool>() { Value = false });
                        this.SaveAbility(null);
                        break;
                    }
            }
            if (SelectedAnimationElement != null)
            {
                OnExpansionUpdateNeeded(this.SelectedAnimationElement, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Paste });
            }
            // UnLock character crowd Tree from updating
            this.LockModelAndMemberUpdate(false);
            Helper.GlobalClipboardObject = null;
            Helper.GlobalClipboardObjectParent = null;
        }

        #endregion

        #region Reference Ability
        private bool CanUpdateReferenceType(object state)
        {
            return this.CurrentReferenceElement != null && this.CurrentReferenceElement.Reference != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void UpdateReferenceType(object state)
        {
            if (this.CurrentReferenceElement != null)
            {
                if (this.CurrentReferenceElement.ReferenceType == ReferenceType.Copy)
                {
                    this.LockModelAndMemberUpdate(true);
                    List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
                    SequenceElement sequenceElement = (this.CurrentReferenceElement.Reference).Clone() as SequenceElement;
                    int order = this.CurrentReferenceElement.Order;
                    sequenceElement.Name = AnimatedAbility.GetAppropriateAnimationName(sequenceElement.Type, flattenedList);
                    if (sequenceElement is SequenceElement && string.IsNullOrEmpty((sequenceElement as AnimationElement).DisplayName))
                        (sequenceElement as AnimationElement).DisplayName = "Sequence: " + (sequenceElement as SequenceElement).SequenceType.ToString();
                    this.RemoveAnimation(null);
                    if (this.SelectedAnimationElement is SequenceElement)
                        (this.SelectedAnimationElement as SequenceElement).AddAnimationElement(sequenceElement, order);
                    else if (this.SelectedAnimationParent is SequenceElement)
                        (this.SelectedAnimationParent as SequenceElement).AddAnimationElement(sequenceElement, order);
                    else
                        this.CurrentAbility.AddAnimationElement(sequenceElement, order);
                    OnAnimationAdded(sequenceElement, null);
                    this.SaveAbility(null);
                    this.CurrentReferenceElement = null;
                    this.IsReferenceAbilitySelected = false;
                    this.LockModelAndMemberUpdate(false);
                }
            }
        }

        #endregion

        #region Unit Pause

        private bool CanConfigureUnitPause(object state)
        {
            return Helper.GlobalVariables_IsPlayingAttack == false;
        }

        private void ConfigureUnitPause(object state)
        {
            if(this.CurrentPauseElement != null)
            {
                if (this.CurrentPauseElement.IsUnitPause)
                    this.CurrentPauseElement.DisplayName = "Pause 1";
                else
                    this.CurrentPauseElement.DisplayName = "Pause " + this.CurrentPauseElement.Time.ToString();                
            }
            this.SaveAbility(state);
        } 

        #endregion

        #region Attack/Defend Animations

        private bool CanConfigureAttackAnimation(object state)
        {
            bool canConfigureAttack = false;
            if (!Helper.GlobalVariables_IsPlayingAttack)
            {
                if (state == null)
                    canConfigureAttack = true;
                else
                    canConfigureAttack = this.CurrentAttackAbility != null && this.CurrentAttackAbility.IsAttack; 
            }

            return canConfigureAttack;
        }

        private void ConfigureAttackAnimation(object state)
        {
            if (state == null)
            {
                if (this.CurrentAttackAbility.IsAttack)
                {
                    this.IsAttackSelected = true;
                }
                else
                {
                    this.IsAttackSelected = false;
                }
                this.IsHitSelected = false;
                this.CurrentAbility = this.CurrentAttackAbility;
                this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
                this.SaveAbility(null);
            }
            else if (state.ToString() == "Attack")
            {
                this.IsAttackSelected = true;
                this.IsHitSelected = false;
                this.CurrentAbility = this.CurrentAttackAbility;
            }
            else if (state.ToString() == "OnHit")
            {
                this.IsAttackSelected = false;
                this.IsHitSelected = true;
                this.CurrentAbility = this.CurrentAttackAbility.OnHitAnimation;
            }
        }

        #endregion

        #region Move Animation Element
        /// <summary>
        /// This method is used for Drag drop inside the treeview and performs a cut paste under the hood
        /// </summary>
        /// <param name="sourceElement"></param>
        /// <param name="targetElement"></param>
        public void MoveSelectedAnimationElement(SequenceElement targetElementParent, int order)
        {
            AnimationElement sourceElement = null;
            SequenceElement sourceElementParent = null;
            if (this.SelectedAnimationParent != null)
            {
                sourceElement = this.SelectedAnimationElement as AnimationElement;
                sourceElementParent = this.SelectedAnimationParent as SequenceElement;
            }
            else
            {
                sourceElement = this.SelectedAnimationElement as AnimationElement;
                sourceElementParent = this.CurrentAbility;
            }
            SequenceElement destinationElementParent = targetElementParent ?? this.CurrentAbility;
            if (sourceElement != null && sourceElementParent != null)
            {
                List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
                sourceElementParent.RemoveAnimationElement(sourceElement);
                sourceElement.Name = AnimatedAbility.GetAppropriateAnimationName(sourceElement.Type, flattenedList);
                if (sourceElement is SequenceElement && string.IsNullOrEmpty((sourceElement as AnimationElement).DisplayName))
                    (sourceElement as AnimationElement).DisplayName = "Sequence: " + (sourceElement as SequenceElement).SequenceType.ToString();
                destinationElementParent.AddAnimationElement(sourceElement as AnimationElement, order);
                OnAnimationAdded(sourceElement, new CustomEventArgs<bool>() { Value = false });
                this.SaveAbility(null);
            }
        }
        /// <summary>
        /// Drag drop between refernce ability grid and animations treview
        /// </summary>
        /// <param name="referenceAbility"></param>
        /// <param name="targetElementParent"></param>
        /// <param name="order"></param>
        public void MoveReferenceAbilityToAnimationElements(AnimatedAbility referenceAbility, SequenceElement targetElementParent, int order)
        {
            SequenceElement destinationElementParent = targetElementParent ?? this.CurrentAbility;
            if (referenceAbility != null)
            {
                AnimationElement animationElement = this.GetAnimationElement(AnimationType.Reference);
                (animationElement as ReferenceAbility).Reference = referenceAbility;
                animationElement.DisplayName = GetDisplayNameFromResourceName(referenceAbility.Name);
                destinationElementParent.AddAnimationElement(animationElement, order);
                OnAnimationElementDraggedFromGrid(animationElement, null);
                SaveAbility(null);
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion
    }
}
=======
﻿using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared.Events;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using System.Collections.ObjectModel;
using Module.Shared;
using System.Windows.Data;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AbilityEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;
        private IResourceRepository resourceRepository;

        public bool isUpdatingCollection = false;
        public object lastAnimationElementsStateToUpdate = null;

        #endregion

        #region Events

        public event EventHandler<CustomEventArgs<bool>> AnimationAdded;
        public void OnAnimationAdded(object sender, CustomEventArgs<bool> e)
        {
            if (AnimationAdded != null)
            {
                AnimationAdded(sender, e);
            }
        }

        public event EventHandler AnimationElementDraggedFromGrid;
        public void OnAnimationElementDraggedFromGrid(object sender, EventArgs e)
        {
            if (AnimationElementDraggedFromGrid != null)
            {
                AnimationElementDraggedFromGrid(sender, e);
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

        public event EventHandler SelectionChanged;
        public void OnSelectionChanged(object sender, EventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(sender, e);
            }
        }

        public event EventHandler<CustomEventArgs<ExpansionUpdateEvent>> ExpansionUpdateNeeded;
        public void OnExpansionUpdateNeeded(object sender, CustomEventArgs<ExpansionUpdateEvent> e)
        {
            if (ExpansionUpdateNeeded != null)
                ExpansionUpdateNeeded(sender, e);
        }

        #endregion

        #region Public Properties
        private Character owner;
        public Character Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
                OnPropertyChanged("Owner");
            }
        }

        private Attack currentAttackAbility;
        public Attack CurrentAttackAbility
        {
            get
            {
                return currentAttackAbility;
            }
            set
            {
                currentAttackAbility = value;
                OnPropertyChanged("CurrentAttackAbility");
                this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
            }
        }
        private AnimatedAbility currentAbility;
        public AnimatedAbility CurrentAbility
        {
            get
            {
                return currentAbility;
            }
            set
            {
                currentAbility = value;
                OnPropertyChanged("CurrentAbility");
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
                this.PasteAnimationCommand.RaiseCanExecuteChanged();
            }
        }
        private bool isShowingAblityEditor;
        public bool IsShowingAbilityEditor
        {
            get
            {
                return isShowingAblityEditor;
            }
            set
            {
                isShowingAblityEditor = value;
                OnPropertyChanged("IsShowingAbilityEditor");
            }
        }

        private IAnimationElement selectedAnimationElement;
        public IAnimationElement SelectedAnimationElement
        {
            get
            {
                return selectedAnimationElement;
            }
            set
            {
                if (selectedAnimationElement != null)
                    (selectedAnimationElement as AnimationElement).PropertyChanged -= SelectedAnimationElement_PropertyChanged;
                if (value != null)
                    (value as AnimationElement).PropertyChanged += SelectedAnimationElement_PropertyChanged;
                selectedAnimationElement = value;
                OnPropertyChanged("SelectedAnimationElement");
                OnPropertyChanged("CanPlayWithNext");
                Filter = string.Empty;
                OnSelectionChanged(value, null);
                this.RemoveAnimationCommand.RaiseCanExecuteChanged();
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
                this.CutAnimationCommand.RaiseCanExecuteChanged();
                this.PasteAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        private void SelectedAnimationElement_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Resource")
            {
                (sender as AnimationElement).DisplayName = GetDisplayNameFromResourceName((sender as AnimationElement).Resource);
                SaveAbility(null);
                DemoAnimation(null);
                SaveResources();
                this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            }
        }

        private IAnimationElement selectedAnimationParent;
        public IAnimationElement SelectedAnimationParent
        {
            get
            {
                return selectedAnimationParent;
            }
            set
            {
                selectedAnimationParent = value;
                this.RemoveAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        private AnimationSequenceType sequenceType;

        public AnimationSequenceType SequenceType
        {
            get
            {
                return sequenceType;
            }
            set
            {
                sequenceType = value;
                OnPropertyChanged("SequenceType");
            }
        }


        private bool isSequenceAbilitySelected;
        public bool IsSequenceAbilitySelected
        {
            get
            {
                return isSequenceAbilitySelected;
            }
            set
            {
                isSequenceAbilitySelected = value;
                OnPropertyChanged("IsSequenceAbilitySelected");
            }
        }

        private bool isPauseElementSelected;
        public bool IsPauseElementSelected
        {
            get
            {
                return isPauseElementSelected;
            }
            set
            {
                isPauseElementSelected = value;
                OnPropertyChanged("IsPauseElementSelected");
            }
        }

        private SequenceElement currentSequenceElement;
        public SequenceElement CurrentSequenceElement
        {
            get
            {
                return currentSequenceElement;
            }
            set
            {
                currentSequenceElement = value;
                OnPropertyChanged("CurrentSequenceElement");
            }
        }

        private PauseElement currentPauseElement;
        public PauseElement CurrentPauseElement
        {
            get
            {
                return currentPauseElement;
            }
            set
            {
                currentPauseElement = value;
                OnPropertyChanged("CurrentPauseElement");
                this.ConfigureUnitPauseCommand.RaiseCanExecuteChanged();
            }
        }

        private bool isReferenceAbilitySelected;
        public bool IsReferenceAbilitySelected
        {
            get
            {
                return isReferenceAbilitySelected;
            }
            set
            {
                isReferenceAbilitySelected = value;
                OnPropertyChanged("IsReferenceAbilitySelected");
            }
        }

        private ReferenceAbility currentReferenceElement;
        public ReferenceAbility CurrentReferenceElement
        {
            get
            {
                return currentReferenceElement;
            }
            set
            {
                currentReferenceElement = value;
                OnPropertyChanged("CurrentReferenceElement");
                this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            }
        }

        private IAnimationElement selectedAnimationElementRoot;
        public IAnimationElement SelectedAnimationElementRoot
        {
            get
            {
                return selectedAnimationElementRoot;
            }
            set
            {
                selectedAnimationElementRoot = value;
            }
        }
        public string OriginalName { get; set; }
        public string OriginalAnimationDisplayName { get; set; }
        private string editableAnimationDisplayName;
        public string EditableAnimationDisplayName
        {
            get
            {
                return editableAnimationDisplayName;
            }
            set
            {
                editableAnimationDisplayName = value;
                OnPropertyChanged("EditableAnimationDisplayName");
            }
        }

        private ObservableCollection<AnimationResource> movResources;
        public ReadOnlyObservableCollection<AnimationResource> MOVResources { get; private set; }

        private CollectionViewSource movResourcesCVS;
        public CollectionViewSource MOVResourcesCVS
        {
            get
            {
                return movResourcesCVS;
            }
        }

        private ObservableCollection<AnimationResource> fxResources;
        public ReadOnlyObservableCollection<AnimationResource> FXResources { get; private set; }

        private CollectionViewSource fxResourcesCVS;
        public CollectionViewSource FXResourcesCVS
        {
            get
            {
                return fxResourcesCVS;
            }
        }

        private ObservableCollection<AnimationResource> soundResources;
        public ReadOnlyObservableCollection<AnimationResource> SoundResources { get; private set; }

        private CollectionViewSource soundResourcesCVS;
        public CollectionViewSource SoundResourcesCVS
        {
            get
            {
                return soundResourcesCVS;
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
                if (SelectedAnimationElement != null)
                    switch (SelectedAnimationElement.Type)
                    {
                        case AnimationType.Movement:
                            movResourcesCVS.View.Refresh();
                            break;
                        case AnimationType.FX:
                            fxResourcesCVS.View.Refresh();
                            break;
                        case AnimationType.Sound:
                            soundResourcesCVS.View.Refresh();
                            break;
                        case AnimationType.Reference:
                            referenceAbilitiesCVS.View.Refresh();
                            break;
                    }
                OnPropertyChanged("Filter");
            }
        }

        public bool CanPlayWithNext
        {
            get
            {
                bool canPlayWithNext = false;
                if (SelectedAnimationElement != null)
                {
                    if (SelectedAnimationElement.Type == AnimationType.FX || SelectedAnimationElement.Type == AnimationType.Movement)
                    {
                        IAnimationElement next = GetNextAnimationElement(SelectedAnimationElement);
                        if (next != null && (next.Type == AnimationType.FX || next.Type == AnimationType.Movement))
                            canPlayWithNext = true;
                    }
                }
                return canPlayWithNext;
            }
        }

        private bool isAttackSelected;
        public bool IsAttackSelected
        {
            get
            {
                return isAttackSelected;
            }
            set
            {
                isAttackSelected = value;
                OnPropertyChanged("IsAttackSelected");
            }
        }

        private bool isHitSelected;
        public bool IsHitSelected
        {
            get
            {
                return isHitSelected;
            }
            set
            {
                isHitSelected = value;
                OnPropertyChanged("IsHitSelected");
            }
        }
        public bool CanEditAbilityOptions
        {
            get
            {
                return !Helper.GlobalVariables_IsPlayingAttack;
            }
        }
        #endregion

        #region Commands


        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> LoadResourcesCommand { get; private set; }
        public DelegateCommand<object> EnterAbilityEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitAbilityRenameCommand { get; private set; }
        public DelegateCommand<object> CancelAbilityEditModeCommand { get; private set; }
        public DelegateCommand<object> AddAnimationElementCommand { get; private set; }
        public DelegateCommand<object> SaveAbilityCommand { get; private set; }
        public DelegateCommand<object> RemoveAnimationCommand { get; private set; }
        public DelegateCommand<object> SaveSequenceCommand { get; private set; }
        public DelegateCommand<object> UpdateSelectedAnimationCommand { get; private set; }
        public DelegateCommand<object> SubmitAnimationElementRenameCommand { get; private set; }
        public DelegateCommand<object> EnterAnimationElementEditModeCommand { get; private set; }
        public DelegateCommand<object> CancelAnimationElementEditModeCommand { get; private set; }
        public DelegateCommand<object> DemoAnimatedAbilityCommand { get; private set; }
        public DelegateCommand<object> DemoAnimationCommand { get; private set; }
        public DelegateCommand<object> CloneAnimationCommand { get; private set; }
        public DelegateCommand<object> CutAnimationCommand { get; private set; }
        public DelegateCommand<object> LinkAnimationCommand { get; private set; }
        public DelegateCommand<object> PasteAnimationCommand { get; private set; }
        public DelegateCommand<object> UpdateReferenceTypeCommand { get; private set; }
        public DelegateCommand<object> ConfigureAttackAnimationCommand { get; private set; }
        public DelegateCommand<object> ConfigureUnitPauseCommand { get; private set; }

        #endregion

        #region Constructor

        public AbilityEditorViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, IResourceRepository resourceRepository, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.resourceRepository = resourceRepository;
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe(this.LoadAnimatedAbility);
            this.eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Subscribe(this.LoadReferenceResource);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.AttackEnded);
            // Unselect everything at the beginning
            this.InitializeAnimationElementSelections();
        }

        #endregion

        #region Initialization

        private void InitializeAnimationElementSelections()
        {
            this.SelectedAnimationElement = null;
            this.SelectedAnimationParent = null;
            this.IsSequenceAbilitySelected = false;
            this.IsReferenceAbilitySelected = false;
            this.IsPauseElementSelected = false;
            this.CurrentSequenceElement = null;
            this.CurrentReferenceElement = null;
            this.CurrentPauseElement = null;
            this.CurrentAbility = null;
            this.CurrentAttackAbility = null;
            this.Owner = null;
        }

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadAbility);
            this.LoadResourcesCommand = new DelegateCommand<object>(this.LoadResources);
            this.SubmitAbilityRenameCommand = new DelegateCommand<object>(this.SubmitAbilityRename);
            this.SaveAbilityCommand = new DelegateCommand<object>(this.SaveAbility, this.CanSaveAbility);
            this.SaveSequenceCommand = new DelegateCommand<object>(this.SaveSequence, this.CanSaveSequence);
            this.EnterAbilityEditModeCommand = new DelegateCommand<object>(this.EnterAbilityEditMode);
            this.CancelAbilityEditModeCommand = new DelegateCommand<object>(this.CancelAbilityEditMode);
            this.AddAnimationElementCommand = new DelegateCommand<object>(this.AddAnimationElement, this.CanAddAnimationElement);
            this.RemoveAnimationCommand = new DelegateCommand<object>(this.RemoveAnimation, this.CanRemoveAnimation);
            this.UpdateSelectedAnimationCommand = new DelegateCommand<object>(this.UpdateSelectedAnimation);
            this.SubmitAnimationElementRenameCommand = new DelegateCommand<object>(this.SubmitAnimationElementRename);
            this.EnterAnimationElementEditModeCommand = new DelegateCommand<object>(this.EnterAnimationElementEditMode, this.CanEnterAnimationElementEditMode);
            this.CancelAnimationElementEditModeCommand = new DelegateCommand<object>(this.CancelAnimationElementEditMode);
            this.DemoAnimatedAbilityCommand = new DelegateCommand<object>(this.DemoAnimatedAbility, this.CanDemoAnimation);
            this.DemoAnimationCommand = new DelegateCommand<object>(this.DemoAnimation, this.CanDemoAnimation);
            this.CloneAnimationCommand = new DelegateCommand<object>(this.CloneAnimation, this.CanCloneAnimation);
            this.CutAnimationCommand = new DelegateCommand<object>(this.CutAnimation, this.CanCutAnimation);
            this.PasteAnimationCommand = new DelegateCommand<object>(this.PasteAnimation, this.CanPasteAnimation);
            this.UpdateReferenceTypeCommand = new DelegateCommand<object>(this.UpdateReferenceType, this.CanUpdateReferenceType);
            this.ConfigureAttackAnimationCommand = new DelegateCommand<object>(this.ConfigureAttackAnimation, this.CanConfigureAttackAnimation);
            this.ConfigureUnitPauseCommand = new DelegateCommand<object>(this.ConfigureUnitPause, this.CanConfigureUnitPause);
        }

        #endregion

        #region Methods

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
            this.SaveAbilityCommand.RaiseCanExecuteChanged();
            this.SaveSequenceCommand.RaiseCanExecuteChanged();
            this.AddAnimationElementCommand.RaiseCanExecuteChanged();
            this.RemoveAnimationCommand.RaiseCanExecuteChanged();
            this.DemoAnimatedAbilityCommand.RaiseCanExecuteChanged();
            this.DemoAnimationCommand.RaiseCanExecuteChanged();
            this.CloneAnimationCommand.RaiseCanExecuteChanged();
            this.CutAnimationCommand.RaiseCanExecuteChanged();
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
            this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
            this.ConfigureUnitPauseCommand.RaiseCanExecuteChanged();
            OnPropertyChanged("CanEditAbilityOptions");
        }

        #endregion

        #region Update Selected Animation

        private void UpdateSelectedAnimation(object state)
        {
            if (state != null) // Update selection
            {
                if (!isUpdatingCollection)
                {
                    IAnimationElement parentAnimationElement;
                    Object selectedAnimationElement = Helper.GetCurrentSelectedAnimationInAnimationCollection(state, out parentAnimationElement);
                    if (selectedAnimationElement != null && selectedAnimationElement is IAnimationElement) // Only update if something is selected
                    {
                        this.SelectedAnimationElement = selectedAnimationElement as IAnimationElement;
                        this.SelectedAnimationParent = parentAnimationElement;
                        this.SetCurrentSequenceAnimation();
                        this.SetCurrentReferenceAbility();
                        this.SetCurrentPauseElement();
                    }
                    else if (selectedAnimationElement == null && (this.CurrentAbility == null || this.CurrentAbility.AnimationElements.Count == 0))
                    {
                        this.SelectedAnimationElement = null;
                        this.SelectedAnimationParent = null;
                        this.IsSequenceAbilitySelected = false;
                        this.IsReferenceAbilitySelected = false;
                        this.IsPauseElementSelected = false;
                        this.CurrentSequenceElement = null;
                        this.CurrentReferenceElement = null;
                        this.CurrentPauseElement = null;
                    }
                }
                else
                    this.lastAnimationElementsStateToUpdate = state;
            }
            else // Unselect
            {
                this.SelectedAnimationElement = null;
                this.SelectedAnimationParent = null;
                this.IsSequenceAbilitySelected = false;
                this.IsReferenceAbilitySelected = false;
                this.IsPauseElementSelected = false;
                this.CurrentSequenceElement = null;
                this.CurrentReferenceElement = null;
                this.CurrentPauseElement = null;
                OnAnimationAdded(null, null);
            }
        }

        private void LockModelAndMemberUpdate(bool isLocked)
        {
            this.isUpdatingCollection = isLocked;
            if (!isLocked)
                this.UpdateAnimationElementTree();
        }

        private void UpdateAnimationElementTree()
        {
            // Update character crowd if necessary
            if (this.lastAnimationElementsStateToUpdate != null)
            {
                this.UpdateSelectedAnimation(lastAnimationElementsStateToUpdate);
                this.lastAnimationElementsStateToUpdate = null;
            }
        }

        private void SetCurrentSequenceAnimation()
        {
            if (this.SelectedAnimationElement is SequenceElement)
            {
                this.IsSequenceAbilitySelected = true;
                this.CurrentSequenceElement = this.SelectedAnimationElement as SequenceElement;
            }
            else if (this.SelectedAnimationParent is SequenceElement)
            {
                this.IsSequenceAbilitySelected = true;
                this.CurrentSequenceElement = this.SelectedAnimationParent as SequenceElement;
            }
            else
            {
                this.IsSequenceAbilitySelected = false;
                this.CurrentSequenceElement = null;
            }
        }

        private void SetCurrentReferenceAbility()
        {
            if (this.SelectedAnimationElement is ReferenceAbility)
            {
                this.CurrentReferenceElement = this.SelectedAnimationElement as ReferenceAbility;
                this.IsReferenceAbilitySelected = true;
                //this.LoadReferenceResource();
            }
            else
            {
                this.CurrentReferenceElement = null;
                this.IsReferenceAbilitySelected = false;
            }
        }

        private void SetCurrentPauseElement()
        {
            if(this.SelectedAnimationElement is PauseElement)
            {
                this.CurrentPauseElement = this.SelectedAnimationElement as PauseElement;
                this.IsPauseElementSelected = true;
            }
            else
            {
                this.CurrentPauseElement = null;
                this.IsPauseElementSelected = false;
            }
        }
        #endregion

        #region Load Animated Ability
        private void LoadAnimatedAbility(Tuple<AnimatedAbility, Character> tuple)
        {
            this.InitializeAnimationElementSelections();
            this.IsShowingAbilityEditor = true;
            this.CurrentAttackAbility = tuple.Item1 as Attack;
            this.CurrentAbility = this.CurrentAttackAbility as AnimatedAbility;
            this.Owner = tuple.Item2 as Character;
            if (this.CurrentAttackAbility.IsAttack)
            {
                this.IsAttackSelected = true;
                this.IsHitSelected = false;
            }
        }
        private void UnloadAbility(object state = null)
        {
            this.CurrentAttackAbility = null;
            //this.Owner.AvailableIdentities.CollectionChanged -= AvailableIdentities_CollectionChanged;
            this.Owner = null;
            this.IsShowingAbilityEditor = false;
        }

        #endregion

        #region Rename
        private void EnterAbilityEditMode(object state)
        {
            this.OriginalName = CurrentAttackAbility.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelAbilityEditMode(object state)
        {
            CurrentAttackAbility.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitAbilityRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);

                bool duplicateName = false;
                if (updatedName != this.OriginalName)
                    duplicateName = this.Owner.AnimatedAbilities.ContainsKey(updatedName);

                if (!duplicateName)
                {
                    RenameAbility(updatedName);
                    OnEditModeLeave(state, null);
                    this.SaveAbility(null);
                    this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, "Rename Ability", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.CancelAbilityEditMode(state);
                }
            }
        }

        private void RenameAbility(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            CurrentAttackAbility.Name = updatedName;
            Owner.AnimatedAbilities.UpdateKey(OriginalName, updatedName);
            OriginalName = null;
        }

        private bool CanEnterAnimationElementEditMode(object state)
        {
            return this.SelectedAnimationElement is PauseElement;
        }

        private void EnterAnimationElementEditMode(object state)
        {
            this.OriginalAnimationDisplayName = (this.SelectedAnimationElement as AnimationElement).DisplayName;
            if (this.SelectedAnimationElement is PauseElement)
                this.EditableAnimationDisplayName = (this.SelectedAnimationElement as PauseElement).Time.ToString();
            OnEditModeEnter(state, null);
        }

        private void CancelAnimationElementEditMode(object state)
        {
            (this.SelectedAnimationElement as AnimationElement).DisplayName = this.OriginalAnimationDisplayName;
            this.OriginalAnimationDisplayName = "";
            OnEditModeLeave(state, null);
        }
        private void SubmitAnimationElementRename(object state)
        {
            if (this.SelectedAnimationElement is PauseElement && this.OriginalAnimationDisplayName != "") // Original Display Name empty means we already cancelled the rename
            {
                string pausePeriod = Helper.GetTextFromControlObject(state);
                int period;
                if (!Int32.TryParse(pausePeriod, out period))
                    pausePeriod = "1";
                else
                    (this.SelectedAnimationElement as PauseElement).Time = period;

                (this.SelectedAnimationElement as PauseElement).DisplayName = "Pause " + pausePeriod.ToString();
                this.OriginalAnimationDisplayName = "";
                OnEditModeLeave(state, null);
                this.SaveAbility(null);
            }
        }
        #endregion

        #region Add Animation Element

        private bool CanAddAnimationElement(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack; 
        }

        private void AddAnimationElement(object state)
        {
            AnimationType animationType = (AnimationType)state;
            AnimationElement animationElement = this.GetAnimationElement(animationType);
            this.AddAnimationElement(animationElement);
            OnAnimationAdded(animationElement, null);
            this.SaveAbility(null);
        }

        private void AddAnimationElement(AnimationElement animationElement)
        {
            int order = GetAppropriateOrderForAnimationElement();
            if (!this.IsSequenceAbilitySelected)
                this.CurrentAbility.AddAnimationElement(animationElement, order);
            else
            {
                if (this.SelectedAnimationElement is SequenceElement)
                    (this.SelectedAnimationElement as SequenceElement).AddAnimationElement(animationElement, order);
                else
                    (this.SelectedAnimationParent as SequenceElement).AddAnimationElement(animationElement, order);
            }
            this.CloneAnimationCommand.RaiseCanExecuteChanged();
        }

        private AnimationElement GetAnimationElement(AnimationType abilityType)
        {
            AnimationElement animationElement = null;
            List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            string fullName = "";
            switch (abilityType)
            {
                case AnimationType.Movement:
                    animationElement = new MOVElement("", "");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Movement, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.FX:
                    animationElement = new FXEffectElement("", "");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.FX, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sound:
                    animationElement = new SoundElement("", "");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Sound, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sequence:
                    animationElement = new SequenceElement("");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Sequence, flattenedList);
                    animationElement.Name = fullName;
                    (animationElement as SequenceElement).SequenceType = AnimationSequenceType.And;
                    animationElement.DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                    break;
                case AnimationType.Pause:
                    animationElement = new PauseElement("", 1);
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Pause, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = "Pause " + (animationElement as PauseElement).Time.ToString();
                    break;
                case AnimationType.Reference:
                    animationElement = new ReferenceAbility("", null);
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationType.Reference, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    //this.LoadReferenceResource();
                    break;
            }

            return animationElement;
        }
        private int GetAppropriateOrderForAnimationElement()
        {
            int order = 0;
            if (this.SelectedAnimationParent == null)
            {
                if (this.SelectedAnimationElement == null)
                {
                    order = this.CurrentAbility.LastOrder + 1;
                }
                else if (this.SelectedAnimationElement is SequenceElement)
                {
                    order = (this.SelectedAnimationElement as SequenceElement).LastOrder + 1;
                }
                else
                {
                    order = this.SelectedAnimationElement.Order + 1;
                    //order = prevOrder + 1;
                    //foreach (var element in this.CurrentAbility.AnimationElements.Where(e => e.Order > prevOrder))
                    //    element.Order += 1;
                }
            }
            else
            {
                if (this.SelectedAnimationParent is SequenceElement)
                {
                    order = this.SelectedAnimationElement.Order + 1;
                    //order = prevOrder + 1;
                    //foreach (var element in (this.SelectedAnimationParent as SequenceElement).AnimationElements.Where(e => e.Order > prevOrder))
                    //    element.Order += 1;
                }
            }
            return order;
        }
        

        #endregion

        #region Save Ability

        private bool CanSaveAbility(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SaveResources()
        {
            this.resourceRepository.SaveMoveResources(this.movResources.ToList());
            this.resourceRepository.SaveFXResources(this.fxResources.ToList());
            this.resourceRepository.SaveSoundResources(this.soundResources.ToList());
        }

        private void SaveAbility(object state)
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(state);
            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
        }

        #endregion

        #region Load Resources
        private void LoadResources(object state)
        {
            List<AnimationResource> moveResourceCollection = this.resourceRepository.GetMoveResources();
            movResources = new ObservableCollection<AnimationResource>(moveResourceCollection);
            MOVResources = new ReadOnlyObservableCollection<AnimationResource>(movResources);

            List<AnimationResource> fxResourceCollection = this.resourceRepository.GetFXResources();
            fxResources = new ObservableCollection<AnimationResource>(fxResourceCollection);
            FXResources = new ReadOnlyObservableCollection<AnimationResource>(fxResources);

            List<AnimationResource> soundResourceCollection = this.resourceRepository.GetSoundResources();
            soundResources = new ObservableCollection<AnimationResource>(soundResourceCollection);
            SoundResources = new ReadOnlyObservableCollection<AnimationResource>(soundResources);

            movResourcesCVS = new CollectionViewSource();
            movResourcesCVS.Source = MOVResources;
            MOVResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("MOVResourcesCVS");

            fxResourcesCVS = new CollectionViewSource();
            fxResourcesCVS.Source = FXResources;
            fxResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("FXResourcesCVS");

            soundResourcesCVS = new CollectionViewSource();
            soundResourcesCVS.Source = SoundResources;
            soundResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("SoundResourcesCVS");
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
            if (animationRes.Reference == this.CurrentAbility)
                return false;
            if (animationRes.Reference != null)
            {
                AnimatedAbility reference = animationRes.Reference;
                if (reference != null && reference.AnimationElements != null && reference.AnimationElements.Count > 0)
                {
                    if (reference.AnimationElements.Contains(this.CurrentAbility))
                        return false;
                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(reference.AnimationElements.ToList());
                    if (elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                        return false;
                }
                ////Check if the referenced ability contains the parent ability
                //if (reference.AnimationElements.Contains(this.CurrentAbility))
                //    return false;
                ////Check if inside the referenced ability there's any reference to the current ability
                //if (reference.AnimationElements.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                //    return false;
            }
            if (string.IsNullOrWhiteSpace(Filter))
            {
                return true;
            }


            if (SelectedAnimationElement != null && SelectedAnimationElement.Resource == animationRes)
            {
                return true;
            }
            bool caseReferences = false;
            if (animationRes.Reference != null && animationRes.Reference.Owner != null)
            {
                caseReferences = new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Name) || new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Owner.Name);
            }
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.TagLine) || new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Name) || caseReferences;
        }

        #endregion

        #region Save Sequence

        private bool CanSaveSequence(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SaveSequence(object state)
        {
            if (this.CurrentSequenceElement != null)
            {
                this.CurrentSequenceElement.DisplayName = "Sequence: " + this.CurrentSequenceElement.SequenceType.ToString();
            }
            this.SaveAbility(state);
        }
        #endregion

        #region Remove Animation

        private bool CanRemoveAnimation(object state)
        {
            return this.SelectedAnimationElement != null && !Helper.GlobalVariables_IsPlayingAttack; 
        }

        private void RemoveAnimation(object state)
        {
            this.LockModelAndMemberUpdate(true);
            if (this.SelectedAnimationParent != null)
            {
                this.DeleteAnimationElementFromParentElementByName(this.SelectedAnimationParent, this.SelectedAnimationElement.Name);
            }
            else
            {
                this.CurrentAbility.RemoveAnimationElement(this.SelectedAnimationElement.Name);
            }
            this.SaveAbility(null);
            this.LockModelAndMemberUpdate(false);
        }

        private void DeleteAnimationElementFromParentElementByName(IAnimationElement parent, string nameOfDeletingAnimation)
        {
            SequenceElement parentSequenceElement = parent as SequenceElement;
            if (parentSequenceElement != null && parentSequenceElement.AnimationElements.Count > 0)
            {
                //var anim = parentSequenceElement.AnimationElements.Where(a => a.Name == nameOfDeletingAnimation).FirstOrDefault();
                parentSequenceElement.RemoveAnimationElement(nameOfDeletingAnimation);
                if (parentSequenceElement.AnimationElements.Count == 0 && parentSequenceElement.Name == this.SelectedAnimationElementRoot.Name)
                {
                    this.SelectedAnimationElementRoot = null;
                }
            }
            OnExpansionUpdateNeeded(parent, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
        }

        #endregion

        #region Utility Methods

        private List<AnimationElement> GetFlattenedAnimationList(List<AnimationElement> animationElementList)
        {
            List<AnimationElement> _list = new List<AnimationElement>();
            foreach (AnimationElement animationElement in animationElementList)
            {
                if (animationElement is SequenceElement)
                {
                    SequenceElement sequenceElement = (animationElement as SequenceElement);
                    if (sequenceElement.AnimationElements != null && sequenceElement.AnimationElements.Count > 0)
                        _list.AddRange(GetFlattenedAnimationList(sequenceElement.AnimationElements.ToList()));
                }
                _list.Add(animationElement);
            }
            return _list;
        }

        private string GetDisplayNameFromResourceName(string resourceName)
        {
            return Path.GetFileNameWithoutExtension(resourceName);
        }

        private IAnimationElement GetNextAnimationElement(IAnimationElement selectedAnimationElement)
        {
            if (selectedAnimationElement.Order == currentAbility.LastOrder)
                return null;
            return currentAbility.AnimationElements.Where(x => { return x.Order == selectedAnimationElement.Order + 1; }).FirstOrDefault();
        }
        #endregion

        #region Demo Animation

        private bool CanDemoAnimation(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void DemoAnimatedAbility(object state)
        {
            Character currentTarget = GetCurrentTarget();
            this.CurrentAbility.Play(Target: currentTarget);
        }

        private void DemoAnimation(object state)
        {
            Character currentTarget = GetCurrentTarget();
            if (this.SelectedAnimationElement != null)
                this.SelectedAnimationElement.Play(Target: currentTarget, forcePlay: true);
        }

        private Character GetCurrentTarget()
        {
            Character currentTarget = null;
            if (!this.CurrentAbility.PlayOnTargeted)
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
            return currentTarget;
        }

        private void SpawnAndTargetOwnerCharacter()
        {
            if (this.Owner != null) // Will be null if the editor isn't loaded yet
            {
                if (!this.Owner.HasBeenSpawned)
                {
                    Crowds.CrowdMemberModel member = this.Owner as Crowds.CrowdMemberModel;
                    if (member.RosterCrowd == null)
                        this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(new Tuple<Crowds.CrowdMemberModel, Crowds.CrowdModel>(member, member.RosterCrowd as Crowds.CrowdModel));
                    member.Spawn(false);
                }
                this.Owner.Target(false);
            }
        }
        #endregion

        #region Clone Animation

        private bool CanCloneAnimation(object state)
        {
            return (this.SelectedAnimationElement != null || (this.SelectedAnimationElement == null && this.CurrentAbility != null && this.CurrentAbility.AnimationElements != null && this.CurrentAbility.AnimationElements.Count > 0)) && !Helper.GlobalVariables_IsPlayingAttack; 
        }
        private void CloneAnimation(object state)
        {
            if (this.SelectedAnimationElement != null)
                Helper.GlobalClipboardObject = this.SelectedAnimationElement; // any animation element
            else
                Helper.GlobalClipboardObject = this.CurrentAbility; // Sequence element
            Helper.GlobalClipboardAction = ClipboardAction.Clone;
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Cut Animation
        private bool CanCutAnimation(object state)
        {
            return (this.SelectedAnimationElement != null) && !Helper.GlobalVariables_IsPlayingAttack; 
        }
        private void CutAnimation(object state)
        {
            if (this.SelectedAnimationParent != null)
            {
                Helper.GlobalClipboardObject = this.SelectedAnimationElement;
                Helper.GlobalClipboardObjectParent = this.SelectedAnimationParent;
            }
            else
            {
                Helper.GlobalClipboardObject = this.SelectedAnimationElement;
                Helper.GlobalClipboardObjectParent = this.CurrentAbility;
            }
            Helper.GlobalClipboardAction = ClipboardAction.Cut;
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Paste Animation
        private bool CanPasteAnimation(object state)
        {
            bool canPaste = false;
            if(!Helper.GlobalVariables_IsPlayingAttack)
            {
                switch (Helper.GlobalClipboardAction)
                {
                    case ClipboardAction.Clone:
                        if (Helper.GlobalClipboardObject != null)
                        {
                            if (Helper.GlobalClipboardObject is SequenceElement)
                            {
                                SequenceElement seqElement = Helper.GlobalClipboardObject as SequenceElement;
                                List<AnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
                                if (!(elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; })))
                                    canPaste = true;
                            }
                            else if (Helper.GlobalClipboardObject is ReferenceAbility)
                            {
                                ReferenceAbility refAbility = Helper.GlobalClipboardObject as ReferenceAbility;
                                if (refAbility.Reference == this.CurrentAbility)
                                    canPaste = false;
                                else if (refAbility.Reference != null && refAbility.Reference.AnimationElements != null && refAbility.Reference.AnimationElements.Count > 0)
                                {
                                    bool refexists = false;
                                    if (refAbility.Reference.AnimationElements.Contains(this.CurrentAbility))
                                        refexists = true;
                                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
                                    if (elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                                        refexists = true;
                                    if (!refexists)
                                        canPaste = true;
                                }
                                else
                                    canPaste = true;
                            }
                            else
                                canPaste = true;
                        }
                        break;
                    case ClipboardAction.Cut:
                        if (Helper.GlobalClipboardObject != null && Helper.GlobalClipboardObjectParent != null)
                        {
                            if (Helper.GlobalClipboardObject is SequenceElement)
                            {
                                SequenceElement seqElement = Helper.GlobalClipboardObject as SequenceElement;
                                List<AnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
                                if (!(elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; })))
                                    canPaste = true;
                            }
                            else if (Helper.GlobalClipboardObject is ReferenceAbility)
                            {
                                ReferenceAbility refAbility = Helper.GlobalClipboardObject as ReferenceAbility;
                                if (refAbility.Reference == this.CurrentAbility)
                                    canPaste = false;
                                else if (refAbility.Reference != null && refAbility.Reference.AnimationElements != null && refAbility.Reference.AnimationElements.Count > 0)
                                {
                                    bool refexists = false;
                                    if (refAbility.Reference.AnimationElements.Contains(this.CurrentAbility))
                                        refexists = true;
                                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
                                    if (elementList.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                                        refexists = true;
                                    if (!refexists)
                                        canPaste = true;
                                }
                                else
                                    canPaste = true;
                            }
                            else
                                canPaste = true;
                        }
                        break;
                }
            }
            return canPaste;
        }
        private void PasteAnimation(object state)
        {
            // Lock animation Tree from updating
            this.LockModelAndMemberUpdate(true);
            List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            switch (Helper.GlobalClipboardAction)
            {
                case ClipboardAction.Clone:
                    {
                        IAnimationElement animationElement = (Helper.GlobalClipboardObject as AnimationElement).Clone() as IAnimationElement;
                        animationElement.Name = AnimatedAbility.GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if (animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, null);
                        this.SaveAbility(null);
                        break;
                    }
                case ClipboardAction.Cut:
                    {
                        SequenceElement parentSequenceElement = Helper.GlobalClipboardObjectParent as SequenceElement;
                        AnimationElement animationElement = Helper.GlobalClipboardObject as AnimationElement;
                        parentSequenceElement.RemoveAnimationElement(animationElement);
                        animationElement.Name = AnimatedAbility.GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if (animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, new CustomEventArgs<bool>() { Value = false });
                        this.SaveAbility(null);
                        break;
                    }
            }
            if (SelectedAnimationElement != null)
            {
                OnExpansionUpdateNeeded(this.SelectedAnimationElement, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Paste });
            }
            // UnLock character crowd Tree from updating
            this.LockModelAndMemberUpdate(false);
            Helper.GlobalClipboardObject = null;
            Helper.GlobalClipboardObjectParent = null;
        }

        #endregion

        #region Reference Ability
        private bool CanUpdateReferenceType(object state)
        {
            return this.CurrentReferenceElement != null && this.CurrentReferenceElement.Reference != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void UpdateReferenceType(object state)
        {
            if (this.CurrentReferenceElement != null)
            {
                if (this.CurrentReferenceElement.ReferenceType == ReferenceType.Copy)
                {
                    this.LockModelAndMemberUpdate(true);
                    List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
                    SequenceElement sequenceElement = (this.CurrentReferenceElement.Reference).Clone() as SequenceElement;
                    int order = this.CurrentReferenceElement.Order;
                    sequenceElement.Name = AnimatedAbility.GetAppropriateAnimationName(sequenceElement.Type, flattenedList);
                    if (sequenceElement is SequenceElement && string.IsNullOrEmpty((sequenceElement as AnimationElement).DisplayName))
                        (sequenceElement as AnimationElement).DisplayName = "Sequence: " + (sequenceElement as SequenceElement).SequenceType.ToString();
                    this.RemoveAnimation(null);
                    if (this.SelectedAnimationElement is SequenceElement)
                        (this.SelectedAnimationElement as SequenceElement).AddAnimationElement(sequenceElement, order);
                    else if (this.SelectedAnimationParent is SequenceElement)
                        (this.SelectedAnimationParent as SequenceElement).AddAnimationElement(sequenceElement, order);
                    else
                        this.CurrentAbility.AddAnimationElement(sequenceElement, order);
                    OnAnimationAdded(sequenceElement, null);
                    this.SaveAbility(null);
                    this.CurrentReferenceElement = null;
                    this.IsReferenceAbilitySelected = false;
                    this.LockModelAndMemberUpdate(false);
                }
            }
        }

        #endregion

        #region Unit Pause

        private bool CanConfigureUnitPause(object state)
        {
            return Helper.GlobalVariables_IsPlayingAttack == false;
        }

        private void ConfigureUnitPause(object state)
        {
            if(this.CurrentPauseElement != null)
            {
                if (this.CurrentPauseElement.IsUnitPause)
                    this.CurrentPauseElement.DisplayName = "Pause 1";
                else
                    this.CurrentPauseElement.DisplayName = "Pause " + this.CurrentPauseElement.Time.ToString();                
            }
            this.SaveAbility(state);
        } 

        #endregion

        #region Attack/Defend Animations

        private bool CanConfigureAttackAnimation(object state)
        {
            bool canConfigureAttack = false;
            if (!Helper.GlobalVariables_IsPlayingAttack)
            {
                if (state == null)
                    canConfigureAttack = true;
                else
                    canConfigureAttack = this.CurrentAttackAbility != null && this.CurrentAttackAbility.IsAttack; 
            }

            return canConfigureAttack;
        }

        private void ConfigureAttackAnimation(object state)
        {
            if (state == null)
            {
                if (this.CurrentAttackAbility.IsAttack)
                {
                    this.IsAttackSelected = true;
                }
                else
                {
                    this.IsAttackSelected = false;
                }
                this.IsHitSelected = false;
                this.CurrentAbility = this.CurrentAttackAbility;
                this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
                this.SaveAbility(null);
            }
            else if (state.ToString() == "Attack")
            {
                this.IsAttackSelected = true;
                this.IsHitSelected = false;
                this.CurrentAbility = this.CurrentAttackAbility;
            }
            else if (state.ToString() == "OnHit")
            {
                this.IsAttackSelected = false;
                this.IsHitSelected = true;
                this.CurrentAbility = this.CurrentAttackAbility.OnHitAnimation;
            }
        }

        #endregion

        #region Move Animation Element
        /// <summary>
        /// This method is used for Drag drop inside the treeview and performs a cut paste under the hood
        /// </summary>
        /// <param name="sourceElement"></param>
        /// <param name="targetElement"></param>
        public void MoveSelectedAnimationElement(SequenceElement targetElementParent, int order)
        {
            AnimationElement sourceElement = null;
            SequenceElement sourceElementParent = null;
            if (this.SelectedAnimationParent != null)
            {
                sourceElement = this.SelectedAnimationElement as AnimationElement;
                sourceElementParent = this.SelectedAnimationParent as SequenceElement;
            }
            else
            {
                sourceElement = this.SelectedAnimationElement as AnimationElement;
                sourceElementParent = this.CurrentAbility;
            }
            SequenceElement destinationElementParent = targetElementParent ?? this.CurrentAbility;
            if (sourceElement != null && sourceElementParent != null)
            {
                List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
                sourceElementParent.RemoveAnimationElement(sourceElement);
                sourceElement.Name = AnimatedAbility.GetAppropriateAnimationName(sourceElement.Type, flattenedList);
                if (sourceElement is SequenceElement && string.IsNullOrEmpty((sourceElement as AnimationElement).DisplayName))
                    (sourceElement as AnimationElement).DisplayName = "Sequence: " + (sourceElement as SequenceElement).SequenceType.ToString();
                destinationElementParent.AddAnimationElement(sourceElement as AnimationElement, order);
                OnAnimationAdded(sourceElement, new CustomEventArgs<bool>() { Value = false });
                this.SaveAbility(null);
            }
        }
        /// <summary>
        /// Drag drop between refernce ability grid and animations treview
        /// </summary>
        /// <param name="referenceAbility"></param>
        /// <param name="targetElementParent"></param>
        /// <param name="order"></param>
        public void MoveReferenceAbilityToAnimationElements(AnimatedAbility referenceAbility, SequenceElement targetElementParent, int order)
        {
            SequenceElement destinationElementParent = targetElementParent ?? this.CurrentAbility;
            if (referenceAbility != null)
            {
                AnimationElement animationElement = this.GetAnimationElement(AnimationType.Reference);
                (animationElement as ReferenceAbility).Reference = referenceAbility;
                animationElement.DisplayName = GetDisplayNameFromResourceName(referenceAbility.Name);
                destinationElementParent.AddAnimationElement(animationElement, order);
                OnAnimationElementDraggedFromGrid(animationElement, null);
                SaveAbility(null);
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion
    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
