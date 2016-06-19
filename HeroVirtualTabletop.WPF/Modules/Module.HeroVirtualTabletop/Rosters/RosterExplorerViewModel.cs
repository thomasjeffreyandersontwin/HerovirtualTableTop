using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Roster
{
    public class RosterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private EventAggregator eventAggregator;

        private bool isPlayingAttack = false;
        private Attack currentAttack = null;
        private Character attackingCharacter = null;
        #endregion

        #region Events

        #endregion

        #region Public Properties
        private HashedObservableCollection<ICrowdMemberModel, string> participants;
        public HashedObservableCollection<ICrowdMemberModel, string> Participants
        {
            get
            {
                if(participants == null)
                    participants = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name, x => x.RosterCrowd.Order, x => x.RosterCrowd.Name, x => x.Name );
                return participants;
            }
            set
            {
                participants = value;
                OnPropertyChanged("Participants");
            }
        }
        private IList selectedParticipants;
        public IList SelectedParticipants
        {
            get
            {
                return selectedParticipants;
            }
            set
            {
                selectedParticipants = value;
                OnPropertyChanged("SelectedParticipants");
                Commands_RaiseCanExecuteChanged();
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
                OnPropertyChanged("ActiveCharacter");
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
        public DelegateCommand<object> ToggleManeuverWithCameraCommand { get; private set; }
        public DelegateCommand<object> EditCharacterCommand { get; private set; }
        public DelegateCommand<object> ActivateCharacterCommand { get; private set; }
        public DelegateCommand<object> ResetCharacterStateCommand { get; private set; }

        #endregion

        #region Constructor

        public RosterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;

            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe(AddParticipants);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(DeleteParticipant);
            this.eventAggregator.GetEvent<CheckRosterConsistencyEvent>().Subscribe(CheckRosterConsistency);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(InitiateRosterCharacterAttack);
            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Subscribe(this.LaunchActiveAttack);
            InitializeCommands();

        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.SpawnCommand = new DelegateCommand<object>(this.Spawn);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(this.ClearFromDesktop, this.CanClearFromDesktop);
            this.ToggleTargetedCommand = new DelegateCommand<object>(this.ToggleTargeted, this.CanToggleTargeted);
            this.SavePositionCommand = new DelegateCommand<object>(this.SavePostion, this.CanSavePostion);
            this.PlaceCommand = new DelegateCommand<object>(this.Place, this.CanPlace);
            this.TargetAndFollowCommand = new DelegateCommand<object>(this.TargetAndFollow, this.CanTargetAndFollow);
            this.MoveTargetToCameraCommand = new DelegateCommand<object>(this.MoveTargetToCamera, this.CanMoveTargetToCamera);
            this.ToggleManeuverWithCameraCommand = new DelegateCommand<object>(this.ToggleManeuverWithCamera, this.CanToggleManeuverWithCamera);
            this.EditCharacterCommand = new DelegateCommand<object>(this.EditCharacter, this.CanEditCharacter);
            this.ActivateCharacterCommand = new DelegateCommand<object>(this.ActivateCharacter);
            this.ResetCharacterStateCommand = new DelegateCommand<object>(this.ResetCharacterState);
        }

        #endregion

        #region Methods

        private void Commands_RaiseCanExecuteChanged()
        {
            this.ClearFromDesktopCommand.RaiseCanExecuteChanged();
            this.ToggleTargetedCommand.RaiseCanExecuteChanged();
            this.SavePositionCommand.RaiseCanExecuteChanged();
            this.PlaceCommand.RaiseCanExecuteChanged();
            this.TargetAndFollowCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCameraCommand.RaiseCanExecuteChanged();
            this.ToggleManeuverWithCameraCommand.RaiseCanExecuteChanged();
            this.EditCharacterCommand.RaiseCanExecuteChanged();
        }
        
        private void CheckRosterConsistency(IEnumerable<CrowdMemberModel> members)
        {
            foreach (CrowdMemberModel member in members)
            {
                if (!Participants.Contains(member))
                {
                    AddParticipants(new List<CrowdMemberModel>() { member });
                }
            }
        }

        public ICrowdMemberModel GetCurrentTarget()
        {
            MemoryElement target = new MemoryElement();
            return this.Participants.FirstOrDefault((x) => {
                return (x as CrowdMemberModel).Label == target.Label; });
        }

        #region Add Participants
        private void AddParticipants(IEnumerable<CrowdMemberModel> crowdMembers)
        {
            foreach (var crowdMember in crowdMembers)
            {
                Participants.Add(crowdMember);
                CheckIfCharacterExistsInGame(crowdMember);
            }
            Participants.Sort();
        }

        private void CheckIfCharacterExistsInGame(CrowdMemberModel crowdMember)
        {
            MemoryElement oldTargeted = new MemoryElement();
            crowdMember.Target();
            MemoryElement currentTargeted = new MemoryElement();
            if (currentTargeted.Label == crowdMember.Label)
            {
                crowdMember.SetAsSpawned();
            }
            try
            {
                oldTargeted.Target();
            }
            catch { }
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
            this.ClearFromDesktop(null);
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }
        #endregion

        #region Spawn
        private void Spawn(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.Spawn();
            }
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region Clear from Desktop
        private bool CanClearFromDesktop(object state)
        {
            if (SelectedParticipants != null && SelectedParticipants.Count > 0)
            {
                return true;
            }
            return false;
        }

        private void ClearFromDesktop(object state)
        {           
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ClearFromDesktop();
            }
            while (SelectedParticipants.Count != 0)
            {
                var participant = SelectedParticipants[0] as CrowdMemberModel;
                Participants.Remove(participant);
                SelectedParticipants.Remove(participant);
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
        private void SavePostion(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.SavePosition();
            }
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
        private void Place(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.Place();
            }
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region ToggleTargeted
        
        private bool CanToggleTargeted(object state)
        {
            bool canToggleTargeted = false;
            if (this.SelectedParticipants != null && SelectedParticipants.Count == 1 && (SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned)
            {
                canToggleTargeted = true;
            }
            return canToggleTargeted;
        }

        private void ToggleTargeted(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ToggleTargeted();
            }
        }

        #endregion

        #region Target And Follow

        private bool CanTargetAndFollow(object state)
        {
            return CanToggleTargeted(state);
        }

        private void TargetAndFollow(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.TargetAndFollow();
            }
        }

        public void TargetOrFollow()
        {

            if (this.CanToggleTargeted(null))
            {
                CrowdMemberModel member = SelectedParticipants[0] as CrowdMemberModel;
                if (member.IsTargeted)
                    member.TargetAndFollow();
                else
                    member.ToggleTargeted();

                if(this.isPlayingAttack)
                {
                    if(member.Name != this.attackingCharacter.Name)
                        this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<Character, Attack>(member as Character, this.currentAttack));
                } 
            }
        }
        public void TargetAndFollow()
        {
            if(this.CanTargetAndFollow(null))
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
                if (!member.HasBeenSpawned)
                {
                    canMoveTargetToCamera = false;
                    break;
                }
            }
            return canMoveTargetToCamera;
        }

        private void MoveTargetToCamera(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.MoveToCamera();
            }
        }

        #endregion

        #region ToggleManeuverWithCamera


        private bool CanToggleManeuverWithCamera(object arg)
        {
            bool canManeuverWithCamera = false;
            if (this.SelectedParticipants != null && SelectedParticipants.Count == 1 && ((SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned || (SelectedParticipants[0] as CrowdMemberModel).ManeuveringWithCamera))
            {
                canManeuverWithCamera = true;
            }
            return canManeuverWithCamera;
        }

        private void ToggleManeuverWithCamera(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ToggleManueveringWithCamera();
            }
            Commands_RaiseCanExecuteChanged();
        }

        public void ToggleManueverWithCamera()
        {
            if(this.CanToggleManeuverWithCamera(null))
            {
                this.ToggleManeuverWithCamera(null);
            }
        }

        #endregion

        #region Edit Character

        private bool CanEditCharacter(object state)
        {
            return this.SelectedParticipants != null && this.SelectedParticipants.Count == 1;
        }

        private void EditCharacter(object state)
        {
            CrowdMemberModel c = this.SelectedParticipants[0] as CrowdMemberModel;
            this.eventAggregator.GetEvent<EditCharacterEvent>().Publish(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(c, null));
        }

        #endregion

        #region Activate Character

        private bool CanActivateCharacter(object state)
        {
            return CanToggleTargeted(state);
        }

        private void ActivateCharacter(object state)
        {
            this.ActiveCharacter = SelectedParticipants[0] as CrowdMemberModel;
            this.eventAggregator.GetEvent<ActivateCharacterEvent>().Publish(this.ActiveCharacter as Character);
        }

        #endregion

        #region Attack Animations

        private void InitiateRosterCharacterAttack(Tuple<Character, Attack> attackInitiatedEventTuple)
        {
            Character attackingCharacter = attackInitiatedEventTuple.Item1;
            Attack attack = attackInitiatedEventTuple.Item2;
            CrowdMemberModel rosterCharacter = this.Participants.FirstOrDefault(p => p.Name == attackingCharacter.Name) as CrowdMemberModel;
            if (rosterCharacter != null && attack != null)
            {
                this.isPlayingAttack = true;
                this.currentAttack = attack;
                this.attackingCharacter = attackingCharacter;
                // Update character properties - icons in roster should show
                ActiveAttackConfiguration activeAttack = new ActiveAttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None };
                rosterCharacter.ActiveAttackConfiguration = activeAttack;
            }
        }

        private void LaunchActiveAttack(Tuple<Character, ActiveAttackConfiguration, Attack> tuple)
        {
            Attack attack = tuple.Item3;
            Character targetCharacter = tuple.Item1;
            ActiveAttackConfiguration attackConfig = tuple.Item2;
            CrowdMemberModel rosterCharacter = this.Participants.FirstOrDefault(p => p.Name == targetCharacter.Name) as CrowdMemberModel;
            if(attackConfig.AttackResult == AttackResultOption.Hit)
            {
                ActiveAttackConfiguration activeAttack = new ActiveAttackConfiguration { AttackMode = AttackMode.Defend, AttackEffectOption = attackConfig.AttackEffectOption };
                targetCharacter.ActiveAttackConfiguration = activeAttack;
            }
            attack.AnimateAttackSequence(attackingCharacter, targetCharacter, attackConfig);
            this.CompleteAttack(null);
        }

        private void CompleteAttack(object state)
        {
            this.isPlayingAttack = false;
            this.currentAttack = null;
            this.attackingCharacter = null;
        }

        private void ResetCharacterState(object state)
        {
            if(this.SelectedParticipants != null && this.SelectedParticipants.Count ==1)
            {
                Character rosterCharacter = this.SelectedParticipants[0] as Character;
                rosterCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.None, AttackEffectOption = AttackEffectOption.None };
                // Write other necessary commands to restore character to normal state in the game
            }
        }

        #endregion

        #endregion
    }
}
