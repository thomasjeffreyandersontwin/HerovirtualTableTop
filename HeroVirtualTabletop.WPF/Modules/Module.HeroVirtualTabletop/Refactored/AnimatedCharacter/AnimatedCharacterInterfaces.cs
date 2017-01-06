using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeroVirtualTabletop.AnimatedCharacter
{
    public interface AnimatedCharacterCommands
    {
        void Activate();
        void DeActivate();
    }

    public interface AnimatedCharacter : AnimatedCharacterCommands, ManagedCharacter
    {
        bool Active { get; set; }
        List<string> AnimatedAbilityNames { get; }
        Dictionary<string, AnimatedAbility> Abilities { get; set; }
        AnimatedAbility GetAbility(string name);
        Dictionary<string, AnimatedAbility> AbilitiesByKeyboardSHortcut { get; set; }
        Dictionary<AnimatableCharacterStateType, AnimatableCharacterState> ActiveCharacterStates { get; set; }
        Dictionary<string, AnimatedAbility> ActivePersistentAbilities { get; set; }

        void AddState(AnimatableCharacterStateType state, bool playImmediately = true);
        void RemoveState(AnimatableCharacterStateType state, bool playImmediately = true);
        void MarkAllAnimatableStatesAsRendered();
        void AddAnimatedAbility(AnimatedAbility ability);
        void RemoveAnimatedAbility(AnimatedAbility ability);

        List<AnimatableCharacterStateType> StatesThatHaveNotBeenRendered { get; set; }
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
