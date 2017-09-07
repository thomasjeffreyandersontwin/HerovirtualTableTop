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

        //refactor to attacking character
        private Attack currentAttack = null;
        public Character AttackingCharacter = null;

        private bool isCharacterReset = false;

        private bool isCharacterDragDropInProgress = false;
        private Character currentDraggingCharacter = null;
        private DateTime lastDesktopMouseDownTime = DateTime.MinValue;

        private bool stopSyncingWithDesktop = false;

        private List<Character> targetCharactersForMove = new List<Character>();
        private List<Character> targetCharacters = new List<Character>();
        private List<CrowdMemberModel> _oldSelection = new List<CrowdMemberModel>();

        Character lastTargetedCharacter = null;
        private Character previousSelectedCharacter = null;
        public bool RosterMouseDoubleClicked = false;
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
                this.TargetOrFollow();
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
                return activeCharacter;
            }
            set
            {
                activeCharacter = value;
                Helper.GlobalVariables_ActiveCharacter = value as Character;
                OnPropertyChanged("ActiveCharacter");
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
        public DelegateCommand<object> TeleportTargetToCameraCommand { get; private set; }

        #endregion

        #region Constructor

        public RosterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, ITargetObserver targetObserver, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.targetObserver = targetObserver;

            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe(AddParticipants);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(DeleteParticipant);
            this.eventAggregator.GetEvent<CheckRosterConsistencyEvent>().Subscribe(CheckRosterConsistency);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(InitiateRosterCharacterAttack);
            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Subscribe(this.LaunchActiveAttack);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.CancelActiveAttack);
            this.eventAggregator.GetEvent<AddOptionEvent>().Subscribe(this.HandleCharacterOptionAddition);

            timer_RespondToDesktop.AutoReset = false;
            timer_RespondToDesktop.Interval = 50;
            timer_RespondToDesktop.Elapsed += timer_RespondToDesktop_Elapsed;

            this.eventAggregator.GetEvent<ListenForTargetChanged>().Subscribe((obj) =>
            {
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
            mouseHandler.OnMouseLeftClickUp.Add(DropDraggedCharacter);
            mouseHandler.OnMouseRightClickUp.Add(DisplayCharacterPopupMenue);
            mouseHandler.OnMouseMove.Add(TargetHoveredCharacter);
            mouseHandler.OnMouseDoubleClick.Add(PlayDefaultAbility);
            mouseHandler.OnMouseTripleClick.Add(ToggleManeuverWithCamera);
        }

        private void InitializeDesktopContextMenuHandlers()
        {
            desktopContextMenu = new DesktopContextMenu();
            desktopContextMenu.ActivateCharacterOptionMenuItemSelected += desktopContextMenu_ActivateCharacterOptionMenuItemSelected;
            desktopContextMenu.ActivateMenuItemSelected += desktopContextMenu_ActivateMenuItemSelected;
            desktopContextMenu.AttackContextMenuDisplayed += desktopContextMenu_AttackContextMenuDisplayed;
            desktopContextMenu.AttackTargetAndExecuteMenuItemSelected += desktopContextMenu_AttackTargetAndExecuteMenuItemSelected;
            desktopContextMenu.AttackTargetMenuItemSelected += desktopContextMenu_AttackTargetMenuItemSelected;
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

        private void InitializeDesktopKeyHanders()
        {
            DesktopKeyEventHandler keyHandler = new DesktopKeyEventHandler(RetrieveEventFromKeyInput);
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
            this.ActivateCharacterCommand = new DelegateCommand<object>(delegate(object state) { this.ActivateCharacter(); }, this.CanActivateCharacter);
            this.ResetCharacterStateCommand = new DelegateCommand<object>(this.ResetCharacterState);
            this.AttackTargetCommand = new DelegateCommand<object>(this.TargetCharacterForAttack);
            this.AttackTargetAndExecuteCommand = new DelegateCommand<object>(this.TargetAndExecuteAttack);
            this.ResetOrientationCommand = new DelegateCommand<object>(delegate(object state) { this.ResetOrientation(); }, this.CanResetOrientation);
            this.ToggleRelativePositioningCommand = new DelegateCommand<object>(this.ToggleRelativePositioning);
            this.CycleCommandsThroughCrowdCommand = new DelegateCommand<object>(delegate(object state) { this.CycleCommandsThroughCrowd(); }, this.CanCycleCommandsThroughCrowd);
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
                    AddParticipants(new List<CrowdMemberModel>() { member });
                    CheckIfCharacterExistsInGame(member);
                }
            }
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
                    if (!(this.IsPlayingAttack && this.AttackingCharacter != null && this.AttackingCharacter.Name == member.Name))
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
                        if (!(this.IsPlayingAttack && this.AttackingCharacter != null && this.AttackingCharacter.Name == member.Name)) // Don't make the attacking character blue
                            member.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
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
                        return c.gamePlayer != null && c.gamePlayer.Pointer == currentTargetPointer;
                    }).FirstOrDefault();
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
                if (Keyboard.Modifiers != ModifierKeys.Control && Keyboard.Modifiers != ModifierKeys.Shift)
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
            });
        }
        #endregion

        #region Target Hovered Character

        public Character GetHoveredCharacter(object state)
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
            Character hovered = GetHoveredCharacter(null);
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
            Character hoveredCharacter = GetHoveredCharacter(null);
            if (hoveredCharacter != null && this.TargetOnHover)
            {
                if (lastTargetedCharacter == null || hoveredCharacter.Label != lastTargetedCharacter.Label)
                {
                    hoveredCharacter.Target();
                }
                lastTargetedCharacter = hoveredCharacter;
            }
        }

        #endregion

        #region Drag Drop

        private void ContinueDraggingCharacter()
        {
            MemoryElement targetedBeforeMouseCLick = new MemoryElement();
            CrowdMemberModel hoveredCharacter = (CrowdMemberModel)GetHoveredCharacter(null);
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

        private void DisplayCharacterPopupMenue()
        {
            CrowdMemberModel character = (CrowdMemberModel)GetCurrentTarget();
            desktopContextMenu.GenerateAndDisplay(character, AttackingCharacter != null ? AttackingCharacter.Name : null, IsPlayingAttack);
        }

        #endregion

        #region Move Character to Desktop Position Clicked

        public void MoveCharacterToDesktopPositionClicked()
        {
            Vector3 mouseDirection = new MouseElement().Position;
            Character activeMovementCharacter = Helper.GlobalVariables_CharacterMovement.Character;
            Character target = this.Participants.FirstOrDefault(p => p.Name == activeMovementCharacter.Name) as Character;
            if (target != null)
                target.MoveToLocation(mouseDirection);
        }

        #endregion

        #region Respond to Desktop Click
        public void RespondToMouseClickBasedOnDesktopState()
        {
            timer_RespondToDesktop.Start(); // We need this timer to avoid firing click events when desktop is no longer in focus, as click fires before focus lost. This also fixes double attack play bug.
        }
        void timer_RespondToDesktop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_RespondToDesktop.Stop();
            if (desktopContextMenu.IsDisplayed == false && mouseHandler.IsDesktopActive)
            {
                if (IsPlayingAttack == false)
                {
                    if (CharacterIsMoving == true)
                    {
                        MoveCharacterToDesktopPositionClicked();
                    }
                    else
                        ContinueDraggingCharacter();
                }
                else if (IsPlayingAttack == true)
                {

                    PlayAttackCycle();
                }
            }
            else
                desktopContextMenu.IsDisplayed = false;
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
            Participants.Sort(ListSortDirection.Ascending, new RosterCrowdMemberModelComparer());
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
        public void Spawn()
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.Spawn();
            }
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
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
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ClearFromDesktop();
            }
            bool cyclingEnabled = false;
            if (this.IsCyclingCommandsThroughCrowd && this.SelectedParticipants != null && this.SelectedParticipants.Count == 1)
                cyclingEnabled = true;
            while (SelectedParticipants.Count != 0)
            {
                var participant = SelectedParticipants[0] as CrowdMemberModel;
                SelectNextCharacterInCrowdCycle();
                if (this.ActiveCharacter == participant)
                    this.ActiveCharacter = null;
                Participants.Remove(participant);
                SelectedParticipants.Remove(participant);
                participant.RosterCrowd = null;
                if (cyclingEnabled)
                    break;
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
            foreach (CrowdMemberModel member in SelectedParticipants)
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
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.Place();
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

        #region Target And/Or Follow
        private bool CanTargetAndFollow(object state)
        {
            return CanToggleTargeted(state);
        }
        public void TargetAndFollow(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                member.TargetAndFollow();
            }
            SelectNextCharacterInCrowdCycle();
        }

        public void TargetOrFollow()
        {
            if (this.CanToggleTargeted(null))
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
            if(this.SelectedParticipants.Count > 1)
            {
                // Adjust destination based on relative position
                // need to choose a character that is "center" character. Should be the one that's closest to destination....
                float closestDistance;
                Character closestCharacter = GetClosestCharacter(out closestDistance);
                Vector3 startingPosition = closestCharacter.CurrentPositionVector;
                Vector3 nextReferenceVector = Vector3.Zero;
                List<Vector3> usedUpPositions = new List<Vector3>();
                foreach (Character c in this.SelectedParticipants)
                {
                    
                    if(this.UseRelativePositioning)
                        c.MoveToLocationWithRelativePositioning(new Camera().GetPositionVector(), closestCharacter, startingPosition);
                    else
                        c.MoveToLocationWithOptimalPositioning(new Camera().GetPositionVector(), closestCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                }
            }
            else
            {
                Character member = this.SelectedParticipants[0] as Character;
                member.MoveToCamera();
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
            Character closestCharacter = GetClosestCharacter(out closestDistance);
            Vector3 startingPosition = closestCharacter.CurrentPositionVector;
            Vector3 nextReferenceVector = Vector3.Zero;
            List<Vector3> usedUpPositions = new List<Vector3>();
            foreach (CrowdMemberModel member in SelectedParticipants)
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
            //this.isMoveToCharacterEnabled = true;
            //this.isMoveToMouseLocationEnabled = false;
            foreach (Character c in this.SelectedParticipants)
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
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                (member as Character).ResetOrientation();
            }
            SelectNextCharacterInCrowdCycle();
        }

        private void ActivateDefaultMovementToActivate(object obj)
        {
            Character character = ((Character)SelectedParticipants[0]);

            Vector3 facing = new Vector3();
            if (SelectedParticipants.Count > 1)
            {
                facing = character.CurrentFacingVector;
            }

            if (character.ActiveMovement == null || character.ActiveMovement.IsActive == false)
            {
                foreach (CrowdMemberModel member in SelectedParticipants)
                {
                    character = (Character)member;
                    character.ActiveMovement = character.DefaultMovementToActivate;
                    if (SelectedParticipants.Count > 1)
                    {
                        character.CurrentFacingVector = facing;
                    }
                    if (character.ActiveMovement != null)
                    {
                        if (!character.ActiveMovement.IsActive)
                            character.ActiveMovement.ActivateMovement();
                    }
                }
            }
            else
            {
                foreach (CrowdMemberModel member in SelectedParticipants)
                {
                    character = (Character)member;
                    character.ActiveMovement = character.DefaultMovementToActivate;
                    if (character.ActiveMovement != null)
                        character.ActiveMovement.DeactivateMovement();
                }

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
            if (this.IsCyclingCommandsThroughCrowd && this.SelectedParticipants != null && this.SelectedParticipants.Count == 1)
            {
                this.stopSyncingWithDesktop = true;
                ICrowdMemberModel cNext = null;
                ICrowdMemberModel cCurrent = null;
                cCurrent = this.SelectedParticipants[0] as ICrowdMemberModel;
                var index = this.Participants.IndexOf(cCurrent as ICrowdMemberModel);

                if (index + 1 == this.Participants.Count)
                {
                    cNext = this.Participants.FirstOrDefault(p => p.RosterCrowd.Name == cCurrent.RosterCrowd.Name) as ICrowdMemberModel;
                }
                else
                {
                    cNext = this.Participants[index + 1] as ICrowdMemberModel;
                    if (cNext != null && cNext.RosterCrowd.Name != cCurrent.RosterCrowd.Name)
                    {
                        cNext = this.Participants.FirstOrDefault(p => p.RosterCrowd.Name == cCurrent.RosterCrowd.Name) as ICrowdMemberModel;
                    }
                }

                if (cNext != null && cNext != cCurrent)
                {
                    SelectedParticipants.Clear();
                    SelectedParticipants.Add(cNext);
                    OnPropertyChanged("SelectedParticipants");
                }
            }
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

        #region Activate Character

        private bool CanActivateCharacter(object state)
        {
            //return CanToggleTargeted(state) && (SelectedParticipants[0] as Character).HasBeenSpawned ;
            return !this.IsPlayingAttack;
        }

        private void ActivateCharacter()
        {
            if(CanActivateCharacter(null))
                ToggleActivateCharacter();
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
                if (this.ActiveCharacter == character)
                {
                    DeactivateCharacter(character);
                }
                else
                {
                    if (this.ActiveCharacter != null)
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
            if (Helper.GlobalVariables_CharacterMovement != null && Helper.GlobalVariables_CharacterMovement.Character == this.ActiveCharacter)
            {
                Helper.GlobalVariables_CharacterMovement.IsPaused = true;
                Helper.GlobalVariables_FormerActiveCharacterMovement = Helper.GlobalVariables_CharacterMovement;
            }
            // Deactivate movements from other characters that are not active
            if (Helper.GlobalVariables_CharacterMovement != null && Helper.GlobalVariables_CharacterMovement.Character != this.ActiveCharacter)
            {
                var otherCharacter = Helper.GlobalVariables_CharacterMovement.Character;
                if (otherCharacter != Helper.GlobalVariables_ActiveCharacter)
                {
                    Helper.GlobalVariables_CharacterMovement.DeactivateMovement();
                }
            }
            this.ActiveCharacter = character as CrowdMemberModel;
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
                    if (SelectedParticipants.Count == 0)
                        return;
                    else
                        character = SelectedParticipants[0] as Character;
                if (character == this.ActiveCharacter)
                {
                    // Resume movements from other characters that were paused
                    if (Helper.GlobalVariables_FormerActiveCharacterMovement != null)
                    {
                        Helper.GlobalVariables_FormerActiveCharacterMovement.IsPaused = false;
                        Helper.GlobalVariables_CharacterMovement = Helper.GlobalVariables_FormerActiveCharacterMovement;
                    }
                    this.ActiveCharacter = null;
                    this.eventAggregator.GetEvent<DeactivateCharacterEvent>().Publish(character);
                    SelectNextCharacterInCrowdCycle();
                }

            };
            //Application.Current.Dispatcher.BeginInvoke(action);
        }

        #endregion

        #region Play Default Ability

        public void PlayDefaultAbility()
        {
            Action d = delegate()
            {
                if (!IsPlayingAttack && SelectedParticipants != null && SelectedParticipants.Count == 1)
                {
                    //lastHoveredCharacter != null && previousSelectedCharacter != null && previousSelectedCharacter.HasBeenSpawned
                    Character abilityPlayingCharacter = null;
                    Character abilityTargetCharacter = null;

                    if (lastTargetedCharacter == null) // not clicked any character on desktop
                    {
                        if (RosterMouseDoubleClicked)
                        {
                            if (this.ActiveCharacter != null)
                            {
                                abilityPlayingCharacter = this.ActiveCharacter as Character;
                                if (this.ActiveCharacter.Name != (SelectedParticipants[0] as Character).Name)
                                {
                                    abilityTargetCharacter = SelectedParticipants[0] as Character;
                                }
                            }
                            else if (previousSelectedCharacter != null)
                            {
                                abilityPlayingCharacter = previousSelectedCharacter;
                                if (previousSelectedCharacter.Name != (SelectedParticipants[0] as Character).Name)
                                {
                                    abilityTargetCharacter = SelectedParticipants[0] as Character;
                                }
                            }
                        }
                        else
                        {
                            if (this.ActiveCharacter != null)
                                abilityPlayingCharacter = this.ActiveCharacter as Character;
                            else
                                abilityPlayingCharacter = SelectedParticipants[0] as Character;
                        }
                    }
                    else
                    {
                        if (this.ActiveCharacter != null)
                        {
                            abilityPlayingCharacter = this.ActiveCharacter as Character;
                            if (this.ActiveCharacter.Name != lastTargetedCharacter.Name)
                            {
                                abilityTargetCharacter = lastTargetedCharacter;
                            }
                        }
                        else if (previousSelectedCharacter != null && previousSelectedCharacter.Name != lastTargetedCharacter.Name)
                        {
                            abilityPlayingCharacter = previousSelectedCharacter;
                            abilityTargetCharacter = lastTargetedCharacter;
                        }
                        else
                        {
                            abilityPlayingCharacter = lastTargetedCharacter;
                        }
                    }

                    RosterMouseDoubleClicked = false;

                    if (abilityPlayingCharacter != null)
                    {
                        var ability = abilityPlayingCharacter.DefaultAbilityToActivate;
                        if(ability != null)
                        {
                            abilityPlayingCharacter.Target(false);
                            abilityPlayingCharacter.ActiveIdentity.RenderWithoutAnimation(target: abilityPlayingCharacter);
                            ability.Play();
                        }
                    }

                    if (this.IsPlayingAttack)
                    {
                        if (abilityTargetCharacter != null)
                        {
                            if (targetCharacters.Count == 0)
                            {
                                this.targetCharacters.Add(abilityTargetCharacter);
                                if (this.currentAttack.IsAreaEffect)
                                    abilityTargetCharacter.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                                attackConfig.AttackMode = AttackMode.Defend;
                                attackConfig.AttackEffectOption = AttackEffectOption.None;
                                abilityTargetCharacter.ActiveAttackConfiguration = attackConfig;
                                abilityTargetCharacter.ActiveAttackConfiguration.IsCenterTarget = this.currentAttack.IsAreaEffect ? false : true;
                                if (PopupService.IsOpen("ActiveAttackView") == false)
                                    this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
                            }
                        }
                        else
                        {
                            Vector3 lastMouseDownPosition = new MouseElement().Position; ;
                            AttackDirection direction = new AttackDirection(lastMouseDownPosition);
                            this.currentAttack.AnimateAttack(direction, AttackingCharacter);
                        }
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        #endregion

        #region Attack / Area Attack

        private void PlayAttackCycle()
        {
            CrowdMemberModel character = (CrowdMemberModel)GetHoveredCharacter(null);
            if (this.currentAttack != null && this.AttackingCharacter != null)
            {
                if (character == this.AttackingCharacter || character == null)
                {
                    Vector3 mouseDirection = new MouseElement().Position;
                    AttackDirection direction = new AttackDirection(mouseDirection);
                    this.currentAttack.AnimateAttack(direction, AttackingCharacter);
                }
                else if(!this.IsPlayingAreaEffect)
                {
                    Action d = delegate ()
                    {
                        this.SelectedParticipants.Clear();
                        this.SelectedParticipants.Add(character);
                        this.TargetAndExecuteAttack(null);
                    };
                    Dispatcher.Invoke(d);
                }
            }

        }

        private void InitializeAttackEventHandlers(Attack attack)
        {
            attack.AttackInitiated -= this.Ability_AttackInitiated;
            attack.AttackInitiated += Ability_AttackInitiated;
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

        private void InitiateRosterCharacterAttack(Tuple<Character, Attack> attackInitiatedEventTuple)
        {
            this.targetCharacters = new List<Character>();
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
                this.AttackingCharacter = attackingCharacter;
                // Update character properties - icons in roster should show
                rosterCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None };
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
                if (!this.IsPlayingAttack && currentTarget != null)
                {
                    if (Helper.GlobalVariables_CharacterMovement != null && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                    {
                        Vector3 destination = new Vector3(currentTarget.Position.X, currentTarget.Position.Y, currentTarget.Position.Z);
                        Character activeMovementChar = Helper.GlobalVariables_CharacterMovement.Character;
                        activeMovementChar.MoveToLocation(destination);
                    }
                }
                else if (currentTarget == null) // Cancel attack
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
            List<Character> defendingCharacters = tuple.Item1;
            Attack attack = tuple.Item2;
            foreach (var defender in defendingCharacters)
            {
                // Commenting out following as we need the fxs to be persisted across attacks
                //defender.Deactivate(); // restore original costume
            }
            if(attack.IsAreaEffect)
                attack.AnimateAttackSequence(AttackingCharacter, defendingCharacters);
            else
            {
                foreach (var defender in defendingCharacters)
                {
                    defender.ActiveAttackConfiguration.IsCenterTarget = true;
                    attack.AnimateAttackSequence(AttackingCharacter, new List<Character> { defender});
                }
            }
            this.ResetAttack(defendingCharacters);
        }

        private void CancelAttack(List<Character> defendingCharacters)
        {
            if (this.AttackingCharacter != null)
                this.AttackingCharacter.Deactivate();
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
            this.currentAttack.Stop();

            // Hide attack icon from attacking character
            if (this.AttackingCharacter != null && this.AttackingCharacter.ActiveAttackConfiguration != null)
                this.AttackingCharacter.ActiveAttackConfiguration.AttackMode = AttackMode.None;
            this.AttackingCharacter = null;

            Helper.GlobalVariables_IsPlayingAttack = false;
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this.currentAttack);
            foreach (var defender in defenders)
            {
                defender.ActiveAttackConfiguration.AttackMode = AttackMode.None;
                //if (!this.currentAttack.OnHitAnimation.Persistent)
                //    this.currentAttack.OnHitAnimation.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(defender); });
                //defender.Deactivate(); // restore original costume
            }

            // Update Mouse cursor
            Mouse.OverrideCursor = Cursors.Arrow;

            this.currentAttack = null;
            this.Commands_RaiseCanExecuteChanged();
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
                            defendingCharacter.GhostShadow.Target(false);
                            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                        }
                        defendingCharacter.Target(false);
                        keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                        keyBindsGenerator.CompleteEvent();
                    }
                    // Else make him stand up 
                    else if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
                    {
                        var globalStandUpAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.STANDUP_ABILITY_NAME);
                        if (globalStandUpAbility != null && globalStandUpAbility.AnimationElements != null && globalStandUpAbility.AnimationElements.Count > 0)
                        {
                            globalStandUpAbility.Play(false, defendingCharacter);
                        }
                    }
                    // Update icons in Roster
                    defendingCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackEffectOption = AttackEffectOption.None, AttackMode = AttackMode.None };
                    this.isCharacterReset = true;
                }
            }
        }

        public void TargetCharacterForAttack(object state)
        {
            foreach (var participant in this.SelectedParticipants)
            {
                Character currentTarget = participant as Character;
                if (currentTarget.Name != this.AttackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                {
                    if (this.targetCharacters.FirstOrDefault(tc => tc.Name == currentTarget.Name) == null)
                        this.targetCharacters.Add(currentTarget);
                    currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                    ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                    attackConfig.AttackMode = AttackMode.Defend;
                    attackConfig.AttackEffectOption = AttackEffectOption.None;
                    currentTarget.ActiveAttackConfiguration = attackConfig;
                }
            }
        }

        public void TargetAndExecuteAttack(object state)
        {
            foreach (var participant in this.SelectedParticipants)
            {
                Character currentTarget = participant as Character;
                if (currentTarget.Name != this.AttackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                {
                    if (this.targetCharacters.FirstOrDefault(tc => tc.Name == currentTarget.Name) == null)
                        this.targetCharacters.Add(currentTarget);
                    currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                    ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                    attackConfig.AttackMode = AttackMode.Defend;
                    attackConfig.AttackEffectOption = AttackEffectOption.None;
                    currentTarget.ActiveAttackConfiguration = attackConfig;
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
        public DesktopKeyEventHandler.EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == Constants.ROSTER_EXPLORER)
            {
                if (inputKey == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.Place;
                }
                else if (inputKey == Key.S && Keyboard.Modifiers == (ModifierKeys.Control))
                {
                    return this.SavePosition;
                }
                else if (inputKey == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.Spawn;
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
                else if (inputKey == Key.G && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.MoveTargetToCamera;
                }
                else if (inputKey == Key.Y && Keyboard.Modifiers == (ModifierKeys.Control))
                {
                    return this.CycleCommandsThroughCrowd;
                }
                else if (inputKey == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ActivateCharacter;
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
                    this.ActivateDefaultMovementToActivate(null);
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
            }
            Character targetedCharacter = GetCurrentTarget() as Character;
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
                        cm.ActivateMovement();
                    else
                        cm.DeactivateMovement();
                }
            }
            return null;
        }

        #endregion

    }
}
