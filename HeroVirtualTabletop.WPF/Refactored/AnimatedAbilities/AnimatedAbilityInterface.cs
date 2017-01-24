using HeroVirtualTableTop.Crowd;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.ManagedCharacter;
using HeroVirtualTableTop.Desktop;
using System.IO;
using System.Windows.Media;
using IrrKlang;
using System.Threading;
namespace HeroVirtualTableTop.AnimatedAbility
{
    

    public interface AnimatedCharacterCommands
    {
        void Activate();
        void DeActivate();
    }

    public interface AnimatedCharacter : AnimatedCharacterCommands, CharacterCrowdMember
    {
        List<FXElement> LoadedFXs { get; }
        bool IsActive { get; set; }

        CharacterActionList<AnimatedAbility> Abilities { get; }
        List<AnimatedAbility> ActivePersistentAbilities { get;}
        void PlayExternalAnimatedAbility(AnimatedAbility ability);

        AnimatedAttack ActiveAttackCycle { get; set; }
        KnockbackCollisionInfo PlayCompleteExternalAttack(AnimatedAttack attack, AttackInstructions instructions);

        List<AnimatableCharacterState> ActiveStates { get; }
        void AddState(AnimatableCharacterState state, bool playImmediately = true);
        void RemoveState(AnimatableCharacterState state, bool playImmediately = true);
        void ResetAllAbiltities();

        Position Facing { get; set; }

        void RemoveStateByName(string name);
    }

    public enum AnimatableCharacterStateType { Stunned = 1, Unconsious = 2, Dead = 3, Dying = 4, KnockBacked = 5, KnockedDown = 6, Attacking = 7, Targeted = 8, Selected = 9, Active = 10 };
    public interface AnimatableCharacterState
    {
        AnimatedCharacter Target { get; set; }
        string StateName { get; set; }
        AnimatedAbility Ability { get; set; }

        void AddToCharacter(AnimatedCharacter character);
        void RemoveFromCharacter(AnimatedCharacter character);

        bool Rendered { get; set; }
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
        List<AnimationElement> AnimationElements { get; }

        void InsertAnimationElement(AnimationElement animationElement);
        void RemoveAnimationElement(AnimationElement animationElement);
        void InsertAnimationElementAfter(AnimationElement toInsert, AnimationElement moveAfter);
        void Stop(AnimatedCharacter target);       
        void Play(AnimatedCharacter target);
    }
    public interface AnimatedAbility : AnimationSequence, CharacterAction
    {
        AnimatedCharacter Target { get; set; }
        string KeyboardShortcut { get; set; }
        bool Persistent { get; set; }
        

        void Play();
        void Play(AnimatedCharacter target);
        void Play(List<AnimatedCharacter> targets);
        void Stop();
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
    public enum KnockbackCollisionType { Wall = 1, Floor = 2, Air = 3 };
    public interface AnimatedAttack : AnimatedAbility
    {
        AttackInstructions ActiveAttack { get; set; }
        AnimatedAbility AttackAnimation { get; set; }
        AnimatedAbility OnHitAnimation { get; set; }
        AnimatedAbility OnMissAnimation { get; set; }

        void StartAttackCycle();
        KnockbackCollisionInfo PlayCompleteAttackCycle(AttackInstructions instructions);
        KnockbackCollisionInfo CompleteTheAttackCycle(AttackInstructions instructions);

        void AnimateAttack();
        void AnimateMiss();
        void AnimateHit();
        KnockbackCollisionInfo AnimateKnockBack();
        Position Direction { get; set; }

    }

    public interface KnockbackCollisionInfo
    {
        KnockbackCollisionType Type { get; set; }
        string CharacterName { get; set; }

    }
    public interface AreaAttackInstructions : AttackInstructions
    {
        void AddDefenders(AnimatedCharacter defender);
        List<AttackInstructions> IndividualTargetInstructions { get; set; }
        AnimatedCharacter CenterTargetCharacter { get; set; }
        Position AttackCenter { get; set; }

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
        AnimationElement Clone();
        
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
        void SetListenerPosition(float posX, float posY, float posZ, float lookDirX, float lookDirY, float lookDirZ);
        float Default3DSoundMinDistance { set; }
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
        void BuildCostumeFileThatWillPlayFX();

        Position AttackDirection { get; set; }
        string CostumeText { get; }   
    }
    public interface ColorElement : AnimationElement
    {

        Color Resource { get; set; }
    }
    
    public interface SequenceElement : AnimationElement, AnimationSequence
    { }

    public interface PauseElement : AnimationElement
    {
        int Duration { get; set; }
        bool IsUnitPause { get; set; }
        int CloseDistanceDelay { get; set; }
        int ShortDistanceDelay { get; set; }
        int MediumDistanceDelay { get; set; }
        int LongDistanceDelay { get; set; }
        Position TargetPosition { get; set; }

    }

    public interface ReferenceElement : AnimationElement
    {
        AnimatedAbility Reference { get; set; }
    }

    public enum AnimationelEmentType { Mov, FX, Sound, Reference, Sequence, Pause }
    public interface AnimationElementRepository
    {
        List<AnimationElement> Filter(string filter, AnimationelEmentType type);
        List<SoundResource> SoundElements { get; set; }
        SoundResource FilteredSoundElements(string filter);

        List<FXResource> FXElements { get; set; }
        List<FXResource> FilteredFXElements(string filter);

        List<MovResource> MovElements { get; set; }
        List<MovResource> FilteredMovs(string filter);

        List<ReferenceResource> ReferenceElements { get; set; }
        List<ReferenceResource> References(string filter);
    }
    public interface AnimatedResource
    {
        
        string Name { get; set; }
        string Tag { get; set; }
    }

    public interface SoundResource: AnimatedResource
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
        AnimatedAbility Ability  { get; set; }
    }


}