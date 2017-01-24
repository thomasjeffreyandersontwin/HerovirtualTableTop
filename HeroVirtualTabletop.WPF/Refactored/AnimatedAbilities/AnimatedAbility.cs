using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;

namespace HeroVirtualTableTop.AnimatedAbility
{
    class AnimatedAbilityImpl : AnimatedAbility, AnimationSequence
    {
        public AnimatedAbility AbilityToPlayOnRemove { get; set; }
        public KeyBindCommandGenerator Generator
        {
            get; set;
        }
        public string KeyboardShortcut
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public int Order
        {
            get; set;
        }
        public AnimatedCharacter Target
        {
            get; set;

        }

        public ManagedCharacter.ManagedCharacter Owner
        {
            get
            {
                return Target as ManagedCharacter.ManagedCharacter;
            }

            set
            {
                if (value is AnimatedCharacter)
                {
                    Target = value as AnimatedCharacter;
                }
            }
        }
        public bool Persistent
        {
            get; set;
        }

        public CharacterAction Clone()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            Play(Target);
        }

        public void Play(List<AnimatedCharacter> targets)
        {
            //
        }
        public void Play(AnimatedCharacter target)
        {
            Sequencer.Play(target);
            AnimatableCharacterState newstate = new AnimatableCharacterStateImpl(this, Target);
            target.AddState(newstate);
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

        private AnimationSequence _sequencer;
        public List<AnimationElement> AnimationElements
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public AnimationSequence Sequencer
        {
            get
            {
                if (_sequencer == null)
                {
                    _sequencer = new AnimationSequenceImpl(this.Target);
                }
                return _sequencer;
            }

        }
        public SequenceType Type
        {
            get
            {
                return Sequencer.Type;
            }

            set
            {
                Sequencer.Type = value;
            }
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
            throw new NotImplementedException();
        }
    }
}
