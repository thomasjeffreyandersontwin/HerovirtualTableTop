using Framework.WPF.Behaviors;
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

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CharacterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private EventAggregator eventAggregator;
        private ICrowdRepository crowdRepository;
        private HashedObservableCollection<ICrowdMemberModel, string> characterCollection;
        private string filter;
        private CrowdModel clipboardObjectOriginalCrowd = null;
        private ClipboardAction clipboardAction;
        private bool isUpdatingCollection = false;
        private object lastCharacterCrowdStateToUpdate = null;

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
                this.PasteCharacterCrowdCommand.RaiseCanExecuteChanged();
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

        public DelegateCommand<object> AddCrowdCommand { get; private set; }
        public DelegateCommand<object> AddCharacterCommand { get; private set; }
        public DelegateCommand<object> DeleteCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitCharacterCrowdRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }
        public DelegateCommand<object> AddToRosterCommand { get; private set; }
        public DelegateCommand<object> CloneCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> PasteCharacterCrowdCommand { get; private set; }
        public ICommand UpdateSelectedCrowdMemberCommand { get; private set; }

        #endregion

        #region Constructor

        public CharacterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, ICrowdRepository crowdRepository, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.crowdRepository = crowdRepository;
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            LoadCrowdCollection();

        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.AddCrowdCommand = new DelegateCommand<object>(this.AddCrowd);
            this.AddCharacterCommand = new DelegateCommand<object>(this.AddCharacter);
            this.DeleteCharacterCrowdCommand = new DelegateCommand<object>(this.DeleteCharacterCrowd, this.CanDeleteCharacterCrowd);
            this.EnterEditModeCommand = new DelegateCommand<object>(this.EnterEditMode, this.CanEnterEditMode);
            this.SubmitCharacterCrowdRenameCommand = new DelegateCommand<object>(this.SubmitCharacterCrowdRename);
            this.CancelEditModeCommand = new DelegateCommand<object>(this.CancelEditMode);
            this.CloneCharacterCrowdCommand = new DelegateCommand<object>(this.CloneCharacterCrowd, this.CanCloneCharacterCrowd);
            this.PasteCharacterCrowdCommand = new DelegateCommand<object>(this.PasteCharacterCrowd, this.CanPasteCharacterCrowd);
            this.AddToRosterCommand = new DelegateCommand<object>(this.AddToRoster);
            UpdateSelectedCrowdMemberCommand = new SimpleCommand
            {
                ExecuteDelegate = x =>
                    UpdateSelectedCrowdMember(x)
            };
        }

        #endregion

        #region Methods

        #region Rename Character or Crowd

        private bool CanEnterEditMode(object state)
        { 
            return !(this.SelectedCrowdModel == null || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.selectedCrowdMemberModel == null));
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
                    this.SaveCrowdCollection();
                    OnEditModeLeave(state, null);
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
            if (this.IsUpdatingCharacter)
            {
                SelectedCrowdMemberModel.Name = updatedName;
                this.characterCollection.UpdateKey(this.OriginalName, updatedName);
                this.OriginalName = null;
            }
            else
            {
                SelectedCrowdModel.Name = updatedName;
                this.CrowdCollection.UpdateKey(this.OriginalName, updatedName);
                this.OriginalName = null;
            }
        }
        private bool CheckDuplicateName(string updatedName)
        {
            bool isDuplicate = false;
            if (updatedName != this.OriginalName)
            {
                if (this.IsUpdatingCharacter)
                {
                    if (this.characterCollection.ContainsKey(updatedName))
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
        private void LoadCrowdCollection()
        {
            //this.BusyService.ShowBusy();
            this.crowdRepository.GetCrowdCollection(this.LoadCrowdCollectionCallback);
        }

        private void LoadCrowdCollectionCallback(List<CrowdModel> crowdList)
        {
            this.CrowdCollection = new HashedObservableCollection<CrowdModel, string>(crowdList,
                (CrowdModel c) => { return c.Name; }
                );
            CrowdModel allCharactersModel = this.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
            if (allCharactersModel == null)
                allCharactersModel = new CrowdModel();
            this.characterCollection = new HashedObservableCollection<ICrowdMemberModel, string>(allCharactersModel.CrowdMemberCollection,
                (ICrowdMemberModel c) => { return c.Name; }
                );
            //this.BusyService.HideBusy();
        }

        #endregion

        #region Update Selected Crowd
        private void UpdateSelectedCrowdMember(object state)
        {
            if (!isUpdatingCollection)
            {
            ICrowdMemberModel selectedCrowdMember;
            Object selectedCrowdModel = Helper.GetCurrentSelectedCrowdInCrowdCollection(state, out selectedCrowdMember);
            CrowdModel crowdModel = selectedCrowdModel as CrowdModel;
            this.SelectedCrowdModel = crowdModel;
                this.SelectedCrowdMemberModel = selectedCrowdMember as CrowdMemberModel;
            }
            else
                this.lastCharacterCrowdStateToUpdate = state;
        }

        private void LockModelAndMemberUpdate(bool isLocked)
        {
            this.isUpdatingCollection = isLocked;
        }
        #endregion

        #region Add Crowd
        private void AddCrowd(object state)
        {
            // Create a new Crowd
            CrowdModel crowdModel = this.GetNewCrowdModel();
            // Lock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(true);
            // Add the new Model
            this.AddNewCrowdModel(crowdModel);
            // Update Repository asynchronously
            this.SaveCrowdCollection();
            // UnLock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(false);
            // Update character crowd if necessary
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
            }
        }
        private CrowdModel GetNewCrowdModel()
        {
            string name = "Crowd";
            string suffix = GetAppropriateCrowdNameSuffix(name);
            return new CrowdModel(name + suffix);
        }

        private string GetAppropriateCrowdNameSuffix(string name)
        {
            string suffix = string.Empty;
            int i = 0;
            while (this.CrowdCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return suffix;
        }

        private void AddNewCrowdModel(CrowdModel crowdModel)
        {
            // Add the crowd to List of Crowd Members as a new Crowd Member
            this.AddCrowdToCrowdCollection(crowdModel);
            // Also add the crowd under any currently selected crowd
            this.AddCrowdToSelectedCrowd(crowdModel);
        }

        private void AddCrowdToCrowdCollection(CrowdModel crowdModel)
        {
            this.CrowdCollection.Add(crowdModel);
        }

        private void AddCrowdToSelectedCrowd(CrowdModel crowdModel)
        {
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name);
                this.SelectedCrowdModel.CrowdMemberCollection.Add(crowdModel);
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
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }

        private Character GetNewCharacter()
        {
            string name = "Character";
            string suffix = GetAppropriateCharacterNameSuffix(name);
            return new CrowdMemberModel(name + suffix);
        }

        private string GetAppropriateCharacterNameSuffix(string name)
        {
            string suffix = string.Empty;
            int i = 0;
            while (this.characterCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return suffix;
        }

        private void AddNewCharacter(Character character)
        {
            // Add the Character under All Characters List
            this.AddCharacterToAllCharactersCrowd(character);
            // Also add the character under any currently selected crowd
            this.AddCharacterToSelectedCrowd(character);
        }

        private CrowdModel CreateAllCharactersCrowd()
        {
            // Create All Characters List if not already there
            CrowdModel crowdModelAllCharacters = new CrowdModel(Constants.ALL_CHARACTER_CROWD_NAME);
                this.CrowdCollection.Add(crowdModelAllCharacters);
                crowdModelAllCharacters.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name);
                this.characterCollection = new HashedObservableCollection<ICrowdMemberModel, string>(crowdModelAllCharacters.CrowdMemberCollection,
                    (ICrowdMemberModel c) => { return c.Name; });
            return crowdModelAllCharacters;
        }

        private void AddCharacterToAllCharactersCrowd(Character character)
        {
            CrowdModel crowdModelAllCharacters = this.CrowdCollection.Where(c => c.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            if (crowdModelAllCharacters == null || crowdModelAllCharacters.CrowdMemberCollection == null || crowdModelAllCharacters.CrowdMemberCollection.Count == 0)
            {
                crowdModelAllCharacters = CreateAllCharactersCrowd();
            }
            crowdModelAllCharacters.CrowdMemberCollection.Add(character as CrowdMemberModel);
            this.characterCollection.Add(character as CrowdMemberModel);
        }

        private void AddCharacterToSelectedCrowd(Character character)
        {
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name);
                this.SelectedCrowdModel.CrowdMemberCollection.Add(character as CrowdMemberModel);
            }
        }
        
        #endregion

        #region Delete Character or Crowd

        public bool CanDeleteCharacterCrowd(object state)
        {
            bool canDeleteCharacterOrCrowd = false;
            if (SelectedCrowdModel != null)
            {
                if (SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                    canDeleteCharacterOrCrowd = true;
                else
                {
                    if (SelectedCrowdMemberModel != null)
                        canDeleteCharacterOrCrowd = true;
                }
            }

            return canDeleteCharacterOrCrowd;
        }

        public void DeleteCharacterCrowd(object state)
        { 
            // Lock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(true);
            // Determine if Character or Crowd is to be deleted
            if (SelectedCrowdMemberModel != null) // Delete Character
            {
                // Check if the Character is in All Characters. If so, prompt
                if(SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME)
                {
                    var chosenOption = this.messageBoxService.ShowDialog(Messages.DELETE_CHARACTER_FROM_ALL_CHARACTERS_CONFIRMATION_MESSAGE, Messages.DELETE_CHARACTER_CAPTION, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    switch (chosenOption)
                    { 
                        case System.Windows.MessageBoxResult.Yes:
                            // Delete the Character from all the crowds
                            DeleteCrowdMemberFromAllCrowdsByName(SelectedCrowdMemberModel.Name);
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
                            break;
                        case System.Windows.MessageBoxResult.No:
                            nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                            DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowdModel);
                            DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowdModel);
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
            // Finally save repository
            this.SaveCrowdCollection();
            // UnLock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(false);
            // Update character crowd if necessary
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
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
                    foreach (CrowdModel cModel in this.CrowdCollection.Where(cm => cm.Name != SelectedCrowdModel.Name))
                    {
                        var crm = cModel.CrowdMemberCollection.Where(cm => cm is CrowdMemberModel && cm.Name == currentCrowdMember.Name).FirstOrDefault();
                        if (crm == null)
                        {
                            if (crowdSpecificCharacters.Where(csc => csc.Name == currentCrowdMember.Name).FirstOrDefault() == null)
                                crowdSpecificCharacters.Add(currentCrowdMember);
                        }
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
            }
            DeleteCrowdMemberFromCharacterCollectionByName(nameOfDeletingCrowdMember);
        }
        private void DeleteCrowdMemberFromCrowdModelByName(CrowdModel crowdModel, string nameOfDeletingCrowdMember)
        {
            if (crowdModel.CrowdMemberCollection != null)
            {
                var crm = crowdModel.CrowdMemberCollection.Where(cm => cm.Name == nameOfDeletingCrowdMember).FirstOrDefault();
                crowdModel.CrowdMemberCollection.Remove(crm); 
            }
        }
        private void DeleteCrowdMemberFromCharacterCollectionByName(string nameOfDeletingCrowdMember)
        {
            var charFromAllCrowd = characterCollection.Where(c => c.Name == nameOfDeletingCrowdMember).FirstOrDefault();
            this.characterCollection.Remove(charFromAllCrowd);
        }
        private void DeleteCrowdMemberFromCharacterCollectionByList(List<ICrowdMemberModel> crowdMembersToDelete)
        {
            foreach(var crowdMemberToDelete in crowdMembersToDelete)
            {
                var deletingCrowdMember = characterCollection.Where(c => c.Name == crowdMemberToDelete.Name).FirstOrDefault();
                characterCollection.Remove(deletingCrowdMember);
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
                    crowdModel.CrowdMemberCollection.Remove(deletingCrowdMemberFromModel);
                }
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
                    crowdModel.CrowdMemberCollection.Remove(crowdModelToDelete); 
            }
        }
        private void DeleteCrowdFromCrowdCollectionByName(string nameOfDeletingCrowdModel)
        {
            var crowdToDelete = this.CrowdCollection.Where(cr => cr.Name == nameOfDeletingCrowdModel).FirstOrDefault();
            this.CrowdCollection.Remove(crowdToDelete);
        }
        #endregion

        #region Save Crowd Collection

        private void SaveCrowdCollection()
        {
            //this.BusyService.ShowBusy();
            this.crowdRepository.SaveCrowdCollection(this.SaveCrowdCollectionCallback, this.CrowdCollection.ToList());
        }

        private void SaveCrowdCollectionCallback()
        {
            //this.BusyService.HideBusy();
        }

        #endregion

        #region Clone Character/Crowd

        public bool CanCloneCharacterCrowd(object state)
        {
            return !(this.SelectedCrowdModel == null || (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.selectedCrowdMemberModel == null));
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

        #region Paste Character/Crowd
        public bool CanPasteCharacterCrowd(object state)
        {
            return this.ClipboardObject != null && !(this.ClipboardObject is CrowdModel && this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME);
        }
        public void PasteCharacterCrowd(object state)
        {
            // Lock character crowd Tree from updating;
            this.LockModelAndMemberUpdate(true);
            switch (clipboardAction)
            {
                case ClipboardAction.Clone:
                    {
                        if (this.ClipboardObject is CrowdMemberModel)
                        {
                            CrowdMemberModel clonedModel = (clipboardObject as CrowdMemberModel).Clone() as CrowdMemberModel;
                            EliminateDuplicateName(clonedModel);
                            this.AddNewCharacter(clonedModel);
                        }
                        else
                        {
                            CrowdModel clonedModel = (clipboardObject as CrowdModel).Clone() as CrowdModel;
                            EliminateDuplicateName(clonedModel);
                            List<ICrowdMemberModel> models = GetFlattenedMemberList(clonedModel.CrowdMemberCollection.ToList());
                            foreach (var member in models)
                            {
                                if (member is CrowdMemberModel)
                                {
                                    this.AddCharacterToAllCharactersCrowd(member as CrowdMemberModel);
                                }
                                else
                                {
                                    this.AddCrowdToCrowdCollection(member as CrowdModel);
                                }
                            }
                            this.AddNewCrowdModel(clonedModel);
                        }
                        clipboardObject = null;
                        break;
                    }
                case ClipboardAction.Cut:
                    {
                        if (this.ClipboardObject is CrowdMemberModel)
                        {
                            //crowdDest.Add(clipboardObject as Character);
                            //if (!(clipboardObjectOriginalCrowd.Name == "All Characters"))
                            //{
                            //    clipboardObjectOriginalCrowd.Remove(clipboardObject as Character);
                            //}
                        }
                        else
                        {
                            //if (crowdDest.Name != "All Characters")
                            //    crowdDest.Add(clipboardObject as Crowd);
                            //clipboardObjectOriginalCrowd.Remove(clipboardObject as Crowd);
                        }
                        clipboardObject = null;
                        clipboardObjectOriginalCrowd = null;
                        break;
                    }
                case ClipboardAction.Link:
                    {
                        if (this.ClipboardObject is CrowdMemberModel)
                        {
                            //Character c = (clipboardObject as Character);
                            //crowdDest.Add(c);
                        }
                        else
                        {
                            //if (crowdDest.Name == "All Characters")
                            //    return;
                            //Crowd c = (clipboardObject as Crowd);
                            //crowdDest.Add(c);
                        }
                        clipboardObject = null;
                        break;
                    }
            }
            this.SaveCrowdCollection();
            if(SelectedCrowdModel != null)
                SelectedCrowdModel.IsExpanded = true;
            // UnLock character crowd Tree from updating
            this.LockModelAndMemberUpdate(false);
            // Update character crowd if necessary
            if (this.lastCharacterCrowdStateToUpdate != null)
            {
                this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
                this.lastCharacterCrowdStateToUpdate = null;
            }
        }

        private void EliminateDuplicateName(ICrowdMemberModel model)
        {
            if (model is CrowdModel)
            { 
                CrowdModel crowdModel = model as CrowdModel;
                string suffix = GetAppropriateCrowdNameSuffix(crowdModel.Name);
                crowdModel.Name += suffix;
                if(crowdModel.CrowdMemberCollection != null)
                {
                    List<ICrowdMemberModel> models = GetFlattenedMemberList(crowdModel.CrowdMemberCollection.ToList());
                    foreach (var member in models)
                    {
                        suffix = (member is CrowdModel) ? GetAppropriateCrowdNameSuffix(member.Name) : GetAppropriateCharacterNameSuffix(member.Name);
                        member.Name += suffix;
                    }
                }
                
            }
            else if (model is CrowdMemberModel)
            {
                string suffix = GetAppropriateCharacterNameSuffix(model.Name);
                model.Name += suffix;
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
                cr.ApplyFilter(filter); //Filter already check
            }
        }

        #endregion

        #region Add To Roster

        private void AddToRoster(object state)
        {
            if (SelectedCrowdMemberModel != null)
                eventAggregator.GetEvent<AddToRosterEvent>().Publish(new Tuple<ICrowdMemberModel, CrowdModel>(SelectedCrowdMemberModel, SelectedCrowdModel));
            else if (SelectedCrowdModel != null)
                eventAggregator.GetEvent<AddToRosterEvent>().Publish(new Tuple<ICrowdMemberModel, CrowdModel>(SelectedCrowdModel, null));
        }

        #endregion

        #endregion
    }
}
