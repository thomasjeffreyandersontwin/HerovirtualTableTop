using HeroVirtualTabletop.AnimatedCharacter;
using HeroVirtualTableTop.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeroVirtualTabletop.MoveableCharacter
{
    public enum MovementDirection { Left = 1, Right = 2, Forward = 3, Backward = 4, Up = 5, Down = 6, Still = 7 }
    public enum MovementDirectionKeys { A = 1, Right = 2, W = 3, S = 4, Space = 5, Z = 6, X = 7 }
    public enum TurnDirection { Left = 1, Right = 2, Up = 3, Down = 4 }
    public enum TurnDirectionKeys { Left_ARROW = 1, Right_ARROW = 2, Up_ARROW = 3, Down_ARROW = 4 }

    public enum DefaultMovements { Walk = 1, Run = 2, Swim = 3 }

    public interface MovementCommands
    {
        int MovementSpeed { get; set; }
        void Increment(MovementDirection direction);
        void IncrementByKeyPress(MovementDirectionKeys key);
        void Move(MovementDirection direction, int distance);
        void MoveForwardTo(Position destination);

        void IncrementTurn(TurnDirection direction);
        void IncrementTurnByKeyPress(TurnDirectionKeys key);
        void Turn(TurnDirection direction, int distance);

        void TurnTowardDestination(Position destination);
        void TurnTowardFacing(int facing);

    }
    public interface MovementInstructions
    {
        MovementDirection LastDirectionMoved { get; set; }
        MovementDirection CurrentDirectionMoving { get; set; }
        Position Destination { get; set; }
        int Distance { get; set; }

    }
    public interface MoveableCharacter : MovementCommands, AnimatedCharacter.AnimatedCharacter
    {
        MovementInstructions MovementInstructions { get; set; }
        Dictionary<string, CharacterMovement> Movements { get; set; }
        Dictionary<string, CharacterMovement> MovementsByKeyboardShortcut { get; set; }
        Dictionary<DefaultMovements, CharacterMovement> DefaultMovements { get; set; }
        CharacterMovement ActiveMovement { get; set; }

        void AddMovement(Movement movement);
        void RemoveMovement(Movement movement);

        void ActivateMovement(string movementName);
        void ActivateMovementBasedOnKeyboardShortcut(string shortcut);
        void ActivateExternalMovement(Movement movement);
        void DeActivateMovement(string movementName);
    }

    public interface Movement
    {
        ThreeDeePositioner Positioner { get; }
        Dictionary<MovementDirection, AnimatedAbility> MovementAnimations { get; set; }

        void Increment(MoveableCharacter character, MovementDirection direction);
        void IncrementByKeyPress(MoveableCharacter character, MovementDirectionKeys key);
        void Move(MoveableCharacter character, MovementDirection direction, int distance);
        void MoveForwardTo(MoveableCharacter character, Position destination);

        void IncrementTurn(MoveableCharacter character, TurnDirection direction);
        void IncrementTurnByKeyPress(MoveableCharacter character, TurnDirectionKeys key);
        void Turn(MoveableCharacter character, TurnDirection direction, int distance);

        void TurnTowardDestination(Position destination);
        void TurnTowardFacing(int facing);

    }

    public interface CharacterMovement : Movement, MovementCommands
    {
        bool IsActive { get; set; }
        Movement Movement { get; set; }
        Position Destination { get; set; }

        void Activate();
        void DeActivate();
    }

    public interface MovementRepository
    {
        Dictionary<string, Movement> Movements { get; set; }
    }

}
