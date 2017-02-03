using System.Collections.Generic;
using HeroSystemEngine.HeroVirtualTableTop.AnimatedCharacter;
using HeroSystemEngine.HeroVirtualTableTop.Crowd;
using HeroSystemEngine.HeroVirtualTableTop.ManagedCharacter;
using HeroSystemEngine.HeroVirtualTableTop.MoveableCharacter;
using HeroSystemEngine.HeroVirtualTableTop.ThreeDeeSpace;
using HeroVirtualTableTop.Crowd;
using ThreeDeePositioner = HeroSystemEngine.HeroVirtualTableTop.Desktop.ThreeDeePositioner;

namespace HeroSystemEngine.HeroVirtualTableTop.ThreeDeeSpace
{
    public interface Position
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float Facing { get; set; }
        float Pitch { get; set; }
        float Roll { get; set; }
        Position Duplicate();
    }

    public interface COHCharacterInMemory
    {
        Position Position { get; set; }
        string Label { get; set; }
        float MemoryAddress { get; set; }

        MemoryManager memoryManager { get; }
        dynamic GetAttributeFromAdress(float address, string varType);
        void SetTargetAttribute(float offset, dynamic value, string varType);
    }

    public interface MemoryManager
    {
        MemoryManager Instance { get; }
        dynamic GetTargetAttribute(float address, string varType);
        void SetTargetAttribute(float address, dynamic value, string varType);
    }

    public interface ThreeDeePositioner
    {
    }
}

namespace HeroSystemEngine.HeroVirtualTableTop
{
    public interface HeroTableTopCharacterRepository
    {
        Dictionary<string, HeroTableTopCharacter> Characters { get; set; }

        string RepoFile { get; set; }
        HeroTableTopCharacter TargetedCharacter { get; }

        CharacterCrowd TargetedCrowd { get; }

        Dictionary<string, CharacterCrowd> Crowd { get; set; }

        HeroTableTopCharacter ReturnCharacter(string name);

        void DeleteCharacter(HeroTableTopCharacter character);
        void AddCharacter(HeroTableTopCharacter character);

        HeroTableTopCharacter NewCharacter();
        AnimatedAbility NewAbility(AnimatedCharacter.AnimatedCharacter character);
        CharacterMovement NewCharacterMovement(MoveableCharacter.MoveableCharacter character, Movement movement);
        AttackInstructions NewAttackInstructions();
        void Load();
        void Save();
        void DeleteCrowd(CharacterCrowd crowd);
        void AddCrowd(CharacterCrowd crowd);
        CharacterCrowd NewCrowd();
    }


    public interface HeroTableTopCharacter : MoveableCharacter.MoveableCharacter
    {
    }
}

namespace HeroSystemEngine.HeroVirtualTableTop.ManagedCharacter
{
    public interface ManagedCharacterCommands
    {
        void SpawnToDesktop();
        void ClearFromDesktop();
        void MoveCharacterToCamera();
    }

    public interface ManagedCharacter : ManagedCharacterCommands
    {
        string Name { get; set; }
        string DesktopLabel { get; set; }
        Position Position { get; set; }
        bool Targeted { get; set; }
        bool ManueveringWithCamera { get; set; }

        COHCharacterInMemory COHPlayer { get; }
        KeyBindCommandGenerator Generator { get; }
        CharacterProgressBarStats ProgressBar { get; set; }

        void ToggleTargeted();

        void UnTarget();

        void Target();
        void TargetAndMoveCameraToCharacter();

        void ToggleManueveringWithCamera();


        void AddIdentity(Identity identity);
        void RemoveIdentity(string identityName);
    }

    public interface KeyBindCommandGenerator
    {
        string GeneratedCommandText { get; set; }
        string Command { get; set; }
        void GenerateKeyBindsForEvent(string function, string parameters);
        void CompleteEvent();
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

    public enum SurfaceType
    {
        Model = 1,
        Costume = 2
    }

    public interface Identity
    {
        string Surface { get; set; }
        ManagedCharacter Character { get; set; }
        KeyBindCommandGenerator Generator { get; }
        SurfaceType Type { get; set; }
        void Play();
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

namespace HeroSystemEngine.HeroVirtualTableTop.Crowd
{
    public interface CrowdMemberCommands
    {
        void SaveCurrentTableTopPosition();
        void PlaceOnTableTop();
        void PlaceOnTableTopUsingRelativePos();
    }

    public interface CrowdMember : CrowdMembership, CrowdMemberCommands
    {
        Position SavedPosition { get; set; }
        CharacterCrowd Crowd { get; set; }
        string DesktopLabel { get; }

        ManagedCharacter.ManagedCharacter Character { get; set; }
    }

    public interface CrowdClipboard
    {
        void CopyToClipboard(CrowdMember member);
        void LinkToClipboard(CrowdMember member);
        void CutToClipboard(CrowdMember member);
        void PasteFromClipboard(CrowdMember member);
    }

    public interface CharacterCrowd : CrowdMembership, ManagedCharacterCommands, CrowdMemberCommands
    {
        bool UseRelativePositioning { get; set; }
        Dictionary<string, CrowdMembership> Members { get; set; }
        CrowdMember AddCharacter(ManagedCharacter.ManagedCharacter character);
        CharacterCrowd AddCrowd(CharacterCrowd crowd);
    }

    public interface Roster : ManagedCharacterCommands, CrowdMemberCommands, AnimatedCharacterCommands
    {
        List<CharacterCrowd> Crowds { get; }
        List<CharacterCrowdMember> Participants { get; }
        Dictionary<string, CharacterCrowd> CrowdsByName { get;}
        Dictionary<string, CharacterCrowdMember> ParticipantsByName { get;} 

        List<CharacterCrowdMember> SelectedParticipants { get; set; }

        CharacterCrowdMember ActiveCharacter { get; set; }
        void SelectParticipants(CharacterCrowdMember participant);
        void UnsSelectParticipant(CharacterCrowdMember participant);
        void ClearAllSelections();
        void SelectAllParticipants();
        void ActivateCharacter(CharacterCrowdMember participant);
        void DeactivateCharacter(CharacterCrowdMember participant);
        void AddParticipant(CharacterCrowdMember participant);
        void RemoveParticipant(CharacterCrowdMember participant);

        void AddCrowd(CharacterCrowd crowd);
        void RemoveCrowd(CharacterCrowd crowd);
        void SelectCrowd(CharacterCrowd crowd);

    }

    public interface CrowdMembership
    {
        string Name { get; set; }
        int Order { get; set; }
        CharacterCrowd Parent { get; set; }
    }
}

namespace HeroSystemEngine.HeroVirtualTableTop.AnimatedCharacter
{
    //to do add to animated character impl and crowdmemberimpl (maybe reverse the inheritance of the two)
    public interface AnimatedCharacterCommands
    {
        void Activate();
        void DeActivate();
    }

    public interface AnimatedCharacter : AnimatedCharacterCommands, ManagedCharacter.ManagedCharacter
    {
        bool Active { get; set; }
        List<string> AnimatedAbilityNames { get; }
        Dictionary<string, AnimatedAbility> Abilities { get; set; }
        Dictionary<string, AnimatedAbility> AbilitiesByKeyboardSHortcut { get; set; }
        Dictionary<AnimatableCharacterStateType, AnimatableCharacterState> ActiveCharacterStates { get; set; }
        Dictionary<string, AnimatedAbility> ActivePersistentAbilities { get; set; }

        List<AnimatableCharacterStateType> StatesThatHaveNotBeenRendered { get; set; }

        AnimatedAttack ActiveAttackCycle { get; set; }
        AnimatedAbility GetAbility(string name);

        void AddState(AnimatableCharacterStateType state, bool playImmediately = true);
        void RemoveState(AnimatableCharacterStateType state, bool playImmediately = true);
        void MarkAllAnimatableStatesAsRendered();
        void AddAnimatedAbility(AnimatedAbility ability);
        void RemoveAnimatedAbility(AnimatedAbility ability);
        void PlayAnimatedAbility(string abilityName);
        void PlayAnimatedAbilityByKeyBoardShortcut(string shortcut);
        void PlayExternalAnimatedAbility(AnimatedAbility ability);
        void StartAttackCycle(string attackName);
        void StartAttackCycleByKeyBoardShortcut(string shortcut);

        KnockbackCollisionInfo CompleteTheAttackCycle(AttackInstructions instructions);
        KnockbackCollisionInfo PlayCompleteAttackCycle(string attackName, AttackInstructions instructions);
        KnockbackCollisionInfo PlayCompleteExternalAttack(AnimatedAttack attack, AttackInstructions instructions);
    }

    public enum AnimatableCharacterStateType
    {
        Stunned = 1,
        Unconsious = 2,
        Dead = 3,
        Dying = 4,
        KnockBacked = 5,
        KnockedDown = 6,
        Attacking = 7,
        Targeted = 8,
        Selected = 9,
        Active = 10
    }

    public interface AnimatableCharacterState
    {
        AnimatedCharacter Character { get; set; }
        AnimatableCharacterStateType State { get; set; }
        AnimatedAbility AddingAnimation { get; set; }
        AnimatedAbility RemovalAnimation { get; set; }

        bool Rendered { get; set; }

        void AddToCharacter(AnimatedCharacter character);
        void RemoveFromCharacter(AnimatedCharacter character);
        void RenderState();
        void RenderRemovalOfState();
    }

    public interface AnimatableCharacterStateRepository
    {
        AnimatableCharacterStateRepository Instance { get; set; }
        Dictionary<AnimatableCharacterStateType, AnimatableCharacterState> AnimatableCharacterStates { get; set; }
        AnimatableCharacterState CreateStateFor(AnimatedCharacter character, AnimatableCharacterStateType state);
    }

    public enum SequenceType
    {
        And = 1,
        Or = 2
    }

    public interface AnimationSequence
    {
        SequenceType Type { get; set; }
        SortedList<int, AnimationElement> AnimationElements { get; set; }
        void AddAnimationElement(int order, AnimationElement animationElement);
        void RemoveAnimationElement(AnimationElement animationElement);
    }

    public interface AnimatedAbility : AnimationSequence
    {
        string Name { get; set; }
        AnimatedCharacter Character { get; set; }
        string KeyboardShortcut { get; set; }
        bool Persistent { get; set; }
        void Play();
    }

    public interface AttackInstructions
    {
        AnimatedCharacter defender { get; set; }
        List<AnimatableCharacterStateType> Impacts { get; set; }
        int KnockbackDistance { get; set; }
        bool AttackHit { get; set; }
    }

    public enum KnockbackCollisionType
    {
        Wall = 1,
        Floor = 2,
        Air = 3
    }

    public interface AnimatedAttack : AnimatedAbility
    {
        AnimatedAbility AttackAnimation { get; set; }
        AnimatedAbility OnHitAnimation { get; set; }

        void StartAttackCycle();
        KnockbackCollisionInfo PlayCompleteAttackCycle(AttackInstructions instructions);
        KnockbackCollisionInfo CompleteTheAttackCycle(AttackInstructions instructions);

        void AnimateAttack();
        void AnimateMiss();
        void AnimateHit();
        KnockbackCollisionInfo AnimateKnockBack();
    }

    public interface KnockbackCollisionInfo
    {
        KnockbackCollisionType Type { get; set; }
        string CharacterName { get; set; }
    }

    public interface AreaAttackInstructions : AttackInstructions
    {
        List<AttackInstructions> IndividualTargetInstructions { get; set; }
        AnimatedCharacter CenterTargetCharacter { get; set; }
        Position AttackCenter { get; set; }
    }

    public interface AreaEffectAttack : AnimatedAttack
    {
        List<KnockbackCollisionInfo> Attack(List<AttackInstructions> instructions);
        new void StartAttackCycle();
        AreaAttackInstructions DetermineTargetsFromPositionOfAttack(int radius, Position attackCenter);
        new List<KnockbackCollisionInfo> PlayCompleteAttackCycle(AttackInstructions instructions);
        new List<KnockbackCollisionInfo> CompleteTheAttackCycle(AttackInstructions instructions);
    }

    public interface AnimationElement
    {
        AnimatedAbility Ability { get; set; }
        int Order { get; set; }
        bool PlayWithNext { get; set; }
        AnimatedCharacter Character { get; set; }
        void Play(bool persistent);
    }

    public interface MovElement : AnimationElement
    {
        MovResource Mov { get; set; }
    }

    public interface SoundElement : AnimationElement
    {
        SoundResource Sound { get; set; }
        bool LoopSound { get; set; }
    }

    public interface FXElement : AnimationElement
    {
        FXResource FX { get; set; }
        Color Color1 { get; set; }
        Color Color2 { get; set; }
        Color Color3 { get; set; }
        void BuildCostumeFileThatWillPlayFX();
    }

    public interface ColorElement : AnimationElement
    {
        Color Color { get; set; }
    }

    public interface Color
    {
    }

    public interface SequenceElement : AnimationElement, AnimationSequence
    {
    }

    public interface PauseElement : AnimationElement
    {
        int Duration { get; set; }
    }

    public interface ReferenceElement : AnimationElement
    {
    }

    public interface AnimationElementRepository
    {
        List<SoundResource> SoundElements { get; set; }

        List<FXResource> FXElements { get; set; }

        List<MovResource> MovElements { get; set; }

        List<ReferenceResource> ReferenceElements { get; set; }
        SoundResource FilteredSoundElements(string filter);
        List<FXResource> FilteredFXElements(string filter);
        List<MovResource> FilteredMovs(string filter);
        List<ReferenceResource> References(string filter);
    }

    public interface SoundResource
    {
    }

    public interface FXResource
    {
    }

    public interface MovResource
    {
    }

    public interface ReferenceResource
    {
    }
}

namespace HeroSystemEngine.HeroVirtualTableTop.MoveableCharacter
{
    public enum MovementDirection
    {
        Left = 1,
        Right = 2,
        Forward = 3,
        Backward = 4,
        Up = 5,
        Down = 6,
        Still = 7
    }

    public enum MovementDirectionKeys
    {
        A = 1,
        Right = 2,
        W = 3,
        S = 4,
        Space = 5,
        Z = 6,
        X = 7
    }

    public enum TurnDirection
    {
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4
    }

    public enum TurnDirectionKeys
    {
        Left_ARROW = 1,
        Right_ARROW = 2,
        Up_ARROW = 3,
        Down_ARROW = 4
    }

    public enum DefaultMovements
    {
        Walk = 1,
        Run = 2,
        Swim = 3
    }

    public interface MovementCommands
    {
        int MovementSpeed { get; set; }
        void Increment(MovementDirection direction);
        void IncrementByKeyPress(MovementDirectionKeys key);
        void Move(MovementDirection direction, int distance);
        void MoveForwardTo(Desktop.Position destination);

        void IncrementTurn(TurnDirection direction);
        void IncrementTurnByKeyPress(TurnDirectionKeys key);
        void Turn(TurnDirection direction, int distance);

        void TurnTowardDestination(Desktop.Position destination);
        void TurnTowardFacing(int facing);
    }

    public interface MovementInstructions
    {
        MovementDirection LastDirectionMoved { get; set; }
        MovementDirection CurrentDirectionMoving { get; set; }
        Desktop.Position Destination { get; set; }
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
        void MoveForwardTo(MoveableCharacter character, Desktop.Position destination);

        void IncrementTurn(MoveableCharacter character, TurnDirection direction);
        void IncrementTurnByKeyPress(MoveableCharacter character, TurnDirectionKeys key);
        void Turn(MoveableCharacter character, TurnDirection direction, int distance);

        void TurnTowardDestination(Desktop.Position destination);
        void TurnTowardFacing(int facing);
    }

    public interface CharacterMovement : Movement, MovementCommands
    {
        bool IsActive { get; set; }
        Movement Movement { get; set; }
        Desktop.Position Destination { get; set; }

        void Activate();
        void DeActivate();
    }

    public interface MovementRepository
    {
        Dictionary<string, Movement> Movements { get; set; }
    }
}

namespace HeroSystemEngine.HeroVirtualTableTop.Desktop
{
    public interface Position
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float Facing { get; set; }
        float Pitch { get; set; }
        float Roll { get; set; }
        Position Duplicate();
    }

    public interface COHCharacterInMemory
    {
        Position Position { get; set; }
        string Label { get; set; }
        float MemoryAddress { get; set; }

        MemoryManager memoryManager { get; }
        dynamic GetAttributeFromAdress(float address, string varType);
        void SetTargetAttribute(float offset, dynamic value, string varType);
    }

    public interface MemoryManager
    {
        MemoryManager Instance { get; }
        dynamic GetTargetAttribute(float address, string varType);
        void SetTargetAttribute(float address, dynamic value, string varType);
    }

    public interface ThreeDeePositioner
    {
    }
}