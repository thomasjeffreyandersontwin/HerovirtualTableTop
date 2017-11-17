using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Microsoft.Xna.Framework;
using Prism.Events;
using Module.Shared;
using Module.Shared.Enumerations;
using Module.Shared.Events;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Sevices;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Identities;

namespace Module.HeroVirtualTabletop.Roster
{
    public class RosterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private ITargetObserver targetObserver;
        private EventAggregator eventAggregator;
        private IDesktopKeyEventHandler desktopKeyEventHandler;

        //refactor to attacking character
        private Attack currentAttack = null;
        public List<Character> AttackingCharacters = new List<Character>();

        private bool isCharacterReset = false;
        public bool isMultiSelecting = false;

        private bool isCharacterDragDropInProgress = false;
        private Character currentDraggingCharacter = null;
        private DateTime lastDesktopMouseDownTime = DateTime.MinValue;
        private Vector3 lastMouseClickLocation = Vector3.Zero;

        public bool stopSyncingWithDesktop = false;

        private List<Character> targetCharactersForMove = new List<Character>();
        private List<Character> targetCharacters = new List<Character>();
        private List<CrowdMemberModel> _oldSelection = new List<CrowdMemberModel>();

        Character lastTargetedCharacter = null;
        private Character previousSelectedCharacter = null;
        public bool RosterMouseDoubleClicked = false;
        private bool moveAndAttackModeOn = false;
        public bool CharacterIsMoving
        {
            get
            {
                return Helper.GlobalVariables_CharacterMovement != null;
            }
        }

        private DesktopContextMenu desktopContextMenu;
        private DesktopMouseEventHandler mouseHandler;

        private System.Timers.Timer timer_RespondToDesktop = new System.Timers.Timer(); 

        #endregion

        public event EventHandler RosterMemberAdded;
        public void OnRosterMemberAdded(object sender, EventArgs e)
        {
            if (RosterMemberAdded != null)
                RosterMemberAdded(sender, e);
        }

        #region Public Properties

        public bool IsPlayingAttack { get; set; }
        public bool IsPlayingAreaEffect { get; set; }

        public IPopupService PopupService
        {
            get { return this.Container.Resolve<IPopupService>(); }
        }

        private HashedObservableCollection<ICrowdMemberModel, string> participants;
        public HashedObservableCollection<ICrowdMemberModel, string> Participants
        {
            get
            {
                if (participants == null)
                    participants = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name, x => x.RosterCrowd.Order, x => x.RosterCrowd.Name, x => x.Name);
                return participants;
            }
            set
            {
                participants = value;
                OnPropertyChanged("Participants");
            }
        }
        private List<ICrowdMemberModel> oldSelections = new List<ICrowdMemberModel>();
        private IList selectedParticipants = new ArrayList();
        public IList SelectedParticipants
        {
            get
            {
                return selectedParticipants;
            }
            set
            {
                MemoryElement targetedBeforeMouseCLick = new MemoryElement();
                
                selectedParticipants = value;
                //synchSelectionWithGame();
                OnPropertyChanged("SelectedParticipants");
                OnPropertyChanged("ShowAttackContextMenu");
                OnPropertyChanged("IsGangModeActive");
                ActivateCharacterCommand.RaiseCanExecuteChanged();
                ActivateGangCommand.RaiseCanExecuteChanged();
                ActivateCrowdAsGangCommand.RaiseCanExecuteChanged();
                ResetDistanceCounterCommand.RaiseCanExecuteChanged();
                ToggleGangModeCommand.RaiseCanExecuteChanged(); 
                this.TargetOrFollow();
                this.RestartDistanceCounting();
                //this.TargetLastSelectedCharacter(oldSelections);
                oldSelections.Clear();
                foreach (var sp in selectedParticipants)
                    oldSelections.Add(sp as ICrowdMemberModel);
                Commands_RaiseCanExecuteChanged();
                MemoryElement target = new MemoryElement();
                if (target.Label != "" && targetedBeforeMouseCLick.Label != target.Label)
                {
                    if (targetedBeforeMouseCLick.Label != "")
                        previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == targetedBeforeMouseCLick.Label) as Character;
                    else
                        previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == target.Label) as Character;
                }
            }
        }

        private ICrowdMemberModel activeCharacter;
        public ICrowdMemberModel ActiveCharacter
        {
            get
            {
                if (Participants.Any(p => (p as Character).IsGangLeader))
                    activeCharacter =  this.Participants.First(p => (p as Character).IsGangLeader);
                else
                    activeCharacter = this.Participants.FirstOrDefault(p => (p as Character).IsActive);

                return activeCharacter;
            }
            //set
            //{
            //    activeCharacter = value;
            //    Helper.GlobalVariables_ActiveCharacter = value as Character;
            //    OnPropertyChanged("ActiveCharacter");
            //}
        }

        private bool isGangActive;
        public bool IsGangActive
        {
            get
            {
                return isGangActive;
            }
            set
            {
                isGangActive = value;
                OnPropertyChanged("IsGangActive");
            }
        }

        private bool isGangModeActive;
        public bool IsGangModeActive
        {
            get
            {
                if (SelectedParticipants != null && SelectedParticipants.Count > 0)
                {
                    var currentRosterCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd;
                    isGangModeActive = currentRosterCrowd.IsGangMode;
                    //if (isGangModeActive)
                    //{
                    //    foreach (CrowdMemberModel sp in SelectedParticipants)
                    //    {
                    //        if (sp.RosterCrowd != currentRosterCrowd)
                    //        {
                    //            isGangModeActive = false;
                    //            break;
                    //        }
                    //    }
                    //}
                }
                else
                    isGangModeActive = false;
                return isGangModeActive;
            }
            set
            {
                isGangModeActive = value;
                if (SelectedParticipants != null && SelectedParticipants.Count > 0)
                {
                    //(SelectedParticipants[0] as CrowdMemberModel).RosterCrowd.IsGangMode = value;
                    foreach(CrowdMemberModel cm in SelectedParticipants)
                    {
                        cm.RosterCrowd.IsGangMode = value;
                    }
                }
                OnPropertyChanged("IsGangModeActive");
            }
        }

        private bool isCyclingCommandsThroughCrowd;
        public bool IsCyclingCommandsThroughCrowd
        {
            get
            {
                return isCyclingCommandsThroughCrowd;
            }
            set
            {
                isCyclingCommandsThroughCrowd = value;
                OnPropertyChanged("IsCyclingCommandsThroughCrowd");
            }
        }

        private bool targetOnHover;
        public bool TargetOnHover
        {
            get
            {
                return targetOnHover;
            }
            set
            {
                targetOnHover = value;
                OnPropertyChanged("TargetOnHover");
            }
        }

        private bool spawnOnClick;
        public bool SpawnOnClick
        {
            get
            {
                return spawnOnClick;
            }
            set
            {
                spawnOnClick = value;
                OnPropertyChanged("SpawnOnClick");
            }
        }

        private bool cloneAndSpawn;
        public bool CloneAndSpawn
        {
            get
            {
                return cloneAndSpawn;
            }
            set
            {
                cloneAndSpawn = value;
                OnPropertyChanged("CloneAndSpawn");
            }
        }

        private bool overheadMode;
        public bool OverheadMode
        {
            get
            {
                return overheadMode;
            }
            set
            {
                overheadMode = value;
                OnPropertyChanged("OverheadMode");
            }
        }

        private bool useRelativePositioning;
        public bool UseRelativePositioning
        {
            get
            {
                return useRelativePositioning;
            }
            set
            {
                useRelativePositioning = value;
                OnPropertyChanged("UseRelativePositioning");
            }
        }

        private Character currentDistanceCountingCharacter;
        public Character CurrentDistanceCountingCharacter
        {
            get
            {
                return currentDistanceCountingCharacter;
            }
            set
            {
                currentDistanceCountingCharacter = value;
                this.ResetDistanceCounterCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("CurrentDistanceCountingCharacter");
            }
        }

        public bool IsOperatingCrowd
        {
            get
            {
                List<Character> characters = GetCharactersToOperateOn();
                Character lastSelectedMember = this.GetLastSelectedCharacter();
                if (!participants.Any(p => p.RosterCrowd == (lastSelectedMember as CrowdMemberModel).RosterCrowd && !characters.Contains(p as Character)))
                {
                    return true;
                }
                return false;
            }
        }

        public CrowdModel SelectedCrowd
        {
            get
            {
                List<Character> characters = GetCharactersToOperateOn();
                CrowdMemberModel last = this.GetLastSelectedCharacter() as CrowdMemberModel;
                return last.RosterCrowd as CrowdModel;
            }
        }

        public bool IsSingleSpawnedCharacterSelected
        {
            get
            {
                return this.SelectedParticipants != null && SelectedParticipants.Count == 1 && (SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned;
            }
        }

        public bool ShowAttackContextMenu
        {
            get
            {
                bool showAttackContextMenu = false;
                if (this.IsPlayingAttack && this.SelectedParticipants != null && this.SelectedParticipants.Count > 0)
                {
                    showAttackContextMenu = true;
                    foreach (var participant in this.SelectedParticipants)
                    {
                        if (!(participant as Character).HasBeenSpawned)
                        {
                            showAttackContextMenu = false;
                            break;
                        }
                    }
                }

                return showAttackContextMenu;
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> SpawnCommand { get; private set; }
        public DelegateCommand<object> SavePositionCommand { get; private set; }
        public DelegateCommand<object> PlaceCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetedCommand { get; private set; }
        public DelegateCommand<object> TargetAndFollowCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToCameraCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToCharacterCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToMouseLocationCommand { get; private set; }
        public DelegateCommand<object> ToggleManeuverWithCameraCommand { get; private set; }
        public DelegateCommand<object> EditCharacterCommand { get; private set; }
        public DelegateCommand<object> ActivateCharacterCommand { get; private set; }
        public DelegateCommand<object> ResetCharacterStateCommand { get; private set; }
        public DelegateCommand<object> AttackTargetCommand { get; private set; }
        public DelegateCommand<object> AttackTargetAndExecuteCommand { get; private set; }
        public DelegateCommand<object> ResetOrientationCommand { get; private set; }
        public DelegateCommand<object> CycleCommandsThroughCrowdCommand { get; private set; }
        public DelegateCommand<object> TargetHoveredCharacterCommand { get; private set; }
        public DelegateCommand<object> DropDraggedCharacterCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetOnHoverCommand { get; private set; }
        public DelegateCommand<object> ToggleRelativePositioningCommand { get; private set; }
        public DelegateCommand ToggleGangModeCommand { get; private set; }
        public DelegateCommand ToggleSpawnOnClickCommand { get; private set; }
        public DelegateCommand ToggleCloneAndSpawnCommand { get; private set; }
        public DelegateCommand ToggleOverheadModeCommand { get; private set; }
        public DelegateCommand<object> TeleportTargetToCameraCommand { get; private set; }
        public DelegateCommand ActivateGangCommand { get; private set; }
        public DelegateCommand AttackTargetAndExecuteCrowdCommand { get; private set; }
        public DelegateCommand ActivateCrowdAsGangCommand { get; private set; }
        public DelegateCommand ResetDistanceCounterCommand { get; private set; }
        public DelegateCommand ScanAndFixMemoryCommand { get; private set; }

        #endregion

        #region Constructor

        public RosterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, ITargetObserver targetObserver, IDesktopKeyEventHandler keyEventHandler, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.targetObserver = targetObserver;
            this.desktopKeyEventHandler = keyEventHandler;

            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe(AddParticipants);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(DeleteParticipant);
            this.eventAggregator.GetEvent<CheckRosterConsistencyEvent>().Subscribe(CheckRosterConsistency);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(InitiateRosterCharacterAttack);
            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Subscribe(this.LaunchActiveAttack);
            this.eventAggregator.GetEvent<CancelActiveAttackEvent>().Subscribe(this.CancelActiveAttack);
            this.eventAggregator.GetEvent<AddOptionEvent>().Subscribe(this.HandleCharacterOptionAddition);
            this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Subscribe(this.PlayMovement);
            this.eventAggregator.GetEvent<SpawnToRosterEvent>().Subscribe(this.SpawnToRoster);

            timer_RespondToDesktop.AutoReset = false;
            timer_RespondToDesktop.Interval = 50;
            timer_RespondToDesktop.Elapsed += timer_RespondToDesktop_Elapsed;

            this.eventAggregator.GetEvent<ListenForTargetChanged>().Subscribe((obj) =>
            {
                this.targetObserver.TargetChanged -= TargetObserver_TargetChanged;
                this.targetObserver.TargetChanged += TargetObserver_TargetChanged;
            });
            this.eventAggregator.GetEvent<StopListeningForTargetChanged>().Subscribe((obj) =>
            {
                this.targetObserver.TargetChanged -= TargetObserver_TargetChanged;
            });

            this.TargetOnHover = true;
            this.UseRelativePositioning = true;
            
            InitializeCommands();

            InitializeMouseHandlers();

            InitializeDesktopContextMenuHandlers();

            InitializeDesktopKeyHanders();
        }

        #endregion

        #region Initialization

        private void InitializeMouseHandlers()
        {
            mouseHandler = new DesktopMouseEventHandler();
            mouseHandler.OnMouseLeftClick.Add(RespondToMouseClickBasedOnDesktopState);
            //mouseHandler.OnMouseLeftClickUp.Add(DropDraggedCharacter);
            mouseHandler.OnMouseRightClickUp.Add(DisplayCharacterPopupMenue);
            mouseHandler.OnMouseMove.Add(TargetHoveredCharacter);
            mouseHandler.OnMouseDoubleClick.Add(ActivateCharacterOrGang);
        }

        private void InitializeDesktopContextMenuHandlers()
        {
            desktopContextMenu = new DesktopContextMenu();
            desktopContextMenu.ActivateCharacterOptionMenuItemSelected += desktopContextMenu_ActivateCharacterOptionMenuItemSelected;
            desktopContextMenu.ActivateMenuItemSelected += desktopContextMenu_ActivateMenuItemSelected;
            desktopContextMenu.ActivateCrowdAsGangMenuItemSelected += desktopContextMenu_ActivateCrowdAsGangMenuItemSelected;
            desktopContextMenu.AttackContextMenuDisplayed += desktopContextMenu_AttackContextMenuDisplayed;
            desktopContextMenu.AttackTargetAndExecuteMenuItemSelected += desktopContextMenu_AttackTargetAndExecuteMenuItemSelected;
            desktopContextMenu.AttackTargetMenuItemSelected += desktopContextMenu_AttackTargetMenuItemSelected;
            desktopContextMenu.AttackTargetAndExecuteCrowdMenuItemSelected += desktopContextMenu_AttackTargetAndExecuteCrowdMenuItemSelected;
            desktopContextMenu.ClearFromDesktopMenuItemSelected += desktopContextMenu_ClearFromDesktopMenuItemSelected;
            desktopContextMenu.CloneAndLinkMenuItemSelected += desktopContextMenu_CloneAndLinkMenuItemSelected;
            desktopContextMenu.DefaultContextMenuDisplayed += desktopContextMenu_DefaultContextMenuDisplayed;
            desktopContextMenu.ManueverWithCameraMenuItemSelected += desktopContextMenu_ManueverWithCameraMenuItemSelected;
            desktopContextMenu.MoveCameraToTargetMenuItemSelected += desktopContextMenu_MoveCameraToTargetMenuItemSelected;
            desktopContextMenu.MoveTargetToCameraMenuItemSelected += desktopContextMenu_MoveTargetToCameraMenuItemSelected;
            desktopContextMenu.MoveTargetToCharacterMenuItemSelected += desktopContextMenu_MoveTargetToCharacterMenuItemSelected;
            desktopContextMenu.PlaceMenuItemSelected += desktopContextMenu_PlaceMenuItemSelected;
            desktopContextMenu.ResetOrientationMenuItemSelected += desktopContextMenu_ResetOrientationMenuItemSelected;
            desktopContextMenu.SavePositionMenuItemSelected += desktopContextMenu_SavePositionMenuItemSelected;
            desktopContextMenu.SpawnMenuItemSelected += desktopContextMenu_SpawnMenuItemSelected;
        }
        public DesktopKeyEventHandler keyHandler;
        private void InitializeDesktopKeyHanders()
        {
            this.desktopKeyEventHandler.AddKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        private void InitializeCommands()
        {
            this.SpawnCommand = new DelegateCommand<object>(delegate(object state) { this.Spawn(); });
            this.ClearFromDesktopCommand = new DelegateCommand<object>(delegate(object state) { this.ClearFromDesktop(); }, this.CanClearFromDesktop);
            this.ToggleTargetedCommand = new DelegateCommand<object>(delegate(object state) { this.ToggleTargeted(); }, this.CanToggleTargeted);
            this.SavePositionCommand = new DelegateCommand<object>(delegate(object state) { this.SavePosition(); }, this.CanSavePostion);
            this.PlaceCommand = new DelegateCommand<object>(delegate(object state) { this.Place(); }, this.CanPlace);
            this.TargetAndFollowCommand = new DelegateCommand<object>(this.TargetAndFollow, this.CanTargetAndFollow);
            this.MoveTargetToCameraCommand = new DelegateCommand<object>(delegate(object state) { this.MoveTargetToCamera(); }, this.CanMoveTargetToCamera);
            this.TeleportTargetToCameraCommand = new DelegateCommand<object>(this.TeleportTargetToCamera, this.CanTeleportTargetToCamera);
            this.MoveTargetToCharacterCommand = new DelegateCommand<object>(delegate(object state) { this.MoveTargetToCharacter(); }, this.CanMoveTargetToCharacter);
            this.MoveTargetToMouseLocationCommand = new DelegateCommand<object>(delegate(object state) { this.MoveTargetToMouseLocation(); }, this.CanMoveTargetToMouseLocation);
            this.ToggleManeuverWithCameraCommand = new DelegateCommand<object>(delegate(object state) { this.ToggleManeuverWithCamera(); }, this.CanToggleManeuverWithCamera);
            this.ToggleTargetOnHoverCommand = new DelegateCommand<object>(this.ToggleTargetOnHover);
            this.EditCharacterCommand = new DelegateCommand<object>(delegate(object state) { this.EditCharacter(); }, this.CanEditCharacter);
            this.ActivateCharacterCommand = new DelegateCommand<object>(delegate(object state) { this.ActivateCharacterOrGang(); }, this.CanActivateCharacterOrGang);
            this.ResetCharacterStateCommand = new DelegateCommand<object>(this.ResetCharacterState);
            this.AttackTargetCommand = new DelegateCommand<object>(this.TargetCharacterForAttack);
            this.AttackTargetAndExecuteCommand = new DelegateCommand<object>(this.TargetAndExecuteAttack);
            this.AttackTargetAndExecuteCrowdCommand = new DelegateCommand(this.TargetAndExecuteCrowd, this.CanTargetAndExecuteCrowd);
            this.ResetOrientationCommand = new DelegateCommand<object>(delegate(object state) { this.ResetOrientation(); }, this.CanResetOrientation);
            this.ResetDistanceCounterCommand = new DelegateCommand(this.ResetDistanceCounter, this.CanResetDistanceCounter);
            this.ToggleRelativePositioningCommand = new DelegateCommand<object>(this.ToggleRelativePositioning);
            this.ToggleGangModeCommand = new DelegateCommand(this.ToggleGangMode, this.CanToggleGangMode);
            this.ToggleSpawnOnClickCommand = new DelegateCommand(this.ToggleSpawnOnClick, this.CanToggleSpawnOnClick);
            this.ToggleCloneAndSpawnCommand = new DelegateCommand(this.ToggleCloneAndSpawn, this.CanToggleCloneAndSpawn);
            this.ToggleOverheadModeCommand = new DelegateCommand(this.ToggleOverheadMode, this.CanToggleOverheadMode);
            this.CycleCommandsThroughCrowdCommand = new DelegateCommand<object>(delegate(object state) { this.CycleCommandsThroughCrowd(); }, this.CanCycleCommandsThroughCrowd);
            this.ActivateGangCommand = new DelegateCommand(this.ToggleActivateGang, this.CanActivateGang);
            this.ActivateCrowdAsGangCommand = new DelegateCommand(this.ToggleActivateCrowdAsGang, this.CanActivateCrowdAsGang);
            this.ScanAndFixMemoryCommand = new DelegateCommand(this.ScanAndFixMemoryPointer);
        }

        #endregion
      
        #region Commands Consistency
        private void Commands_RaiseCanExecuteChanged()
        {
            this.ClearFromDesktopCommand.RaiseCanExecuteChanged();
            this.ToggleTargetedCommand.RaiseCanExecuteChanged();
            this.SavePositionCommand.RaiseCanExecuteChanged();
            this.PlaceCommand.RaiseCanExecuteChanged();
            this.TargetAndFollowCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCharacterCommand.RaiseCanExecuteChanged();
            this.MoveTargetToMouseLocationCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCameraCommand.RaiseCanExecuteChanged();
            this.ToggleManeuverWithCameraCommand.RaiseCanExecuteChanged();
            this.EditCharacterCommand.RaiseCanExecuteChanged();
            this.ActivateCharacterCommand.RaiseCanExecuteChanged();
            this.ResetOrientationCommand.RaiseCanExecuteChanged();
            this.ToggleSpawnOnClickCommand.RaiseCanExecuteChanged();
            this.ToggleCloneAndSpawnCommand.RaiseCanExecuteChanged();
            this.ToggleOverheadModeCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Get Characters to Operate on

        private List<Character> GetCharactersToOperateOn(bool considerGangMode = true)
        {
            List<Character> characters = new List<Characters.Character>();

            foreach(Character c in this.SelectedParticipants)
            {
                if (!characters.Contains(c))
                {
                    if(considerGangMode && (c as CrowdMemberModel).RosterCrowd.IsGangMode)
                    {
                        foreach (Character gangmember in Participants.Where(p => c != p && p.RosterCrowd.IsGangMode && (c as CrowdMemberModel).RosterCrowd == p.RosterCrowd))
                            characters.Add(gangmember);
                    }
                    characters.Add(c);
                }
            }

            return characters.Distinct().ToList();
        }

        #endregion

        #region Get Last Selected Character

        private Character GetLastSelectedCharacter()
        {
            int highestIndex = 0;
            foreach(Character c in this.SelectedParticipants)
            {
                int currentIndex = this.Participants.IndexOf(c as CrowdMemberModel);
                if (currentIndex > highestIndex)
                    highestIndex = currentIndex;
            }
            return this.Participants[highestIndex] as Character;
        }

        #endregion

        #region Add to Selections

        public void AddCrowdMembersToSelection(string crowdName)
        {
            foreach(Character c in this.Participants.Where(p => p.RosterCrowd != null && p.RosterCrowd.Name == crowdName))
            {
                this.SelectedParticipants.Add(c);
            }
        }

        #endregion

        #region Import Roster Member and Check Consistency
        public void RaiseEventToImportRosterMember()
        {
            this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(null);
        }
        private void CheckRosterConsistency(IEnumerable<CrowdMemberModel> members)
        {
            foreach (CrowdMemberModel member in members)
            {
                if (!Participants.Contains(member))
                {
                    InitializeAttackEventHandlers(member);
                    CheckIfCharacterExistsInGame(member);
                }
            }
            Action d = delegate ()
            {
                foreach(var m in members)
                {
                    if (!Participants.Contains(m))
                        Participants.Add(m);
                }
                this.eventAggregator.GetEvent<RosterSyncCompletedEvent>().Publish(null);
                Participants.Sort(ListSortDirection.Ascending, new RosterCrowdMemberModelComparer());
                OnRosterMemberAdded(null, null);
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        #endregion

        #region Character Targeting and Syncing with Game

        public ICrowdMemberModel GetCurrentTarget()
        {
            MemoryElement target = new MemoryElement();
            return this.Participants.FirstOrDefault((x) =>
            {
                return (x as CrowdMemberModel).Label == target.Label;
            });
        }
        private void synchSelectionWithGame()
        {
            List<CrowdMemberModel> unselected = _oldSelection.Except(SelectedParticipants.Cast<CrowdMemberModel>()).ToList();
            unselected.ForEach(
                (member) =>
                {
                    if (!member.HasBeenSpawned)
                        return;
                    if (!(this.IsPlayingAttack && this.AttackingCharacters.Count > 0 && this.AttackingCharacters.Contains(member)))
                        member.Deactivate();
                    _oldSelection.Remove(member);
                });
            List<CrowdMemberModel> selected = SelectedParticipants.Cast<CrowdMemberModel>().Except(_oldSelection).ToList();
            if (selected.Count > 1)
                selected.ForEach(
                    (member) =>
                    {
                        if (!member.HasBeenSpawned)
                            return;
                        //if (!(this.IsPlayingAttack && this.AttackingCharacters.Count > 0 && this.AttackingCharacters.Contains(member))) // Don't make the attacking character blue
                        //    member.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                        _oldSelection.Add(member);
                    });
        }
        private void TargetObserver_TargetChanged(object sender, EventArgs e)
        {
            try
            {
                uint currentTargetPointer = targetObserver.CurrentTargetPointer;
                CrowdMemberModel currentTarget = (CrowdMemberModel)Participants.DefaultIfEmpty(null).Where(
                    (p) =>
                    {
                        Character c = p as Character;
                        return c != null && c.gamePlayer != null && c.gamePlayer.Pointer == currentTargetPointer;
                    }).FirstOrDefault();
                if(currentTarget != null)
                    AddDesktopTargetToRosterSelection(currentTarget);
            }
            catch (TaskCanceledException ex)
            {

            }
        }
        public void AddDesktopTargetToRosterSelection(Character currentTarget)
        {
            Dispatcher.Invoke(() =>
            {
                if (SelectedParticipants == null)
                    SelectedParticipants = new ObservableCollection<object>() as IList;
                if (currentTarget == null)
                    return;
                if (Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    if (SelectedParticipants != null && !this.stopSyncingWithDesktop)
                        SelectedParticipants.Clear();
                    //else if (stopSyncingWithDesktop)
                        //stopSyncingWithDesktop = false;
                }
                else
                {
                    //stopSyncingWithDesktop = true;
                }

                if (!SelectedParticipants.Contains(currentTarget))
                {
                    if (!stopSyncingWithDesktop)
                    {
                        SelectedParticipants.Add(currentTarget);
                        OnPropertyChanged("SelectedParticipants");
                    }
                    else
                    {
                        this.stopSyncingWithDesktop = false;
                    }
                }
                if(isMultiSelecting && stopSyncingWithDesktop)
                {
                    isMultiSelecting = false;
                    stopSyncingWithDesktop = false;
                }
            });
        }
        #endregion

        #region Target Hovered Character

        public Character GetHoveredCharacter()
        {
            MouseElement hoveredElement = new MouseElement();
            if (hoveredElement.HoveredInfo != "")
            {
                return (Character)this.Participants.FirstOrDefault(p => p.Name == hoveredElement.Name);
            }
            return null;
        }

        public Character GetHoveredOrTargetedCharacter(MemoryElement targetedBeforeMouseCLick)
        {
            Character hovered = GetHoveredCharacter();
            if (hovered != null)
            {
                return hovered;
            }
            else
            {
                System.Threading.Thread.Sleep(200);
                MemoryElement target = new MemoryElement();
                if (target.Label != "" && (targetedBeforeMouseCLick == null || targetedBeforeMouseCLick.Label != target.Label))
                {
                    return (CrowdMemberModel)GetCurrentTarget();
                }

            }
            return null;
        }

        public void TargetHoveredCharacter()
        {
            Character hoveredCharacter = GetHoveredCharacter();
            if (hoveredCharacter != null)
            {
                if (this.TargetOnHover)
                {
                    if (lastTargetedCharacter == null || hoveredCharacter.Label != lastTargetedCharacter.Label)
                    {
                        hoveredCharacter.Target();
                    }
                    lastTargetedCharacter = hoveredCharacter; 
                }
                else if(this.CurrentDistanceCountingCharacter != null)
                {
                    this.CurrentDistanceCountingCharacter.UpdateDistanceCount(hoveredCharacter.CurrentPositionVector);
                }
            }
        }

        #endregion

        #region Drag Drop

        private void ContinueDraggingCharacter()
        {
            MemoryElement targetedBeforeMouseCLick = new MemoryElement();
            CrowdMemberModel hoveredCharacter = (CrowdMemberModel)GetHoveredCharacter();
            if (hoveredCharacter != null)
            {
                this.currentDraggingCharacter = hoveredCharacter;
                this.isCharacterDragDropInProgress = true;
                lastDesktopMouseDownTime = DateTime.UtcNow;
                this.lastTargetedCharacter = hoveredCharacter;
                if (targetedBeforeMouseCLick.Label != (hoveredCharacter as Character).Label)
                {
                    previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == targetedBeforeMouseCLick.Label) as Character;
                }
            }
            else
                this.lastTargetedCharacter = null;
            return;
        }
        private void DropDraggedCharacter()
        {
            if (currentDraggingCharacter != null && lastDesktopMouseDownTime != DateTime.MinValue && isCharacterDragDropInProgress)
            {
                System.Threading.Thread.Sleep(500);

                Vector3 mouseUpPosition = new MouseElement().Position;
                if (!IsPlayingAttack)
                {
                    if (!currentDraggingCharacter.HasBeenSpawned)
                    {
                        currentDraggingCharacter.Spawn();
                    }
                    if (Vector3.Distance(currentDraggingCharacter.CurrentPositionVector, mouseUpPosition) > 5)
                    {
                        currentDraggingCharacter.UnFollow();
                        if (this.IsGangActive && currentDraggingCharacter.IsActive)
                        {
                            Vector3 startingPosition = currentDraggingCharacter.CurrentPositionVector;
                            Vector3 nextReferenceVector = Vector3.Zero;
                            List<Vector3> usedUpPositions = new List<Vector3>();
                            foreach (Character c in this.Participants.Where(p => (p as Character).IsActive))
                            {

                                if (this.UseRelativePositioning)
                                    c.MoveToLocationWithRelativePositioning(mouseUpPosition, currentDraggingCharacter, startingPosition);
                                else
                                    c.MoveToLocationWithOptimalPositioning(mouseUpPosition, currentDraggingCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                            }
                        }
                        else if (this.IsGangModeActive && (currentDraggingCharacter as CrowdMemberModel).RosterCrowd.IsGangMode)
                        {
                            Vector3 startingPosition = currentDraggingCharacter.CurrentPositionVector;
                            Vector3 nextReferenceVector = Vector3.Zero;
                            List<Vector3> usedUpPositions = new List<Vector3>();
                            foreach (Character c in this.Participants.Where(p => p.RosterCrowd.IsGangMode))
                            {

                                if (this.UseRelativePositioning)
                                    c.MoveToLocationWithRelativePositioning(mouseUpPosition, currentDraggingCharacter, startingPosition);
                                else
                                    c.MoveToLocationWithOptimalPositioning(mouseUpPosition, currentDraggingCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                            }
                        }
                        else
                            currentDraggingCharacter.MoveToLocation(mouseUpPosition);
                    }
                }
            }
            currentDraggingCharacter = null;
            lastDesktopMouseDownTime = DateTime.MinValue;
            isCharacterDragDropInProgress = false;
        }

        #endregion

        #region Display Popup Menu
        int numRetryPopupMenu = 3;

        private void DisplayCharacterPopupMenue()
        {
            Action d = delegate ()
            {
                CrowdMemberModel character = (CrowdMemberModel)GetCurrentTarget();
                if (AttackingCharacters.Contains(character) && numRetryPopupMenu > 0)
                {
                    numRetryPopupMenu--;
                    DisplayCharacterPopupMenue();
                }
                else
                {
                    desktopContextMenu.GenerateAndDisplay(character, AttackingCharacters.Select(ac => ac.Name).ToList(), IsPlayingAttack);
                    numRetryPopupMenu = 3;
                }
                Vector3 mousePosition = new MouseElement().Position;
                this.CurrentDistanceCountingCharacter.UpdateDistanceCount(mousePosition);
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 500);
            adex.ExecuteAsyncDelegate();
        }

        #endregion

        #region Move Character to Desktop Position Clicked

        public void MoveCharacterToDesktopPositionClicked()
        {
            Vector3 mouseDirection = new MouseElement().Position;
            Character activeMovementCharacter = Helper.GlobalVariables_CharacterMovement.Character;
            Character target = this.Participants.FirstOrDefault(p => p.Name == activeMovementCharacter.Name) as Character;
            if (this.IsGangActive && target.IsActive)
            {
                Character gangLeader = Participants.FirstOrDefault(p => (p as Character).IsGangLeader) as Character;
                Vector3 startingPosition = gangLeader.CurrentPositionVector;
                Vector3 nextReferenceVector = Vector3.Zero;
                List<Vector3> usedUpPositions = new List<Vector3>();
                foreach (Character c in this.Participants.Where(p => (p as Character).IsActive))
                {
                    if (this.UseRelativePositioning)
                        c.MoveToLocationWithRelativePositioning(mouseDirection, gangLeader, startingPosition);
                    else
                        c.MoveToLocationWithOptimalPositioning(mouseDirection, gangLeader, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                }
            }
            else if (this.IsGangModeActive && (target as CrowdMemberModel).RosterCrowd.IsGangMode)
            {
                Vector3 startingPosition = target.CurrentPositionVector;
                Vector3 nextReferenceVector = Vector3.Zero;
                List<Vector3> usedUpPositions = new List<Vector3>();
                foreach (Character c in this.Participants.Where(p => p.RosterCrowd.IsGangMode))
                {
                    if (this.UseRelativePositioning)
                        c.MoveToLocationWithRelativePositioning(mouseDirection, target, startingPosition);
                    else
                        c.MoveToLocationWithOptimalPositioning(mouseDirection, target, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                }
            }
            else if (target != null)
                target.MoveToLocation(mouseDirection);
        }

        #endregion

        #region Respond to Desktop Click
        public void RespondToMouseClickBasedOnDesktopState()
        {
            currentTarget = this.SelectedParticipants != null && this.SelectedParticipants.Count > 0 ? this.SelectedParticipants[0] as Character: null;
            timer_RespondToDesktop.Start(); // We need this timer to avoid firing click events when desktop is no longer in focus, as click fires before focus lost. This also fixes double attack play bug.
        }
        void timer_RespondToDesktop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_RespondToDesktop.Stop();
            Action d = delegate ()
            {
                if (desktopContextMenu.IsDisplayed == false && mouseHandler.IsDesktopActive)
                {
                    if (IsPlayingAttack == false)
                    {
                        if(Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
                        {
                            this.moveAndAttackModeOn = true;
                            Action d1 = delegate ()
                            {
                                this.PlayDefaultAbility();
                            };
                            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d1, 500);
                            adex.ExecuteAsyncDelegate();
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            Action d1 = delegate ()
                            {
                                this.PlayDefaultAbility();
                            };
                            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d1, 500);
                            adex.ExecuteAsyncDelegate();
                        }
                        else if(Keyboard.Modifiers == ModifierKeys.Alt)
                        {
                            Action d1 = delegate ()
                            {
                                this.ActivateDefaultMovementToActivate();
                            };
                            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d1, 500);
                            adex.ExecuteAsyncDelegate();
                        }
                        else if (CharacterIsMoving == true)
                        {
                            MoveCharacterToDesktopPositionClicked();
                        }
                        else if (this.SpawnOnClick)
                        {
                            Vector3 mousePosition = new MouseElement().Position;
                            this.lastMouseClickLocation = mousePosition;
                            if (this.CloneAndSpawn)
                            {
                                InitiateCloneAndSpawn();
                            }
                            else
                                this.Spawn(mousePosition);
                        }
                        else
                            ContinueDraggingCharacter();
                    } 
                    else
                    {
                        PlayAttackCycle();
                    }
                }
                else
                    desktopContextMenu.IsDisplayed = false;
            };
            Dispatcher.Invoke(d);
        }

        #endregion

        #region Add Participants
        private void AddParticipants(IEnumerable<CrowdMemberModel> crowdMembers)
        {
            foreach (var crowdMember in crowdMembers)
            {
                Participants.Add(crowdMember);
                InitializeAttackEventHandlers(crowdMember);
                //CheckIfCharacterExistsInGame(crowdMember);
            }
            // preserve selections
            List<Character> savedSelections = new List<Character>();
            if (SelectedParticipants.Count > 0)
            {
                foreach (Character c in SelectedParticipants)
                    savedSelections.Add(c);
            }
            Participants.Sort(ListSortDirection.Ascending, new RosterCrowdMemberModelComparer());
            if (savedSelections != null)
                savedSelections.ForEach(ss => SelectedParticipants.Add(ss));
            OnRosterMemberAdded(null, null);
        }

        private void CheckIfCharacterExistsInGame(Character crowdMember)
        {
            this.eventAggregator.GetEvent<StopListeningForTargetChanged>().Publish(null);
            MemoryElement oldTargeted = new MemoryElement();
            crowdMember.Target();
            MemoryElement currentTargeted = new MemoryElement();
            if (currentTargeted.Label == crowdMember.Label)
            {
                crowdMember.SetAsSpawned();
                if(crowdMember.ActiveIdentity.Type == IdentityType.Model)
                {
                    if (crowdMember.GhostShadow == null)
                        crowdMember.CreateGhostShadow();
                    CheckIfCharacterExistsInGame(crowdMember.GhostShadow);
                    if(!crowdMember.GhostShadow.HasBeenSpawned)
                        crowdMember.SuperImposeGhost();
                }
            }
            try
            {
                oldTargeted.Target();
            }
            catch { }

            crowdMember.IsSyncedWithGame = true;
            this.eventAggregator.GetEvent<ListenForTargetChanged>().Publish(null);
        }
        #endregion

        #region Delete Participant
        private void DeleteParticipant(ICrowdMemberModel crowdMember)
        {
            if (this.SelectedParticipants == null)
                this.SelectedParticipants = new List<CrowdMemberModel>();
            this.SelectedParticipants.Clear();
            if (crowdMember is CrowdMemberModel)
            {
                this.SelectedParticipants.Add(crowdMember);
            }
            else if (crowdMember is CrowdModel)
            {
                var participants = this.Participants.Where(p => p.RosterCrowd.Name == crowdMember.Name);
                foreach (var participant in participants)
                    this.SelectedParticipants.Add(participant);
            }
            this.ClearFromDesktop();
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        #endregion

        #region Spawn

        public void Spawn(List<Character> charactersToOperateOn = null)
        {
            if (CloneAndSpawn)
            {
                InitiateCloneAndSpawn();
                return;
            }
            if (charactersToOperateOn == null)
                charactersToOperateOn = GetCharactersToOperateOn();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                member.Spawn(false);
            }
            var kg = new KeyBindsGenerator();
            kg.CompleteEvent();

            Character mainCharacter = charactersToOperateOn[0] as Character;
            Vector3 nextReferenceVector = Vector3.Zero;
            List<Vector3> usedUpPositions = new List<Vector3>();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                member.Target();
                member.WaitUntilTargetIsRegistered();
                member.PlaceOptimallyAround(mainCharacter, ref nextReferenceVector, ref usedUpPositions);
                if (member.ActiveIdentity.Type == IdentityType.Model)
                    member.SuperImposeGhost();
                member.UpdateDistanceCount();
            }
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }

        public void Spawn(Vector3 locationVector, List<Character> charactersToOperateOn = null)
        {
            if (charactersToOperateOn == null)
                charactersToOperateOn = GetCharactersToOperateOn();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                member.Spawn(false);
            }
            var kg = new KeyBindsGenerator();
            kg.CompleteEvent();
            
            Vector3 nextReferenceVector = Vector3.Zero;
            List<Vector3> usedUpPositions = new List<Vector3>();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                member.Target();
                member.WaitUntilTargetIsRegistered();

                if(charactersToOperateOn.Count > 1)
                    member.PlaceOptimallyAround(locationVector, ref nextReferenceVector, ref usedUpPositions);
                else
                    member.CurrentPositionVector = locationVector;
                if (member.ActiveIdentity.Type == IdentityType.Model)
                    member.SuperImposeGhost();
                member.UpdateDistanceCount();
            }
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }

        private void InitiateCloneAndSpawn()
        {
            List<Character> characters = GetCharactersToOperateOn();
            object cloneParam = characters;
            if (characters.Count > 0)
            {
                CrowdMemberModel first = characters[0] as CrowdMemberModel;
                if (!participants.Any(p => p.RosterCrowd == first.RosterCrowd && !characters.Contains(p as Character)))
                {
                    // entire roster crowd needs to clone
                    cloneParam = first.RosterCrowd;
                }
                this.eventAggregator.GetEvent<CloneAndSpawnCrowdMemberEvent>().Publish(cloneParam);
            }
        }

        private void SpawnToRoster(IEnumerable<CrowdMemberModel> rosterMembers)
        {
            AddParticipants(rosterMembers);
            //this.SelectedParticipants.Clear();
            List<Character> charactersToSpawn = new List<Character>();
            foreach (var member in rosterMembers)
            {
                charactersToSpawn.Add(member);
                //this.SelectedParticipants.Add(member);
            }
            if (this.lastMouseClickLocation == Vector3.Zero)
                lastMouseClickLocation = new Camera().GetPositionVector();
            this.Spawn(this.lastMouseClickLocation, charactersToSpawn);
            lastMouseClickLocation = Vector3.Zero;
        }
    
        #endregion

        #region Clear from Desktop
        private bool CanClearFromDesktop(object state)
        {
            if (SelectedParticipants != null && SelectedParticipants.Count > 0 && !this.IsPlayingAttack)
            {
                return true;
            }
            return false;
        }
        public void ClearFromDesktop()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                member.ClearFromDesktop(false);
            }
            new KeyBindsGenerator().CompleteEvent();
            SelectNextCharacterInCrowdCycle();
            if(charactersToOperateOn.Any(c => c.IsActive))
            {
                this.DeactivateCharacter();
                this.DeactivateGang();
            }
            foreach (CrowdMemberModel participant in charactersToOperateOn)
            {
                Participants.Remove(participant);
                participant.RosterCrowd = null;
            }
            Commands_RaiseCanExecuteChanged();
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }
        #endregion

        #region Save Positon

        private bool CanSavePostion(object state)
        {
            bool canSavePosition = false;
            if (this.SelectedParticipants != null)
            {
                foreach (var c in this.SelectedParticipants)
                {
                    var character = c as Character;
                    if (character != null && character.HasBeenSpawned)
                    {
                        canSavePosition = true;
                        break;
                    }
                }
            }
            return canSavePosition;
        }
        public void SavePosition()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                member.SavePosition();
            }
            SelectNextCharacterInCrowdCycle();
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            this.PlaceCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Place
        private bool CanPlace(object state)
        {
            bool canPlace = false;
            if (this.SelectedParticipants != null)
            {
                foreach (var c in this.SelectedParticipants)
                {
                    var crowdMemberModel = c as CrowdMemberModel;
                    if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name == Constants.ALL_CHARACTER_CROWD_NAME && crowdMemberModel.SavedPosition != null)
                    {
                        canPlace = true;
                        break;
                    }
                    else if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                    {
                        CrowdModel rosterCrowdModel = crowdMemberModel.RosterCrowd as CrowdModel;
                        if (rosterCrowdModel.SavedPositions.ContainsKey(crowdMemberModel.Name))
                        {
                            canPlace = true;
                            break;
                        }
                    }
                }
            }
            return canPlace;                                                                                                                                                                        
        }
        public void Place()
        {
            var charactersToOperateOn = GetCharactersToOperateOn();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                //member.Place();
                
                if(!member.HasBeenSpawned)
                    member.Spawn(false);
            }
            var kbg = new KeyBindsGenerator();
            kbg.CompleteEvent();
            foreach(CrowdMember member in charactersToOperateOn)
            {
                member.Target();
                if (member.RosterCrowd != null)
                {
                    member.RosterCrowd.Place(member);
                }
                member.SuperImposeGhost();
            }
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region Toggle Targeted

        private bool CanToggleTargeted(object state)
        {
            bool canToggleTargeted = false;
            if (IsSingleSpawnedCharacterSelected)
            {
                canToggleTargeted = true;
            }
            return canToggleTargeted;
        }

        public void ToggleTargeted()
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                member.ToggleTargeted();
            }
            SelectNextCharacterInCrowdCycle();
        }


        #endregion

        #region Toggle Target on Hover

        private void ToggleTargetOnHover(object state)
        {
            this.TargetOnHover = !this.TargetOnHover;
        }

        #endregion

        #region Toggle Relative Positioning

        private void ToggleRelativePositioning(object state)
        {
            this.UseRelativePositioning = !this.UseRelativePositioning;
        }

        #endregion

        #region Toggle Gang Mode

        private bool CanToggleGangMode()
        {
            return this.SelectedParticipants != null && this.SelectedParticipants.Count > 0;
        }

        private void ToggleGangMode()
        {
            this.IsGangModeActive = !this.IsGangModeActive;
            if(this.IsGangModeActive && this.ActiveCharacter != null)
            {
                CrowdModel gangCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd as CrowdModel;
                if((this.ActiveCharacter as CrowdMemberModel).RosterCrowd == gangCrowd)
                {
                    ToggleActivateCrowdAsGang();
                }
            }
        }

        #endregion

        #region Target And/Or Follow
        private bool CanTargetAndFollow(object state)
        {
            return SelectedParticipants.Count > 0 && (SelectedParticipants[0] as Character).HasBeenSpawned;
        }
        public void TargetAndFollow(object obj)
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            var member = charactersToOperateOn[0];
            if (!member.IsSyncedWithGame)
                CheckIfCharacterExistsInGame(member);
            member.TargetAndFollow();
            SelectNextCharacterInCrowdCycle();
        }

        public void TargetOrFollow()
        {
            if (SelectedParticipants != null && SelectedParticipants.Count == 1)
            {
                CrowdMemberModel member = SelectedParticipants[0] as CrowdMemberModel;
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                if (member.IsTargeted)
                {
                    if (this.IsPlayingAttack)
                        member.Target();
                    else if (this.isCharacterReset) // character has been selected to reset state, so just target
                    {
                        this.isCharacterReset = false; // reset this flag
                        member.Target();
                    }
                    //else
                    //    member.TargetAndFollow();
                }
                else
                    member.ToggleTargeted();

                //if (this.isPlayingAttack)
                //{
                //    if (member.Name != this.AttackingCharacter.Name && member.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && member.Name != Constants.DEFAULT_CHARACTER_NAME)
                //    {
                //        if (!isPlayingAreaEffect)
                //        { 
                //            if (PopupService.IsOpen("ActiveAttackView") == false)
                //            {
                //                if (this.targetCharacters.FirstOrDefault(tc => tc.Name == member.Name) == null)
                //                    this.targetCharacters.Add(member);
                //                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                //                attackConfig.AttackMode = AttackMode.Defend;
                //                attackConfig.AttackEffectOption = AttackEffectOption.None;
                //                member.ActiveAttackConfiguration = attackConfig;
                //                member.ActiveAttackConfiguration.IsCenterTarget = true;
                //                this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
                //            } 
                //        }
                //    }
                //}
            }
        }
        public void TargetAndFollow()
        {
            if (this.CanTargetAndFollow(null))
            {
                this.TargetAndFollow(null);
            }
        }

        public void TargetLastSelectedCharacter(IList oldSelectedParticipants)
        {
            ICrowdMemberModel lastSelected = null;
            if (oldSelectedParticipants != null && oldSelectedParticipants.Count < SelectedParticipants.Count)
            {
                foreach (var sp in SelectedParticipants)
                {
                    if (!oldSelectedParticipants.Contains(sp))
                    {
                        lastSelected = (ICrowdMemberModel)sp;
                        break;
                    }
                }
            }
            else {
                if (SelectedParticipants.Count > 0)
                    lastSelected = SelectedParticipants[0] as ICrowdMemberModel;
            }
            if (lastSelected != null)
                (lastSelected as Character).Target();
        }

        #endregion

        #region Move Target to Camera

        private bool CanMoveTargetToCamera(object arg)
        {
            bool canMoveTargetToCamera = true;
            if (this.SelectedParticipants == null)
            {
                canMoveTargetToCamera = false;
                return canMoveTargetToCamera;
            }
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                if (!member.HasBeenSpawned)
                {
                    canMoveTargetToCamera = false;
                    break;
                }
            }
            return canMoveTargetToCamera;
        }

        public void MoveTargetToCamera()
        {
            // Adjust destination based on relative position
            // need to choose a character that is "center" character. Should be the one that's closest to destination....
            float closestDistance;
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            Character closestCharacter = GetClosestCharacter(out closestDistance);
            Vector3 startingPosition = closestCharacter.CurrentPositionVector;
            Vector3 nextReferenceVector = Vector3.Zero;
            List<Vector3> usedUpPositions = new List<Vector3>();
            foreach (Character c in charactersToOperateOn)
            {             
                if (this.UseRelativePositioning)
                    c.MoveToLocationWithRelativePositioning(new Camera().GetPositionVector(), closestCharacter, startingPosition);
                else
                    c.MoveToLocationWithOptimalPositioning(new Camera().GetPositionVector(), closestCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
            }

            SelectNextCharacterInCrowdCycle();
        }

        private Character GetClosestCharacter(out float distance)
        {
            var cameraPositionVector = new Camera().GetPositionVector();
            distance = Int32.MaxValue;
            Character closestCharacter = null;
            foreach(Character c in this.SelectedParticipants)
            {
                var distanceFromCamera = Vector3.Distance(c.CurrentPositionVector, cameraPositionVector);
                if(distanceFromCamera < distance)
                {
                    distance = distanceFromCamera;
                    closestCharacter = c;
                }
            }
            return closestCharacter;
        }

        #endregion

        #region Teleport Target to Camera

        private bool CanTeleportTargetToCamera(object state)
        {
            return CanMoveTargetToCamera(state);
        }

        private void TeleportTargetToCamera(object state)
        {
            float closestDistance;
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            Character closestCharacter = GetClosestCharacter(out closestDistance);
            Vector3 startingPosition = closestCharacter.CurrentPositionVector;
            Vector3 nextReferenceVector = Vector3.Zero;
            List<Vector3> usedUpPositions = new List<Vector3>();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                if (this.UseRelativePositioning)
                    member.TeleportToCameraWithRelativePositioning(closestCharacter, startingPosition);
                else
                    member.TeleportToCameraWithOptimalPositioning(closestCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
            }

            SelectNextCharacterInCrowdCycle();
        }

        #endregion

        #region Move Target to Character

        private bool CanMoveTargetToCharacter(object arg)
        {
            bool canMoveTargetToCharacter = true;
            if (this.SelectedParticipants == null)
            {
                canMoveTargetToCharacter = false;
            }
            else
            {
                foreach (CrowdMemberModel member in SelectedParticipants)
                {
                    if (!member.IsSyncedWithGame)
                        CheckIfCharacterExistsInGame(member);
                    if (!member.HasBeenSpawned)
                    {
                        canMoveTargetToCharacter = false;
                        break;
                    }
                }
            }

            return canMoveTargetToCharacter;
        }

        public void MoveTargetToCharacter()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach (Character c in charactersToOperateOn)
                this.targetCharactersForMove.Add(c);
            targetObserver.TargetChanged -= RosterTargetUpdated;
            targetObserver.TargetChanged += RosterTargetUpdated;
        }

        #endregion

        #region Move Target to Mouse Location

        private bool CanMoveTargetToMouseLocation(object arg)
        {
            bool canMoveTargetToMouseLocation = true;
            if (this.SelectedParticipants == null)
            {
                canMoveTargetToMouseLocation = false;
            }
            else
            {
                foreach (CrowdMemberModel member in SelectedParticipants)
                {
                    if (!member.IsSyncedWithGame)
                        CheckIfCharacterExistsInGame(member);
                    if (!member.HasBeenSpawned)
                    {
                        canMoveTargetToMouseLocation = false;
                        break;
                    }
                }
            }
            return canMoveTargetToMouseLocation;
        }

        public void MoveTargetToMouseLocation()
        {
            //this.isMoveToMouseLocationEnabled = !this.isMoveToMouseLocationEnabled;
        }

        #endregion

        #region Reset Orientation

        private bool CanResetOrientation(object arg)
        {
            bool canResetOrientation = true;
            if (this.SelectedParticipants == null)
            {
                canResetOrientation = false;
                return canResetOrientation;
            }
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                if (!member.HasBeenSpawned)
                {
                    canResetOrientation = false;
                    break;
                }
            }
            return canResetOrientation;
        }

        public void ResetOrientation()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach(var character in charactersToOperateOn)
            {
                character.ResetOrientation();
            }
            
            SelectNextCharacterInCrowdCycle();
        }

        #endregion

        #region Activate Default Movement

        public void ActivateDefaultMovementToActivate()
        {
            Character character = ((Character)SelectedParticipants[0]);
            if (this.ActiveCharacter == null)
            {
                character = SelectedParticipants[0] as Character;
            }
            else
                character = this.ActiveCharacter as Character;

            Vector3 facing = new Vector3();
            if (SelectedParticipants.Count > 1)
            {
                facing = character.CurrentFacingVector;
            }

            if (character.ActiveMovement == null || character.ActiveMovement.IsActive == false)
            {
                //foreach (CrowdMemberModel member in SelectedParticipants)
                //{
                //    character = (Character)member;
                //    character.ActiveMovement = character.DefaultMovementToActivate;
                //    if (SelectedParticipants.Count > 1)
                //    {
                //        character.CurrentFacingVector = facing;
                //    }
                //    if (character.ActiveMovement != null)
                //    {
                //        if (!character.ActiveMovement.IsActive)
                //            this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(character.ActiveMovement);
                //    }
                //}
                this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(character.DefaultMovementToActivate);
            }
            else
            {
                //foreach (CrowdMemberModel member in SelectedParticipants)
                //{
                //    character = (Character)member;
                //    character.ActiveMovement = character.DefaultMovementToActivate;
                //    if (character.ActiveMovement != null)
                //        this.eventAggregator.GetEvent<StopMovementEvent>().Publish(character.ActiveMovement);
                //}
                if (character.ActiveMovement != null)
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(character.ActiveMovement);
            }
        }


        #endregion

        #region ToggleManeuverWithCamera
        private bool CanToggleManeuverWithCamera(object arg)
        {
            bool canManeuverWithCamera = false;
            if (!IsPlayingAttack && this.SelectedParticipants != null && SelectedParticipants.Count == 1 && ((SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned || (SelectedParticipants[0] as CrowdMemberModel).ManeuveringWithCamera))
            {
                canManeuverWithCamera = true;
            }
            return canManeuverWithCamera;
        }
        private void ToggleManeuverWithCamera(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                member.ToggleManueveringWithCamera();
            }
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }


        public void ToggleManeuverWithCamera()
        {
            if (this.CanToggleManeuverWithCamera(null))
            {
                this.ToggleManeuverWithCamera(null);
            }
        }

        #endregion

        #region Cycle Commands Through Crowd

        private bool CanCycleCommandsThroughCrowd(object state)
        {
            return true;
        }
        private void CycleCommandsThroughCrowd()
        {
            this.IsCyclingCommandsThroughCrowd = !this.IsCyclingCommandsThroughCrowd;
        }
        private void SelectNextCharacterInCrowdCycle()
        {
            if (this.IsCyclingCommandsThroughCrowd)
            {
                this.stopSyncingWithDesktop = true;
                List<ICrowdMemberModel> cNext = new List<ICrowdMemberModel>();
                ICrowdMemberModel cCurrent = this.GetLastSelectedCharacter() as ICrowdMemberModel;
                if (!IsOperatingCrowd)
                {
                    var index = this.Participants.IndexOf(cCurrent as ICrowdMemberModel);

                    if (index + 1 == this.Participants.Count)
                    {
                        cNext.Add(this.Participants.FirstOrDefault() as ICrowdMemberModel);
                    }
                    else
                    {
                        cNext.Add(this.Participants[index + 1] as ICrowdMemberModel);
                    }
                }
                else
                {
                    CrowdModel nextCrowd = GetNextCrowd();
                    if(nextCrowd != null)
                    {
                        foreach (Character c in this.Participants.Where(p => p.RosterCrowd == nextCrowd))
                            cNext.Add(c as ICrowdMemberModel);
                    }
                }

                if (cNext.Count > 0 && !cNext.Any(c => c == cCurrent))
                {
                    SelectedParticipants.Clear();
                    foreach(var c in cNext)
                        SelectedParticipants.Add(c);
                    OnPropertyChanged("SelectedParticipants");
                }
            }
        }

        private CrowdModel GetNextCrowd()
        {
            var crowd = this.SelectedCrowd;
            var last = this.GetLastSelectedCharacter();
            int currIndex = Participants.IndexOf(last as CrowdMemberModel);
            if(crowd != null)
            {
                var nextChar = Participants.FirstOrDefault(p => p.RosterCrowd != crowd && Participants.IndexOf(p) > currIndex);
                if (nextChar != null)
                    return nextChar.RosterCrowd as CrowdModel;
                else
                {
                    var firstPart = Participants.First();
                    if (firstPart.RosterCrowd != crowd)
                        return firstPart.RosterCrowd as CrowdModel;
                }
            }
            return null;
        }

        #endregion

        #region Distance Counter

        private bool CanResetDistanceCounter()
        {
            return SelectedParticipants != null && SelectedParticipants.Count > 0 && this.CurrentDistanceCountingCharacter != null;
        }

        private void ResetDistanceCounter()
        {
            if(this.CurrentDistanceCountingCharacter != null)
            {
                this.CurrentDistanceCountingCharacter.CurrentDistanceCount = 0f;
                this.CurrentDistanceCountingCharacter.CurrentStartingPositionVectorForDistanceCounting = Vector3.Zero;
            }
        }

        private void RestartDistanceCounting()
        {
            if (SelectedParticipants != null && SelectedParticipants.Count > 0)
            {
                if (this.IsPlayingAttack)
                {
                    if(this.AttackingCharacters.Count > 1 && this.IsGangActive)
                    {
                        this.CurrentDistanceCountingCharacter = Participants.First(p => (p as Character).IsGangLeader) as Character;
                    }
                    else
                    {
                        this.CurrentDistanceCountingCharacter = this.AttackingCharacters.First();  
                    }
                    if(SelectedParticipants.Count > 0)
                    {
                        foreach(Character c in SelectedParticipants)
                        {
                            if (!AttackingCharacters.Contains(c))
                            {
                                this.CurrentDistanceCountingCharacter.UpdateDistanceCount(c.CurrentPositionVector);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ResetDistanceCounter();
                    if (SelectedParticipants.Count > 1 && IsGangActive)
                        this.CurrentDistanceCountingCharacter = Participants.First(p => (p as Character).IsGangLeader) as Character;
                    else
                        this.CurrentDistanceCountingCharacter = SelectedParticipants[0] as Character;
                    if (!CurrentDistanceCountingCharacter.HasBeenSpawned)
                        this.CurrentDistanceCountingCharacter = null;
                    else
                    {
                        this.CurrentDistanceCountingCharacter.CurrentStartingPositionVectorForDistanceCounting = this.CurrentDistanceCountingCharacter.CurrentPositionVector;
                    }
                }
            }
        }
    
        private void IncrementDistanceCount(Character character)
        {
            if (IsPlayingAttack)
            {

            }
            else
            {

            }
        }

        #endregion

        #region Scan and Fix Memory Pointers

        private void ScanAndFixMemoryPointer()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach (Character c in charactersToOperateOn)
                c.ScanAndFixMemoryPointer();
        }

        #endregion

        #region Edit Character

        private bool CanEditCharacter(object state)
        {
            return this.SelectedParticipants != null && this.SelectedParticipants.Count == 1 && !this.IsPlayingAttack;
        }

        private void EditCharacter()
        {
            if (CanEditCharacter(null))
            {
                CrowdMemberModel c = this.SelectedParticipants[0] as CrowdMemberModel;
                this.eventAggregator.GetEvent<EditCharacterEvent>().Publish(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(c, null));
            }
        }

        #endregion

        #region Spawn On Click

        private bool CanToggleSpawnOnClick()
        {
            return !this.IsPlayingAttack;
        }

        private void ToggleSpawnOnClick()
        {
            this.SpawnOnClick = !this.SpawnOnClick;
        }

        #endregion

        #region Clone and Spawn

        private bool CanToggleCloneAndSpawn()
        {
            return !this.IsPlayingAttack;
        }

        private void ToggleCloneAndSpawn()
        {
            this.CloneAndSpawn = !this.CloneAndSpawn;
        }

        #endregion
         
        #region Overhead Mode

        private bool CanToggleOverheadMode()
        {
            return true;
        }

        private void ToggleOverheadMode()
        {
            this.OverheadMode = !this.OverheadMode;

            if(this.OverheadMode)
                IconInteractionUtility.ExecuteCmd("bindloadfile required_keybinds_alt.txt");
            else
                IconInteractionUtility.ExecuteCmd("bindloadfile required_keybinds.txt");
        }

        #endregion

        #region Activate Character/Gang

        private bool CanActivateCharacterOrGang(object state)
        {
            //return CanToggleTargeted(state) && (SelectedParticipants[0] as Character).HasBeenSpawned ;
            return !this.IsPlayingAttack && SelectedParticipants.Count > 0;
        }

        private void ActivateCharacter()
        {
            if(CanActivateCharacterOrGang(null))
                ToggleActivateCharacter();
        }

        private void ActivateCharacterOrGang()
        {
            if (!IsPlayingAttack)
            {
                if (SelectedParticipants.Count > 1)
                {
                    ToggleActivateGang();
                }
                else if (IsGangModeActive)
                {
                    ToggleActivateCrowdAsGang();
                }
                else
                {
                    ActivateCharacter();
                }
            }
        }

        public void ToggleActivateCharacter(Character character = null, string selectedOptionGroupName = null, string selectedOptionName = null)
        {
            Action action = delegate()
            {
                if (character == null)
                    if (SelectedParticipants.Count == 0)
                        return;
                    else
                        character = SelectedParticipants[0] as CrowdMemberModel;
                if (character.IsActive)
                {
                    if (this.IsGangActive)
                        DeactivateGang();
                    else
                        DeactivateCharacter(character);
                }
                else
                {
                    if (this.IsGangActive)
                        DeactivateGang();
                    else if (this.ActiveCharacter != null)
                        DeactivateCharacter(this.ActiveCharacter as Character);
                    ActivateCharacter(character, selectedOptionGroupName, selectedOptionName);
                }

            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void ActivateCharacter(Character character, string selectedOptionGroupName = null, string selectedOptionName = null)
        {
            if (!character.HasBeenSpawned)
                character.Spawn();
            // Pause movements from other characters that were active
            if (Helper.GlobalVariables_CharacterMovement != null && Helper.GlobalVariables_CharacterMovement.Character.IsActive)
            {
                Helper.GlobalVariables_CharacterMovement.IsPaused = true;
                Helper.GlobalVariables_FormerActiveCharacterMovement = Helper.GlobalVariables_CharacterMovement;
            }
            // Deactivate movements from other characters that are not active
            if (Helper.GlobalVariables_CharacterMovement != null && !Helper.GlobalVariables_CharacterMovement.Character.IsActive)
            {
                var otherCharacter = Helper.GlobalVariables_CharacterMovement.Character;
                if (otherCharacter != Helper.GlobalVariables_ActiveCharacter)
                {
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(Helper.GlobalVariables_CharacterMovement);
                }
            }
            character.SetActive();
            // Now resume any paused movements for the activated character
            var pausedMovement = character.Movements.FirstOrDefault(cm => cm.IsPaused && Helper.GlobalVariables_FormerActiveCharacterMovement == cm);
            if (pausedMovement != null)
            {
                pausedMovement.IsPaused = false;
            }
            this.eventAggregator.GetEvent<ActivateCharacterEvent>().Publish(new Tuple<Character, string, string>(character, selectedOptionGroupName, selectedOptionName));
            SelectNextCharacterInCrowdCycle();
        }

        private void DeactivateCharacter(Character character = null)
        {
            //Action action = delegate()
            {
                if (character == null)
                {
                    if (this.ActiveCharacter != null)
                        character = this.ActiveCharacter as Character;
                    else if (SelectedParticipants.Count == 0)
                        return;
                    else
                        character = SelectedParticipants[0] as Character;
                }
                if (character.IsActive)
                {
                    // Resume movements from other characters that were paused
                    if (Helper.GlobalVariables_FormerActiveCharacterMovement != null)
                    {
                        Helper.GlobalVariables_FormerActiveCharacterMovement.IsPaused = false;
                        Helper.GlobalVariables_CharacterMovement = Helper.GlobalVariables_FormerActiveCharacterMovement;
                    }
                    character.ResetActive();
                    this.eventAggregator.GetEvent<DeactivateCharacterEvent>().Publish(character);
                    SelectNextCharacterInCrowdCycle();
                }

            };
            //Application.Current.Dispatcher.BeginInvoke(action);
        }

        private bool CanActivateGang()
        {
            return !IsPlayingAttack && SelectedParticipants.Count > 1;
        }

        private bool CanActivateCrowdAsGang()
        {
            return !IsPlayingAttack && SelectedParticipants != null && SelectedParticipants.Count == 1;
        }

        private void ToggleActivateGang()
        {
            if (!CanActivateGang())
                return;
            if (IsGangActive)
            {
                bool needToActivateAnotherGang = true;
                foreach (Character p in SelectedParticipants)
                {
                    if (p.IsActive)
                        needToActivateAnotherGang = false;
                }
                DeactivateGang();
                if (needToActivateAnotherGang)
                    ActivateSelectedCharactersAsGang();
            }
            else
            {
                if (this.ActiveCharacter != null)
                    DeactivateCharacter(this.ActiveCharacter as Character);
                ActivateSelectedCharactersAsGang();
            }
            
        }

        private void ActivateSelectedCharactersAsGang()
        {
            List<Character> gangMembers = new List<Character>();
            foreach (var c in this.SelectedParticipants)
            {
                Character character = c as Character;
                gangMembers.Add(character);
            }
            ActivateGang(gangMembers);
        }
        private void ToggleActivateCrowdAsGang()
        {
            if (!CanActivateCrowdAsGang())
                return;
            if (IsGangActive)
            {
                bool needToActivateAnotherGang = true;
                var targetedCharacter = GetCurrentTarget();
                foreach (Character p in Participants.Where(p => p.RosterCrowd == targetedCharacter.RosterCrowd))
                {
                    if (p.IsActive)
                        needToActivateAnotherGang = false;
                }
                DeactivateGang();
                if (needToActivateAnotherGang)
                    ActivateCrowdAsGang();
            }
            else
            {
                if (this.ActiveCharacter != null)
                    DeactivateCharacter(this.ActiveCharacter as Character);
                ActivateCrowdAsGang();
            }

        }
    
        private void ActivateCrowdAsGang()
        {
            var targetedCharacter = GetCurrentTarget();
            if (SelectedParticipants.Contains(targetedCharacter))
            {
                List<Character> gangMembers = new List<Character>();
                foreach(Character c in Participants.Where(p => p.RosterCrowd == targetedCharacter.RosterCrowd))
                {
                    gangMembers.Add(c);
                }
                ActivateGang(gangMembers);
            }
        }

        private void ActivateGang(List<Character> gangMembers)
        {
            Character targetedCharacter = GetCurrentTarget() as Character;
            foreach(var gm in gangMembers)
            {
                gm.SetActive();
                if (gm == targetedCharacter)
                {
                    gm.IsGangLeader = true;
                    //InitiateGangMovementHandlers(gm);
                }
            }
            if(!gangMembers.Any(c => c.IsGangLeader))
            {
                gangMembers[0].IsGangLeader = true;
                //InitiateGangMovementHandlers(gangMembers[0]);
            }
            this.IsGangActive = true;
            this.eventAggregator.GetEvent<ActivateGangEvent>().Publish(gangMembers);
        }
        
        private void DeactivateGang()
        {
            foreach (var c in this.Participants)
            {
                Character character = c as Character;
                character.ResetActive();
            }
            this.IsGangActive = false;
            this.eventAggregator.GetEvent<DeactivateGangEvent>().Publish(null);
        }

        private void InitiateGangMovementHandlers(Character gangLeader)
        {
            foreach(CharacterMovement cm in gangLeader.Movements)
            {
                cm.MovementActivatedForGangLeader -= this.GangLeader_OnMovementActivated;
                cm.MovementActivatedForGangLeader += this.GangLeader_OnMovementActivated;

                cm.MovementDeactivatedForGangLeader -= this.GangLeader_OnMovementDeactivated;
                cm.MovementDeactivatedForGangLeader += this.GangLeader_OnMovementDeactivated;
            }
        }

        private void DisableGangMovementHandlers(Character gangLeader)
        {
            foreach (CharacterMovement cm in gangLeader.Movements)
            {
                cm.MovementActivatedForGangLeader -= this.GangLeader_OnMovementActivated;

                cm.MovementDeactivatedForGangLeader -= this.GangLeader_OnMovementDeactivated;
            }
        }

        private void GangLeader_OnMovementActivated(object sender, CustomEventArgs<CharacterMovement> e)
        {
            if (this.IsGangActive)       
            {
                CharacterMovement cm = e.Value;
                foreach(Character c in this.Participants.Where(p => (p as Character).IsActive && !(p as Character).IsGangLeader))
                {
                    cm.ActivateMovement(c);
                }
            }
        }

        private void GangLeader_OnMovementDeactivated(object sender, CustomEventArgs<CharacterMovement> e)
        {
            if (this.IsGangActive)
            {
                CharacterMovement cm = e.Value;
                foreach (Character c in this.Participants.Where(p => (p as Character).IsActive && !(p as Character).IsGangLeader))
                {
                    cm.DeactivateMovement(c);
                }
            }
        }

        #endregion

        #region Play Default Ability

        public void PlayDefaultAbility()
        {
            Action d = delegate()
            {
                if (!IsPlayingAttack && SelectedParticipants != null && (SelectedParticipants.Count == 1 || (SelectedParticipants.Count > 1 && IsGangModeActive) ))
                {
                    Character abilityPlayingCharacter = null;

                    if (this.ActiveCharacter == null)
                    {
                        abilityPlayingCharacter = SelectedParticipants[0] as Character;
                        //if(!abilityPlayingCharacter.IsActive)
                        //    ToggleActivateCrowdAsGang();
                    }
                    else
                        abilityPlayingCharacter = this.ActiveCharacter as Character;

                    RosterMouseDoubleClicked = false;

                    if (abilityPlayingCharacter != null)
                    {
                        var ability = abilityPlayingCharacter.DefaultAbilityToActivate;
                        if(ability != null)
                        {
                            if (IsGangModeActive)
                            {
                                foreach(Character character in Participants.Where(p => p.RosterCrowd == (abilityPlayingCharacter as CrowdMemberModel).RosterCrowd))
                                {
                                    ability.Play(Target: character);
                                }
                            }
                            else
                            {
                                abilityPlayingCharacter.Target(false);
                                //abilityPlayingCharacter.ActiveIdentity.RenderWithoutAnimation(target: abilityPlayingCharacter);
                                ability.Play();
                            }
                        }
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        #endregion

        #region Play Movement

        private void PlayMovement(CharacterMovement characterMovement)
        {
            List<Character> movementTargets = new List<Character>();
            if (this.IsGangModeActive)
            {
                var gangCrowd = this.Participants.Where(p => p.RosterCrowd.IsGangMode);
                if (gangCrowd.Contains(characterMovement.Character as ICrowdMemberModel))
                {
                    foreach (Character c in gangCrowd.Where(gm => gm.RosterCrowd == (characterMovement.Character as CrowdMemberModel).RosterCrowd))
                    {
                        movementTargets.Add(c);
                    }
                }
                else
                    movementTargets.Add(characterMovement.Character);              
            }
            else if (this.IsGangActive)
            {
                var gangLeader = this.Participants.FirstOrDefault(p => (p as Character).IsGangLeader);
                if(characterMovement.Character == gangLeader)
                {
                    foreach (Character c in this.Participants.Where(p => (p as Character).IsActive))
                        movementTargets.Add(c);
                }
                else
                    movementTargets.Add(characterMovement.Character);
            }
            else
                movementTargets.Add(characterMovement.Character);

            if(Participants.Any(p => (p as Character).Movements.Any(m => m.IsActive) && !movementTargets.Contains(p as Character)))
            {
                foreach(CharacterMovement cm in Participants.Where(p => (p as Character).Movements.Any(m => m.IsActive) 
                && !movementTargets.Contains(p as Character)).SelectMany(p => (p as Character).Movements.Where(m => m.IsActive)))
                {
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(cm);
                }
            }

            this.eventAggregator.GetEvent<PlayMovementConfirmedEvent>().Publish(new Tuple<CharacterMovement, List<Character>>(characterMovement, movementTargets));
        }

        #endregion

        #region Attack / Area Attack
        int numRetryHover = 3;
        Character currentTarget = null;
        bool dontFireAttack = false;

        private void PlayAttackCycle()
        {
            CrowdMemberModel character = (CrowdMemberModel)GetHoveredCharacter();
            Vector3 mousePosition = new MouseElement().Position;
            this.CurrentDistanceCountingCharacter.UpdateDistanceCount(mousePosition);
            if (character == null && numRetryHover > 0)
            {
                numRetryHover--;
                Action d = delegate ()
                {
                    PlayAttackCycle();
                };
                AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 20);
                adex.ExecuteAsyncDelegate();
            }
            else if (this.currentAttack != null && this.AttackingCharacters.Count > 0)
            {
                numRetryHover = 3;
                Character latestTarget = GetCurrentTarget() as Character;
                if(character == null && latestTarget != currentTarget)
                {
                    currentTarget = null;
                    character = latestTarget as CrowdMemberModel;
                }
                if (!dontFireAttack &&(this.AttackingCharacters.Contains(character) || character == null))
                {
                    AttackDirection direction = new AttackDirection(mousePosition);
                    if(!this.currentAttack.IsExecutionInProgress)
                        this.currentAttack.AnimateAttack(direction, AttackingCharacters);
                }
                else
                {
                    Action d = delegate ()
                    {
                        if(Keyboard.Modifiers == ModifierKeys.Shift)
                        {
                            this.SelectedParticipants.Clear();
                            this.SelectedParticipants.Add(character);
                            this.TargetCharacterForAttack(null);
                        }
                        else if (!dontFireAttack)
                        {
                            this.SelectedParticipants.Clear();
                            this.SelectedParticipants.Add(character);
                            this.TargetAndExecuteAttack(null);
                        }
                    };
                    Dispatcher.Invoke(d);
                }
            }

        }

        private void InitializeAttackEventHandlers(Attack attack)
        {
            attack.AttackInitiated -= this.Ability_AttackInitiated;
            attack.AttackCompleted -= this.Ability_AttackCompleted;
            attack.AttackInitiated += this.Ability_AttackInitiated;
            attack.AttackCompleted += this.Ability_AttackCompleted;
        }

        private void InitializeAttackEventHandlers(Character character)
        {
            foreach (var ability in character.AnimatedAbilities)
            {
                var attack = ability as Attack;
                InitializeAttackEventHandlers(attack);
            }
        }

        private void Ability_AttackInitiated(object sender, EventArgs e)
        {
            Character targetCharacter = sender as Character;
            CustomEventArgs<Attack> customEventArgs = e as CustomEventArgs<Attack>;
            if (targetCharacter != null && customEventArgs != null)
            {
                Helper.GlobalVariables_IsPlayingAttack = true;
                // Change mouse pointer to bulls eye
                Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                Dispatcher.Invoke(() => { Mouse.OverrideCursor = cursor; });
                // Inform Roster to update attacker
                this.eventAggregator.GetEvent<AttackInitiatedEvent>().Publish(new Tuple<Character, Attack>(targetCharacter, customEventArgs.Value));
            }
        }

        private void Ability_AttackCompleted(object sender, CustomEventArgs<List<Character>> e)
        {
            this.ResetAttack(e.Value);
        }

        private void InitiateRosterCharacterAttack(Tuple<Character, Attack> attackInitiatedEventTuple)
        {
            this.targetCharacters = new List<Character>();
            this.RestartDistanceCounting();
            Character attackingCharacter = attackInitiatedEventTuple.Item1;
            Attack attack = attackInitiatedEventTuple.Item2;
            CrowdMemberModel rosterCharacter = this.Participants.FirstOrDefault(p => p.Name == attackingCharacter.Name) as CrowdMemberModel;
            if (rosterCharacter != null && attack != null)
            {
                this.IsPlayingAttack = true;
                if (attack.IsAreaEffect)
                {
                    this.IsPlayingAreaEffect = true;
                }
                Commands_RaiseCanExecuteChanged();
                OnPropertyChanged("ShowAttackContextMenu");
                targetObserver.TargetChanged += RosterTargetUpdated;
                this.currentAttack = attack;
                //this.AttackingCharacter = attackingCharacter;
                this.AttackingCharacters.Clear();
                if (attackingCharacter.IsGangLeader)
                {
                    foreach (Character c in this.Participants.Where(p => (p as Character).IsActive && (p as Character).HasBeenSpawned))
                    {
                        this.AttackingCharacters.Add(c);
                        c.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None };
                    }
                }
                else if (this.IsGangModeActive && (attackingCharacter as CrowdMemberModel).RosterCrowd.IsGangMode)
                {
                    foreach (Character c in this.Participants.Where(p => p.RosterCrowd == (attackingCharacter as CrowdMemberModel).RosterCrowd && (p as Character).HasBeenSpawned))
                    {
                        this.AttackingCharacters.Add(c);
                        c.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None };
                    }  
                }
                else
                {
                    this.AttackingCharacters.Add(attackingCharacter);
                    // Update character properties - icons in roster should show
                    rosterCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None };
                }
            }
        }

        private void RosterTargetUpdated(object sender, EventArgs e)
        {
            uint currentTargetPointer = targetObserver.CurrentTargetPointer;
            CrowdMemberModel currentTarget = (CrowdMemberModel)Participants.DefaultIfEmpty(null).Where(
                (p) =>
                {
                    Character c = p as Character;
                    return c.gamePlayer != null && c.gamePlayer.Pointer == currentTargetPointer;
                }).FirstOrDefault();
            Action action = delegate()
            {
                //if (this.isPlayingAttack && currentTarget != null)
                //{
                //    if (currentTarget.Name != this.AttackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                //    {
                //        if (!this.isPlayingAreaEffect)
                //        {
                //            if (targetCharacters.Count == 0 && PopupService.IsOpen("ActiveAttackView") == false)// choose only one character for vanilla attack
                //            {
                //                this.targetCharacters.Add(currentTarget);
                //                //currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                //                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                //                attackConfig.AttackMode = AttackMode.Defend;
                //                attackConfig.AttackEffectOption = AttackEffectOption.None;
                //                currentTarget.ActiveAttackConfiguration = attackConfig;
                //                currentTarget.ActiveAttackConfiguration.IsCenterTarget = true;
                //                //if (PopupService.IsOpen("ActiveAttackView") == false)
                //                this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
                //            }
                //        }
                //    }
                //}
                //else 
                //if (!this.IsPlayingAttack && currentTarget != null)
                //{
                //    if (Helper.GlobalVariables_CharacterMovement != null && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                //    {
                //        Vector3 destination = new Vector3(currentTarget.Position.X, currentTarget.Position.Y, currentTarget.Position.Z);
                //        Character activeMovementChar = Helper.GlobalVariables_CharacterMovement.Character;
                //        activeMovementChar.MoveToLocation(destination);
                //    }
                //}
                //
                //else 
                if (currentTarget == null) // Cancel attack
                {
                    this.CancelActiveAttack(this.currentAttack);
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void ConfirmActiveAttack()
        {
            if(IsPlayingAttack)
                this.eventAggregator.GetEvent<ConfirmAttackEvent>().Publish(null);
        }

        private void CancelActiveAttack(object state)
        {
            Action action = delegate()
            {
                if (this.IsPlayingAttack)
                {
                    Helper.GlobalVariables_IsPlayingAttack = false;
                    Commands_RaiseCanExecuteChanged();
                    List<Character> defendingCharacters = new List<Character>();
                    if (state is List<Character>)
                        defendingCharacters = state as List<Character>;
                    this.CancelAttack(defendingCharacters);
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void HandleCharacterOptionAddition(ICharacterOption option)
        {
            if (option is Attack)
                this.InitializeAttackEventHandlers(option as Attack);
        }

        private void LaunchActiveAttack(Tuple<List<Character>, Attack> tuple)
        {
            targetObserver.TargetChanged -= RosterTargetUpdated;
            List<Character> defendingCharacters = tuple.Item1;
            Attack attack = tuple.Item2;
            foreach (var defender in defendingCharacters)
            {
                // Commenting out following as we need the fxs to be persisted across attacks
                //defender.Deactivate(); // restore original costume
            }
            if(attack.IsAreaEffect)
                attack.AnimateAttackSequence(AttackingCharacters, defendingCharacters);
            else
            {
                foreach (var defender in defendingCharacters)
                {
                    defender.ActiveAttackConfiguration.IsCenterTarget = true;
                }
                attack.AnimateAttackSequence(AttackingCharacters, defendingCharacters);
            }
        }

        private void CancelAttack(List<Character> defendingCharacters)
        {
            if (this.AttackingCharacters.Count > 0)
                this.AttackingCharacters.ForEach(ac => ac.Deactivate());
            foreach (var defender in defendingCharacters)
            {
                defender.Deactivate(); // restore original costume
            }
            this.ResetAttack(defendingCharacters);
        }

        private void ResetAttack(List<Character> defenders)
        {
            this.IsPlayingAttack = false;
            this.IsPlayingAreaEffect = false;
            targetObserver.TargetChanged -= RosterTargetUpdated;
            //this.currentAttack.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(); });
            this.currentAttack.Stop(useMemoryTargeting:true);

            Action d = delegate ()
            {
                // Hide attack icon from attacking character
                if (this.AttackingCharacters.Count > 0)
                    this.AttackingCharacters.ForEach(ac =>
                    {
                        ac.ActiveAttackConfiguration.AttackMode = AttackMode.None;
                        ac.ScanAndFixMemoryPointer();
                        });
                this.AttackingCharacters.Clear();

                Helper.GlobalVariables_IsPlayingAttack = false;
                this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this.currentAttack);
                foreach (var defender in defenders)
                 {
                    defender.ActiveAttackConfiguration.AttackMode = AttackMode.None;
                    defender.ActiveAttackConfiguration.AttackResults = null;
                    defender.ScanAndFixMemoryPointer();
                    //if (!this.currentAttack.OnHitAnimation.Persistent)
                    //    this.currentAttack.OnHitAnimation.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(defender); });
                    //defender.Deactivate(); // restore original costume
                }

                // Update Mouse cursor
                Mouse.OverrideCursor = Cursors.Arrow;
                this.currentAttack = null;
                this.moveAndAttackModeOn = false;
                this.RestartDistanceCounting();
                this.Commands_RaiseCanExecuteChanged();
            };
            Application.Current.Dispatcher.Invoke(d);
        }

        private void ResetCharacterState(object state)
        {
            if (state != null && this.Participants != null)
            {
                string charName = state.ToString();
                Character defendingCharacter = this.Participants.FirstOrDefault(p => p.Name == charName) as Character;
                if (defendingCharacter != null && defendingCharacter.ActiveAttackConfiguration != null)
                {
                    // If he is just stunned make him normal
                    if (defendingCharacter.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Stunned)
                    {
                        KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();

                        if (defendingCharacter.ActiveIdentity.Type == IdentityType.Model && defendingCharacter.GhostShadow != null && defendingCharacter.GhostShadow.HasBeenSpawned)
                        {
                            defendingCharacter.GhostShadow.Target(!defendingCharacter.IsInViewForTargeting);
                            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                        }
                        defendingCharacter.Target(!defendingCharacter.IsInViewForTargeting);
                        keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                        keyBindsGenerator.CompleteEvent();
                    }
                    // Else make him stand up 
                    else if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
                    {
                        var globalStandUpAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.STANDUP_ABILITY_NAME);
                        if (globalStandUpAbility != null && globalStandUpAbility.AnimationElements != null && globalStandUpAbility.AnimationElements.Count > 0)
                        {
                            globalStandUpAbility.Play(false, defendingCharacter, false, !defendingCharacter.IsInViewForTargeting);
                        }
                    }
                    // Update icons in Roster
                    defendingCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackEffectOption = AttackEffectOption.None, AttackMode = AttackMode.None, AttackResults = null };
                    this.isCharacterReset = true;
                }
            }
        }
         
        public void TargetCharacterForAttack(object state)
        {
            dontFireAttack = true;
            AddAttackTargets();
            Action d = delegate ()
            {
                dontFireAttack = false;
            };
            AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 500);
            adex.ExecuteAsyncDelegate();
        }

        public void TargetAndExecuteAttack(object state)
        {
            AddAttackTargets();
            LaunchAttackConfiguration();
        }

        public bool CanTargetAndExecuteCrowd()
        {
            return SelectedParticipants != null && SelectedParticipants.Count > 0 && (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd != null; 
        }

        public void TargetAndExecuteCrowd()
        {
            CrowdModel selectedCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd as CrowdModel;
            AddToAttackTarget(selectedCrowd);
            LaunchAttackConfiguration();
        }

        private void AddAttackTargets()
        {
            if (IsGangModeActive)
            {
                CrowdModel selectedCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd as CrowdModel;
                AddToAttackTarget(selectedCrowd);
            }
            else
            {
                foreach (Character participant in this.SelectedParticipants)
                {
                    AddToAttackTarget(participant);
                }
            }
        }

        private void AddToAttackTarget(CrowdModel crowd)
        {
            foreach (Character c in Participants.Where(p => p.RosterCrowd == crowd && (p as Character).HasBeenSpawned))
            {
                AddToAttackTarget(c);
            }
        }

        private void AddToAttackTarget(Character character)
        {
            if (!this.AttackingCharacters.Contains(character) && character.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && character.Name != Constants.DEFAULT_CHARACTER_NAME)
            {
                if (this.targetCharacters.FirstOrDefault(tc => tc.Name == character.Name) == null)
                    this.targetCharacters.Add(character);
                //currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                attackConfig.AttackMode = AttackMode.Defend;
                attackConfig.AttackEffectOption = AttackEffectOption.None;
                if (this.moveAndAttackModeOn)
                    attackConfig.MoveAttackerToTarget = true;

                character.ActiveAttackConfiguration = attackConfig;
            }
        }

        private void LaunchAttackConfiguration()
        {
            foreach (var currentTarget in targetCharacters)
            {
                if (this.AttackingCharacters.Count > 0 && (IsGangActive || AttackingCharacters.All(ac => (ac as CrowdMemberModel).RosterCrowd.IsGangMode)))
                {
                    currentTarget.ActiveAttackConfiguration.AttackResults = new ObservableCollection<AttackResult>();
                    this.AttackingCharacters.ForEach(ac => currentTarget.ActiveAttackConfiguration.AttackResults.Add(new AttackResult { Attacker = ac, IsHit = false, AttackResultOption = AttackResultOption.Miss }));
                }
            }
            if (PopupService.IsOpen("ActiveAttackView") == false)
                this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
        }

        private void ConfirmAttack(object state)
        {
            if(this.IsPlayingAttack)
            {
                if(PopupService.IsOpen("ActiveAttackView") == true)
                {
                    this.eventAggregator.GetEvent<ConfirmAttackEvent>().Publish(null);
                }
            }
        }

        #endregion

        #region Desktop Context Menu Handlers
        void desktopContextMenu_SpawnMenuItemSelected(object sender, EventArgs e)
        {
            this.Spawn();
        }

        void desktopContextMenu_SavePositionMenuItemSelected(object sender, EventArgs e)
        {
            this.SavePosition();
        }

        void desktopContextMenu_ResetOrientationMenuItemSelected(object sender, EventArgs e)
        {
            this.ResetOrientation();
        }

        void desktopContextMenu_PlaceMenuItemSelected(object sender, EventArgs e)
        {
            this.Place();
        }

        private void desktopContextMenu_MoveTargetToCharacterMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            string destharacterName = e.Value as string;
            Character character = this.Participants.FirstOrDefault(p => p.Name == destharacterName) as Character;
            if (character != null)
            {
                Vector3 destination = new Vector3(character.Position.X, character.Position.Y, character.Position.Z);
                foreach (Character c in this.SelectedParticipants)
                {
                    c.MoveToLocation(destination);
                }
            }
        }

        void desktopContextMenu_MoveTargetToCameraMenuItemSelected(object sender, EventArgs e)
        {
            this.MoveTargetToCamera();
        }

        void desktopContextMenu_MoveCameraToTargetMenuItemSelected(object sender, EventArgs e)
        {
            this.TargetAndFollow(true);
        }

        void desktopContextMenu_ManueverWithCameraMenuItemSelected(object sender, EventArgs e)
        {
            this.ToggleManeuverWithCamera();
        }

        void desktopContextMenu_DefaultContextMenuDisplayed(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
        }

        void desktopContextMenu_CloneAndLinkMenuItemSelected(object sender, EventArgs e)
        {
            Character character = SelectedParticipants != null && SelectedParticipants.Count == 1 ? SelectedParticipants[0] as Character : null;
            this.eventAggregator.GetEvent<CloneLinkCrowdMemberEvent>().Publish(character as CrowdMemberModel);
        }

        void desktopContextMenu_ClearFromDesktopMenuItemSelected(object sender, EventArgs e)
        {
            this.ClearFromDesktop();
        }

        void desktopContextMenu_AttackTargetMenuItemSelected(object sender, EventArgs e)
        {
            this.TargetCharacterForAttack(null);
        }

        void desktopContextMenu_AttackTargetAndExecuteMenuItemSelected(object sender, EventArgs e)
        {
            this.TargetAndExecuteAttack(null);
        }

        void desktopContextMenu_AttackTargetAndExecuteCrowdMenuItemSelected(object sender, EventArgs e)
        {
            this.TargetAndExecuteCrowd();
        }

        private void desktopContextMenu_AttackContextMenuDisplayed(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
        }

        private void desktopContextMenu_ActivateMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                this.ToggleActivateCharacter(character);
        }

        private void desktopContextMenu_ActivateCrowdAsGangMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                this.ToggleActivateCrowdAsGang();
        }

        private void desktopContextMenu_ActivateCharacterOptionMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Object[] parameters = e.Value as Object[];
            if (parameters != null && parameters.Length == 3)
            {
                Character character = parameters[0] as Character;
                string optionGroupName = parameters[1] as string;
                string optionName = parameters[2] as string;
                this.ToggleActivateCharacter(character, optionGroupName, optionName);
            }
        }

        #endregion

        #region Desktop Key Handling
        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == Constants.ROSTER_EXPLORER || Helper.GlobalVariables_CurrentActiveWindowName == Constants.ACTIVE_CHARACTER_WIDGET)
            {
                if (inputKey == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.Place;
                }
                else if (inputKey == Key.S && Keyboard.Modifiers == (ModifierKeys.Control))
                {
                    return this.SavePosition;
                }
                else if (inputKey == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.Spawn();
                }
                else if (inputKey == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ToggleTargeted;
                }
                else if (inputKey == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ToggleManeuverWithCamera;
                }
                else if (inputKey == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.TargetAndFollow;
                }
                else if (inputKey == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.EditCharacter;
                }
                else if (inputKey == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.MoveTargetToCamera;
                }
                else if (inputKey == Key.Y && Keyboard.Modifiers == (ModifierKeys.Control))
                {
                    return this.CycleCommandsThroughCrowd;
                }
                else if(inputKey == Key.X && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ActivateCharacterOrGang;
                }
                else if (inputKey == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ToggleGangMode;
                }
                else if (inputKey == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
                {
                    return this.ConfirmActiveAttack;
                }
                else if (inputKey == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ResetOrientation;
                }
                else if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ClearFromDesktop;
                }
                else if (inputKey == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    //Jeff fixed activating keystroke problem so works without activating a character
                    return ActivateDefaultMovementToActivate;
                }
                else if (inputKey == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.TeleportTargetToCamera(null);
                }
                else if (inputKey == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleTargetOnHover(null);
                }
                else if (inputKey == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleRelativePositioning(null);
                }
                else if(inputKey == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleSpawnOnClick();
                }
                else if (inputKey == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleCloneAndSpawn();
                }
                else if (inputKey == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleOverheadMode();
                }
            }

            if (this.ActiveCharacter != null)
            {
                if (inputKey == Key.F1)
                {
                    return this.PlayDefaultAbility;
                }
                else if (inputKey == Key.F2)
                {
                    return ActivateDefaultMovementToActivate;
                }
            }
            Character targetedCharacter = null;
            if (IsGangActive)
            {
                targetedCharacter = Participants.FirstOrDefault(p => (p as Character).IsGangLeader) as Character;
            }
            else
            {
                targetedCharacter = GetCurrentTarget() as Character;
            }
            if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift) && targetedCharacter != null && targetedCharacter.AnimatedAbilities.Any(ab => ab.ActivateOnKey == vkCode))
            {
                var activeAbility = targetedCharacter.AnimatedAbilities.First(ab => ab.ActivateOnKey == vkCode);
                targetedCharacter.Target(false);
                targetedCharacter.ActiveIdentity.RenderWithoutAnimation(target: targetedCharacter);
                activeAbility.Play();
            }
            else if (targetedCharacter != null && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                CharacterMovement cm = null;
                if (targetedCharacter.Movements.Any(m => m.ActivationKey == vkCode))
                {
                    cm = targetedCharacter.Movements.First(m => m.ActivationKey == vkCode);
                }
                else if (inputKey == Key.K)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Walk");
                }
                else if (inputKey == Key.U)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Run");
                }
                else if (inputKey == Key.S)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Swim");
                }
                else if (inputKey == Key.P)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Steampack");
                }
                else if (inputKey == Key.F)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Fly");
                }
                else if (inputKey == Key.B)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Beast");
                }
                else if (inputKey == Key.J)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Ninja");
                }
                else if (inputKey == Key.T)
                {
                    targetedCharacter.TeleportToCamera();
                }
                
                if (cm != null)
                {
                    if (!cm.IsActive)
                    {
                        this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(cm);
                    }
                    else
                        this.eventAggregator.GetEvent<StopMovementEvent>().Publish(cm);
                }
            }
            else if(inputKey == Key.Escape)
            {
                if (this.IsPlayingAttack)
                {
                    foreach (var c in this.targetCharacters)
                    {
                        c.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.None, AttackEffectOption = AttackEffectOption.None };
                    }
                    this.eventAggregator.GetEvent<CloseActiveAttackWidgetEvent>().Publish(null);
                    this.eventAggregator.GetEvent<CancelActiveAttackEvent>().Publish(this.targetCharacters);
                    this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this.targetCharacters);
                }
                else if (CharacterIsMoving)
                {
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(Helper.GlobalVariables_CharacterMovement);
                }
                else if(this.ActiveCharacter != null)
                {
                    if (this.IsGangActive)
                        DeactivateGang();
                    else
                        this.DeactivateCharacter(this.ActiveCharacter as Character);
                }
            }
            return null;
        }

        #endregion
    }
}
