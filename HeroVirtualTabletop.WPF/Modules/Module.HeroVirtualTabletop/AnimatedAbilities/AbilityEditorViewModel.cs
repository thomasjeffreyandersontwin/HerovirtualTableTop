using Framework.WPF.Behaviors;
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

        private CharacterExplorerViewModel charExpVM;

        public bool isUpdatingCollection = false;
        public object lastAnimationElementsStateToUpdate = null;

        #endregion

        #region Events

        public event EventHandler AnimationAdded;
        public void OnAnimationAdded(object sender, EventArgs e)
        {
            if (AnimationAdded != null)
            {
                AnimationAdded(sender, e);
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

        #endregion

        #region Constructor

        public AbilityEditorViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe(this.LoadAnimatedAbility);
            this.eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Subscribe(this.LoadReferenceResource);
            // Unselect everything at the beginning
            this.SelectedAnimationElement = null;
            this.SelectedAnimationParent = null;
            this.IsSequenceAbilitySelected = false;
            this.IsReferenceAbilitySelected = false;
            this.CurrentSequenceElement = null;
            this.CurrentReferenceElement = null;
        }
        
        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadAbility);
            this.LoadResourcesCommand = new DelegateCommand<object>(this.LoadResources);
            this.SubmitAbilityRenameCommand = new DelegateCommand<object>(this.SubmitAbilityRename);
            this.SaveAbilityCommand = new DelegateCommand<object>(this.SaveAbility);
            this.SaveSequenceCommand = new DelegateCommand<object>(this.SaveSequence);
            this.EnterAbilityEditModeCommand = new DelegateCommand<object>(this.EnterAbilityEditMode);
            this.CancelAbilityEditModeCommand = new DelegateCommand<object>(this.CancelAbilityEditMode);
            this.AddAnimationElementCommand = new DelegateCommand<object>(this.AddAnimationElement);
            this.RemoveAnimationCommand = new DelegateCommand<object>(this.RemoveAnimation, this.CanRemoveAnimation);
            this.UpdateSelectedAnimationCommand = new DelegateCommand<object>(this.UpdateSelectedAnimation);
            this.SubmitAnimationElementRenameCommand = new DelegateCommand<object>(this.SubmitAnimationElementRename);
            this.EnterAnimationElementEditModeCommand = new DelegateCommand<object>(this.EnterAnimationElementEditMode, this.CanEnterAnimationElementEditMode);
            this.CancelAnimationElementEditModeCommand = new DelegateCommand<object>(this.CancelAnimationElementEditMode);
            this.DemoAnimatedAbilityCommand = new DelegateCommand<object>(this.DemoAnimatedAbility);
            this.DemoAnimationCommand = new DelegateCommand<object>(this.DemoAnimation);
            this.CloneAnimationCommand = new DelegateCommand<object>(this.CloneAnimation, this.CanCloneAnimation);
            this.CutAnimationCommand = new DelegateCommand<object>(this.CutAnimation, this.CanCutAnimation);
            this.PasteAnimationCommand = new DelegateCommand<object>(this.PasteAnimation, this.CanPasteAnimation);
            this.UpdateReferenceTypeCommand = new DelegateCommand<object>(this.UpdateReferenceType);
        }

        #endregion

        #region Methods

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
                    }
                    else if(selectedAnimationElement == null && (this.CurrentAbility == null || this.CurrentAbility.AnimationElements.Count == 0))
                    {
                        this.SelectedAnimationElement = null;
                        this.SelectedAnimationParent = null;
                        this.IsSequenceAbilitySelected = false;
                        this.IsReferenceAbilitySelected = false;
                        this.CurrentSequenceElement = null;
                        this.CurrentReferenceElement = null;
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
                this.CurrentSequenceElement = null;
                this.CurrentReferenceElement = null;
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
        #endregion

        #region Load Animated Ability
        private void LoadAnimatedAbility(Tuple<AnimatedAbility, Character> tuple)
        {
            this.IsShowingAbilityEditor = true;
            this.CurrentAbility = tuple.Item1 as AnimatedAbility;
            this.Owner = tuple.Item2 as Character;
        }
        private void UnloadAbility(object state = null)
        {
            this.CurrentAbility = null;
            //this.Owner.AvailableIdentities.CollectionChanged -= AvailableIdentities_CollectionChanged;
            this.Owner = null;
            this.IsShowingAbilityEditor = false;
        }

        #endregion

        #region Rename
        private void EnterAbilityEditMode(object state)
        {
            this.OriginalName = CurrentAbility.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelAbilityEditMode(object state)
        {
            CurrentAbility.Name = this.OriginalName;
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
            CurrentAbility.Name = updatedName;
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
            List<IAnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            string fullName = "";
            switch(abilityType)
            {
                case AnimationType.Movement:
                    animationElement = new MOVElement("", "");
                    fullName = GetAppropriateAnimationName(AnimationType.Movement, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.FX:
                    animationElement = new FXEffectElement("", "");
                    fullName = GetAppropriateAnimationName(AnimationType.FX, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sound:
                    animationElement = new SoundElement("", "");
                    fullName = GetAppropriateAnimationName(AnimationType.Sound, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sequence:
                    animationElement = new SequenceElement("");
                    fullName = GetAppropriateAnimationName(AnimationType.Sequence, flattenedList);
                    animationElement.Name = fullName;
                    (animationElement as SequenceElement).SequenceType = AnimationSequenceType.And;
                    animationElement.DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                    break;
                case AnimationType.Pause:
                    animationElement = new PauseElement("", 1);
                    fullName = GetAppropriateAnimationName(AnimationType.Pause, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = "Pause " + (animationElement as PauseElement).Time.ToString();
                    break;
                case AnimationType.Reference:
                    animationElement = new ReferenceAbility("", null);
                    fullName = GetAppropriateAnimationName(AnimationType.Reference, flattenedList);
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
            if(this.SelectedAnimationParent == null)
            {
                if(this.SelectedAnimationElement == null)
                {
                    order = this.CurrentAbility.AnimationElements.Count + 1;
                }
                else if(this.SelectedAnimationElement is SequenceElement)
                {
                    order = (this.SelectedAnimationElement as SequenceElement).AnimationElements.Count + 1;
                }
                else
                {
                    int prevOrder = this.SelectedAnimationElement.Order;
                    order = prevOrder + 1;
                    foreach (var element in this.CurrentAbility.AnimationElements.Where(e => e.Order > prevOrder))
                        element.Order += 1;
                }
            }
            else
            {
                if(this.SelectedAnimationParent is SequenceElement)
                {
                    int prevOrder = this.SelectedAnimationElement.Order;
                    order = prevOrder + 1;
                    foreach (var element in (this.SelectedAnimationParent as SequenceElement).AnimationElements.Where(e => e.Order > prevOrder))
                        element.Order += 1;
                }
            }
            return order;
        }
        private string GetAppropriateAnimationName(AnimationType animationType, List<IAnimationElement> collection)
        {
            string name = "";
            switch(animationType)
            {
                case AnimationType.Movement:
                    name = "Mov Element";
                    break;
                case AnimationType.FX:
                    name = "FX Element";
                    break;
                case AnimationType.Pause:
                    name = "Pause Element";
                    break;
                case AnimationType.Sequence:
                    name = "Seq Element";
                    break;
                case AnimationType.Sound:
                    name = "Sound Element";
                    break;
                case AnimationType.Reference:
                    name = "Ref Element";
                    break;
            }
            
            string suffix = " 1";
            string rootName = name;
            int i = 1;
            Regex reg = new Regex(@"\d+");
            MatchCollection matches = reg.Matches(name);
            if (matches.Count > 0)
            {
                int k;
                Match match = matches[matches.Count - 1];
                if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out k))
                {
                    i = k + 1;
                    suffix = string.Format(" {0}", i);
                    rootName = name.Substring(0, match.Index).TrimEnd();
                }
            }
            string newName = rootName + suffix;
            while (collection.Where(a=>a.Name == newName).FirstOrDefault() != null)
            {
                suffix = string.Format(" {0}", ++i);
                newName = rootName + suffix;
            }
            return newName;
        }

        #endregion

        #region Save Ability

        private void SaveAbility(object state)
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(state);
            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
        }

        #endregion

        #region Load Resources
        private void LoadResources(object state)
        {
            movResources = new ObservableCollection<AnimationResource>();
            MOVResources = new ReadOnlyObservableCollection<AnimationResource>(movResources);
            fxResources = new ObservableCollection<AnimationResource>();
            FXResources = new ReadOnlyObservableCollection<AnimationResource>(fxResources);
            soundResources = new ObservableCollection<AnimationResource>();
            SoundResources = new ReadOnlyObservableCollection<AnimationResource>(soundResources);


            Assembly assembly = Assembly.GetExecutingAssembly();

            string resName = "Module.HeroVirtualTabletop.Resources.MOVElements.csv";
            using (StreamReader Sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
            {
                while (!Sr.EndOfStream)
                {
                    string resLine = Sr.ReadLine();
                    string[] resArray = resLine.Split(';');
                    movResources.Add(new AnimationResource(resArray[1], GetDisplayNameFromResourceName(resArray[1]), tags: resArray[0]));
                }
            }

            resName = "Module.HeroVirtualTabletop.Resources.FXElements.csv";
            using (StreamReader Sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
            {
                while (!Sr.EndOfStream)
                {
                    string resLine = Sr.ReadLine();
                    string[] resArray = resLine.Split(';');
                    fxResources.Add(new AnimationResource(resArray[2], resArray[1], tags: resArray[0]));
                }
            }

            var soundFiles = Directory.EnumerateFiles
                        (Path.Combine(
                            Settings.Default.CityOfHeroesGameDirectory,
                            Constants.GAME_SOUND_FOLDERNAME),
                        "*.ogg", SearchOption.AllDirectories);//.OrderBy(x => { return Path.GetFileNameWithoutExtension(x); });

            foreach (string file in soundFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string[] tmpTags = file.Substring(Settings.Default.CityOfHeroesGameDirectory.Length +
                    Constants.GAME_SOUND_FOLDERNAME.Length + 2).Split('\\');
                string[] tags = new string[tmpTags.Length];
                tags[0] = "sound";
                Array.Copy(tmpTags, 0, tags, 1, tmpTags.Length - 1); // add sound tag and remove the actual file name
                soundResources.Add(new AnimationResource(file, name, tags));
            }

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
            referenceAbilitiesCVS = new CollectionViewSource();
            referenceAbilitiesCVS.Source = new ObservableCollection<AnimationResource>(abilityCollection.Select((x) => { return new AnimationResource(x, x.Name); }));
            referenceAbilitiesCVS.View.Filter += ResourcesCVS_Filter;
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
                    List<IAnimationElement> elementList = this.GetFlattenedAnimationList(reference.AnimationElements.ToList());
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
            if (animationRes.Reference != null)
            {
                caseReferences = new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Name);
            }
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.TagLine) || caseReferences;
        }

        #endregion

        #region Save Sequence
        private void SaveSequence(object state)
        {
            if(this.CurrentSequenceElement != null)
            {
                this.CurrentSequenceElement.DisplayName = "Sequence: " + this.CurrentSequenceElement.SequenceType.ToString();
            }
            this.SaveAbility(state);
        }
        #endregion

        #region Remove Animation

        private bool CanRemoveAnimation(object state)
        {
            return this.SelectedAnimationElement != null;
        }

        private void RemoveAnimation(object state)
        {
            this.LockModelAndMemberUpdate(true);
            if(this.SelectedAnimationParent != null)
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
                if(parentSequenceElement.AnimationElements.Count == 0 && parentSequenceElement.Name == this.SelectedAnimationElementRoot.Name)
                {
                    this.SelectedAnimationElementRoot = null;
                }
            }
            OnExpansionUpdateNeeded(parent, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
        }

        #endregion

        #region Utility Methods

        private List<IAnimationElement> GetFlattenedAnimationList(List<IAnimationElement> animationElementList)
        {
            List<IAnimationElement> _list = new List<IAnimationElement>();
            foreach (IAnimationElement animationElement in animationElementList)
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

        private void DemoAnimatedAbility(object state)
        {
            Character currentTarget = GetCurrentTarget();
            this.CurrentAbility.Play(Target: currentTarget);
        }

        private void DemoAnimation(object state)
        {
            Character currentTarget = GetCurrentTarget();
            this.SelectedAnimationElement.Play(Target: currentTarget);
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
                if(currentTarget == null)
                {
                    this.SpawnAndTargetOwnerCharacter();
                    currentTarget = this.Owner;
                }
            }
            return currentTarget;
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
            this.Owner.Target(false);
        }
        #endregion

        #region Clone Animation

        private bool CanCloneAnimation(object state)
        {
            return (this.SelectedAnimationElement != null  || (this.SelectedAnimationElement == null && this.CurrentAbility != null && this.CurrentAbility.AnimationElements != null && this.CurrentAbility.AnimationElements.Count > 0));
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
        public bool CanCutAnimation(object state)
        {
            return (this.SelectedAnimationElement != null);
        }
        public void CutAnimation(object state)
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

        #region Link Animation

        #endregion

        #region Paste Animation
        public bool CanPasteAnimation(object state)
        {
            bool canPaste = false;
            switch (Helper.GlobalClipboardAction)
            {
                case ClipboardAction.Clone:
                    if (Helper.GlobalClipboardObject != null)
                    {
                        if (Helper.GlobalClipboardObject is SequenceElement)
                        {
                            SequenceElement seqElement = Helper.GlobalClipboardObject as SequenceElement;
                            List<IAnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
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
                                List<IAnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
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
                            List<IAnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
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
                                List<IAnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
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
            return canPaste;
        }
        public void PasteAnimation(object state)
        {
            // Lock animation Tree from updating
            this.LockModelAndMemberUpdate(true);
            List<IAnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            switch (Helper.GlobalClipboardAction)
            {
                case ClipboardAction.Clone:
                    {
                        IAnimationElement animationElement = (Helper.GlobalClipboardObject as AnimationElement).Clone() as IAnimationElement;
                        animationElement.Name = GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if(animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, null);
                        this.SaveAbility(null);
                        break;
                    }
                case ClipboardAction.Cut:
                    {
                        SequenceElement parentSequenceElement = Helper.GlobalClipboardObjectParent as SequenceElement;
                        IAnimationElement animationElement = Helper.GlobalClipboardObject as IAnimationElement;
                        parentSequenceElement.RemoveAnimationElement(animationElement);
                        animationElement.Name = GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if (animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, null);
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
        private void UpdateReferenceType(object state)
        {
            if(this.CurrentReferenceElement != null)
            {
                if(this.CurrentReferenceElement.ReferenceType == ReferenceType.Copy)
                {
                    this.LockModelAndMemberUpdate(true);
                    List<IAnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
                    SequenceElement sequenceElement = (this.CurrentReferenceElement.Reference).Clone() as SequenceElement;
                    int order = this.CurrentReferenceElement.Order;
                    sequenceElement.Name = GetAppropriateAnimationName(sequenceElement.Type, flattenedList);
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

        #endregion
    }
}
