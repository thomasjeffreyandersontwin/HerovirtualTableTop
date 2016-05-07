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
            }
        }

        private CrowdMemberModel selectedCrowdMember;
        public CrowdMemberModel SelectedCrowdMember
        {
            get
            {
                return selectedCrowdMember;
            }
            set
            {
                selectedCrowdMember = value;
                this.DeleteCharacterCrowdCommand.RaiseCanExecuteChanged();
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
            return !(this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME && this.selectedCrowdMember == null);
        }
        private void EnterEditMode(object state)
        {
            if (this.SelectedCrowdMember != null)
            {
                this.OriginalName = SelectedCrowdMember.Name;
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
                SelectedCrowdMember.Name = this.OriginalName;
            else
                SelectedCrowdModel.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void RenameCharacterCrowd(string updatedName)
        {
            if (this.IsUpdatingCharacter)
            {
                SelectedCrowdMember.Name = updatedName;
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
            ICrowdMemberModel selectedCrowdMember;
            Object selectedCrowdModel = Helper.GetCurrentSelectedCrowdInCrowdCollection(state, out selectedCrowdMember);
            CrowdModel crowdModel = selectedCrowdModel as CrowdModel;
            this.SelectedCrowdModel = crowdModel;
            this.SelectedCrowdMember = selectedCrowdMember as CrowdMemberModel;
        }
        #endregion

        #region Add Crowd
        private void AddCrowd(object state)
        {
            // Create a new Crowd
            CrowdModel crowdModel = this.GetNewCrowdModel();
            // Add the crowd to List of Crowd Members as a new Crowd Member
            this.CrowdCollection.Add(crowdModel);
            // Also add the crowd under any currently selected crowd
            if(this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMemberModel>();
                this.SelectedCrowdModel.CrowdMemberCollection.Add(crowdModel);
            }
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }
        private CrowdModel GetNewCrowdModel()
        {
            string name = "Crowd";
            string suffix = string.Empty;
            int i = 0;
            while (this.CrowdCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return new CrowdModel(name + suffix);
        }

        #endregion

        #region Add Character

        private void AddCharacter(object state)
        {
            // Create a new Character
            Character character = this.GetNewCharacter();
            // Create All Characters List if not already there
            CrowdModel crowdModelAllCharacters = this.CrowdCollection.Where(c => c.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            if (crowdModelAllCharacters == null || crowdModelAllCharacters.CrowdMemberCollection == null || crowdModelAllCharacters.CrowdMemberCollection.Count == 0)
            {
                crowdModelAllCharacters = new CrowdModel(Constants.ALL_CHARACTER_CROWD_NAME);
                this.CrowdCollection.Add(crowdModelAllCharacters);
                crowdModelAllCharacters.CrowdMemberCollection = new ObservableCollection<ICrowdMemberModel>();
                this.characterCollection = new HashedObservableCollection<ICrowdMemberModel, string>(crowdModelAllCharacters.CrowdMemberCollection,
                    (ICrowdMemberModel c) => { return c.Name; });
            }
            // Add the Character under All Characters List
            crowdModelAllCharacters.CrowdMemberCollection.Add(character as CrowdMemberModel);
            this.characterCollection.Add(character as CrowdMemberModel);
            // Also add the character under any currently selected crowd
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMemberModel>();
                this.SelectedCrowdModel.CrowdMemberCollection.Add(character as CrowdMemberModel);
            }
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }

        private Character GetNewCharacter()
        {
            string name = "Character";
            string suffix = string.Empty;
            int i = 0;
            while (this.characterCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return new CrowdMemberModel(name + suffix);
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
                    if (SelectedCrowdMember != null)
                        canDeleteCharacterOrCrowd = true;
                }
            }

            return canDeleteCharacterOrCrowd;
        }

        public void DeleteCharacterCrowd(object state)
        { 
            // Determine if Character or Crowd is to be deleted
            if (SelectedCrowdMember != null) // Delete Character
            {
                // Check if the Character is in All Characters. If so, prompt
                if(SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME)
                {
                    var chosenOption = this.messageBoxService.ShowDialog(Messages.DELETE_CHARACTER_FROM_ALL_CHARACTERS_CONFIRMATION_MESSAGE, Messages.DELETE_CHARACTER_CAPTION, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    switch (chosenOption)
                    { 
                        case System.Windows.MessageBoxResult.Yes:
                            // Delete the Character from all the crowds
                            DeleteCrowdMemberFromAllCrowdsByName(SelectedCrowdMember.Name);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    // Delete the Character from all occurances of this crowd
                    DeleteCrowdMemberFromCrowdModelByName(SelectedCrowdModel, SelectedCrowdMember.Name);
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

        #region Filter CrowdMembers

        private void ApplyFilter()
        {
            foreach (CrowdModel cr in CrowdCollection)
            {
                cr.ApplyFilter(filter);
            }
        }

        #endregion

        #endregion
    }
}
