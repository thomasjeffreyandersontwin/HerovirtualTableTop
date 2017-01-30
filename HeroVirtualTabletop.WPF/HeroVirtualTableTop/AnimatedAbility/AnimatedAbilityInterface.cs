using System.Collections.Generic;
using System.Windows.Media;
using HeroVirtualTableTop.Crowd;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;

namespace HeroVirtualTableTop.AnimatedAbility
{
    public interface AnimatedCharacterCommands
    {
        void Activate();
        void DeActivate();
    }

    public class DefaultAbilities
    {
        public static string UnderAttack => "UnderAttack";

        public static string Strike => "Strike";

        public static string Dodge => "Dodge";

        public static string Stunned => "Stunned";

        public static string Unconsious => "Unconsious";

        public static string Hit => "Hit";

        public static string Miss => "Miss";

        public static string Dead => "Dead";

        public static string Dying => "Dying";

        public static string CharacterName => "Default";
    }


    public interface AnimatedCharacterRepository : CrowdRepository
    {
        Dictionary<string, AnimatedCharacter> CharacterByName { get; }
        List<AnimatedCharacter> Characters { get; }
        AnimatedCharacter NewCrowd(string name = "Character");
    }

    public interface AnimatedCharacter : AnimatedCharacterCommands, CharacterCrowdMember
    {
        AnimatedCharacterRepository Repository { get; }
        List<FXElement> LoadedFXs { get; }
        CharacterActionList<AnimatedAbility> Abilities { get; }
        List<AnimatedAbility> ActivePersistentAbilities { get; }
        List<AnimatableCharacterState> ActiveStates { get; }

        bool IsActive { get; set; }
        AnimatedAbility ActiveAttack { get; set; }

        Position Facing { get; set; }
        void PlayExternalAnimatedAbility(AnimatedAbility ability);
        KnockbackCollisionInfo PlayCompleteExternalAttack(AnimatedAttack attack, AttackInstructions instructions);
        void AddState(AnimatableCharacterState state, bool playImmediately = true);
        void AddDefaultState(string state, bool playImmediately = true);
        void RemoveState(AnimatableCharacterState state, bool playImmediately = true);
        void ResetAllAbiltitiesAndState();
        void RemoveStateByName(string name);
        void TurnTowards(Position position);
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
        AnimatedCharacter Target { get; set; }
        string StateName { get; set; }
        AnimatedAbility Ability { get; set; }

        bool Rendered { get; set; }
        bool AbilityAlreadyPlayed { get; set; }

        void AddToCharacter(AnimatedCharacter character);
        void RemoveFromCharacter(AnimatedCharacter character);

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
        List<AnimationElement> AnimationElements { get; }

        void InsertManyAnimationElements(List<AnimationElement> animationElements);
        void InsertAnimationElement(AnimationElement animationElement);
        void RemoveAnimationElement(AnimationElement animationElement);
        void InsertAnimationElementAfter(AnimationElement toInsert, AnimationElement moveAfter);
        void Stop(AnimatedCharacter target);
        void Play(AnimatedCharacter target);
        void Play(List<AnimatedCharacter> target);
    }

    public interface AnimatedAbility : AnimationSequence, CharacterAction
    {
        AnimatedCharacter Target { get; set; }
        string KeyboardShortcut { get; set; }
        bool Persistent { get; set; }
        AnimationSequence Sequencer { get; }
        AnimatedAbility StopAbility { get; set; }
        void Play();
        new void Play(AnimatedCharacter target);
        new void Play(List<AnimatedCharacter> targets);
        void Stop();
        AnimatedAbility Clone(AnimatedCharacter target);
    }

    public interface AnimatedAbilityRepository
    {
        AnimatedAbility NewAbility(AnimatedCharacter owner);

        SoundElement NewSoundElement(AnimatedAbility parentAbility);
        FXElement NewFXElement(AnimatedAbility parentAbility);
        MovElement NewMOVElement(AnimatedAbility parentAbility);
        PauseElement NewPauselement(AnimatedAbility parentAbility);
        SoundElement NewSequenceElement(AnimatedAbility parentAbility);
        ReferenceElement NewRefElement(AnimatedAbility parentAbility);
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
        AttackInstructions ActiveAttack { get; set; }
        AnimatedAbility AttackAnimation { get; set; }
        AnimatedAbility OnHitAnimation { get; set; }
        AnimatedAbility OnMissAnimation { get; set; }
        Position Direction { get; set; }

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
        void AddDefenders(AnimatedCharacter defender);
    }

    public interface AreaEffectAttack : AnimatedAttack
    {
        new void StartAttackCycle();
        List<KnockbackCollisionInfo> PlayCompleteAttackCycle(List<AttackInstructions> instructions);

        AreaAttackInstructions DetermineTargetsFromPositionOfAttack(int radius, Position attackCenter);
        new List<KnockbackCollisionInfo> CompleteTheAttackCycle(AttackInstructions instructions);
    }

    public interface AnimationElement
    {
        AnimationSequence ParentSequence { get; set; }
        int Order { get; set; }

        AnimatedCharacter Target { get; set; }

        bool PlayWithNext { get; set; }
        bool Persistent { get; set; }
        void Play();
        void Play(List<AnimatedCharacter> targets);
        void Play(AnimatedCharacter target);

        void Stop();
        void Stop(AnimatedCharacter target);
        void StopResource(AnimatedCharacter target);
        List<AnimationElement> AddToFlattendedList(List<AnimationElement> list);
        void DeactivatePersistent();
        AnimationElement Clone(AnimatedCharacter target);
    }

    public interface MovElement : AnimationElement
    {
        MovResource Mov { get; set; }
    }

    public interface SoundElement : AnimationElement
    {
        SoundResource Sound { get; set; }
        Position PlayingLocation { get; set; }

        bool Active { get; set; }
        SoundEngineWrapper SoundEngine { get; set; }
        string SoundFileName { get; }
    }

    public interface SoundEngineWrapper
    {
        float Default3DSoundMinDistance { set; }
        void SetListenerPosition(float posX, float posY, float posZ, float lookDirX, float lookDirY, float lookDirZ);
        void Play3D(string soundFilename, float posX, float posY, float posZ, bool playLooped);
        void StopAllSounds();
    }

    public interface FXElement : AnimationElement
    {
        FXResource FX { get; set; }
        Color Color1 { get; set; }
        Color Color2 { get; set; }
        Color Color3 { get; set; }
        Color Color4 { get; set; }

        string CostumeFilePath { get; }
        string ModifiedCostumeFilePath { get; }
        bool ModifiedCostumeContainsFX { get; }

        Position AttackDirection { get; set; }
        string CostumeText { get; }
        Position Destination { get; set; }
        bool IsDirectional { get; set; }
        void BuildCostumeFileThatWillPlayFX();
    }

    public interface ColorElement : AnimationElement
    {
        Color Resource { get; set; }
    }

    public interface SequenceElement : AnimationElement, AnimationSequence
    {
        AnimationSequence Sequencer { get; }
    }

    public interface PauseElement : AnimationElement
    {
        int Duration { get; set; }
        bool IsUnitPause { get; set; }
        int CloseDistanceDelay { get; set; }
        int ShortDistanceDelay { get; set; }
        int MediumDistanceDelay { get; set; }
        int LongDistanceDelay { get; set; }
        Position TargetPosition { get; set; }
        PauseBasedOnDistanceManager DistanceDelayManager { get; set; }
    }

    public interface PauseBasedOnDistanceManager
    {
        PauseElement PauseElement { get; set; }
        double Distance { get; set; }
        double Duration { get; }
    }

    public interface ReferenceElement : AnimationElement
    {
        AnimatedAbility Reference { get; set; }

        SequenceElement Copy(AnimatedCharacter destination);
    }

    public enum AnimationelEmentType
    {
        Mov,
        FX,
        Sound,
        Reference,
        Sequence,
        Pause
    }

    public interface AnimationElementRepository
    {
        List<SoundResource> SoundElements { get; set; }

        List<FXResource> FXElements { get; set; }

        List<MovResource> MovElements { get; set; }

        List<ReferenceResource> ReferenceElements { get; set; }
        List<AnimationElement> Filter(string filter, AnimationelEmentType type);
        SoundResource FilteredSoundElements(string filter);
        List<FXResource> FilteredFXElements(string filter);
        List<MovResource> FilteredMovs(string filter);
        List<ReferenceResource> References(string filter);
    }

    public interface AnimatedResource
    {
        string Name { get; set; }
        string Tag { get; set; }
    }

    public interface SoundResource : AnimatedResource
    {
        string FullResourcePath { get; set; }
    }

    public interface FXResource : AnimatedResource
    {
        string FullResourcePath { get; set; }
    }

    public interface MovResource
    {
        string FullResourcePath { get; set; }
    }

    public interface ReferenceResource
    {
        AnimatedCharacter Character { get; set; }
        AnimatedAbility Ability { get; set; }
    }
}