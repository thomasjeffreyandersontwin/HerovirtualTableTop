using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.OptionGroups;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Movements
{
    public class Movement: CharacterOption
    {
        [JsonConstructor]
        private Movement() { }

        public Movement(string name)
        {
            this.Name = name;
            this.AddDefaultMemberAbilities();
        }

        private ObservableCollection<MovementMember> movmementMembers;
        public ObservableCollection<MovementMember> MovementMembers
        {
            get
            {
                return movmementMembers;
            }
            set
            {
                movmementMembers = value;
                OnPropertyChanged("MovementMembers");
            }
        }

        public void Move(MovementDirection direction, double distance, Character target)
        {

        }

        public void MoveBasedOnKey(string key, Character target)
        {

        }

        public void MoveToLocation(IMemoryElementPosition destination, Character target)
        {

        }

        public void Turn(MovementDirection direction, Character target)
        {

        }

        public void TurnBasedOnKey(string key, Character target)
        {

        }

        private void AddDefaultMemberAbilities()
        {
            if (this.MovementMembers == null || this.MovementMembers.Count == 0)
            {
                this.MovementMembers = new ObservableCollection<MovementMember>();

                MovementMember movementMemberRight = new MovementMember { MovementDirection = MovementDirection.Right, MemberName = "Move Right" };
                movementMemberRight.MemberAbility = new ReferenceAbility("Move Right", null);
                movementMemberRight.MemberAbility.DisplayName = "Move Right";
                this.MovementMembers.Add(movementMemberRight);

                MovementMember movementMemberLeft = new MovementMember { MovementDirection = MovementDirection.Left, MemberName = "Move Left" };
                movementMemberLeft.MemberAbility = new ReferenceAbility("Move Left", null);
                movementMemberLeft.MemberAbility.DisplayName = "Move Left";
                this.MovementMembers.Add(movementMemberLeft);

                MovementMember movementMemberForward = new MovementMember { MovementDirection = MovementDirection.Forward, MemberName = "Move Forward" };
                movementMemberForward.MemberAbility = new ReferenceAbility("Move Forward", null);
                movementMemberForward.MemberAbility.DisplayName = "Move Forward";
                this.MovementMembers.Add(movementMemberForward);

                MovementMember movementMemberBackward = new MovementMember { MovementDirection = MovementDirection.Backward, MemberName = "Move Backward" };
                movementMemberBackward.MemberAbility = new ReferenceAbility("Move Backward", null);
                movementMemberBackward.MemberAbility.DisplayName = "Move Backward";
                this.MovementMembers.Add(movementMemberBackward);

                MovementMember movementMemberUpward = new MovementMember { MovementDirection = MovementDirection.Upward, MemberName = "Move Up" };
                movementMemberUpward.MemberAbility = new ReferenceAbility("Move Up", null);
                movementMemberUpward.MemberAbility.DisplayName = "Move Up";
                this.MovementMembers.Add(movementMemberUpward);

                MovementMember movementMemberDownward = new MovementMember { MovementDirection = MovementDirection.Downward, MemberName = "Move Down" };
                movementMemberDownward.MemberAbility = new ReferenceAbility("Move Down", null);
                movementMemberDownward.MemberAbility.DisplayName = "Move Down";
                this.MovementMembers.Add(movementMemberDownward);

                MovementMember movementMemberStill = new MovementMember { MovementDirection = MovementDirection.Still, MemberName = "Still" };
                movementMemberStill.MemberAbility = new ReferenceAbility("Still", null);
                movementMemberStill.MemberAbility.DisplayName = "Still";
                this.MovementMembers.Add(movementMemberStill);
            }
        }
    }

    public class MovementMember  : NotifyPropertyChanged
    {
        private ReferenceAbility memberAbility;
        public ReferenceAbility MemberAbility
        {
            get
            {
                return memberAbility;
            }
            set
            {
                memberAbility = value;
                OnPropertyChanged("MemberAbility");
            }
        }

        private string memberName;
        public string MemberName
        {
            get
            {
                return memberName;
            }
            set
            {
                memberName = value;
                OnPropertyChanged("MemberName");
            }
        }

        private MovementDirection movementDirection;
        public MovementDirection MovementDirection
        {
            get
            {
                return movementDirection;
            }
            set
            {
                movementDirection = value;
                OnPropertyChanged("MovementDirection");
            }
        }
    }

    public class MovementInstruction
    {
        public double Distance { get; set; }
        public IMemoryElementPosition Destination { get; set; }
        public int Ground { get; set; }
        public List<IMemoryElementPosition> IncrementalPositions { get; set; }
        public MovementDirection CurrentDirection { get; set; }
        public MovementDirection LastDirection { get; set; }
        public Character MovableCharacter { get; set; }
    }

    public class MovementProcessor
    {
        public IMemoryElementPosition IncrementPosition(MovementDirection direction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }

        public List<IMemoryElementPosition> CalculateIncrementalPositions(MovementDirection direction, Vector3 facing, Vector3 pitch)
        {
            return new List<IMemoryElementPosition>();
        }

        public IMemoryElementPosition IncrementPositionFromInstruction(MovementInstruction instruction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }
        public Vector3 CalculateNewFacingPitch(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public bool ArePositionsBesideEachOther(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return false;
        }
        public Vector3 CalculateTurn(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public Vector3 CalculateCameraDistance(IMemoryElementPosition cameraPosition, IMemoryElementPosition characterPosition)
        {
            return new Vector3();
        }
    }
}
