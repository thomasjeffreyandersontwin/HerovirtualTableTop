using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.OptionGroups;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        }

        private AnimatedAbility moveRightAbility;
        public AnimatedAbility MoveRightAbility
        {
            get
            {
                return moveRightAbility;
            }
            set
            {
                moveRightAbility = value;
                OnPropertyChanged("MoveRightAbility");
            }
        }
        private AnimatedAbility moveLeftAbility;
        public AnimatedAbility MoveLeftAbility
        {
            get
            {
                return moveLeftAbility;
            }
            set
            {
                moveLeftAbility = value;
                OnPropertyChanged("MoveLeftAbility");
            }
        }
        private AnimatedAbility moveForwardAbility;
        public AnimatedAbility MoveForwardAbility
        {
            get
            {
                return moveForwardAbility;
            }
            set
            {
                moveForwardAbility = value;
                OnPropertyChanged("MoveForwardAbility");
            }
        }
        private AnimatedAbility moveBackwardAbility;
        public AnimatedAbility MoveBackwardAbility
        {
            get
            {
                return moveBackwardAbility;
            }
            set
            {
                moveBackwardAbility = value;
                OnPropertyChanged("MoveBackwardAbility");
            }
        }
        private AnimatedAbility moveUpwardAbility;
        public AnimatedAbility MoveUpwardAbility
        {
            get
            {
                return moveUpwardAbility;
            }
            set
            {
                moveUpwardAbility = value;
                OnPropertyChanged("MoveUpwardAbility");
            }
        }
        private AnimatedAbility moveDownwardAbility;
        public AnimatedAbility MoveDownwardAbility
        {
            get
            {
                return moveDownwardAbility;
            }
            set
            {
                moveDownwardAbility = value;
                OnPropertyChanged("MoveDownwardAbility");
            }
        }
        private AnimatedAbility moveStillAbility;
        public AnimatedAbility MoveStillAbility
        {
            get
            {
                return moveStillAbility;
            }
            set
            {
                moveStillAbility = value;
                OnPropertyChanged("MoveStillAbility");
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
        public Vector3 CalculateCamearaDistance(IMemoryElementPosition cameraPosition, IMemoryElementPosition characterPosition)
        {
            return new Vector3();
        }
    }
}
