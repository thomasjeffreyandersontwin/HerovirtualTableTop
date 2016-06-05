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

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AbilityEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;

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
                //if (editedAbility != null)
                //{
                //    editedAbility.PropertyChanged -= EditedIdentity_PropertyChanged;
                //}
                currentAbility = value;
                //if (editedAbility != null)
                //{
                //    editedAbility.PropertyChanged += EditedIdentity_PropertyChanged;
                //}
                OnPropertyChanged("CurrentAbility");
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
                selectedAnimationElement = value;
                OnPropertyChanged("SelectedAnimationElement");
                OnSelectionChanged(value, null);
                //if(value != null && this.SelectedResourceAnimationElement != null)
                //{
                //    this.SelectedResourceAnimationElement.Resource = this.SelectedAnimationElement.Resource;
                //}
                this.RemoveAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        private IAnimationElement selectedResourceAnimationElement;
        public IAnimationElement SelectedResourceAnimationElement
        {
            get
            {
                return selectedResourceAnimationElement;
            }
            set
            {
                selectedResourceAnimationElement = value;
                string resourceName = selectedResourceAnimationElement.Resource;
                if(this.SelectedAnimationElement != null)
                {
                    SelectedAnimationElement.Resource = resourceName;
                    DemoAnimation();
                    (SelectedAnimationElement as AnimationElement).DisplayName = this.GetDisplayNameFromResourceName(resourceName);
                    this.SaveAbility(null);
                }
                OnPropertyChanged("SelectedResourceAnimationElement");
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

        private ObservableCollection<MOVElement> movElements;
        public ReadOnlyObservableCollection<MOVElement> MOVElements { get; private set; }

        private CollectionViewSource movElementsCVS;
        public CollectionViewSource MOVElementsCVS
        {
            get
            {
                return movElementsCVS;
            }
        }

        private ObservableCollection<FXEffectElement> fxElements;
        public ReadOnlyObservableCollection<FXEffectElement> FXElements { get; private set; }

        private CollectionViewSource fxElementsCVS;
        public CollectionViewSource FXElementsCVS
        {
            get
            {
                return fxElementsCVS;
            }
        }

        private ObservableCollection<SoundElement> soundElements;
        public ReadOnlyObservableCollection<SoundElement> SoundElements { get; private set; }

        private CollectionViewSource soundElementsCVS;
        public CollectionViewSource SoundElementsCVS
        {
            get
            {
                return soundElementsCVS;
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
                            movElementsCVS.View.Refresh();
                            break;
                        case AnimationType.FX:
                            fxElementsCVS.View.Refresh();
                            break;
                        case AnimationType.Sound:
                            soundElementsCVS.View.Refresh();
                            break;
                    }
                OnPropertyChanged("Filter");
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> CloseEditorCommand { get; private set; }
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

        #endregion

        #region Constructor

        public AbilityEditorViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            LoadResources();
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe(this.LoadAnimatedAbility);
            this.SelectedResourceAnimationElement = new AnimationElement("");
        }
        
        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadAbility);
            this.SubmitAbilityRenameCommand = new DelegateCommand<object>(this.SubmitAbilityRename);
            this.SaveAbilityCommand = new DelegateCommand<object>(this.SaveAbility);
            this.SaveSequenceCommand = new DelegateCommand<object>(this.SaveSequence);
            this.EnterAbilityEditModeCommand = new DelegateCommand<object>(this.EnterAbilityEditMode);
            this.CancelAbilityEditModeCommand = new DelegateCommand<object>(this.CancelAbilityEditMode);
            this.AddAnimationElementCommand = new DelegateCommand<object>(this.AddAnimationElement);
            this.RemoveAnimationCommand = new DelegateCommand<object>(this.RemoveAnimation, this.CanRemoveAnimation);
            UpdateSelectedAnimationCommand = new DelegateCommand<object>(this.UpdateSelectedAnimation);
            this.SubmitAnimationElementRenameCommand = new DelegateCommand<object>(this.SubmitAnimationElementRename);
            this.EnterAnimationElementEditModeCommand = new DelegateCommand<object>(this.EnterAnimationElementEditMode, this.CanEnterAnimationElementEditMode);
            this.CancelAnimationElementEditModeCommand = new DelegateCommand<object>(this.CancelAnimationElementEditMode);
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
                    }
                    else if(selectedAnimationElement == null && (this.CurrentAbility == null || this.CurrentAbility.AnimationElements.Count == 0))
                    {
                        this.SelectedAnimationElement = null;
                        this.SelectedAnimationParent = null;
                        this.IsSequenceAbilitySelected = false;
                        this.CurrentSequenceElement = null;
                    }
                }
                else
                    this.lastAnimationElementsStateToUpdate = state;
            }
            else // Unselect
            {
                this.SelectedAnimationElement = null;
                this.SelectedAnimationParent = null;
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
            int order = GetAppropriateOrderForAnimationElement();
            if(!this.IsSequenceAbilitySelected)
                this.CurrentAbility.AddAnimationElement(animationElement, order);
            else
            {
                if (this.SelectedAnimationElement is SequenceElement)
                    (this.SelectedAnimationElement as SequenceElement).AddAnimationElement(animationElement, order);
                else
                    (this.SelectedAnimationParent as SequenceElement).AddAnimationElement(animationElement, order);
            }
            OnAnimationAdded(animationElement, null);
            this.SaveAbility(null);
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
                    fullName = GetAppropriateAnimationName("Mov Element", flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.FX:
                    animationElement = new FXEffectElement("", "");
                    fullName = GetAppropriateAnimationName("FX Element", flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sound:
                    animationElement = new SoundElement("", "");
                    fullName = GetAppropriateAnimationName("Sound Element", flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = fullName;
                    break;
                case AnimationType.Sequence:
                    animationElement = new SequenceElement("");
                    fullName = GetAppropriateAnimationName("Seq Element", flattenedList);
                    animationElement.Name = fullName;
                    (animationElement as SequenceElement).SequenceType = AnimationSequenceType.And;
                    animationElement.DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                    break;
                case AnimationType.Pause:
                    animationElement = new PauseElement("", 1);
                    fullName = GetAppropriateAnimationName("Pause Element", flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = "Pause " + (animationElement as PauseElement).Time.ToString();
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
        private string GetAppropriateAnimationName(string name, List<IAnimationElement> collection)
        {
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
        }

        private void LoadResources()
        {
            movElements = new ObservableCollection<MOVElement>();
            MOVElements = new ReadOnlyObservableCollection<MOVElement>(movElements);
            fxElements = new ObservableCollection<FXEffectElement>();
            FXElements = new ReadOnlyObservableCollection<FXEffectElement>(fxElements);
            soundElements = new ObservableCollection<SoundElement>();
            SoundElements = new ReadOnlyObservableCollection<SoundElement>(soundElements);

            Assembly assembly = Assembly.GetExecutingAssembly();

            string resName = "Module.HeroVirtualTabletop.Resources.MOVElements.csv";
            using (StreamReader Sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
            {
                while (!Sr.EndOfStream)
                {
                    string resLine = Sr.ReadLine();
                    string[] resArray = resLine.Split(';');
                    movElements.Add(new MOVElement(Path.GetFileNameWithoutExtension(resArray[1]), resArray[1], tags: resArray[0]));
                }
            }

            resName = "Module.HeroVirtualTabletop.Resources.FXElements.csv";
            using (StreamReader Sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
            {
                while (!Sr.EndOfStream)
                {
                    string resLine = Sr.ReadLine();
                    string[] resArray = resLine.Split(';');
                    fxElements.Add(new FXEffectElement(resArray[1], resArray[2], tags: resArray[0]));
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
                string[] tags = file.Substring(Settings.Default.CityOfHeroesGameDirectory.Length +
                    Constants.GAME_SOUND_FOLDERNAME.Length + 2).Split('\\');
                tags = tags.Take(tags.Count() - 1).ToArray(); //remove the actual file name
                soundElements.Add(new SoundElement(name, file, tags: tags));
            }

            movElementsCVS = new CollectionViewSource();
            movElementsCVS.Source = MOVElements;
            MOVElementsCVS.View.Filter += ResourcesCVS_Filter;

            fxElementsCVS = new CollectionViewSource();
            fxElementsCVS.Source = FXElements;
            fxElementsCVS.View.Filter += ResourcesCVS_Filter;

            soundElementsCVS = new CollectionViewSource();
            soundElementsCVS.Source = SoundElements;
            soundElementsCVS.View.Filter += ResourcesCVS_Filter;
        }

        private void LoadResourcesNew()
        {

        }

        private bool ResourcesCVS_Filter(object item)
        {
            if (string.IsNullOrWhiteSpace(Filter))
            {
                return true;
            }

            IAnimationElement animationItem = item as IAnimationElement;
            if (SelectedAnimationElement != null && SelectedAnimationElement.Resource == animationItem.Resource)
            {
                return true;
            }
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationItem.TagLine);
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
                // Will need to add more logic during remove animation from sequence story
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


        #endregion

        #region Demo Animation
        
        private void DemoAnimation()
        {
            //We need to make a call to charexplorer and roster to spawn and target
            if (!this.Owner.HasBeenSpawned)
                this.Owner.Spawn(false);
            this.Owner.Target(false);
            this.SelectedAnimationElement.Play();
        }
        #endregion

        #endregion
    }
}
