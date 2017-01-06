using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Sevices;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Module.HeroVirtualTabletop.Roster
{
    public class Roster : NotifyPropertyChanged // , IRoster
    {
        #region Private Variables

        private ITargetObserver targetObserver; //DI

        private bool _isCharacterReset = false;

        public bool IsCharacterDragDropInProgress = false;
        private Character _currentDraggingCharacter = null;
        private DateTime _lastDesktopMouseDownTime = DateTime.MinValue;

        private bool _stopSyncingWithDesktop = false;

        private List<Character> _targetCharactersForMove = new List<Character>();
        private List<Character> _targetCharacters = new List<Character>();
        private List<CrowdMemberModel> _oldSelection = new List<CrowdMemberModel>();

        Character lastTargetedCharacter = null;
        private Character previousSelectedCharacter = null;
        public bool RosterMouseDoubleClicked = false;

        #endregion

        #region Events

        public event EventHandler CharacterActionExecuted;
        public void OnCharacterActionExecuted(object sender, EventArgs e)
        {
            if (CharacterActionExecuted != null)
                CharacterActionExecuted(sender, e);
        }

        public event EventHandler RosterSaved;
        public void OnRosterSaved(object sender, EventArgs e)
        {
            if (RosterSaved != null)
                RosterSaved(sender, e);
        }

        public event EventHandler RosterSelectionChanged;
        public void OnRosterSelectionChanged(object sender, EventArgs e)
        {
            if (RosterSelectionChanged != null)
                RosterSelectionChanged(sender, e);
        }

        public event EventHandler RosterMemberAdded;
        public void OnRosterMemberAdded(object sender, EventArgs e)
        {
            if (RosterMemberAdded != null)
                RosterMemberAdded(sender, e);
        }

        #endregion

        #region Public Properties
        public bool IsCharacterMoving
        {
            get
            {
                return Helper.GlobalVariables_CharacterMovement != null;
            }
        }

        public bool IsAnyCharacterSelected
        {
            get
            {
                return SelectedParticipants != null && SelectedParticipants.Count > 0;
            }
        }

        public bool IsEverySelectedCharacterSpawned
        {
            get
            {
                
                bool isEverySelectedCharacterSpawned = true;
                if (this.SelectedParticipants != null)
                {
                    foreach (CrowdMemberModel member in SelectedParticipants)
                    {
                        if (!member.IsSyncedWithGame)
                            SyncRosterMemeberWithGame(member);
                        isEverySelectedCharacterSpawned &= member.HasBeenSpawned;
                    }
                }
                else
                    isEverySelectedCharacterSpawned = false;
                return isEverySelectedCharacterSpawned;
            }
        }

        public bool IsAnySelectedCharacterSpawned
        {
            get
            {
                bool isAnySelectedCharacterSpawned = false;
                if (this.SelectedParticipants != null)
                {
                    foreach (CrowdMemberModel member in this.SelectedParticipants)
                    {
                        if (!member.IsSyncedWithGame)
                            SyncRosterMemeberWithGame(member);
                        if (member.HasBeenSpawned)
                        {
                            isAnySelectedCharacterSpawned = true;
                            break;
                        }
                    }
                }
                return isAnySelectedCharacterSpawned;
            }
        }
        public bool IsSingleSpawnedCharacterSelected
        {
            get
            {
                return this.SelectedParticipants != null && SelectedParticipants.Count == 1 && (SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned;
            }
        }

        private DesktopContextMenu DesktopContextMenu;
        
        //refactor hide behind Roster model class, including all logic
        private HashedObservableCollection<ICrowdMemberModel, string> participants;
        public HashedObservableCollection<ICrowdMemberModel, string> Participants
        {
            get
            {
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
                MemoryElement targetedBeforeMouseClick = new MemoryElement();// REFACTOR?
                selectedParticipants = value;
                SynchSelectionWithGame();
                OnPropertyChanged("SelectedParticipants");
                OnPropertyChanged("ShowAttackContextMenu"); // REFACTOR
                this.TargetOrFollow(); // REFACTOR
                this.RosterSelectionChanged(this, null);
                MemoryElement target = new MemoryElement();// REFACTOR?
                if (target.Label != "" && targetedBeforeMouseClick.Label != target.Label)
                {
                    if (targetedBeforeMouseClick.Label != "")
                        previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == targetedBeforeMouseClick.Label) as Character;
                    else
                        previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == target.Label) as Character;
                }
            }
        }

        public ICrowdMemberModel CurrentTarget
        {
            get
            {
                MemoryElement target = new MemoryElement();
                return this.Participants.FirstOrDefault((x) =>
                {
                    return (x as CrowdMemberModel).Label == target.Label;
                });
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

        #endregion

        #region Listen For Target Changed

        public void ListenForTargetChanged()
        {
            this.targetObserver.TargetChanged += TargetObserver_TargetChanged;
        }

        public void StopListeningForTargetChanged()
        {
            this.targetObserver.TargetChanged -= TargetObserver_TargetChanged;
        }

        #endregion

        #region Constructor

        public Roster() // Inject dependencies here
        {
            if (participants == null)
                participants = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name, x => x.RosterCrowd.Order, x => x.RosterCrowd.Name, x => x.Name);
        }

        #endregion

        //refactor to desktop class
        #region Desktop Interaction

        // REFACTOR with Desktop and take some code back to VM - the control key stuff
        public void AddDesktopTargetToRosterSelection(CrowdMemberModel currentTarget)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                if (SelectedParticipants == null)
                    SelectedParticipants = new ObservableCollection<object>() as IList;
                if (currentTarget == null)
                    return;
                if ((bool)Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Func<bool>(() => { return Keyboard.Modifiers != ModifierKeys.Control; })))
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        if (SelectedParticipants != null && !this._stopSyncingWithDesktop)
                            SelectedParticipants.Clear();
                    });
                }
                if (!SelectedParticipants.Contains(currentTarget))
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        if (!_stopSyncingWithDesktop)
                        {
                            SelectedParticipants.Add(currentTarget);
                            OnPropertyChanged("SelectedParticipants");
                        }
                        else
                        {
                            this._stopSyncingWithDesktop = false;
                        }
                    });
                }
            });
        }
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
                if (target.Label != "" && targetedBeforeMouseCLick.Label != target.Label)
                {
                    return CurrentTarget as Character;
                }
            }
            return null;
        }
        public void TargetHoveredCharacter()
        {
            Character hoveredCharacter = GetHoveredCharacter(null);
            if (hoveredCharacter != null)
            {
                if (lastTargetedCharacter == null || hoveredCharacter.Label != lastTargetedCharacter.Label)
                {
                    hoveredCharacter.Target();
                }
                lastTargetedCharacter = hoveredCharacter;
            }
        }
        private void DropDraggedCharacter()
        {
            if (_currentDraggingCharacter != null && _lastDesktopMouseDownTime != DateTime.MinValue && IsCharacterDragDropInProgress)
            {
                System.Threading.Thread.Sleep(500);

                Vector3 mouseUpPosition = new MouseElement().Position;
                if (!_isPlayingAttack)
                {
                    if (!_currentDraggingCharacter.HasBeenSpawned)
                    {
                        _currentDraggingCharacter.Spawn();
                    }
                    if (Vector3.Distance(_currentDraggingCharacter.CurrentPositionVector, mouseUpPosition) > 5)
                    {
                        _currentDraggingCharacter.UnFollow();
                        _currentDraggingCharacter.MoveToLocation(mouseUpPosition);
                    }
                }
            }
            _currentDraggingCharacter = null;
            _lastDesktopMouseDownTime = DateTime.MinValue;
            IsCharacterDragDropInProgress = false;
        }
        public void MoveCharacterToDesktopPositionClicked()
        {
            Vector3 mouseDirection = new MouseElement().Position;
            Character activeMovementCharacter = Helper.GlobalVariables_CharacterMovement.Character;
            Character target = this.Participants.FirstOrDefault(p => p.Name == activeMovementCharacter.Name) as Character;
            if (target != null)
                target.MoveToLocation(mouseDirection);
        }
        private void ContinueDraggingCharacter()
        {
            MemoryElement targetedBeforeMouseCLick = new MemoryElement();
            CrowdMemberModel hoveredCharacter = (CrowdMemberModel)GetHoveredCharacter(null);
            if (hoveredCharacter != null)
            {
                this._currentDraggingCharacter = hoveredCharacter;
                this.IsCharacterDragDropInProgress = true;
                _lastDesktopMouseDownTime = DateTime.UtcNow;
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
        #endregion

        #region Private Methods

        private void ExecuteRosterCharacterAction(RosterCharacterAction action)
        {
            foreach (CrowdMemberModel member in this.SelectedParticipants)
            {
                switch (action)
                {
                    case RosterCharacterAction.Spawn:
                        member.Spawn();
                        break;
                    case RosterCharacterAction.Place:
                        member.Place();
                        break;
                    case RosterCharacterAction.SavePosition:
                        member.SavePosition();
                        break;
                    case RosterCharacterAction.ClearFromDesktop:
                        member.ClearFromDesktop();
                        break;
                    case RosterCharacterAction.MoveTargetToCamera:
                        member.MoveToCamera();
                        break;
                    case RosterCharacterAction.MoveTargetToCharacter:

                        break;
                    case RosterCharacterAction.MoveTargetToMouseLocation:
                        //member.MoveToLocation();
                        break;
                    case RosterCharacterAction.Activate:
                        member.Activate();
                        break;
                    case RosterCharacterAction.PlayDefaultAbility:

                        break;
                    case RosterCharacterAction.ResetOrientation:
                        member.ResetOrientation();
                        break;
                    case RosterCharacterAction.ToggleTargeted:
                        if (!member.IsSyncedWithGame)
                            SyncRosterMemeberWithGame(member);
                        member.ToggleTargeted();
                        break;
                    case RosterCharacterAction.TargetAndFollow:
                        member.TargetAndFollow();
                        break;
                    case RosterCharacterAction.ToggleManueverWithCamera:
                        if (!member.IsSyncedWithGame)
                            SyncRosterMemeberWithGame(member);
                        member.ToggleManueveringWithCamera();
                        break;
                }
            }
            if (action == RosterCharacterAction.ClearFromDesktop)
                UpdateParticipantsAfterClearFromDesktop();
            else
                SelectNextCharacterInCrowdCycle();
            // Now fire an event that UI layer (viewmodel) can handle
            OnCharacterActionExecuted(this, null);
        }

        private void CheckRosterConsistency(IEnumerable<CrowdMemberModel> members)
        {
            foreach (CrowdMemberModel member in members)
            {
                if (!Participants.Contains(member))
                {
                    AddParticipants(new List<CrowdMemberModel>() { member });
                    SyncRosterMemeberWithGame(member);
                }
            }
        }

        private void SynchSelectionWithGame()
        {
            List<CrowdMemberModel> unselected = _oldSelection.Except(SelectedParticipants.Cast<CrowdMemberModel>()).ToList();
            unselected.ForEach(
                (member) =>
                {
                    if (!member.HasBeenSpawned)
                        return;
                    if (!(this._isPlayingAttack && this.AttackingCharacter != null && this.AttackingCharacter.Name == member.Name))
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
                        if (!(this._isPlayingAttack && this.AttackingCharacter != null && this.AttackingCharacter.Name == member.Name)) // Don't make the attacking character blue
                            member.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                        _oldSelection.Add(member);
                    });
        }

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
            this.SaveRoster();
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

        private void SyncRosterMemeberWithGame(CrowdMemberModel crowdMember)
        {
            StopListeningForTargetChanged();
            crowdMember.SyncWithGame();
            ListenForTargetChanged();
        }

        #endregion

        #region Add Participants
        public void AddParticipants(IEnumerable<CrowdMemberModel> crowdMembers)
        {
            foreach (var crowdMember in crowdMembers)
            {
                Participants.Add(crowdMember);
                InitializeAttackEventHandlers(crowdMember);
            }
            Participants.Sort(ListSortDirection.Ascending, new RosterCrowdMemberModelComparer());
            OnRosterMemberAdded(null, null);
        }

        #endregion

        // Refactor Roster Roster.Spawn same with below methods

        #region Roster Save

        public void SaveRoster()
        {
            RosterSaved(this, null);
        }

        #endregion

        #region Spawn
        public void Spawn()
        {
            ExecuteRosterCharacterAction(RosterCharacterAction.Spawn);
        }

        #endregion

        #region Clear from Desktop
        public bool CanClearFromDesktop(object state)
        {
            return IsAnyCharacterSelected && !_isPlayingAttack;
        }
        public void ClearFromDesktop()
        {
            ExecuteRosterCharacterAction(RosterCharacterAction.ClearFromDesktop);         
            this.SaveRoster();
        }

        private void UpdateParticipantsAfterClearFromDesktop()
        {
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
        }
        #endregion

        #region Save Positon

        public bool CanSavePostion(object state)
        {
            return IsAnySelectedCharacterSpawned;
        }
        public void SavePosition()
        {
            ExecuteRosterCharacterAction(RosterCharacterAction.SavePosition);
            this.SaveRoster();
        }

        #endregion

        #region Place
        public bool CanPlace(object state)
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
            ExecuteRosterCharacterAction(RosterCharacterAction.Place);
        }
        #endregion

        #region ToggleTargeted

        private bool CanToggleTargeted(object state)
        {
            return IsSingleSpawnedCharacterSelected;
        }

        public void ToggleTargeted()
        {
            ExecuteRosterCharacterAction(RosterCharacterAction.ToggleTargeted);
        }

        #endregion

        #region Target And Follow
        public bool CanTargetAndFollow(object state)
        {
            return IsSingleSpawnedCharacterSelected;
        }
        public void TargetAndFollow()
        {
            ExecuteRosterCharacterAction(RosterCharacterAction.TargetAndFollow);
        }

        public void TargetOrFollow()
        {
            if (IsSingleSpawnedCharacterSelected)
            {
                //refactor this code to the roster and character
                CrowdMemberModel member = SelectedParticipants[0] as CrowdMemberModel;
                if (!member.IsSyncedWithGame)
                    SyncRosterMemeberWithGame(member);
                if (member.IsTargeted)
                {
                    member.Target();
                    this._isCharacterReset = false; // reset this flag
                }
                else
                    member.ToggleTargeted();

                if (this._isPlayingAttack)
                {
                    if (member.Name != this.AttackingCharacter.Name && member.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && member.Name != Constants.DEFAULT_CHARACTER_NAME)
                    {
                        if (!DesktopContextMenu.IsPlayingAreaEffect) // change to this._isPlayingAreaEffect
                        {
                            if (this._targetCharacters.FirstOrDefault(tc => tc.Name == member.Name) == null)
                                this._targetCharacters.Add(member);
                            ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                            attackConfig.AttackMode = AttackMode.Defend;
                            attackConfig.AttackEffectOption = AttackEffectOption.None;
                            member.ActiveAttackConfiguration = attackConfig;
                            member.ActiveAttackConfiguration.IsCenterTarget = true;
                            if (PopupService.IsOpen("ActiveAttackView") == false)
                                this.EventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this._targetCharacters, this._currentAttack));
                        }
                    }
                }
            }
        }

        #endregion

        #region Move Target to Camera

        private bool CanMoveTargetToCamera(object arg)
        {
            return IsEverySelectedCharacterSpawned;
        }

        public void MoveTargetToCamera()
        {
            this.ExecuteRosterCharacterAction(RosterCharacterAction.MoveTargetToCamera);
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
                        SyncRosterMemeberWithGame(member);
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
                this._targetCharactersForMove.Add(c);
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
                        SyncRosterMemeberWithGame(member);
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
            return IsAnySelectedCharacterSpawned;
        }

        public void ResetOrientation()
        {
            this.ExecuteRosterCharacterAction(RosterCharacterAction.ResetOrientation);
        }

        #endregion

        #region Activate Default Movement

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
            return IsAnyCharacterSelected && ((SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned || (SelectedParticipants[0] as CrowdMemberModel).ManeuveringWithCamera);
        }
        public void ToggleManeuverWithCamera()
        {
            ExecuteRosterCharacterAction(RosterCharacterAction.ToggleManueverWithCamera);
        }

        #endregion

        #region Cycle Through Crowd

        //refactor these are unecessary
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
            if (this.IsCyclingCommandsThroughCrowd && IsAnyCharacterSelected)
            {
                this._stopSyncingWithDesktop = true;
                ICrowdMemberModel cNext = null;
                ICrowdMemberModel cCurrent = null;
                cCurrent = this.SelectedParticipants[0] as ICrowdMemberModel;
                var index = this.Participants.IndexOf(cCurrent as ICrowdMemberModel);

                if (index + 1 == this.Participants.Count)
                {
                    cNext = this.Participants.FirstOrDefault(p => p.RosterCrowd == cCurrent.RosterCrowd) as ICrowdMemberModel;
                }
                else
                {
                    cNext = this.Participants[index + 1] as ICrowdMemberModel;
                    if (cNext != null && cNext.RosterCrowd != cCurrent.RosterCrowd)
                    {
                        cNext = this.Participants.FirstOrDefault(p => p.RosterCrowd == cCurrent.RosterCrowd) as ICrowdMemberModel;
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

        #region Activate Character

        private bool CanActivateCharacter(object state)
        {
            return IsSingleSpawnedCharacterSelected && !this._isPlayingAttack;
        }

        private void ActivateCharacter()
        {
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
            this.EventAggregator.GetEvent<ActivateCharacterEvent>().Publish(new Tuple<Character, string, string>(character, selectedOptionGroupName, selectedOptionName));
            SelectNextCharacterInCrowdCycle();
        }

        private void DeactivateCharacter(Character character = null)
        {
            Action action = delegate()
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
                    this.EventAggregator.GetEvent<DeactivateCharacterEvent>().Publish(character);
                    SelectNextCharacterInCrowdCycle();
                }

            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        #endregion

        #region Play Default Ability
        //refactor down to roster and / or animated character
        public void PlayDefaultAbility()
        {
            Action d = delegate()
            {
                if (SelectedParticipants != null && SelectedParticipants.Count == 1)
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
                        ability.Play();
                    }

                    if (this._isPlayingAttack)
                    {
                        if (abilityTargetCharacter != null)
                        {
                            if (_targetCharacters.Count == 0)
                            {
                                this._targetCharacters.Add(abilityTargetCharacter);
                                if (this._currentAttack.IsAreaEffect)
                                    abilityTargetCharacter.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                                attackConfig.AttackMode = AttackMode.Defend;
                                attackConfig.AttackEffectOption = AttackEffectOption.None;
                                abilityTargetCharacter.ActiveAttackConfiguration = attackConfig;
                                abilityTargetCharacter.ActiveAttackConfiguration.IsCenterTarget = this._currentAttack.IsAreaEffect ? false : true;
                                if (PopupService.IsOpen("ActiveAttackView") == false)
                                    this.EventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this._targetCharacters, this._currentAttack));
                            }
                        }
                        else
                        {
                            Vector3 lastMouseDownPosition = new MouseElement().Position; ;
                            AttackDirection direction = new AttackDirection(lastMouseDownPosition);
                            this._currentAttack.AnimateAttack(direction, AttackingCharacter);
                        }
                    }
                }
                else if (SelectedParticipants != null && SelectedParticipants.Count == 1)
                {

                }
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        #endregion

        #region Attack / Area Attack

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
                Mouse.OverrideCursor = cursor;
                // Inform Roster to update attacker
                this.EventAggregator.GetEvent<AttackInitiatedEvent>().Publish(new Tuple<Character, Attack>(targetCharacter, customEventArgs.Value));
            }
        }

        private void InitiateRosterCharacterAttack(Tuple<Character, Attack> attackInitiatedEventTuple)
        {
            this._targetCharacters = new List<Character>();
            Character attackingCharacter = attackInitiatedEventTuple.Item1;
            Attack attack = attackInitiatedEventTuple.Item2;
            CrowdMemberModel rosterCharacter = this.Participants.FirstOrDefault(p => p.Name == attackingCharacter.Name) as CrowdMemberModel;
            if (rosterCharacter != null && attack != null)
            {
                this._isPlayingAttack = true;
                if (attack.IsAreaEffect)
                {
                    DesktopContextMenu.IsPlayingAreaEffect = true;
                }
                Commands_RaiseCanExecuteChanged();
                targetObserver.TargetChanged += RosterTargetUpdated;
                this._currentAttack = attack;
                this.AttackingCharacter = attackingCharacter;
                // Update character properties - icons in roster should show
                rosterCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None };
            }
        }
        //Refactor State / logic the attacking character (ie AttackingCharacter.CurrentAttack)
        private Attack _currentAttack = null;
        public Character AttackingCharacter = null;
        private bool _isPlayingAttack = false;
        private bool _isPlayingAreaEffect = false;
        private void PlayAttackCycle()
        {
            //CrowdMemberModel character  = (CrowdMemberModel)GetHoveredCharacter(null);    
            if (this._currentAttack != null && this.AttackingCharacter != null)
            {
                if (this._isPlayingAttack)
                {
                    //refactor to AttackingCharacter.AnimateAttack();
                    Vector3 mouseDirection = new MouseElement().Position;
                    AttackDirection direction = new AttackDirection(mouseDirection);
                    this._currentAttack.AnimateAttack(direction, AttackingCharacter);
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
                if (this._isPlayingAttack && currentTarget != null)
                {
                    if (currentTarget.Name != this.AttackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                    {
                        if (!DesktopContextMenu.IsPlayingAreaEffect)
                        {
                            if (_targetCharacters.Count == 0)// choose only one character for vanilla attack
                            {
                                this._targetCharacters.Add(currentTarget);
                                //currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                                attackConfig.AttackMode = AttackMode.Defend;
                                attackConfig.AttackEffectOption = AttackEffectOption.None;
                                currentTarget.ActiveAttackConfiguration = attackConfig;
                                currentTarget.ActiveAttackConfiguration.IsCenterTarget = true;
                                if (PopupService.IsOpen("ActiveAttackView") == false)
                                    this.EventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this._targetCharacters, this._currentAttack));
                            }
                        }
                    }
                }
                else if (!this._isPlayingAttack && currentTarget != null)
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
                    this.CancelActiveAttack(this._currentAttack);
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void CancelActiveAttack(object state)
        {
            Action action = delegate()
            {
                if (this._isPlayingAttack)
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
                defender.Deactivate(); // restore original costume
            }
            attack.AnimateAttackSequence(AttackingCharacter, defendingCharacters);
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
            this._isPlayingAttack = false;
            DesktopContextMenu.IsPlayingAreaEffect = false;
            targetObserver.TargetChanged -= RosterTargetUpdated;
            //this.currentAttack.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(); });
            this._currentAttack.Stop();

            // Hide attack icon from attacking character
            if (this.AttackingCharacter != null && this.AttackingCharacter.ActiveAttackConfiguration != null)
                this.AttackingCharacter.ActiveAttackConfiguration.AttackMode = AttackMode.None;
            this.AttackingCharacter = null;

            Helper.GlobalVariables_IsPlayingAttack = false;
            this.EventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this._currentAttack);
            foreach (var defender in defenders)
            {
                defender.ActiveAttackConfiguration.AttackMode = AttackMode.None;
                //if (!this.currentAttack.OnHitAnimation.Persistent)
                //    this.currentAttack.OnHitAnimation.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(defender); });
                //defender.Deactivate(); // restore original costume
            }

            // Update Mouse cursor
            Mouse.OverrideCursor = Cursors.Arrow;

            this._currentAttack = null;
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
                    this._isCharacterReset = true;
                }
            }
        }

        public void TargetCharacterForAreaAttack(object state)
        {
            foreach (var participant in this.SelectedParticipants)
            {
                Character currentTarget = participant as Character;
                if (currentTarget.Name != this.AttackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                {
                    if (this._targetCharacters.FirstOrDefault(tc => tc.Name == currentTarget.Name) == null)
                        this._targetCharacters.Add(currentTarget);
                    currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                    ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                    attackConfig.AttackMode = AttackMode.Defend;
                    attackConfig.AttackEffectOption = AttackEffectOption.None;
                    currentTarget.ActiveAttackConfiguration = attackConfig;
                }
            }
        }

        public void TargetAndExecuteAreaAttack(object state)
        {
            foreach (var participant in this.SelectedParticipants)
            {
                Character currentTarget = participant as Character;
                if (currentTarget.Name != this.AttackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                {
                    if (this._targetCharacters.FirstOrDefault(tc => tc.Name == currentTarget.Name) == null)
                        this._targetCharacters.Add(currentTarget);
                    currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                    ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                    attackConfig.AttackMode = AttackMode.Defend;
                    attackConfig.AttackEffectOption = AttackEffectOption.None;
                    currentTarget.ActiveAttackConfiguration = attackConfig;
                }
            }
            if (PopupService.IsOpen("ActiveAttackView") == false)
                this.EventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this._targetCharacters, this._currentAttack));
        }

        public bool ShowAttackContextMenu
        {
            get
            {
                bool showAttackContextMenu = false;
                if (DesktopContextMenu.IsPlayingAreaEffect && this.SelectedParticipants != null && this.SelectedParticipants.Count > 0)
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
    }

    public enum RosterCharacterAction
    {
        Spawn,
        Place,
        SavePosition,
        ClearFromDesktop,
        ToggleTargeted,
        TargetAndFollow,
        MoveTargetToCamera,
        MoveTargetToCharacter,
        MoveTargetToMouseLocation,
        ResetOrientation,
        ToggleManueverWithCamera,
        Activate,
        PlayDefaultAbility
    }
}
