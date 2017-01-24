using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using HeroVirtualTableTop.Crowd;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;

namespace HeroVirtualTableTop.AnimatedAbility
{
    class AnimatedCharacterImpl : CharacterCrowdMemberImpl, AnimatedCharacter
    {
        public AnimatedCharacterImpl(Crowd.Crowd parent, DesktopCharacterTargeter targeter, KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities, CrowdRepository repo):base(parent,targeter,generator,camera,identities,repo)
        {
            _loadedFXs = new List<FXElement>();
        }

        private List<FXElement> _loadedFXs;
        public List<FXElement> LoadedFXs
        {
            get
            {
                return _loadedFXs;
            }
        }

        public CharacterActionList<AnimatedAbility> Abilities
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public AnimatedAttack ActiveAttackCycle
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                
            }
        }

        public List<AnimatedAbility> ActivePersistentAbilities
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private List<AnimatableCharacterState> _activeStates;
        public List<AnimatableCharacterState> ActiveStates
        {
            get
            {
                if (_activeStates == null)
                {
                    _activeStates = new List<AnimatableCharacterState>();
                }
                return _activeStates;
            }

        }
        public Position Facing { get; set; }
        public bool IsActive { get; set; }    
        public void Activate()
        {
            throw new NotImplementedException();
        }
        public void AddState(AnimatableCharacterState state, bool playImmediately = true)
        {
            ActiveStates.Add(state);
        }
        public void DeActivate()
        {
            throw new NotImplementedException();
        }
        public KnockbackCollisionInfo PlayCompleteExternalAttack(AnimatedAttack attack, AttackInstructions instructions)
        {
            throw new NotImplementedException();
        }
        public void PlayExternalAnimatedAbility(AnimatedAbility ability)
        {
            throw new NotImplementedException();
        }
        public void RemoveState(AnimatableCharacterState state, bool playImmediately = true)
        {
            ActiveStates.Remove(state);
            //
            AnimatedAbility remove = state.Ability.AbilityToPlayOnRemove;
            remove.Play(this);
        }
        public void ResetAllAbiltities()
        {
            throw new NotImplementedException();
        }
    }

    class AnimatableCharacterStateImpl : AnimatableCharacterState
    {
        public AnimatableCharacterStateImpl(AnimatedAbility ability, AnimatedCharacter target)
        {
            StateName = ability.Name;
            Target = target;
            Ability = ability;
        }
        public AnimatedAbility Ability
        {
            get;
            set;
        }
        public AnimatedCharacter Target
        {
            get;
            set;
        }
        public bool Rendered
        {
            get;
            set;
        }
        public string StateName
        {
            get;
            set;
        }
        public void AddToCharacter(AnimatedCharacter character)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromCharacter(AnimatedCharacter character)
        {
            throw new NotImplementedException();
        }

        public void RenderRemovalOfState()
        {
            throw new NotImplementedException();
        }
    }
}
