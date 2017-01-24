using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace HeroVirtualTableTop.Desktop
{
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
        Position Duplicate();
        bool IsWithin(float dist, Position position, out float calculatedDistance);
        void MoveTo(Position destination);
        Vector3 Vector { get; }
        float DistanceFrom(Position targetPos);
    }
    public interface DesktopCharacterMemoryInstance
    {
        Position Position { get; set; }
        String Label { get; set; }
        float MemoryAddress { get; set; }
        void Target();
        MemoryManager memoryManager { get; }
        dynamic GetAttributeFromAdress(float address, string varType);
        void SetTargetAttribute(float offset, dynamic value, string varType);

        DesktopCharacterMemoryInstance WaitUntilTargetIsRegistered();
    }

    


    public interface DesktopCharacterTargeter
    {
        DesktopCharacterMemoryInstance TargetedInstance { get; set; }
    }
    public interface MemoryManager
    {
        MemoryManager Instance { get; }
        dynamic GetTargetAttribute(float address, string varType);
        void SetTargetAttribute(float address, dynamic value, string varType);
    }
    public interface ThreeDeePositioner
    { }
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
}
