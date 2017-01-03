using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
namespace HeroVirtualTableTop.ManagedCharacter
{

    using HeroVirtualTableTop.Desktop;
    using Framework.WPF.Library;

    public interface ManagedCharacterCommands
    {
        void SpawnToDesktop(bool completeEvent = true);
        void ClearFromDesktop(bool completeEvent = true);
        void MoveCharacterToCamera(bool completeEvent = true);
    }
    public interface ManagedCharacter : ManagedCharacterCommands
    {
        DesktopCharacterTargeter Targeter { get; set; }
        string Name { get; set; }
        string DesktopLabel { get; }
        Position Position { get; }

        void ToggleTargeted();

        void UnTarget(bool completeEvent = true);
        bool IsTargeted { get; set; }
        void Target(bool completeEvent = true);
        void TargetAndMoveCameraToCharacter(bool completeEvent = true);

        bool IsFollowed { get; set; }
        void Follow(bool completeEvent = true);
        void UnFollow(bool completeEvent = true);

        void ToggleManueveringWithCamera();
        bool IsManueveringWithCamera { get; set; }

        bool IsSpawned { get; set; }
        new void SpawnToDesktop(bool completeEvent = true);


        CharacterActionList<Identity> Identities { get; }

        DesktopCharacterMemoryInstance MemoryInstance { get; set; }
        KeyBindCommandGenerator Generator { get; }
        CharacterProgressBarStats ProgressBar { get; set; }
        Camera Camera { get; }

    }
    
    public enum CharacterActionType { Movement, Identity, Ability}
    public interface CharacterAction
    {
        string Name { get; set; }
        void Render(bool completeEvent = true);
        int Order { get; set; }
        ManagedCharacter Owner { get; set; }
        KeyBindCommandGenerator Generator { get; set; }

    }
    public interface CharacterActionList<T>:IDictionary<string,T> where T : CharacterAction 
    {
        T Active {get; set;}
        T Default {get; set; }
        string GetNewValidActionName(string name = null);
        T this[int index] { get; set; }
        void Insert(T action);
        void InsertAfter(T action, T precedingAction);
        void RemoveAction(T Action);
        T CreateNew();
        CharacterActionType Type { get; }
    }


    public interface Camera
    {
        Camera Instance { get; }
        KeyBindCommandGenerator Generator { get; }
        Position Position { get; }
        Identity CameraIdentity { get; }

        ManagedCharacter ManueveringCharacter { get; set; }
        void MoveToCharacter(ManagedCharacter character);
        void ActivateCameraIdentity();
        void ActivateManueveringCharacterIdentity();

        void DisableMovement();
    }
    public enum SurfaceType { Model = 1, Costume = 2 }
    public interface Identity : CharacterAction
    {
        string Surface { get; set; }
        SurfaceType Type { get; set; }
    }
    public interface CharacterProgressBarStats
    {
        ManagedCharacter Character { get; set; }
        int CurrentStun { get; set; }
        int MaxStun { get; set; }
        int CurrentEnd { get; set; }
        int MaxEnd { get; set; }

        MemoryManager manager { get; }
        void UpdateStatusBars();
    }


}



