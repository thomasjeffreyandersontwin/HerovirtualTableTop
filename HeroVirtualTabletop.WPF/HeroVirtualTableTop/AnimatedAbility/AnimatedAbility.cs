using System;
using System.Collections.Generic;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;

namespace HeroVirtualTableTop.AnimatedAbility
{
    public class AnimatedAbilityImpl : AnimatedAbility
    {
        private AnimationSequence _sequencer;

        private AnimatedCharacter _target;

        public AnimatedAbilityImpl(AnimationSequence clonedsequencer)
        {
            _sequencer = clonedsequencer;
        }

        public AnimatedAbilityImpl()
        {
        }

        public KeyBindCommandGenerator Generator { get; set; }

        public string KeyboardShortcut { get; set; }

        public string Name { get; set; }

        public int Order { get; set; }

        public bool Persistent { get; set; }

        public AnimatedCharacter Target
        {
            get { return _target; }
            set
            {
                if (_sequencer is AnimationElement)
                    (_sequencer as AnimationElement).Target = value;
                _target = value;
            }
        }

        public ManagedCharacter.ManagedCharacter Owner
        {
            get { return Target; }

            set
            {
                if (value is AnimatedCharacter)
                    Target = value as AnimatedCharacter;
            }
        }

        public AnimatedAbility StopAbility { get; set; }

        public CharacterAction Clone()
        {
            throw new NotImplementedException();
        }

        public AnimationSequence Sequencer => _sequencer ?? (_sequencer = new AnimationSequenceImpl(Target));

        public void Play()
        {
            Play(Target);
        }

        public void Play(List<AnimatedCharacter> targets)
        {
            Sequencer.Play(targets);
        }

        public void Play(AnimatedCharacter target)
        {
            Sequencer.Play(target);
            if (Persistent)
            {
                AnimatableCharacterState newstate = new AnimatableCharacterStateImpl(this, Target);
                newstate.AbilityAlreadyPlayed = true;
                target.AddState(newstate);
            }
        }

        public void Stop()
        {
            Stop(Target);
        }

        public void Stop(AnimatedCharacter target)
        {
            Sequencer.Stop(target);
            target.RemoveStateByName(Name);
        }

        public List<AnimationElement> AnimationElements => _sequencer?.AnimationElements;

        public SequenceType Type
        {
            get { return Sequencer.Type; }

            set { Sequencer.Type = value; }
        }

        public void InsertManyAnimationElements(List<AnimationElement> animationElements)
        {
            Sequencer.InsertManyAnimationElements(animationElements);
        }

        public void InsertAnimationElement(AnimationElement animationElement)
        {
            Sequencer.InsertAnimationElement(animationElement);
        }

        public void InsertAnimationElementAfter(AnimationElement toInsert, AnimationElement moveAfter)
        {
            Sequencer.InsertAnimationElementAfter(toInsert, moveAfter);
        }

        public void RemoveAnimationElement(AnimationElement animationElement)
        {
            Sequencer.RemoveAnimationElement(animationElement);
        }

        public void Render(bool completeEvent = true)
        {
            Play();
        }

        public AnimatedAbility Clone(AnimatedCharacter target)
        {
            var clonedSequence = ((AnimationSequenceImpl) Sequencer).Clone(target) as AnimationSequence;

            AnimatedAbility clone = new AnimatedAbilityImpl(clonedSequence);
            clone.Target = target;
            clone.Name = Name;
            clone.KeyboardShortcut = KeyboardShortcut;
            clone.Persistent = Persistent;
            clone.Target = Target;
            clone.StopAbility = StopAbility.Clone(target);
            return clone;
        }

        public void Stop(List<AnimatedCharacter> targets)
        {
        }

        public bool Equals(AnimatedAbility other)
        {
            if (other.KeyboardShortcut != KeyboardShortcut) return false;
            if (other.Name != Name) return false;
            if (other.Order != Order) return false;
            if (other.Persistent != Persistent) return false;
            if (other.Sequencer.Equals(Sequencer) == false) return false;
            return true;
        }
    }
}