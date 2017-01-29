using System.Collections.Generic;
using HeroVirtualTableTop.Desktop;

namespace HeroVirtualTableTop.ManagedCharacter
{
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
        bool IsTargeted { get; set; }

        bool IsFollowed { get; set; }
        bool IsManueveringWithCamera { get; set; }

        bool IsSpawned { get; set; }


        CharacterActionList<Identity> Identities { get; }

        DesktopCharacterMemoryInstance MemoryInstance { get; set; }
        KeyBindCommandGenerator Generator { get; set; }
        CharacterProgressBarStats ProgressBar { get; set; }
        Camera Camera { get; set; }

        void ToggleTargeted();

        void UnTarget(bool completeEvent = true);
        void Target(bool completeEvent = true);
        void TargetAndMoveCameraToCharacter(bool completeEvent = true);
        void Follow(bool completeEvent = true);
        void UnFollow(bool completeEvent = true);

        void ToggleManueveringWithCamera();
        new void SpawnToDesktop(bool completeEvent = true);
    }

    public enum CharacterActionType
    {
        Movement,
        Identity,
        Ability
    }

    public interface CharacterAction
    {
        string Name { get; set; }
        int Order { get; set; }
        ManagedCharacter Owner { get; set; }
        KeyBindCommandGenerator Generator { get; set; }
        void Render(bool completeEvent = true);
        CharacterAction Clone();
    }

    public interface CharacterActionList<T> : IDictionary<string, T> where T : CharacterAction
    {
        ManagedCharacter Owner { get; }
        T Active { get; set; }
        T Default { get; set; }
        T this[int index] { get; set; }
        CharacterActionType Type { get; }
        void Deactivate();
        string GetNewValidActionName(string name = null);

        void Insert(T action);
        void InsertAfter(T action, T precedingAction);
        void RemoveAction(T Action);
        T AddNew(T newItem);
        CharacterActionList<T> Clone();
        void AddMany(List<T> list);
        void PlayByKey(string key);
    }


    public interface Camera
    {
        KeyBindCommandGenerator Generator { get; }
        Position Position { get; set; }
        Identity Identity { get; }

        ManagedCharacter ManueveringCharacter { get; set; }
        void MoveToTarget(bool completeEvent = true);
        void ActivateCameraIdentity();
        void ActivateManueveringCharacterIdentity();

        void DisableMovement();
    }

    public enum SurfaceType
    {
        Model = 1,
        Costume = 2
    }

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