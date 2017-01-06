
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HeroSystemEngine.HeroVirtualTableTop.ThreeDeeSpace{

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
        String Label { get; set; }
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
    { }

}

namespace HeroSystemEngine.HeroVirtualTableTop
{
    using HeroSystemEngine.HeroVirtualTableTop.MoveableCharacter;
    using HeroSystemEngine.HeroVirtualTableTop.Crowd;
    using HeroSystemEngine.HeroVirtualTableTop.AnimatedCharacter;

    public interface HeroTableTopCharacterRepository
    {
        
        Dictionary<string, HeroTableTopCharacter> Characters { get; set; }

        HeroTableTopCharacter ReturnCharacter(string name);

        string RepoFile { get; set; }
        HeroTableTopCharacter TargetedCharacter { get; }

        void DeleteCharacter(HeroTableTopCharacter character);
        void AddCharacter(HeroTableTopCharacter character);

        HeroTableTopCharacter NewCharacter();
        AnimatedAbility NewAbility(AnimatedCharacter.AnimatedCharacter character);
        CharacterMovement NewCharacterMovement(MoveableCharacter.MoveableCharacter character, Movement movement);
        AttackInstructions NewAttackInstructions();
        void Load();
        void Save();

        CharacterCrowd TargetedCrowd { get; }

        Dictionary<string, CharacterCrowd> Crowd { get; set; }
        void DeleteCrowd(CharacterCrowd crowd);
        void AddCrowd(CharacterCrowd crowd);
        CharacterCrowd NewCrowd();


    }


    public interface HeroTableTopCharacter: MoveableCharacter.MoveableCharacter
    {
    }
}

namespace HeroSystemEngine.HeroVirtualTableTop.ManagedCharacter
{
    using HeroSystemEngine.HeroVirtualTableTop.ThreeDeeSpace;
    

    public interface ManagedCharacterCommands
    {
        void SpawnToDesktop();
        void ClearFromDesktop();
        void MoveCharacterToCamera();
    }
    public interface ManagedCharacter: ManagedCharacterCommands
    {
        string Name { get; set; }
        string DesktopLabel { get; set;}
        Position Position { get; set; }

        void ToggleTargeted();
        
        void UnTarget();
        bool Targeted { get; set; }

        void Target();
        void TargetAndMoveCameraToCharacter();
        
        void ToggleManueveringWithCamera();
        bool ManueveringWithCamera { get; set; }

        void AddIdentity(Identity identity);
        void RemoveIdentity(string identityName);

        COHCharacterInMemory COHPlayer { get; }
        KeyBindCommandGenerator Generator { get; }
        CharacterProgressBarStats ProgressBar { get; set; }

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
        Identity CameraIdentity { get;}

        ManagedCharacter ManueveringCharacter { get; set; }
        void MoveToCharacter(ManagedCharacter character);
        void ActivateCameraIdentity();
        void ActivateManueveringCharacterIdentity();

        void DisableMovement();
    }
    public enum SurfaceType {Model =1, Costume =2 }
    public interface Identity
    {
        string Surface { get; set; }
        ManagedCharacter Character { get; set; }
        KeyBindCommandGenerator Generator { get; }
        SurfaceType Type { get; set; }
        void Render();
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
    using HeroSystemEngine.HeroVirtualTableTop.ManagedCharacter;
    using HeroSystemEngine.HeroVirtualTableTop.ThreeDeeSpace;
    using HeroVirtualTableTop.AnimatedCharacter;

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
        string DesktopLabel { get;}

        ManagedCharacter Character { get; set; }
    }

    public interface CrowdClipboard {
        void CopyToClipboard(CrowdMember member);
        void LinkToClipboard(CrowdMember member);
        void CutToClipboard(CrowdMember member);
        void PasteFromClipboard(CrowdMember member);
    }

    public interface CharacterCrowd : CrowdMembership, ManagedCharacterCommands, CrowdMemberCommands
    {
        bool UseRelativePositioning { get; set; }
        Dictionary<string, CrowdMembership> Members { get; set; }
        CrowdMember AddCharacter(ManagedCharacter character);
        CharacterCrowd AddCrowd(CharacterCrowd crowd);
    }

    public interface Roster: ManagedCharacterCommands, CrowdMemberCommands, AnimatedCharacterCommands
    {
        Dictionary<string, CharacterCrowd> Crowds { get; set; }
        Dictionary<string, AnimatedCharacter> Participants { get; set; }

        List<CrowdMembership> SelectedParticipants { get; set; }
        void AddMemberToSelection(CrowdMembership member);
        void RemoveMemberFromSelection(CrowdMembership member);
        void ClearMembersFromSelection();

        CrowdMember ActiveCharacter { get; set; }
        void ActivateCharacter(CrowdMember crowdMember);
        void DeactivateCharacter(CrowdMember crowdMember);
        void AddMember(AnimatedCharacter member);
        void RemoveMember(AnimatedCharacter member);

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
    using HeroSystemEngine.HeroVirtualTableTop.Crowd;
    using HeroSystemEngine.HeroVirtualTableTop.ThreeDeeSpace;

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
        AnimatedAbility GetAbility(string name);
        Dictionary<string, AnimatedAbility> AbilitiesByKeyboardSHortcut { get; set; }
        Dictionary<AnimatableCharacterStateType, AnimatableCharacterState> ActiveCharacterStates { get; set;}
        Dictionary<string, AnimatedAbility> ActivePersistentAbilities { get; set; }

        void AddState(AnimatableCharacterStateType state, bool playImmediately = true);
        void RemoveState(AnimatableCharacterStateType state, bool playImmediately = true);
        void MarkAllAnimatableStatesAsRendered();
        void AddAnimatedAbility(AnimatedAbility ability);
        void RemoveAnimatedAbility(AnimatedAbility ability);

        List <AnimatableCharacterStateType> StatesThatHaveNotBeenRendered { get; set; }
        void PlayAnimatedAbility(string abilityName);
        void PlayAnimatedAbilityByKeyBoardShortcut(string shortcut);
        void PlayExternalAnimatedAbility(AnimatedAbility ability);

        AnimatedAttack ActiveAttackCycle { get; set; }
        void StartAttackCycle(string attackName);
        void StartAttackCycleByKeyBoardShortcut(string shortcut);

        KnockbackCollisionInfo CompleteTheAttackCycle(AttackInstructions instructions);
        KnockbackCollisionInfo PlayCompleteAttackCycle(string attackName, AttackInstructions instructions);
        KnockbackCollisionInfo PlayCompleteExternalAttack(AnimatedAttack attack, AttackInstructions instructions);

        }

    public enum AnimatableCharacterStateType { Stunned = 1, Unconsious = 2, Dead = 3, Dying = 4, KnockBacked = 5, KnockedDown = 6, Attacking = 7, Targeted = 8, Selected = 9, Active = 10 };
    public interface AnimatableCharacterState
    {
        AnimatedCharacter Character { get; set; }
        AnimatableCharacterStateType State { get; set; }
        AnimatedAbility AddingAnimation { get; set; }
        AnimatedAbility RemovalAnimation { get; set; }

        void AddToCharacter(AnimatedCharacter character);
        void RemoveFromCharacter(AnimatedCharacter character);

        bool Rendered { get; set; }
        void RenderState();
        void RenderRemovalOfState();
    }
    public interface AnimatableCharacterStateRepository
    {
        AnimatableCharacterStateRepository Instance { get; set; }
        Dictionary<AnimatableCharacterStateType, AnimatableCharacterState> AnimatableCharacterStates { get; set; }
        AnimatableCharacterState CreateStateFor(AnimatedCharacter character, AnimatableCharacterStateType state);
    }

    public enum SequenceType { And = 1, Or = 2 }
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
    public enum KnockbackCollisionType { Wall = 1, Floor = 2, Air = 3 };
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
    public interface MovElement: AnimationElement
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
        Color Color {get; set;}
    }
    public interface Color
    { }
    public interface SequenceElement : AnimationElement, AnimationSequence
    { }
    public interface PauseElement : AnimationElement
    {
        int Duration { get; set; }

    }
    public interface ReferenceElement : AnimationElement
    { }

    public interface AnimationElementRepository
    {
        List<SoundResource> SoundElements { get; set; }
        SoundResource FilteredSoundElements(string filter);

        List<FXResource> FXElements { get; set; }
        List<FXResource> FilteredFXElements(string filter);

        List<MovResource> MovElements { get; set; }
        List<MovResource> FilteredMovs(string filter);

        List<ReferenceResource> ReferenceElements { get; set; }
        List<ReferenceResource> References(string filter);
    }
    public interface SoundResource
    { }
    public interface FXResource
    { }
    public interface MovResource
    { }
    public interface ReferenceResource
    { }
}

namespace HeroSystemEngine.HeroVirtualTableTop.MoveableCharacter {
    using HeroSystemEngine.HeroVirtualTableTop.AnimatedCharacter;
    using HeroSystemEngine.HeroVirtualTableTop.Desktop;


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
    public interface MoveableCharacter : MovementCommands, AnimatedCharacter
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

namespace HeroSystemEngine.HeroVirtualTableTop.Desktop{

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
        String Label { get; set; }
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
    { }

}
 