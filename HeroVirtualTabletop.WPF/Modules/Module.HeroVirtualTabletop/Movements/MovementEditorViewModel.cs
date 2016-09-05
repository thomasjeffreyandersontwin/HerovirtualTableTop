using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
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

        private Character currentCharacter;
        public Character CurrentCharacter
        {
            get
            {
                return currentCharacter;
            }
            set
            {
                currentCharacter = value;
                OnPropertyChanged("CurrentCharacter");
            }
        }

        private Movement currentMovement;
        public Movement CurrentMovement
        {
            get
            {
                return currentMovement;
            }
            set
            {
                currentMovement = value;
                if (currentMovement != null && this.CurrentCharacter != null && this.CurrentCharacter.DefaultMovement == currentMovement)
                    this.IsDefaultMovementLoaded = true;
                else
                    this.IsDefaultMovementLoaded = false;
                OnPropertyChanged("CurrentMovement");
                this.SaveMovementCommand.RaiseCanExecuteChanged();
                this.RemoveMovementCommand.RaiseCanExecuteChanged();
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
            this.EnterMovementEditModeCommand = new DelegateCommand<object>(this.EnterMovementditMode);
            this.CancelMovementEditModeCommand = new DelegateCommand<object>(this.CancelMovementEditMode);
            this.AddMovementCommand = new DelegateCommand<object>(this.AddMovement, this.CanAddMovement);
            this.RemoveMovementCommand = new DelegateCommand<object>(this.RemoveMovement, this.CanRemoveMovement);
            this.SubmitMovementRenameCommand = new DelegateCommand<object>(this.SubmitMovementRename);
            this.SetDefaultMovementCommand = new DelegateCommand<object>(this.SetDefaultMovement, this.CanSetDefaultMovement);
            this.LoadResourcesCommand = new DelegateCommand<object>(this.LoadResources);
        }

        private void InitializeMovementSelections()
        {
            this.CurrentMovement = null;
            this.CurrentCharacter = null;
        }

        #endregion

        #region Methods

        #region Load Movement

        private void LoadMovement(Tuple<Movement, Character> tuple)
        {
            this.InitializeMovementSelections();
            this.IsShowingMovementEditor = true;
            this.CurrentCharacter = tuple.Item2;
            this.CurrentMovement = tuple.Item1;
            this.AvailableMovements = new ObservableCollection<Movement>(this.CurrentCharacter.Movements);
        }

        private void LoadMovement(Movement movement)
        {
            this.CurrentMovement = movement;
        }

        #endregion

        #region Rename Movement

        private void EnterMovementditMode(object state)
        {
            this.OriginalName = CurrentMovement.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelMovementEditMode(object state)
        {
            CurrentMovement.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitMovementRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);

                bool duplicateName = false;
                if (updatedName != this.OriginalName)
                    duplicateName = this.CurrentCharacter.Movements.ContainsKey(updatedName);

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
            CurrentMovement.Name = updatedName;
            // TODO: need to update each character that has this movement to use the updated name
            this.CurrentCharacter.Movements.UpdateKey(OriginalName, updatedName);
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
            Movement movement = new Movement("");
            this.AddMovement(movement);
            OnMovementAdded(movement, null);
            this.SaveMovement(null);
        }

        private void AddMovement(Movement movement)
        {
            
        }

        #endregion

        #region Remove Movement

        private bool CanRemoveMovement(object state)
        {
            return this.CurrentMovement != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void RemoveMovement(object state)
        {
            // TODO: Remove current movement from all characters
            this.SaveMovement(null);
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
            this.CurrentMovement = null;
            this.CurrentCharacter = null;
            this.IsShowingMovementEditor = false;
        }
        #endregion

        #region Demo Movement

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
            return this.CurrentMovement != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SetDefaultMovement(object state)
        {
            if(this.IsDefaultMovementLoaded)
            {
                this.CurrentCharacter.DefaultMovement = this.CurrentMovement;
            }
            else
            {
                this.CurrentCharacter.DefaultMovement = null;
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
            if (animationRes.Reference != null)
            {
                caseReferences = new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Name);
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
