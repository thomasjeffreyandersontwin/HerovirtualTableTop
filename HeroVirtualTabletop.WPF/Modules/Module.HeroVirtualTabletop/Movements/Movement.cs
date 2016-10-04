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
    public class CharacterMovement: CharacterOption
    {
        [JsonConstructor]
        private CharacterMovement() { }

        public CharacterMovement(string name, Character owner = null)
        {
            this.Name = name;
            this.Character = owner;
        }

        public Character Character
        {
            get;
            set;
        }

        public Movement Movement
        {
            get;
            set;
        }
    }
    public class Movement : NotifyPropertyChanged
    {
        [JsonConstructor]
        private Movement() { }

        public Movement(string name)
        {
            this.Name = name;
            this.AddDefaultMemberAbilities();
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
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

                MovementMember movementMemberLeft = new MovementMember { MovementDirection = MovementDirection.Left, MemberName = "Left" };
                movementMemberLeft.MemberAbility = new ReferenceAbility("Left", null);
                movementMemberLeft.MemberAbility.DisplayName = "Left";
                this.MovementMembers.Add(movementMemberLeft);

                MovementMember movementMemberRight = new MovementMember { MovementDirection = MovementDirection.Right, MemberName = "Right" };
                movementMemberRight.MemberAbility = new ReferenceAbility("Right", null);
                movementMemberRight.MemberAbility.DisplayName = "Right";
                this.MovementMembers.Add(movementMemberRight);

                MovementMember movementMemberForward = new MovementMember { MovementDirection = MovementDirection.Forward, MemberName = "Forward" };
                movementMemberForward.MemberAbility = new ReferenceAbility("Forward", null);
                movementMemberForward.MemberAbility.DisplayName = "Forward";
                this.MovementMembers.Add(movementMemberForward);

                MovementMember movementMemberBackward = new MovementMember { MovementDirection = MovementDirection.Backward, MemberName = "Back" };
                movementMemberBackward.MemberAbility = new ReferenceAbility("Back", null);
                movementMemberBackward.MemberAbility.DisplayName = "Back";
                this.MovementMembers.Add(movementMemberBackward);

                MovementMember movementMemberUpward = new MovementMember { MovementDirection = MovementDirection.Upward, MemberName = "Up" };
                movementMemberUpward.MemberAbility = new ReferenceAbility("Up", null);
                movementMemberUpward.MemberAbility.DisplayName = "Up";
                this.MovementMembers.Add(movementMemberUpward);

                MovementMember movementMemberDownward = new MovementMember { MovementDirection = MovementDirection.Downward, MemberName = "Down" };
                movementMemberDownward.MemberAbility = new ReferenceAbility("Down", null);
                movementMemberDownward.MemberAbility.DisplayName = "Down";
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
