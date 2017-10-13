﻿using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Framework.WPF.Services.MessageBoxService;
using Module.Shared.Messages;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Module.Shared.Events;
using System.Windows;
using System.IO;
using System.Reflection;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Converters;
using Module.HeroVirtualTabletop.Desktop;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CharacterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private EventAggregator eventAggregator;
        private ICrowdRepository crowdRepository;
        private IDesktopKeyEventHandler desktopKeyEventHandler;
        

        private string filter;
        private string containerWindowName = "";
        private CrowdModel clipboardObjectOriginalCrowd = null;
        private ClipboardAction clipboardAction;
        public bool isUpdatingCollection = false;
        private bool crowdCollectionLoaded = false;
        private bool rosterSyncNeeded = false;
        public object lastCharacterCrowdStateToUpdate = null;
        private List<Tuple<string, string>> rosterCrowdCharacterMembershipKeys;

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

        public event EventHandler<CustomEventArgs<string>> EditNeeded;
        public void OnEditNeeded(object sender, CustomEventArgs<string> e)
        {
            if (EditNeeded != null)
            {
                EditNeeded(sender, e);
            }
        }

        public event EventHandler FlattenNumberRequired;
        public void OnFlattenNumberRequired(object sender, EventArgs e)
        {
            if (FlattenNumberRequired != null)
                FlattenNumberRequired(sender, e);
        }

        public event EventHandler FlattenNumberEntryFinished;
        public void OnFlattenNumberEntryFinished(object sender, EventArgs e)
        {
            if (FlattenNumberEntryFinished != null)
                FlattenNumberEntryFinished(sender, e);
        }

        public event EventHandler FlattenNumberSubmitted;
        public void OnFlattenNumberSubmitted(object sender, EventArgs e)
        {
            if (EditModeEnter != null)
                FlattenNumberSubmitted(sender, e);
        }

        public event EventHandler<CustomEventArgs<ExpansionUpdateEvent>> ExpansionUpdateNeeded;
        public void OnExpansionUpdateNeeded(object sender, CustomEventArgs<ExpansionUpdateEvent> e)
        {
            if (ExpansionUpdateNeeded != null)
                ExpansionUpdateNeeded(sender, e);
        }
        #endregion

        #region Public Properties

        private HashedObservableCollection<CrowdModel, string> crowdCollection;
        public HashedObservableCollection<CrowdModel, string> CrowdCollection
        {
            get
            {
                return crowdCollection;
            }
            set
            {
                crowdCollection = value;
                OnPropertyChanged("CrowdCollection");
            }
        }

        private CrowdModel selectedCrowdModel;
        public CrowdModel SelectedCrowdModel
        {
            get
            {
                return selectedCrowdModel;
            }
            set
            {
                selectedCrowdModel = value;
                this.DeleteCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.EnterEditModeCommand.RaiseCanExecuteChanged();
                this.CloneCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.CutCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.LinkCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.PasteCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.RemoveAllActionsCommand.RaiseCanExecuteChanged();
                this.AddToRosterCommand.RaiseCanExecuteChanged();
                this.FlattenCrowdCopyCommand.RaiseCanExecuteChanged();
                this.NumberedFlattenCrowdCopyCommand.RaiseCanExecuteChanged();
            }
        }

        private CrowdMemberModel selectedCrowdMemberModel;
        public CrowdMemberModel SelectedCrowdMemberModel
        {
            get
            {
                return selectedCrowdMemberModel;
            }
            set
            {
                selectedCrowdMemberModel = value;
                this.DeleteCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.EnterEditModeCommand.RaiseCanExecuteChanged();
                this.CloneCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.CutCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.LinkCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.PasteCharacterCrowdCommand.RaiseCanExecuteChanged();
                this.AddToRosterCommand.RaiseCanExecuteChanged();
                this.EditCharacterCommand.RaiseCanExecuteChanged();
                this.CopyAllActionsCommand.RaiseCanExecuteChanged();
                this.RemoveAllActionsCommand.RaiseCanExecuteChanged();
                this.PasteActionsAsReferencesCommand.RaiseCanExecuteChanged();
            }
        }

        private CrowdModel selectedCrowdParent;
        public CrowdModel SelectedCrowdParent
        {
            get
            {
                return selectedCrowdParent;
            }
            set
            {
                selectedCrowdParent = value;
            }
        }

        private CrowdModel allCharactersCrowd;
        public CrowdModel AllCharactersCrowd
        {
            get
            {
                GetOrCreateAllCharactersCrowd();
                return allCharactersCrowd;
            }
        }

        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                OnPropertyChanged("Filter");
                ApplyFilter();
            }
        }
        private int flattenNumber;
        public int FlattenNumber
        {
            get
            {
                return flattenNumber;
            }
            set
            {
                flattenNumber = value;
                OnPropertyChanged("FlattenNumber");
            }
        }

        private object clipboardObject;
        public object ClipboardObject
        {
            get 
            { 
                return clipboardObject; 
            }
            set 
            { 
                clipboardObject = value;
                this.PasteCharacterCrowdCommand.RaiseCanExecuteChanged();
            }
        }

        public string OriginalName { get; set; }
        public bool IsUpdatingCharacter { get; set; }

        #endregion

        #region Commands

        public DelegateCommand<object> LoadCrowdCollectionCommand { get; private set; }
        public DelegateCommand<object> AddCrowdCommand { get; private set; }
        public DelegateCommand<object> AddCharacterCommand { get; private set; }
        public DelegateCommand<object> SaveCommand { get; private set; }
        public DelegateCommand<object> DeleteCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitCharacterCrowdRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }
        public DelegateCommand<object> AddToRosterCommand { get; private set; }
        public DelegateCommand<object> CloneCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> CutCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> LinkCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> FlattenCrowdCopyCommand { get; private set; }
        public DelegateCommand<object> NumberedFlattenCrowdCopyCommand { get; private set; }
        public DelegateCommand<object> PasteCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> EditCharacterCommand { get; private set; }
        public ICommand UpdateSelectedCrowdMemberCommand { get; private set; }
        public DelegateCommand<object> AddCrowdFromModelsCommand { get; private set; }
        public DelegateCommand<object> CopyAllActionsCommand { get; private set; }
        public DelegateCommand<object> PasteActionsAsReferencesCommand { get; private set; }
        public DelegateCommand<object> RemoveAllActionsCommand { get; private set; }
        public DelegateCommand<object> CheckRosterConsistencyCommand { get; private set; }

        #endregion

        #region Constructor

        public CharacterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, ICrowdRepository crowdRepository, IDesktopKeyEventHandler keyEventHandler, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.crowdRepository = crowdRepository;
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.desktopKeyEventHandler = keyEventHandler;
            InitializeCommands();
            //this.eventAggregator.GetEvent<SaveCrowdEvent>().Subscribe(this.SaveCrowdCollection);
            this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Subscribe(this.AddToRoster);
            this.eventAggregator.GetEvent<StopAllActiveAbilitiesEvent>().Subscribe(this.StopAllActiveAbilities);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.AttackEnded);
            this.eventAggregator.GetEvent<CloneLinkCrowdMemberEvent>().Subscribe(this.CloneLinkCharacter);
            this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Subscribe(this.GetAbilityCollection);
            //this.eventAggregator.GetEvent<NeedIdentityCollectionRetrievalEvent>().Subscribe(this.GetIdentityCollection);
            this.eventAggregator.GetEvent<NeedDefaultCharacterRetrievalEvent>().Subscribe(this.GetDefaultCharacter);
            this.eventAggregator.GetEvent<RemoveMovementEvent>().Subscribe(this.DeleteMovement);
            this.eventAggregator.GetEvent<GameLoadedEvent>().Subscribe(this.CheckRosterConsistency);
            this.eventAggregator.GetEvent<RosterSyncCompletedEvent>().Subscribe(this.HideBusyAnimation);

            InitializeDesktopKeyHanders();
        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.LoadCrowdCollectionCommand = new DelegateCommand<object>(this.LoadCrowdCollection);
            this.CheckRosterConsistencyCommand = new DelegateCommand<object>(this.CheckRosterConsistency);
            this.AddCrowdCommand = new DelegateCommand<object>(this.AddCrowd);
            this.AddCharacterCommand = new DelegateCommand<object>(this.AddCharacter);
            this.SaveCommand = new DelegateCommand<object>(this.SaveCrowdCollection);
            this.DeleteCharacterCrowdCommand = new DelegateCommand<object>(this.DeleteCharacterCrowd, this.CanDeleteCharacterCrowd);
            this.EnterEditModeCommand = new DelegateCommand<object>(this.EnterEditMode, this.CanEnterEditMode);
            this.SubmitCharacterCrowdRenameCommand = new DelegateCommand<object>(this.SubmitCharacterCrowdRename);
            this.CancelEditModeCommand = new DelegateCommand<object>(this.CancelEditMode);
            this.CloneCharacterCrowdCommand = new DelegateCommand<object>(this.CloneCharacterCrowd, this.CanCloneCharacterCrowd);
            this.CutCharacterCrowdCommand = new DelegateCommand<object>(this.CutCharacterCrowd, this.CanCutCharacterCrowd);
            this.LinkCharacterCrowdCommand = new DelegateCommand<object>(this.LinkCharacterCrowd, this.CanLinkCharacterCrowd);
            this.FlattenCrowdCopyCommand = new DelegateCommand<object>(this.FlattenCopyCrowd, this.CanFlattenCopyCrowd);
            this.NumberedFlattenCrowdCopyCommand = new DelegateCommand<object>(this.NumberedFlattenCopyCrowd, this.CanFlattenCopyCrowd);
            this.PasteCharacterCrowdCommand = new DelegateCommand<object>(this.PasteCharacterCrowd, this.CanPasteCharacterCrowd);
            this.AddToRosterCommand = new DelegateCommand<object>(this.AddToRoster, this.CanAddToRoster);
            this.EditCharacterCommand = new DelegateCommand<object>(this.EditCharacter, this.CanEditCharacter);
            this.AddCrowdFromModelsCommand = new DelegateCommand<object>(this.AddCrowdFromModels);
            this.CopyAllActionsCommand = new DelegateCommand<object>(this.CopyAllActions, this.CanCopyAllActions);
            this.PasteActionsAsReferencesCommand = new DelegateCommand<object>(this.PasteActionsAsReferences, this.CanPasteActionsAsReferences);
            this.RemoveAllActionsCommand = new DelegateCommand<object>(this.RemoveAllActions, this.CanRemoveAllActions);
            UpdateSelectedCrowdMemberCommand = new SimpleCommand
            {
                ExecuteDelegate = x =>
                    UpdateSelectedCrowdMember(x)
            };
        }

        private void InitializeDesktopKeyHanders()
        {
            this.desktopKeyEventHandler.AddKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        #endregion

        #region Methods

        #region Command Disabling during Attack

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
            this.DeleteCharacterCrowdCommand.RaiseCanExecuteChanged();
            this.CloneCharacterCrowdCommand.RaiseCanExecuteChanged();
            this.CutCharacterCrowdCommand.RaiseCanExecuteChanged();
            this.LinkCharacterCrowdCommand.RaiseCanExecuteChanged();
            this.PasteCharacterCrowdCommand.RaiseCanExecuteChanged();
            this.EditCharacterCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Rename Character or Crowd

        private bool CanEnterEditMode(object state)
        {
            return !(this.SelectedCrowdModel == null || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.selectedCrowdMemberModel == null) || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel != null && (this.SelectedCrowdMemberModel.Name == Constants.DEFAULT_CHARACTER_NAME || this.SelectedCrowdMemberModel.Name == Constants.COMBAT_EFFECTS_CHARACTER_NAME)));
        }
        private void EnterEditMode(object state)
        {
            if (this.SelectedCrowdMemberModel != null)
            {
                this.OriginalName = SelectedCrowdMemberModel.Name;
                this.IsUpdatingCharacter = true;
            }
            else
            {
                this.OriginalName = SelectedCrowdModel.Name;
                this.IsUpdatingCharacter = false;
            }
            OnEditModeEnter(state, null);
        }

        private void SubmitCharacterCrowdRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);
                bool duplicateName = CheckDuplicateName(updatedName);
                if (!duplicateName)
                {
                    RenameCharacterCrowd(updatedName);
                    OnEditModeLeave(state, null);
                    //this.SaveCrowdCollection();
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, Messages.DUPLICATE_NAME_CAPTION, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    this.CancelEditMode(state);
                }  
            }
            
        }

        private void CancelEditMode(object state)
        {
            if (this.IsUpdatingCharacter)
                SelectedCrowdMemberModel.Name = this.OriginalName;
            else
                SelectedCrowdModel.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void RenameCharacterCrowd(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            if (this.IsUpdatingCharacter)
            {
                if (SelectedCrowdMemberModel == null)
                {
                    return;
                }
                SelectedCrowdMemberModel.Name = updatedName;
                //this.characterCollection.UpdateKey(this.OriginalName, updatedName);
                //this.characterCollection.Sort();
                this.OriginalName = null;
            }
            else
            {
                if (SelectedCrowdModel == null)
                {
                    return;
                }
                SelectedCrowdModel.Name = updatedName;
                this.CrowdCollection.UpdateKey(this.OriginalName, updatedName);
                this.CrowdCollection.Sort(ListSortDirection.Ascending, new CrowdMemberModelComparer());
                this.OriginalName = null;
            }

            List<CrowdMemberModel> rosterCharacters = new List<CrowdMemberModel>();
            eventAggregator.GetEvent<AddToRosterEvent>().Publish(rosterCharacters); // sending empty list so that roster sorts its elements
        }
        private bool CheckDuplicateName(string updatedName)
        {
            bool isDuplicate = false;
            if (updatedName != this.OriginalName)
            {
                if (this.IsUpdatingCharacter)
                {
                    if (this.AllCharactersCrowd.CrowdMemberCollection.ContainsKey(updatedName))
                    {
                        isDuplicate = true;
                    }
                }
                else
                {
                    if (this.CrowdCollection.ContainsKey(updatedName))
                    {
                        isDuplicate = true;
                    }
                }
            }
            return isDuplicate;
        }
        #endregion

        #region Load Crowd Collection
        private void LoadCrowdCollection(object state)
        {
            this.containerWindowName = Helper.GetContainerWindowName(state);
            if(!crowdCollectionLoaded)
            {
                this.BusyService.ShowBusy(new string[] { containerWindowName });
                this.crowdRepository.GetCrowdCollection(this.LoadCrowdCollectionCallback);
            }
        }

        private void LoadCrowdCollectionCallback(List<CrowdModel> crowdList)
        {
            this.eventAggregator.GetEvent<StopListeningForTargetChanged>().Publish(null);
            this.CrowdCollection = new HashedObservableCollection<CrowdModel, string>(crowdList,
                (CrowdModel c) => { return c.Name; }, (CrowdModel c) => { return c.Order; }, (CrowdModel c) => { return c.Name; }
                );
            Action d =
               delegate ()
               {
                   this.AddDefaultCharactersWithDefaultAbilities();
                   this.AddDefaultMovementsToCharacters();
                   this.crowdCollectionLoaded = true;
                   if (rosterSyncNeeded)
                       CheckRosterConsistency(null);
                   else
                       this.BusyService.HideBusy();
                   //this.AddCrowdsFromFile(); // Add models for Jeff
               };
            Application.Current.Dispatcher.BeginInvoke(d);
            
            this.eventAggregator.GetEvent<ListenForTargetChanged>().Publish(null);
            try
            {
                this.CrowdCollection.Sort(ListSortDirection.Ascending, new CrowdMemberModelComparer());
            }
            catch(Exception ex)
            {

            }         
        }

        private async void CheckRosterConsistency(object state)
        {
            this.rosterSyncNeeded = true;
            if (crowdCollectionLoaded)
            {
                this.BusyService.ShowBusy(new string[] { containerWindowName });
                Action d = delegate ()
                {
                    var rosterMembers = GetFlattenedMemberList(crowdCollection.Cast<ICrowdMemberModel>().ToList()).Where(x => { return x.RosterCrowd != null; }).Cast<CrowdMemberModel>();
                    eventAggregator.GetEvent<CheckRosterConsistencyEvent>().Publish(rosterMembers);
                    this.rosterSyncNeeded = false;
                };
                await Task.Run(d);
            }
        }

        private void HideBusyAnimation(object state)
        {
            this.BusyService.HideAllBusy();
        }

        private void AddDefaultMovementsToCharacters()
        {
            foreach(var c in this.AllCharactersCrowd.CrowdMemberCollection)
            {
                (c as Character).AddDefaultMovements();
            }
        }

        private void AddDefaultCharactersWithDefaultAbilities()
        {
            var defaultCharacter = this.AllCharactersCrowd.CrowdMemberCollection.FirstOrDefault(m=>m.Name == Constants.DEFAULT_CHARACTER_NAME);
            var combatEffectsCharacter = this.AllCharactersCrowd.CrowdMemberCollection.FirstOrDefault(m => m.Name == Constants.COMBAT_EFFECTS_CHARACTER_NAME);
            if(defaultCharacter == null || combatEffectsCharacter == null)
            {
                var defaultCrowdCollection = this.crowdRepository.LoadDefaultCrowdMembers();
                if (defaultCharacter == null)
                {
                    defaultCharacter = defaultCrowdCollection[0].CrowdMemberCollection.FirstOrDefault(cm => cm.Name == Constants.DEFAULT_CHARACTER_NAME);
                    if (defaultCharacter != null)
                    {
                        this.AddCharacterToAllCharactersCrowd(defaultCharacter as Character);
                    }
                }
                if (combatEffectsCharacter == null)
                {
                    combatEffectsCharacter = defaultCrowdCollection[0].CrowdMemberCollection.FirstOrDefault(cm => cm.Name == Constants.COMBAT_EFFECTS_CHARACTER_NAME);
                    if (defaultCharacter != null)
                    {
                        this.AddCharacterToAllCharactersCrowd(combatEffectsCharacter as Character);
                    }
                }
            }
            Helper.GlobalDefaultAbilities = (defaultCharacter as Character).AnimatedAbilities.ToList();
            Helper.GlobalCombatAbilities = (combatEffectsCharacter as Character).AnimatedAbilities.ToList();
            Helper.GlobalMovements = (defaultCharacter as Character).Movements.ToList();
        }

        #endregion

        #region Update Selected Crowd
        private void UpdateSelectedCrowdMember(object state)
        {
            if (state != null) // Update selection
            {
                if (!isUpdatingCollection)
                {
                    ICrowdMemberModel selectedCrowdMember;
                    Object selectedCrowdModel = Helper.GetCurrentSelectedCrowdInCrowdCollection(state, out selectedCrowdMember);
                    CrowdModel crowdModel = selectedCrowdModel as CrowdModel;
                    if(crowdModel != null) // Only update if something is selected
                    {
                        this.SelectedCrowdModel = crowdModel;
                        this.SelectedCrowdMemberModel = selectedCrowdMember as CrowdMemberModel;
                    }
                }
                else
                    this.lastCharacterCrowdStateToUpdate = state; 
            }
            else // Unselect
            {
                this.SelectedCrowdModel = null;
                this.SelectedCrowdMemberModel = null;
                this.SelectedCrowdParent = null;
                OnEditNeeded(null, null);
            }
        }

        private void LockModelAndMemberUpdate(bool isLocked)
        {
            this.isUpdatingCollection = isLocked;
            if (!isLocked)
                this.UpdateCharacterCrowdTree();
        }

        private void UpdateCharacterCrowdTree()
        {
            // Update character crowd if necessary
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
            }
        }
        #endregion

        #region Add Crowd

        private CrowdModel lastCrowd = null;

        private void AddCrowdsFromFile()
        {
            if (this.CrowdCollection.FirstOrDefault(c => c.Name == "COHModels") == null)
                this.AddCrowd("COHModels", null);
            lastCrowd = this.CrowdCollection.FirstOrDefault(c => c.Name == "COHModels");
            var fileName = "npc.mnu";
            string[] alllines = File.ReadAllLines(fileName);
            try
            {
                foreach (string line in alllines)
                {
                    if (line.Trim().StartsWith("{"))
                    {

                    }
                    else if (line.Trim().StartsWith("Menu", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string name = line.Replace("Menu ", "");
                        name = name.Replace("\"", "");
                        name = name.Trim();
                        this.AddCrowd(name, lastCrowd);
                    }
                    else if (line.Trim().StartsWith("}"))
                    {
                        lastCrowd = lastCrowd.ParentCrowd as CrowdModel;
                    }
                    else if (line.Trim().StartsWith("Option", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string first = line.Replace("Option ", "").Trim();
                        int upto = first.IndexOf("\"", 1);
                        string name = first.Substring(1, upto - 1);
                        name = name.Trim();

                        string model = first.Substring(upto + 1);
                        model = model.Replace("\"", "").Trim();
                        string[] tokens = model.Split(' ');
                        model = tokens[1];
                        this.AddCharacter(name, model, lastCrowd);
                    }
                    else
                    {

                    }
                }
            }
            catch(Exception ex)
            {

            }
            
        }

        private void AddCrowd(string name, Crowd parent)
        {
            this.SelectedCrowdModel = lastCrowd;
            CrowdModel crowdModel = this.GetNewCrowdModel(name);
            crowdModel.ParentCrowd = parent;
            this.LockModelAndMemberUpdate(true);
            this.AddNewCrowdModel(crowdModel);
            this.LockModelAndMemberUpdate(false);
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
            }
            lastCrowd = crowdModel;
            this.SelectedCrowdModel = crowdModel;
        }

        private void AddCharacter(string name, string model, Crowd parent)
        {
            this.SelectedCrowdModel = lastCrowd;
            Character character = this.GetNewCharacter(name, model, IdentityType.Model);
            this.AddNewCharacter(character);
            lastCrowd = parent as CrowdModel;
            this.SelectedCrowdModel = lastCrowd;
        }

        private void AddCrowd(object state)
        {
            // Create a new Crowd
            CrowdModel crowdModel = this.GetNewCrowdModel();
            // Lock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(true);
            // Add the new Model
            this.AddNewCrowdModel(crowdModel);
            // Update Repository asynchronously
            //this.SaveCrowdCollection();
            // UnLock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(false);
            // Update character crowd if necessary
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
            }

            // Enter Edit mode for the added model
            OnEditNeeded(crowdModel, null);
        }

        private void AddCrowdFromModels(object state)
        {
            // Create a new Crowd
            CrowdModel crowdModel = this.GetNewCrowdModel("Crowd From Models");
            // Lock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(true);
            // Add the new Model
            this.AddNewCrowdModel(crowdModel);
            // Update Repository asynchronously
            //this.SaveCrowdCollection();
            // UnLock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(false);
            // Update character crowd if necessary
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
            }

            this.eventAggregator.GetEvent<CreateCrowdFromModelsEvent>().Publish(crowdModel);
        }

        private CrowdModel GetNewCrowdModel(string name = "Crowd")
        {
            //string name = "Crowd";
            string fullName = GetAppropriateCrowdName(name);
            return new CrowdModel(fullName);
        }

        private string GetAppropriateCrowdName(string name, CrowdModel crowdModel = null)
        {
            if (crowdModel == null)
                crowdModel = this.SelectedCrowdModel;
            string suffix = string.Empty;
            string rootName = name;
            int i = 0;
            Regex reg = new Regex(@"\((\d+)\)");
            MatchCollection matches = reg.Matches(name);
            if (matches.Count > 0)
            {
                int k;
                Match match = matches[matches.Count - 1];
                if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out k))
                {
                    i = k + 1;
                    suffix = string.Format(" ({0})", i);
                    rootName = name.Substring(0, match.Index).TrimEnd();
                }
            }
            string newName = rootName + suffix;
            
            if(crowdModel == null || crowdModel == this.AllCharactersCrowd)
            {
                while (this.CrowdCollection.ContainsKey(newName))
                {
                    suffix = string.Format(" ({0})", ++i);
                    newName = rootName + suffix;
                }
            }
            else
            {
                while (crowdModel.CrowdMemberCollection.ContainsKey(newName))
                {
                    suffix = string.Format(" ({0})", ++i);
                    newName = rootName + suffix;
                }
            }
            
            return newName;
        }

        private void AddNewCrowdModel(CrowdModel crowdModel)
        {
            // Also add the crowd under any currently selected crowd
            this.AddCrowdToSelectedCrowd(crowdModel);
            // Add the crowd to List of Crowd Members as a new Crowd Member
            this.AddCrowdToCrowdCollection(crowdModel);
            
        }

        private void AddCrowdToCrowdCollection(CrowdModel crowdModel)
        {
            if (this.SelectedCrowdModel == null || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME))
            {
                this.CrowdCollection.Add(crowdModel);
                this.CrowdCollection.Sort(ListSortDirection.Ascending, new CrowdMemberModelComparer()); 
            }
        }

        private void AddCrowdToSelectedCrowd(CrowdModel crowdModel)
        {
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                //if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                //    this.SelectedCrowdModel.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(ListSortDirection.Ascending, x => x.Name);
                this.SelectedCrowdModel.Add(crowdModel);
                //this.SelectedCrowdModel.IsExpanded = true;
                //OnExpansionNeeded(this.SelectedCrowdModel, null);
            }
        }

        #endregion

        #region Add Character

        private void AddCharacter(object state)
        {
            // Create a new Character
            Character character = this.GetNewCharacter();
            // Now Add this
            this.AddNewCharacter(character);
            // Add default movements
            character.AddDefaultMovements();
            // Update Repository asynchronously
            //this.SaveCrowdCollection();
            // Enter edit mode for the added character
            OnEditNeeded(character, null); 
        }

        public Character GetNewCharacter(string name = "Character", string surface = null, IdentityType type = IdentityType.Model)
        {
            string fullName = GetAppropriateCharacterName(name);
            return new CrowdMemberModel(fullName, surface, type);
        }

        public string GetAppropriateCharacterName(string name)
        {
            string suffix = string.Empty;
            string rootName = name;
            int i = 0;
            Regex reg = new Regex(@"\((\d+)\)");
            MatchCollection matches = reg.Matches(name);
            if (matches.Count > 0)
            {
                int k;
                Match match = matches[matches.Count - 1];
                if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out k))
                {
                    i = k + 1;
                    suffix = string.Format(" ({0})", i);
                    rootName = name.Substring(0, match.Index).TrimEnd();
                }
            }
            string newName = rootName + suffix;
            while (this.AllCharactersCrowd.CrowdMemberCollection.ContainsKey(newName))
            {
                suffix = string.Format(" ({0})", ++i);
                newName = rootName + suffix;
            }
            return newName;
        }

        private void AddNewCharacter(Character character)
        {
            // Add the Character under All Characters List
            this.AddCharacterToAllCharactersCrowd(character);
            // Also add the character under any currently selected crowd
            this.AddCharacterToCrowd(character, this.SelectedCrowdModel);
        }

        private void GetOrCreateAllCharactersCrowd()
        {
            if (this.CrowdCollection != null)
            {
                allCharactersCrowd = this.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
                if (allCharactersCrowd == null)
                {
                    CreateAllCharactersCrowd();
                }
            }
        }

        private void CreateAllCharactersCrowd()
        {
            CrowdModel crowdModelAllCharacters = new CrowdModel(Constants.ALL_CHARACTER_CROWD_NAME, -1);
                this.CrowdCollection.Add(crowdModelAllCharacters);
                this.CrowdCollection.Sort(ListSortDirection.Ascending, new CrowdMemberModelComparer());
            this.allCharactersCrowd = crowdModelAllCharacters;
        }

        private void AddCharacterToAllCharactersCrowd(Character character)
        {
            AllCharactersCrowd.Add(character as CrowdMemberModel);
        }

        private void AddCharacterToCrowd(Character character, CrowdModel crowdModel)
        {
            if (crowdModel != null && crowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                crowdModel.Add(character as CrowdMemberModel);
                //crowdModel.IsExpanded = true;
            }
        }
        
        #endregion

        #region Delete Character or Crowd

        public bool CanDeleteCharacterCrowd(object state)
        {
            bool canDeleteCharacterOrCrowd = false;
            if (SelectedCrowdModel != null && !Helper.GlobalVariables_IsPlayingAttack)
            {
                if (SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                    canDeleteCharacterOrCrowd = true;
                else
                {
                    if (SelectedCrowdMemberModel != null)
                    {
                        if(SelectedCrowdMemberModel.Name != Constants.DEFAULT_CHARACTER_NAME && SelectedCrowdMemberModel.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME)
                            canDeleteCharacterOrCrowd = true;
                    }
                }
            }

            return canDeleteCharacterOrCrowd;
        }

        public void DeleteCharacterCrowd(object state)
        { 
            // Lock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(true);
            ICrowdMemberModel rosterMember = null;
            // Determine if Character or Crowd is to be deleted
            if (SelectedCrowdMemberModel != null) // Delete Character
            {
                if (SelectedCrowdMemberModel.RosterCrowd != null && SelectedCrowdMemberModel.RosterCrowd.Name == SelectedCrowdModel.Name)
                    rosterMember = SelectedCrowdMemberModel;
                // Check if the Character is in All Characters. If so, prompt
                if(SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME)
                {
                    var chosenOption = this.messageBoxService.ShowDialog(Messages.DELETE_CHARACTER_FROM_ALL_CHARACTERS_CONFIRMATION_MESSAGE, Messages.DELETE_CHARACTER_CAPTION, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    switch (chosenOption)
                    { 
                        case System.Windows.MessageBoxResult.Yes:
                            // Delete the Character from all the crowds
                            DeleteCrowdMemberFromAllCrowdsByName(SelectedCrowdMemberModel.Name);
                            rosterMember = SelectedCrowdMemberModel; // Removing character from all crowds, so remove from roster irrespective of roster crowd.
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    // Delete the Character from all occurances of this crowd
                    DeleteCrowdMemberFromCrowdModelByName(SelectedCrowdModel, SelectedCrowdMemberModel.Name);
                }
            }
            else // Delete Crowd
            {
                //If it is a nested crowd, just delete it from the parent
                if (this.SelectedCrowdParent != null)
                {
                    string nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                    DeleteNestedCrowdFromCrowdModelByName(SelectedCrowdParent, nameOfDeletingCrowdModel);
                }
                // Check if there are containing characters. If so, prompt
                else if (SelectedCrowdModel.CrowdMemberCollection != null && SelectedCrowdModel.CrowdMemberCollection.Where(cm => cm is CrowdMember).Count() > 0)
                {
                    var chosenOption = this.messageBoxService.ShowDialog(Messages.DELETE_CONTAINING_CHARACTERS_FROM_CROWD_PROMPT_MESSAGE, Messages.DELETE_CROWD_CAPTION, System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Warning);
                    switch (chosenOption)
                    {
                        case System.Windows.MessageBoxResult.Yes:
                            // Delete crowd specific characters from All Characters and this crowd
                            List<ICrowdMemberModel> crowdSpecificCharacters = FindCrowdSpecificCrowdMembers(this.selectedCrowdModel);
                            string nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                            DeleteCrowdMembersFromAllCrowdsByList(crowdSpecificCharacters);
                            DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowdModel);
                            DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowdModel);
                            rosterMember = SelectedCrowdModel;
                            break;
                        case System.Windows.MessageBoxResult.No:
                            nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                            DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowdModel);
                            DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowdModel);
                            rosterMember = SelectedCrowdModel;
                            break;
                        default:
                            break;
                    }
                }
                // or just delete the crowd from crowd collection and other crowds
                else
                {
                    string nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                    DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowdModel);
                    DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowdModel);
                }
            }
            // Update ability collections
            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            this.eventAggregator.GetEvent<NeedIdentityCollectionRetrievalEvent>().Publish(null);
            // Finally save repository
            //this.SaveCrowdCollection();
            // UnLock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(false);
            // Update character crowd if necessary
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
            }
            if (rosterMember != null)
                this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Publish(rosterMember);
            if (this.SelectedCrowdModel != null)
            {
                OnExpansionUpdateNeeded(this.SelectedCrowdModel, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
            }
        }

        private List<ICrowdMemberModel> FindCrowdSpecificCrowdMembers(CrowdModel crowdModel)
        {
            List<ICrowdMemberModel> crowdSpecificCharacters = new List<ICrowdMemberModel>();
            foreach (ICrowdMemberModel cMember in crowdModel.CrowdMemberCollection)
            {
                if (cMember is CrowdMemberModel)
                {
                    CrowdMemberModel currentCrowdMember = cMember as CrowdMemberModel;
                    foreach (CrowdModel cModel in this.CrowdCollection.Where(cm => cm.Name != crowdModel.Name && cm.Name != AllCharactersCrowd.Name))
                    {
                        List<ICrowdMemberModel> flattenedMembers = GetFlattenedMemberList(cModel.CrowdMemberCollection.ToList()).Where(m => m is CrowdMemberModel).ToList();
                        var crm = flattenedMembers.Where(cm => cm.Name == currentCrowdMember.Name).FirstOrDefault();
                        if (crm == null || cModel.Name == AllCharactersCrowd.Name)
                        {
                            if (crowdSpecificCharacters.Where(csc => csc.Name == currentCrowdMember.Name).FirstOrDefault() == null)
                                crowdSpecificCharacters.Add(currentCrowdMember);
                        }
                        else
                            break;
                    }
                }
            }
            return crowdSpecificCharacters;
        }

        private void DeleteCrowdMemberFromAllCrowdsByName(string nameOfDeletingCrowdMember)
        {
            foreach (CrowdModel cModel in this.CrowdCollection)
            {
                DeleteCrowdMemberFromCrowdModelByName(cModel, nameOfDeletingCrowdMember);
                DeleteCrowdMemberFromNestedCrowdByName(cModel, nameOfDeletingCrowdMember);
            }
            DeleteCrowdMemberFromCharacterCollectionByName(nameOfDeletingCrowdMember);
        }

        private void DeleteCrowdMemberFromNestedCrowdByName(CrowdModel crowdModel, string nameOfDeletingCrowdMember)
        {
            if(crowdModel.CrowdMemberCollection != null && crowdModel.CrowdMemberCollection.Count > 0)
            {
                foreach(var cm in crowdModel.CrowdMemberCollection)
                {
                    if(cm is CrowdModel)
                    {
                        var cmm = cm as CrowdModel;
                        if (cmm.CrowdMemberCollection != null)
                        {
                            var crm = cmm.CrowdMemberCollection.Where(cmmm => cmmm.Name == nameOfDeletingCrowdMember).FirstOrDefault();
                            if(crm != null)
                                cmm.Remove(crm);
                            DeleteCrowdMemberFromNestedCrowdByName(cmm, nameOfDeletingCrowdMember);
                        }
                    }
                }
            }
        }
        private void DeleteCrowdMemberFromCrowdModelByName(CrowdModel crowdModel, string nameOfDeletingCrowdMember)
        {
            if (crowdModel.CrowdMemberCollection != null)
            {
                var crm = crowdModel.CrowdMemberCollection.Where(cm => cm.Name == nameOfDeletingCrowdMember).FirstOrDefault();
                crowdModel.Remove(crm); 
            }
            OnExpansionUpdateNeeded(crowdModel, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
        }
        private void DeleteCrowdMemberFromCharacterCollectionByName(string nameOfDeletingCrowdMember)
        {
            var charFromAllCrowd = AllCharactersCrowd.CrowdMemberCollection.Where(c => c.Name == nameOfDeletingCrowdMember).FirstOrDefault();
            this.AllCharactersCrowd.Remove(charFromAllCrowd);
        }
        private void DeleteCrowdMemberFromCharacterCollectionByList(List<ICrowdMemberModel> crowdMembersToDelete)
        {
            foreach(var crowdMemberToDelete in crowdMembersToDelete)
            {
                var deletingCrowdMember = AllCharactersCrowd.CrowdMemberCollection.Where(c => c.Name == crowdMemberToDelete.Name).FirstOrDefault();
                AllCharactersCrowd.Remove(deletingCrowdMember);
            }
        }

        private void DeleteCrowdMembersFromAllCrowdsByList(List<ICrowdMemberModel> crowdMembersToDelete)
        {
            foreach (CrowdModel cModel in this.CrowdCollection)
            {
                DeleteCrowdMembersFromCrowdModelByList(cModel, crowdMembersToDelete);
            }
            DeleteCrowdMemberFromCharacterCollectionByList(crowdMembersToDelete);
        }
        private void DeleteCrowdMembersFromCrowdModelByList(CrowdModel crowdModel, List<ICrowdMemberModel> crowdMembersToDelete)
        {
            if (crowdModel.CrowdMemberCollection != null)
            {
                foreach (var crowdMemberToDelete in crowdMembersToDelete)
                {
                    var deletingCrowdMemberFromModel = crowdModel.CrowdMemberCollection.Where(cm => cm.Name == crowdMemberToDelete.Name).FirstOrDefault();
                    crowdModel.Remove(deletingCrowdMemberFromModel);
                }
                OnExpansionUpdateNeeded(crowdModel, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
            }
        }
        private void DeleteNestedCrowdFromAllCrowdsByName(string nameOfDeletingCrowdModel)
        {
            foreach (CrowdModel cModel in this.CrowdCollection)
            {
                DeleteNestedCrowdFromCrowdModelByName(cModel, nameOfDeletingCrowdModel);
            }
        }
        private void DeleteNestedCrowdFromCrowdModelByName(CrowdModel crowdModel, string nameOfDeletingCrowdModel)
        {
            if (crowdModel.CrowdMemberCollection != null)
            {
                var crowdModelToDelete = crowdModel.CrowdMemberCollection.Where(cm => cm.Name == nameOfDeletingCrowdModel).FirstOrDefault();
                if (crowdModelToDelete != null)
                    crowdModel.Remove(crowdModelToDelete);
                OnExpansionUpdateNeeded(crowdModel, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
            }
        }
        private void DeleteCrowdFromCrowdCollectionByName(string nameOfDeletingCrowdModel)
        {
            var crowdToDelete = this.CrowdCollection.Where(cr => cr.Name == nameOfDeletingCrowdModel).FirstOrDefault();
            this.CrowdCollection.Remove(crowdToDelete);
        }
        #endregion

        #region Save Crowd Collection

        private void SaveCrowdCollection(object o = null)
        {
            this.BusyService.ShowBusy(new string[] { containerWindowName});
            this.crowdRepository.SaveCrowdCollection(this.SaveCrowdCollectionCallback, this.CrowdCollection.ToList());
        }

        private void SaveCrowdCollectionCallback()
        {
            
            Action d =
               delegate()
               {
                   this.BusyService.HideBusy();
                   this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Publish(null);
               };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        #endregion

        #region Clone Character/Crowd

        public bool CanCloneCharacterCrowd(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack && !(this.SelectedCrowdModel == null || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.selectedCrowdMemberModel == null) || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel != null && (this.SelectedCrowdMemberModel.Name == Constants.DEFAULT_CHARACTER_NAME || this.SelectedCrowdMemberModel.Name == Constants.COMBAT_EFFECTS_CHARACTER_NAME)));
        }
        public void CloneCharacterCrowd(object state)
        {
            if (this.SelectedCrowdMemberModel != null)
                this.ClipboardObject = this.SelectedCrowdMemberModel;
            else
                this.ClipboardObject = this.SelectedCrowdModel;
            clipboardAction = ClipboardAction.Clone;
        }

        #endregion

        #region Cut Character/Crowd

        public bool CanCutCharacterCrowd(object state)
        {
            bool canCut = true;
            if (Helper.GlobalVariables_IsPlayingAttack)
                canCut = false;
            else if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel == null)
            {
                canCut = false;
            }
            else if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel != null && (this.SelectedCrowdMemberModel.Name == Constants.DEFAULT_CHARACTER_NAME || this.SelectedCrowdMemberModel.Name == Constants.COMBAT_EFFECTS_CHARACTER_NAME))
                canCut = false;
            return canCut;
        }
        public void CutCharacterCrowd(object state)
        {
            if (this.SelectedCrowdMemberModel != null)
            {
                this.ClipboardObject = this.SelectedCrowdMemberModel;
                this.clipboardObjectOriginalCrowd = this.SelectedCrowdModel;
            }
            else
            {
                this.ClipboardObject = this.SelectedCrowdModel;
                this.clipboardObjectOriginalCrowd = this.SelectedCrowdParent;
            }
            clipboardAction = ClipboardAction.Cut;
        }

        #endregion

        #region Link Character/Crowd
        public bool CanLinkCharacterCrowd(object state)
        {
            bool canLink = true;
            if (Helper.GlobalVariables_IsPlayingAttack)
                canLink = false;
            else if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel == null)
            {
                canLink = false;
            }
            else if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel != null && (this.SelectedCrowdMemberModel.Name == Constants.DEFAULT_CHARACTER_NAME || this.SelectedCrowdMemberModel.Name == Constants.COMBAT_EFFECTS_CHARACTER_NAME))
                canLink = false;
            return canLink;
        }
        public void LinkCharacterCrowd(object state)
        {
            if (this.SelectedCrowdMemberModel != null)
            {
                this.ClipboardObject = this.SelectedCrowdMemberModel;
            }
            else
            {
                this.ClipboardObject = this.SelectedCrowdModel;
            }
            clipboardAction = ClipboardAction.Link;
        }
        #endregion

        #region CloneLink
        public void CloneLinkCharacter(ICrowdMemberModel character)
        {
            this.ClipboardObject = character;
            clipboardAction = ClipboardAction.CloneLink;
        }

        #endregion

        #region Flatten Copy Crowd

        public bool CanFlattenCopyCrowd(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack && this.SelectedCrowdModel!=null && this.SelectedCrowdModel.Name != AllCharactersCrowd.Name;
        }
        public void FlattenCopyCrowd(object state)
        {
            this.ClipboardObject = this.SelectedCrowdModel;
            clipboardAction = ClipboardAction.FlattenCopy;
            this.FlattenNumber = -1;
        }
        #endregion

        #region Flatten Copy Crowd
        public void NumberedFlattenCopyCrowd(object state)
        {
            this.ClipboardObject = this.SelectedCrowdModel;
            clipboardAction = ClipboardAction.NumberedFlattenCopy;
            OnFlattenNumberRequired(this, null);
        }
        #endregion

        #region Paste Character/Crowd
        public bool CanPasteCharacterCrowd(object state)
        {
            bool canPaste = false;
            if (!Helper.GlobalVariables_IsPlayingAttack)
            {
                switch (this.clipboardAction)
                {
                    case ClipboardAction.Clone:
                        canPaste = this.ClipboardObject != null
                            //if we are cloning a crowd we can not paste to all characters crowd
                            && !(this.ClipboardObject is CrowdModel && this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME)
                            //if we are cloning a crowd we can not paste inside itself
                            && ((this.ClipboardObject is CrowdModel && this.SelectedCrowdModel != this.ClipboardObject) || this.ClipboardObject is CrowdMemberModel);
                        break;
                    case ClipboardAction.Cut:
                        if (this.ClipboardObject != null && this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                        {
                            if (this.ClipboardObject is CrowdModel)
                            {
                                CrowdModel cutCrowdModel = this.ClipboardObject as CrowdModel;
                                if (cutCrowdModel.Name != this.SelectedCrowdModel.Name)
                                {
                                    if (cutCrowdModel.CrowdMemberCollection != null)
                                    {
                                        if (!IsCrowdNestedWithinContainerCrowd(SelectedCrowdModel.Name, cutCrowdModel))
                                            canPaste = true;
                                    }
                                    else
                                        canPaste = true;
                                }
                            }
                            else
                                canPaste = true;
                        }
                        break;
                    case ClipboardAction.Link:
                        if (this.ClipboardObject != null && this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                        {
                            if (this.ClipboardObject is CrowdModel)
                            {
                                CrowdModel linkedCrowdModel = this.ClipboardObject as CrowdModel;
                                if (linkedCrowdModel.Name != this.SelectedCrowdModel.Name)
                                {
                                    if (linkedCrowdModel.CrowdMemberCollection != null)
                                    {
                                        if (!IsCrowdNestedWithinContainerCrowd(SelectedCrowdModel.Name, linkedCrowdModel))
                                            canPaste = true;
                                    }
                                    else
                                        canPaste = true;
                                }
                            }
                            else
                                canPaste = true;
                        }
                        break;
                    case ClipboardAction.CloneLink:
                        if (this.ClipboardObject != null)
                            canPaste = true;
                        break;
                    case ClipboardAction.FlattenCopy:
                    case ClipboardAction.NumberedFlattenCopy:
                        if (this.ClipboardObject != null && this.ClipboardObject is CrowdModel)
                            canPaste = true;
                        break;
                } 
            }
            return canPaste;
        }
        public void PasteCharacterCrowd(object state)
        {
            bool saveNeeded = true;
            // Lock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(true);
            switch (clipboardAction)
            {
                case ClipboardAction.Clone:
                    {
                        if (this.ClipboardObject is CrowdMemberModel)
                        {
                            CrowdMemberModel clonedModel = (clipboardObject as CrowdMemberModel).Clone() as CrowdMemberModel;
                            //EliminateDuplicateName(clonedModel);
                            clonedModel.Name = GetAppropriateCharacterName(clonedModel.Name);
                            this.AddNewCharacter(clonedModel);
                            OnEditNeeded(clonedModel, null);
                        }
                        else
                        {
                            CrowdModel clonedModel = (clipboardObject as CrowdModel).Clone() as CrowdModel;
                            //EliminateDuplicateName(clonedModel);
                            clonedModel.Name = GetAppropriateCrowdName(clonedModel.Name);
                            if (clonedModel.CrowdMemberCollection != null)
                            {
                                List<ICrowdMemberModel> models = GetFlattenedMemberList(clonedModel.CrowdMemberCollection.ToList());
                                foreach (var member in models)
                                {
                                    if (member is CrowdMemberModel)
                                    {
                                        member.Name = GetAppropriateCharacterName(member.Name);
                                        this.AddCharacterToAllCharactersCrowd(member as CrowdMemberModel);
                                    }
                                    else
                                    {
                                        //member.Name = GetAppropriateCrowdName(member.Name);
                                        this.AddCrowdToCrowdCollection(member as CrowdModel);
                                    }
                                } 
                            }
                            this.AddNewCrowdModel(clonedModel);
                            OnEditNeeded(clonedModel, null);
                            clipboardObject = null;
                        }
                        break;
                    }
                case ClipboardAction.Cut:
                    {
                        ICrowdMemberModel model = this.ClipboardObject as ICrowdMemberModel;
                        if (MemberExistsInCrowd(model, this.SelectedCrowdModel))
                        {
                            saveNeeded = false;
                            break;
                        }
                        if (this.ClipboardObject is CrowdMemberModel)
                        {
                            CrowdMemberModel cutCharacter = this.ClipboardObject as CrowdMemberModel;
                            this.AddCharacterToCrowd(cutCharacter, this.SelectedCrowdModel);
                            CrowdModel sourceCrowdModel = this.clipboardObjectOriginalCrowd as CrowdModel;
                            if (sourceCrowdModel != null && sourceCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                                this.DeleteCrowdMemberFromCrowdModelByName(sourceCrowdModel, cutCharacter.Name);
                        }
                        else
                        {
                            CrowdModel cutCrowd = this.ClipboardObject as CrowdModel;
                            this.AddCrowdToSelectedCrowd(cutCrowd);
                            CrowdModel sourceCrowdModel = this.clipboardObjectOriginalCrowd as CrowdModel;
                            if (sourceCrowdModel != null)
                                this.DeleteNestedCrowdFromCrowdModelByName(sourceCrowdModel, cutCrowd.Name);
                        }
                        clipboardObject = null;
                        clipboardObjectOriginalCrowd = null;
                        break;
                    }
                case ClipboardAction.Link:
                    {
                        ICrowdMemberModel model = this.ClipboardObject as ICrowdMemberModel;
                        if (MemberExistsInCrowd(model, this.SelectedCrowdModel))
                        {
                            saveNeeded = false;
                            break;
                        }
                        if (this.ClipboardObject is CrowdMemberModel)
                        {
                            CrowdMemberModel linkedCharacter = this.ClipboardObject as CrowdMemberModel;
                            this.AddCharacterToCrowd(linkedCharacter, this.SelectedCrowdModel);
                        }
                        else
                        {
                            CrowdModel linkedCrowd = this.ClipboardObject as CrowdModel;
                            this.AddCrowdToSelectedCrowd(linkedCrowd);
                        }
                        clipboardObject = null;
                        break;
                    }
                case ClipboardAction.CloneLink:
                    {
                        ICrowdMemberModel model = this.ClipboardObject as ICrowdMemberModel;
                        if (this.SelectedCrowdModel == null || (this.SelectedCrowdModel != null && MemberExistsInCrowd(model, this.SelectedCrowdModel)) || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME))
                        {   // Do clone paste
                            this.SelectedCrowdModel = this.SelectedCrowdModel ?? model.RosterCrowd as CrowdModel;
                            CrowdMemberModel clonedModel = (clipboardObject as CrowdMemberModel).Clone() as CrowdMemberModel;
                            //EliminateDuplicateName(clonedModel);
                            clonedModel.Name = GetAppropriateCharacterName(clonedModel.Name);
                            this.AddNewCharacter(clonedModel);
                            OnEditNeeded(clonedModel, null);
                        }
                        else
                        {   // Do Link Paste
                            this.AddCharacterToCrowd(model as Character, this.SelectedCrowdModel);
                        }
                        clipboardObject = null;
                        break;
                    }
                case ClipboardAction.FlattenCopy:
                case ClipboardAction.NumberedFlattenCopy:
                    this.SelectedCrowdModel = this.AllCharactersCrowd;
                    CrowdModel crowdToFlatten = this.ClipboardObject as CrowdModel;
                    CrowdModel newCrowd = this.GetNewCrowdModel(crowdToFlatten.Name + " Flattened");
                    //EliminateDuplicateName(newCrowd);
                    newCrowd.Name = GetAppropriateCrowdName(newCrowd.Name);

                    List<ICrowdMemberModel> flattenedMembers = GetFlattenedMemberList(crowdToFlatten.CrowdMemberCollection.ToList()).Where(m => m is CrowdMemberModel).ToList();

                    int skipNumber = this.FlattenNumber;
                    for(int i =0; i < flattenedMembers.Count; i++)
                    {
                        if (skipNumber > 0)
                        {
                            if (i > 0 && i < skipNumber)
                            {
                                continue;
                            }
                            else
                            {
                                newCrowd.Add(flattenedMembers[i]);
                                skipNumber += i > 0 ? this.FlattenNumber : i;
                            }
                        }
                        else
                        {
                            newCrowd.Add(flattenedMembers[i]);
                        }
                    }
                    this.AddNewCrowdModel(newCrowd);
                    OnEditNeeded(newCrowd, null);
                    clipboardObject = null;
                    this.FlattenNumber = -1;
                    OnFlattenNumberEntryFinished(this, null);
                    break;
            }
            //if(saveNeeded)
            //    this.SaveCrowdCollection();
            if (SelectedCrowdModel != null)
            {
                OnExpansionUpdateNeeded(this.SelectedCrowdModel, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Paste});
            }
            // UnLock character crowd Tree from updating
            this.LockModelAndMemberUpdate(false);
        }

        #endregion

        #region Filter CrowdMembers

        private void ApplyFilter()
        {
            foreach (CrowdModel cr in CrowdCollection)
            {
                cr.ResetFilter();
            }

            foreach (CrowdModel cr in CrowdCollection)
            {
                cr.ApplyFilter(filter); 
                //OnExpansionUpdateNeeded(cr, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Filter });
            }
       }

        #endregion

        #region Add To Roster

        private bool CanAddToRoster(object state)
        {
            return !(this.SelectedCrowdMemberModel == null && this.SelectedCrowdModel == null); 
        }
        
        private void AddToRoster(object state)
        {
            this.LockModelAndMemberUpdate(true);
            AddToRoster(new Tuple<CrowdMemberModel, CrowdModel>(SelectedCrowdMemberModel, SelectedCrowdModel));
            this.LockModelAndMemberUpdate(false);
        }

        private void AddToRoster(Tuple<CrowdMemberModel, CrowdModel> data)
        {
            CrowdMemberModel crowdMember = data != null ? data.Item1 : this.SelectedCrowdMemberModel;
            CrowdModel rosterCrowd = data != null ? data.Item2 : this.SelectedCrowdModel;
            List<CrowdMemberModel> rosterCharacters = new List<CrowdMemberModel>();
            if (rosterCrowd == null)
            {
                rosterCrowd = AllCharactersCrowd;
            }
            if (crowdMember != null)
            {
                if (crowdMember.RosterCrowd == null)
                {
                    crowdMember.RosterCrowd = rosterCrowd;
                    rosterCharacters.Add(crowdMember);
                }
                else if (crowdMember.RosterCrowd.Name != SelectedCrowdModel.Name)
                {
                    //////NO NEED TO CLONE - REQUIREMENT WITHDRAWN
                    //// This character is already added to roster under another crowd, so need to make a clone first
                    //CrowdMemberModel clonedModel = crowdMember.Clone() as CrowdMemberModel;
                    ////EliminateDuplicateName(clonedModel);
                    //clonedModel.Name = GetAppropriateCharacterName(clonedModel.Name);
                    //this.AddNewCharacter(clonedModel);
                    //// Now send to roster the cloned character
                    //clonedModel.RosterCrowd = rosterCrowd;
                    //rosterCharacters.Add(clonedModel);
                }
            }
            else
            {
                // Need to check every character inside this crowd whether they are already added or not
                // If a character is already added, we need to make clone of it and pass only the cloned copy to the roster, not the original copy
                this.rosterCrowdCharacterMembershipKeys = new List<Tuple<string, string>>();
                ConstructRosterCrowdCharacterMembershipKeys(rosterCrowd);
                foreach (Tuple<string, string> tuple in this.rosterCrowdCharacterMembershipKeys)
                {
                    string crowdMemberModelName = tuple.Item1;
                    string crowdModelName = tuple.Item2;
                    var crowdModel = this.CrowdCollection[crowdModelName];
                    IEnumerable<ICrowdMemberModel> modelList = this.CrowdCollection;
                    if (crowdModel == null)
                        crowdModel = FindNestedCrowd(modelList.ToList(), crowdModelName) as CrowdModel;
                    var crowdMemberModel = crowdModel.CrowdMemberCollection.Where(c => c.Name == crowdMemberModelName).FirstOrDefault() as CrowdMemberModel;
                    if (crowdMemberModel != null && crowdMemberModel.RosterCrowd == null)
                    {
                        // Character not added to roster yet, so just add to roster with the current crowdmodel
                        crowdMemberModel.RosterCrowd = crowdModel;
                        rosterCharacters.Add(crowdMemberModel);
                    }
                    else
                    {
                        //////NO NEED TO CLONE - REQUIREMENT WITHDRAWN
                        //// This character is already added to roster under another crowd, so need to make a clone first
                        //CrowdMemberModel clonedModel = crowdMemberModel.Clone() as CrowdMemberModel;
                        ////EliminateDuplicateName(clonedModel);
                        //clonedModel.Name = GetAppropriateCharacterName(clonedModel.Name);
                        //this.AddCharacterToAllCharactersCrowd(clonedModel);
                        //this.AddCharacterToCrowd(clonedModel, crowdModel);
                        //// Now send to roster the cloned character
                        //clonedModel.RosterCrowd = crowdModel;
                        //rosterCharacters.Add(clonedModel);
                    }
                }
            }

            if (rosterCharacters.Count > 0)
                eventAggregator.GetEvent<AddToRosterEvent>().Publish(rosterCharacters);
                //this.SaveCrowdCollection();
        }

        public void ConstructRosterCrowdCharacterMembershipKeys(CrowdModel crowdModel)
        {
            foreach (ICrowdMemberModel model in crowdModel.CrowdMemberCollection)
            {
                if (model is CrowdMemberModel)
                {
                    var crowdMemberModel = model as CrowdMemberModel;
                    if(crowdMemberModel.RosterCrowd == null)
                        this.rosterCrowdCharacterMembershipKeys.Add(new Tuple<string, string>(crowdMemberModel.Name, crowdModel.Name));
                    else if (crowdMemberModel.RosterCrowd.Name != crowdModel.Name)
                        this.rosterCrowdCharacterMembershipKeys.Add(new Tuple<string,string>(crowdMemberModel.Name, crowdModel.Name));
                }
                else
                    ConstructRosterCrowdCharacterMembershipKeys(model as CrowdModel);
            }
        }

        #endregion

        #region Edit Character

        private bool CanEditCharacter(object state)
        {
            return this.SelectedCrowdMemberModel != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void EditCharacter(object state)
        {
            this.eventAggregator.GetEvent<EditCharacterEvent>().Publish(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(this.SelectedCrowdMemberModel, this.AllCharactersCrowd.CrowdMemberCollection));
        }

        #endregion

        #region Retrieve Ability Collection

        private void GetAbilityCollection(object state)
        {
            Task.Run(() => this.GetAbilityCollectionAsync());
        }
        private async Task GetAbilityCollectionAsync()
        {
            if (crowdCollectionLoaded)
            {
                var abilityCollection = await GetAbilities();
                Action d = delegate ()
                {
                    this.eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Publish(abilityCollection);
                };
                Application.Current.Dispatcher.BeginInvoke(d); 
            }
        }

        private async Task<ObservableCollection<AnimatedAbility>> GetAbilities()
        {
            ObservableCollection<AnimatedAbility> abilityCollection = null;
            Action d = delegate ()
            {
                abilityCollection = new ObservableCollection<AnimatedAbility>(this.AllCharactersCrowd.CrowdMemberCollection.SelectMany((character) => { return (character as CrowdMemberModel).AnimatedAbilities; }).Distinct());
            };
            await Application.Current.Dispatcher.BeginInvoke(d);
            return abilityCollection;
        }

        #endregion

        #region Retrieve Identity Collection
        private void GetIdentityCollection(object state)
        {
            var identityCollection = new ObservableCollection<Identity>(this.AllCharactersCrowd.CrowdMemberCollection.SelectMany((character) => { return (character as CrowdMemberModel).AvailableIdentities; }).Distinct(new IdentityComparer()));
            this.eventAggregator.GetEvent<FinishedIdentityCollectionRetrievalEvent>().Publish(identityCollection);
        }

        #endregion

        #region Movements
        private void GetDefaultCharacter(object state)
        {
            Action<Character> getMovementCollectionCallback = state as Action<Character>;
            if(getMovementCollectionCallback != null)
            {
                var defaultCharacter = this.AllCharactersCrowd.CrowdMemberCollection.Where(cm => cm.Name == Constants.DEFAULT_CHARACTER_NAME).FirstOrDefault() as Character;
                getMovementCollectionCallback(defaultCharacter);
            }
        }

        private void DeleteMovement(object state)
        {
            string movementName = state as string;
            if(!string.IsNullOrEmpty(movementName))
            {
                var characterList = this.AllCharactersCrowd.CrowdMemberCollection.Where(c => (c as Character).Movements.FirstOrDefault(m => m.Name == movementName) != null).ToList();
                foreach(Character character in characterList)
                {
                    CharacterMovement cm = character.Movements.FirstOrDefault(m => m.Name == movementName);
                    character.Movements.Remove(cm);
                    if (character.DefaultMovement != null && character.DefaultMovement.Name == movementName)
                        character.DefaultMovement = null;
                    //if (character.ActiveMovement != null && character.ActiveMovement.Name == movementName)
                    //    character.ActiveMovement = null;
                }
            }
        }

        #endregion

        #region Drag Drop CrowdMembers

        public void DragDropSelectedCrowdMember(CrowdModel targetCrowdModel)
        {
            bool saveNeeded = false;
            this.LockModelAndMemberUpdate(true);
            if(this.SelectedCrowdMemberModel != null) // dragged a Character
            {
                // avoid linking or cloning of default and combat effect crowds
                // avoid dragging to all characters crowd
                if(this.SelectedCrowdMemberModel.Name != Constants.DEFAULT_CHARACTER_NAME && this.SelectedCrowdMemberModel.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && targetCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                {  
                    if (this.SelectedCrowdModel.Name == targetCrowdModel.Name)
                    {
                        // It is in the same crowd, so clone
                        CrowdMemberModel clonedModel = this.SelectedCrowdMemberModel.Clone() as CrowdMemberModel;
                        //EliminateDuplicateName(clonedModel);
                        clonedModel.Name = GetAppropriateCharacterName(clonedModel.Name);
                        this.AddNewCharacter(clonedModel);
                        saveNeeded = true;
                        OnEditNeeded(clonedModel, new CustomEventArgs<string>() { Value = "EditAfterDragDrop" });
                    }
                    else
                    {
                        // It is dragged to a different crowd, so link
                        if (!MemberExistsInCrowd(this.SelectedCrowdMemberModel, targetCrowdModel))
                        {
                            saveNeeded = true;
                            this.AddCharacterToCrowd(this.SelectedCrowdMemberModel, targetCrowdModel);
                        }
                    }
                }
            }
            else // dragged a Crowd
            {
                // link the crowd but don't create circular reference, and avoid all characters crowd
                if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME && targetCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME && targetCrowdModel.Name != this.SelectedCrowdModel.Name)
                {
                    bool canLinkCrowd = false;
                    if (SelectedCrowdModel.CrowdMemberCollection != null)
                    {
                        if (!IsCrowdNestedWithinContainerCrowd(targetCrowdModel.Name, SelectedCrowdModel))
                            canLinkCrowd = true;
                    }
                    else
                        canLinkCrowd = true;
                    if(canLinkCrowd)
                    {
                        saveNeeded = true;
                        if (!MemberExistsInCrowd(this.SelectedCrowdModel, targetCrowdModel))
                        {
                            targetCrowdModel.Add(this.SelectedCrowdModel); // Linking
                        }
                        else
                        {
                            // Cloning
                            CrowdModel clonedModel = this.SelectedCrowdModel.Clone() as CrowdModel;
                            //EliminateDuplicateName(clonedModel);
                            clonedModel.Name = GetAppropriateCrowdName(clonedModel.Name);
                            if (clonedModel.CrowdMemberCollection != null)
                            {
                                List<ICrowdMemberModel> models = GetFlattenedMemberList(clonedModel.CrowdMemberCollection.ToList());
                                foreach (var member in models)
                                {
                                    if (member is CrowdMemberModel)
                                    {
                                        member.Name = GetAppropriateCharacterName(member.Name);
                                        this.AddCharacterToAllCharactersCrowd(member as CrowdMemberModel);
                                    }
                                    else
                                    {
                                        //member.Name = GetAppropriateCrowdName(member.Name);
                                        this.AddCrowdToCrowdCollection(member as CrowdModel);
                                    }
                                }
                            }
                            targetCrowdModel.Add(clonedModel);
                            this.AddCrowdToCrowdCollection(clonedModel);
                            OnEditNeeded(clonedModel, new CustomEventArgs<string>() { Value = "EditAfterDragDrop" });
                            clipboardObject = null;
                        }
                    }
                }
            }
            //if(saveNeeded)
            //    this.SaveCrowdCollection();
            if (targetCrowdModel != null)
            {
                OnExpansionUpdateNeeded(targetCrowdModel, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.DragDrop });
            }
            this.LockModelAndMemberUpdate(false);
        }

        #endregion

        #region Desktop Key Handling
        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == Constants.CHARACTER_EXPLORER)
            {
                if (inputKey == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.CloneCharacterCrowdCommand.CanExecute(null))
                        this.CloneCharacterCrowdCommand.Execute(null);
                }
                else if (inputKey == Key.X && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.CutCharacterCrowdCommand.CanExecute(null))
                        this.CutCharacterCrowdCommand.Execute(null);
                }
                else if (inputKey == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.PasteCharacterCrowdCommand.CanExecute(null))
                        this.PasteCharacterCrowdCommand.Execute(null);
                }
                else if (inputKey == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.LinkCharacterCrowdCommand.CanExecute(null))
                        this.LinkCharacterCrowdCommand.Execute(null);
                }
                else if (inputKey == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.EditCharacterCommand.CanExecute(null))
                        this.EditCharacterCommand.Execute(null);
                }
                else if (inputKey == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.FlattenCrowdCopyCommand.CanExecute(null))
                        this.FlattenCrowdCopyCommand.Execute(null);
                }
                else if (inputKey == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.NumberedFlattenCrowdCopyCommand.CanExecute(null))
                        this.NumberedFlattenCrowdCopyCommand.Execute(null);
                }
                else if ((inputKey == Key.OemPlus || inputKey == Key.Add) && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.AddCharacterCommand.CanExecute(null))
                        this.AddCharacterCommand.Execute(null);
                }
                else if ((inputKey == Key.OemPlus || inputKey == Key.Add) && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    if (this.AddCrowdCommand.CanExecute(null))
                        this.AddCrowdCommand.Execute(null);
                }
                else if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.DeleteCharacterCrowdCommand.CanExecute(null))
                        this.DeleteCharacterCrowdCommand.Execute(null);
                }
                else if (inputKey == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.AddToRosterCommand.CanExecute(null))
                        this.AddToRosterCommand.Execute(null);
                }
                else if (inputKey == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.SaveCommand.Execute(null);
                }
            }
            
            return null;
        }

        #endregion


        #region Utility Methods
        private void EliminateDuplicateName(ICrowdMemberModel model)
        {
            if (model is CrowdModel)
            {
                CrowdModel crowdModel = model as CrowdModel;
                crowdModel.Name = GetAppropriateCrowdName(crowdModel.Name);
                if (crowdModel.CrowdMemberCollection != null)
                {
                    List<ICrowdMemberModel> models = GetFlattenedMemberList(crowdModel.CrowdMemberCollection.ToList());
                    foreach (var member in models)
                    {
                        member.Name = (member is CrowdModel) ? GetAppropriateCrowdName(member.Name) : GetAppropriateCharacterName(member.Name);
                    }
                }
            }
            else if (model is CrowdMemberModel)
            {
                model.Name = GetAppropriateCharacterName(model.Name);
            }
        }

        private List<ICrowdMemberModel> GetFlattenedMemberList(List<ICrowdMemberModel> list)
        {
            List<ICrowdMemberModel> _list = new List<ICrowdMemberModel>();
            foreach (ICrowdMemberModel cm in list)
            {
                if (cm is CrowdModel)
                {
                    CrowdModel crm = (cm as CrowdModel);
                    if (crm.CrowdMemberCollection != null && crm.CrowdMemberCollection.Count > 0)
                        _list.AddRange(GetFlattenedMemberList(crm.CrowdMemberCollection.ToList()));
                }
                _list.Add(cm);
            }
            return _list;
        }
        private bool MemberExistsInCrowd(ICrowdMemberModel model, CrowdModel crowdModel)
        {
            bool memberExists = false;
            if (crowdModel.CrowdMemberCollection != null)
            {
                var alreadyExistingCrowdMember = crowdModel.CrowdMemberCollection.Where(c => c.Name == model.Name).FirstOrDefault();
                if (alreadyExistingCrowdMember != null)
                {
                    memberExists = true;
                }
            }
            return memberExists;
        }
        private bool IsCrowdNestedWithinContainerCrowd(string crowdModelName, CrowdModel containerCrowdModel)
        {
            bool isNested = false;
            if (containerCrowdModel.CrowdMemberCollection != null)
            {
                List<ICrowdMemberModel> models = GetFlattenedMemberList(containerCrowdModel.CrowdMemberCollection.ToList());
                var model = models.Where(m => m.Name == crowdModelName).FirstOrDefault();
                if (model != null)
                    isNested = true;
            }
            return isNested;
        }

        private ICrowdMemberModel FindNestedCrowd(List<ICrowdMemberModel> list, string crowdName)
        {
            ICrowdMemberModel crowd = null;
            foreach(var cm in list)
            {
                if (cm is CrowdModel)
                {
                    CrowdModel crm = (cm as CrowdModel);
                    if(crm.Name == crowdName)
                    {
                        crowd = crm;
                        break;
                    }
                    else if (crm.CrowdMemberCollection != null && crm.CrowdMemberCollection.Count > 0)
                    {
                        crowd = FindNestedCrowd(crm.CrowdMemberCollection.ToList(), crowdName);
                        if (crowd != null)
                            break;
                    }
                }
            }
            return crowd;
        }

        private void StopAllActiveAbilities(object obj)
        {
            if (this.AllCharactersCrowd != null && this.AllCharactersCrowd.CrowdMemberCollection != null && this.AllCharactersCrowd.CrowdMemberCollection.Count > 0)
            {
                AnimatedAbilities.AnimatedAbility[] actives =
                        new ObservableCollection<AnimatedAbilities.AnimatedAbility>(
                            this.AllCharactersCrowd.CrowdMemberCollection.SelectMany(
                                (character) => { return (character as CrowdMemberModel).AnimatedAbilities; })
                                ).ToArray();
                for (int i = 0; i < actives.Count(); i++)
                {
                    actives[i].Stop();
                } 
            }
        }

        #endregion

        #region Copy/Remove All Actions As References

        private Character sourceCharacterForActionCopy;

        private bool CanCopyAllActions(object state)
        {
            return this.SelectedCrowdMemberModel != null;
        }

        private void CopyAllActions(object state)
        {
            this.sourceCharacterForActionCopy = this.SelectedCrowdMemberModel;
        }

        private bool CanPasteActionsAsReferences(object state)
        {
            return sourceCharacterForActionCopy != null && sourceCharacterForActionCopy.AnimatedAbilities.Count > 0;
        }

        private void PasteActionsAsReferences(object state)
        {
            if(this.SelectedCrowdMemberModel != null)
            {
                Character targetCharacter = this.SelectedCrowdMemberModel;
                PasteAbilities(targetCharacter);
                PasteIdentities(targetCharacter);
                PasteMovements(targetCharacter);
            }
            else if(this.SelectedCrowdModel != null)
            {
                List<ICrowdMemberModel> flattenedMembers = GetFlattenedMemberList(this.SelectedCrowdModel.CrowdMemberCollection.ToList()).Where(m => m is CrowdMemberModel).Distinct().ToList();
                foreach(var m in flattenedMembers)
                {
                    Character targetCharacter = m as Character;
                    PasteAbilities(targetCharacter);
                    PasteIdentities(targetCharacter);
                    PasteMovements(targetCharacter);
                }
            }

            sourceCharacterForActionCopy = null;
            this.CopyAllActionsCommand.RaiseCanExecuteChanged();
            this.RemoveAllActionsCommand.RaiseCanExecuteChanged();
            this.PasteActionsAsReferencesCommand.RaiseCanExecuteChanged();

            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            
        }

        private void PasteAbilities(Character targetCharacter)
        {
            foreach (AnimatedAbility ability in sourceCharacterForActionCopy.AnimatedAbilities)
            {
                string attackName = GetNewValidAbilityName(targetCharacter, ability.Name);
                Attack attack = new Attack(attackName, owner: targetCharacter);
                var refAbility = new ReferenceAbility("Reference - 1", null);
                AnimationResource refResource = new AnimationResource(ability);
                refAbility.Resource = refResource;
                refAbility.DisplayName = Path.GetFileNameWithoutExtension(refAbility.Resource);
                attack.AddAnimationElement(refAbility);
                if (ability.IsAttack)
                {
                    attack.IsAttack = true;
                    attack.IsAreaEffect = ability.IsAreaEffect;
                    var refAbilityOnHit = new ReferenceAbility("Reference - 1", null);
                    AnimationResource refResourceOnHit = new AnimationResource((ability as Attack).OnHitAnimation);
                    refAbilityOnHit.Resource = refResourceOnHit;
                    refAbilityOnHit.DisplayName = Path.GetFileNameWithoutExtension(refAbilityOnHit.Resource);
                    attack.OnHitAnimation.AddAnimationElement(refAbilityOnHit);
                }
                targetCharacter.AnimatedAbilities.Add(attack);

                this.eventAggregator.GetEvent<AddOptionEvent>().Publish(attack);
            }
        }

        private void PasteIdentities(Character targetCharacter)
        {
            for (int i = 1; i < sourceCharacterForActionCopy.AvailableIdentities.Count; i++)
            {
                // What if identity orders are changed??? Currently only the first one would be preserved
                Identity identity = sourceCharacterForActionCopy.AvailableIdentities[i].Clone();
                identity.Name = GetNewValidIdentityName(targetCharacter, identity.Name);
                if (identity.AnimationOnLoad != null)
                    identity.AnimationOnLoad.Owner = targetCharacter;
                targetCharacter.AvailableIdentities.Add(identity);
                this.eventAggregator.GetEvent<AddOptionEvent>().Publish(identity);
            }
        }

        private void PasteMovements(Character targetCharacter)
        {
            string[] defaultMovementNames = new string[] { "Walk", "Run", "Swim" };
            foreach (CharacterMovement cm in sourceCharacterForActionCopy.Movements.Where(m => !defaultMovementNames.Contains(m.Name)))
            {
                CharacterMovement characterMovement = cm.Clone();
                characterMovement.Name = GetNewValidCharacterMovementName(targetCharacter, characterMovement.Name);
                targetCharacter.Movements.Add(characterMovement);
                this.eventAggregator.GetEvent<AddOptionEvent>().Publish(characterMovement);
            }
        }

        public string GetNewValidAbilityName(Character character, string name = "Ability")
        {
            string suffix = string.Empty;
            int i = 0;
            while ((character.AnimatedAbilities.Cast<ICharacterOption>().Any((ICharacterOption opt) => { return opt.Name == name + suffix; })))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return string.Format("{0}{1}", name, suffix).Trim();
        }

        public string GetNewValidIdentityName(Character character, string name = "Identity")
        {
            string suffix = string.Empty;
            int i = 0;
            while ((character.AvailableIdentities.Cast<ICharacterOption>().Any((ICharacterOption opt) => { return opt.Name == name + suffix; })))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return string.Format("{0}{1}", name, suffix).Trim();
        }

        public string GetNewValidCharacterMovementName(Character character, string name = "Movement")
        {
            string suffix = string.Empty;
            int i = 0;
            while ((character.Movements.Cast<ICharacterOption>().Any((ICharacterOption opt) => { return opt.Name == name + suffix; })))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return string.Format("{0}{1}", name, suffix).Trim();
        }

        private bool CanRemoveAllActions(object state)
        {
            return this.SelectedCrowdMemberModel != null || (this.SelectedCrowdModel != null && this.SelectedCrowdModel != this.AllCharactersCrowd);
        }

        private void RemoveAllActions(object state)
        {
            if(this.SelectedCrowdMemberModel != null)
            {
                this.RemoveAbilities(this.SelectedCrowdMemberModel);
                this.RemoveIdentities(this.SelectedCrowdMemberModel);
                this.RemoveMovements(this.SelectedCrowdMemberModel);
            }
            else
            {
                List<ICrowdMemberModel> flattenedMembers = GetFlattenedMemberList(this.SelectedCrowdModel.CrowdMemberCollection.ToList()).Where(m => m is CrowdMemberModel).ToList();
                foreach (var m in flattenedMembers)
                {
                    Character targetCharacter = m as Character;
                    this.RemoveAbilities(targetCharacter);
                    this.RemoveIdentities(targetCharacter);
                    this.RemoveMovements(targetCharacter);
                }
            }
        }

        private void RemoveAbilities(Character character)
        {
            var abilities = character.AnimatedAbilities.ToList();
            foreach(var ability in abilities)
            {
                var abilityToRemove = character.AnimatedAbilities.FirstOrDefault(a => a.Name == ability.Name);
                character.AnimatedAbilities.Remove(abilityToRemove);
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Publish(abilityToRemove);
            }
        }

        private void RemoveIdentities(Character character)
        {
            var identities = character.AvailableIdentities.ToList();
            for(int i = 1; i < identities.Count; i++)
            {
                var identityToRemove = character.AvailableIdentities.FirstOrDefault(a => a.Name == identities[i].Name);
                character.AvailableIdentities.Remove(identityToRemove);
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Publish(identityToRemove);
            }
        }

        private void RemoveMovements(Character character)
        {
            var movements = character.Movements.ToList();
            string[] defaultMovementNames = new string[] { "Walk", "Run", "Swim" };
            foreach (var movement in movements.Where(m => !defaultMovementNames.Contains(m.Name)))
            {
                var movementToRemove = character.Movements.FirstOrDefault(a => a.Name == movement.Name);
                character.Movements.Remove(movementToRemove);
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Publish(movementToRemove);
            }
        }

        #endregion

        #endregion
    }
}
