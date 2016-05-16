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
using System.ComponentModel;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CharacterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private EventAggregator eventAggregator;
        private ICrowdRepository crowdRepository;
        //private HashedObservableCollection<ICrowdMemberModel, string> characterCollection;
        
        private string filter;
        private CrowdModel clipboardObjectOriginalCrowd = null;
        private ClipboardAction clipboardAction;
        private bool isUpdatingCollection = false;
        private object lastCharacterCrowdStateToUpdate = null;
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

        public event EventHandler SelectionUpdated;
        public void OnSelectionUpdated(object sender, EventArgs e)
        {
            if (SelectionUpdated != null)
            {
                SelectionUpdated(sender, e);
            }
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
                this.AddToRosterCommand.RaiseCanExecuteChanged();
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
                if (allCharactersCrowd == null)
                {
                    allCharactersCrowd = this.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
                }
                if (allCharactersCrowd == null)
                {
                    CreateAllCharactersCrowd();
                }
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
        public DelegateCommand<object> CutCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> LinkCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> PasteCharacterCrowdCommand { get; private set; }
        public DelegateCommand<object> EditCharacterCommand { get; private set; }
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
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Subscribe(this.SaveCrowdCollection);
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
            this.CutCharacterCrowdCommand = new DelegateCommand<object>(this.CutCharacterCrowd, this.CanCutCharacterCrowd);
            this.LinkCharacterCrowdCommand = new DelegateCommand<object>(this.LinkCharacterCrowd, this.CanLinkCharacterCrowd);
            this.PasteCharacterCrowdCommand = new DelegateCommand<object>(this.PasteCharacterCrowd, this.CanPasteCharacterCrowd);
            this.AddToRosterCommand = new DelegateCommand<object>(this.AddToRoster, this.CanAddToRoster);
            this.EditCharacterCommand = new DelegateCommand<object>(this.EditCharacter, this.CanEditCharacter);
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
                //this.characterCollection.UpdateKey(this.OriginalName, updatedName);
                //this.characterCollection.Sort();
                this.OriginalName = null;
            }
            else
            {
                SelectedCrowdModel.Name = updatedName;
                this.CrowdCollection.UpdateKey(this.OriginalName, updatedName);
                this.CrowdCollection.Sort();
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
        private void LoadCrowdCollection()
        {
            //this.BusyService.ShowBusy();
            this.crowdRepository.GetCrowdCollection(this.LoadCrowdCollectionCallback);
        }

        private void LoadCrowdCollectionCallback(List<CrowdModel> crowdList)
        {
            this.CrowdCollection = new HashedObservableCollection<CrowdModel, string>(crowdList,
                (CrowdModel c) => { return c.Name; }, (CrowdModel c) => { return c.Order; }, (CrowdModel c) => { return c.Name; }
                );
            
            //this.BusyService.HideBusy();
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
                    this.SelectedCrowdModel = crowdModel;
                    this.SelectedCrowdMemberModel = selectedCrowdMember as CrowdMemberModel;
                }
                else
                    this.lastCharacterCrowdStateToUpdate = state; 
            }
            else // Unselect
            {
                this.SelectedCrowdModel = null;
                this.SelectedCrowdMemberModel = null;
                this.SelectedCrowdParent = null;
                OnSelectionUpdated(null, null);
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
        private void AddCrowd(object state)
        {
            // Create a new Crowd
            CrowdModel crowdModel = this.GetNewCrowdModel();
            // Lock character crowd Tree from updating;
            //this.LockModelAndMemberUpdate(true);
            // Add the new Model
            this.AddNewCrowdModel(crowdModel);
            // Update Repository asynchronously
            this.SaveCrowdCollection();
            // UnLock character crowd Tree from updating;
            //this.LockModelAndMemberUpdate(false);
            // Update character crowd if necessary
            //if (this.lastCharacterCrowdStateToUpdate != null)
            //{
            //    this.UpdateSelectedCrowdMember(lastCharacterCrowdStateToUpdate);
            //    this.lastCharacterCrowdStateToUpdate = null;
            //}

            //Update selection in treeview 
            OnSelectionUpdated(crowdModel, null);
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
            //Methods Swapped by Chris
            // Also add the crowd under any currently selected crowd
            this.AddCrowdToSelectedCrowd(crowdModel);
            // Add the crowd to List of Crowd Members as a new Crowd Member
            this.AddCrowdToCrowdCollection(crowdModel);
        }

        private void AddCrowdToCrowdCollection(CrowdModel crowdModel)
        {
            this.CrowdCollection.Add(crowdModel);
            this.CrowdCollection.Sort();
        }

        private void AddCrowdToSelectedCrowd(CrowdModel crowdModel)
        {
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                //if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                //    this.SelectedCrowdModel.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(ListSortDirection.Ascending, x => x.Name);
                this.SelectedCrowdModel.Add(crowdModel);
                this.SelectedCrowdModel.IsExpanded = true;
                //this.SelectedCrowdModel.CrowdMemberCollection.Sort();
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

            //Update selection in treeview 
            OnSelectionUpdated(character, null); //Not working
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
            while (this.AllCharactersCrowd.CrowdMemberCollection.ContainsKey(name + suffix))
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
            this.AddCharacterToCrowd(character, this.SelectedCrowdModel);
        }

        private void CreateAllCharactersCrowd()
        {
            CrowdModel crowdModelAllCharacters = new CrowdModel(Constants.ALL_CHARACTER_CROWD_NAME, -1);
            this.CrowdCollection.Add(crowdModelAllCharacters);
            this.CrowdCollection.Sort();
            this.allCharactersCrowd = crowdModelAllCharacters;
        }

        private void AddCharacterToAllCharactersCrowd(Character character)
        {
            if (AllCharactersCrowd == null)
                CreateAllCharactersCrowd();
            AllCharactersCrowd.Add(character as CrowdMemberModel);
        }

        private void AddCharacterToCrowd(Character character, CrowdModel crowdModel)
        {
            if (crowdModel != null && crowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                crowdModel.Add(character as CrowdMemberModel);
                crowdModel.IsExpanded = true;
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
                crowdModel.Remove(crm); 
            }
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

        #region Cut Character/Crowd

        public bool CanCutCharacterCrowd(object state)
        {
            bool canCut = true;
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel == null)
            {
                canCut = false;
            }
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
            bool canCut = true;
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.SelectedCrowdMemberModel == null)
            {
                canCut = false;
            }
            return canCut;
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

        #region Paste Character/Crowd
        public bool CanPasteCharacterCrowd(object state)
        {
            bool canPaste = false;
            switch(this.clipboardAction)
            {
                case ClipboardAction.Clone:
                    canPaste = this.ClipboardObject != null && !(this.ClipboardObject is CrowdModel && this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME);
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
                            EliminateDuplicateName(clonedModel);
                            this.AddNewCharacter(clonedModel);
                        }
                        else
                        {
                            CrowdModel clonedModel = (clipboardObject as CrowdModel).Clone() as CrowdModel;
                            EliminateDuplicateName(clonedModel);
                            if (clonedModel.CrowdMemberCollection != null)
                            {
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
                            }
                            this.AddNewCrowdModel(clonedModel);
                        }
                        clipboardObject = null;
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
            }
            if(saveNeeded)
                this.SaveCrowdCollection();
            if(SelectedCrowdModel != null)
                SelectedCrowdModel.IsExpanded = true;
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
                cr.ApplyFilter(filter); //Filter already check
            }
        }

        #endregion

        #region Add To Roster

        private bool CanAddToRoster(object state)
        {
            return !(this.SelectedCrowdMemberModel == null && this.SelectedCrowdModel == null); ;
        }
        private void AddToRoster(object state)
        {
            this.LockModelAndMemberUpdate(true);
            bool saveNeeded = false;
            List<CrowdMemberModel> rosterCharacters = new List<CrowdMemberModel>();
            if (SelectedCrowdMemberModel != null)
            {
                if(SelectedCrowdMemberModel.RosterCrowd == null)
                {
                    SelectedCrowdMemberModel.RosterCrowd = this.SelectedCrowdModel;
                    rosterCharacters.Add(SelectedCrowdMemberModel);
                }
                else if(SelectedCrowdMemberModel.RosterCrowd.Name != SelectedCrowdModel.Name)
                {
                    // This character is already added to roster under another crowd, so need to make a clone first
                    CrowdMemberModel clonedModel = SelectedCrowdMemberModel.Clone() as CrowdMemberModel;
                    EliminateDuplicateName(clonedModel); 
                    this.AddNewCharacter(clonedModel);
                    saveNeeded = true;
                    // Now send to roster the cloned character
                    clonedModel.RosterCrowd = this.SelectedCrowdModel;
                    rosterCharacters.Add(clonedModel);
                }
            }   
            else
            {
                // Need to check every character inside this crowd whether they are already added or not
                // If a character is already added, we need to make clone of it and pass only the cloned copy to the roster, not the original copy
                this.rosterCrowdCharacterMembershipKeys = new List<Tuple<string, string>>();
                ConstructRosterCrowdCharacterMembershipKeys(this.SelectedCrowdModel);
                foreach (Tuple<string, string> tuple in this.rosterCrowdCharacterMembershipKeys)
                {
                    string crowdMemberModelName = tuple.Item1;
                    string crowdModelName = tuple.Item2;
                    var crowdModel = this.CrowdCollection[crowdModelName];
                    var crowdMemberModel = crowdModel.CrowdMemberCollection.Where(c => c.Name == crowdMemberModelName).First() as CrowdMemberModel;
                    if (crowdMemberModel.RosterCrowd == null)
                    {
                        // Character not added to roster yet, so just add to roster with the current crowdmodel
                        crowdMemberModel.RosterCrowd = crowdModel;
                        rosterCharacters.Add(crowdMemberModel);
                    }
                    else
                    {
                        // This character is already added to roster under another crowd, so need to make a clone first
                        CrowdMemberModel clonedModel = crowdMemberModel.Clone() as CrowdMemberModel;
                        EliminateDuplicateName(clonedModel);
                        this.AddCharacterToAllCharactersCrowd(clonedModel);
                        this.AddCharacterToCrowd(clonedModel, crowdModel);
                        saveNeeded = true;
                        // Now send to roster the cloned character
                        clonedModel.RosterCrowd = crowdModel;
                        rosterCharacters.Add(clonedModel);
                    }
                }
            }

            if (rosterCharacters.Count > 0)
                eventAggregator.GetEvent<AddToRosterEvent>().Publish(rosterCharacters);
            if (saveNeeded)
                this.SaveCrowdCollection();
            this.LockModelAndMemberUpdate(false);
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
            return this.SelectedCrowdMemberModel != null;
        }

        private void EditCharacter(object state)
        {
            this.eventAggregator.GetEvent<EditCharacterEvent>().Publish(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(this.SelectedCrowdMemberModel, this.AllCharactersCrowd.CrowdMemberCollection));
        }

        #endregion

        #region Utility Methods
        private void EliminateDuplicateName(ICrowdMemberModel model)
        {
            if (model is CrowdModel)
            {
                CrowdModel crowdModel = model as CrowdModel;
                string suffix = GetAppropriateCrowdNameSuffix(crowdModel.Name);
                crowdModel.Name += suffix;
                if (crowdModel.CrowdMemberCollection != null)
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

        #endregion

        #endregion
    }
}
