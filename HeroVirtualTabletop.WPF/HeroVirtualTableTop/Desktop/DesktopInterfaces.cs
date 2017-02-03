using System.Collections.Generic;
using HeroVirtualTableTop.Movement;
using Microsoft.Xna.Framework;

namespace HeroVirtualTableTop.Desktop
{
    public enum DesktopCommand
    {
        TargetName,
        PrevSpawn,
        NextSpawn,
        RandomSpawn,
        Fly,
        EditPos,
        DetachCamera,
        NoClip,
        AccessLevel,
        Command,
        SpawnNpc,
        Rename,
        LoadCostume,
        MoveNPC,
        DeleteNPC,
        ClearNPC,
        Move,
        TargetEnemyNear,
        LoadBind,
        BeNPC,
        SaveBind,
        GetPos,
        CamDist,
        Follow,
        LoadMap,
        BindLoadFile,
        Macro,
        NOP,
        PopMenu
    }
    public interface KeyBindCommandGenerator
    {
        string GeneratedCommandText { get; set; }
        string Command { get; set; }
        void GenerateDesktopCommandText(DesktopCommand command, params string[] parameters);
        string CompleteEvent();
    }

    public interface IconInteractionUtility
    {
        void RunCOHAndLoadDLL(string path);
        void ExecuteCmd(string command);
        string GeInfoFromNpcMouseIsHoveringOver();
        string GetMouseXYZString();
        string GetCollisionInfo(float sourceX, float sourceY, float sourceZ, float destX, float destY, float destZ);
    }

    public interface Position
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float Facing { get; set; }
        float Pitch { get; set; }
        float Roll { get; set; }
        Vector3 Vector { get; }
        Position JustMissedPosition { get; set; }
        Position Duplicate();
        bool IsWithin(float dist, Position position, out float calculatedDistance);
        void MoveTo(Position destination);
        float DistanceFrom(Position targetPos);
        void TurnTowards(Position position);
        Matrix RotationMatrix { get; }
        float Calculate2DAngleBetweenVectors(Vector3 v1, Vector3 v2, out bool isClockwiseTurn);

    }

    public interface DesktopMemoryCharacter
    {
        Position Position { get; set; }
        string Label { get; set; }
        float MemoryAddress { get; set; }
        MemoryManager memoryManager { get; }
        void Target();
        dynamic GetAttributeFromAdress(float address, string varType);
        void SetTargetAttribute(float offset, dynamic value, string varType);

        DesktopMemoryCharacter WaitUntilTargetIsRegistered();
    }


    public interface DesktopCharacterTargeter
    {
        DesktopMemoryCharacter TargetedInstance { get; set; }
    }
    public interface MemoryManager
    {
        MemoryManager Instance { get; }
        dynamic GetTargetAttribute(float address, string varType);
        void SetTargetAttribute(float address, dynamic value, string varType);
    }

    public class Collision
    {
        public Vector3 BodyCollisionOffsetVector { get; set; }
        public BodyPart CollisionBodyPart { get; set; }
        public float CollisionDistance { get; set; }
        public Vector3 CollisionPoint { get; set; }
    }

   
    public interface DesktopCharacter
    {
        DesktopMemoryCharacter MemoryInstance { get; set; }
        List<DesktopCharacterBodyPart> BodyParts { get; set; }
        Position Position { get; set; }

        DesktopDirection LastDirection { get; set; }
        DesktopDirection CurrentDirection { get; set; }
        Position Direction { get; set; }
        void TurnTowardsDestination();
        void TurnTowardFacing(int facing);

        Position Collision { get; }
        bool WillCollide { get; }

        Position Destination { get; set; }
        Desktop.Position OriginalDestination { get; set; }
        Position AllowableDestination { get; }
        Position NextTravelPointThatAvoidsCollision { get; }
        Position NextCollision { get; set; }

        float MovementUnit { get; }


        bool UsingGravity { get; set; }
        void ApplyGravityToDestination();



        int Distance { get; set; }
    }
    public enum BodyPart
    {
        None,
        Top,
        TopMiddle,
        Middle,
        BottomMiddle,
        BottomSemiMiddle,
        Bottom
    }
    public interface DesktopCharacterBodyPart
    {
        BodyPart Part { get; set; }
        Position CollisionPoint { get; }
        Position OffestPosition { get; set; }
    }
    public enum Direction
    {
        Left = 1,
        Right = 2,
        Forward = 3,
        Backward = 4,
        Up = 5,
        Down = 6,
        Still = 7
    }
    public interface DesktopDirection
    {
        Direction Direction { get; set; }
        double RotationAngle { get; }
    }

    
}