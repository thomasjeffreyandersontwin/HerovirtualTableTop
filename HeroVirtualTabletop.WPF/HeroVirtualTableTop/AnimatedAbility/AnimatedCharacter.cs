using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HeroVirtualTableTop.Crowd;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;
using HeroVirtualTableTop.Attack;
namespace HeroVirtualTableTop.AnimatedAbility
{
    public class AnimatedCharacterImpl : ManagedCharacterImpl, AnimatedCharacter, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private List<AnimatableCharacterState> _activeStates;

        private List<FXElement> _loadedFXs;

        public AnimatedCharacterImpl( DesktopCharacterTargeter targeter,
            KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities,
            AnimatedCharacterRepository repo) : base( targeter, generator, camera, identities)
        {
            _loadedFXs = new List<FXElement>();
            Abilities = new CharacterActionListImpl<AnimatedAbility>(CharacterActionType.Ability, generator, this);
            loadDefaultAbilities();
            _repo = repo;

        }

        private AnimatedCharacterRepository _repo;

        public AnimatedCharacterRepository Repository
        {
            get { return _repo ?? null; }
            set { _repo = value; }
        }

        public CharacterActionList<AnimatedAbility> Abilities { get; }

        private AnimatedAttack _activeAttack;

        public AnimatedAttack ActiveAttack
        {
            get { return _activeAttack; }
            set
            {
                _activeAttack = value;
                NotifyPropertyChanged();
            }
        }

        public Position Facing { get; set; }

        private bool isActive;
        public bool IsActive {
            get { return isActive; }
            set
            {
                isActive = value;
                NotifyPropertyChanged();
            }

        }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public List<AnimatedAbility> ActivePersistentAbilities
        {
            get { throw new NotImplementedException(); }
        }

        public List<FXElement> LoadedFXs => _loadedFXs ?? (_loadedFXs = new List<FXElement>());

        public List<AnimatableCharacterState> ActiveStates => _activeStates ?? (_activeStates = new List<AnimatableCharacterState>());

        public void AddState(AnimatableCharacterState state, bool playImmediately = true)
        {
            ActiveStates.Add(state);
            if (state.AbilityAlreadyPlayed == false)
            {
                state.Ability.Play(this);
                state.AbilityAlreadyPlayed = true;
            }
        }

        public void AddDefaultState(string defaultState, bool playImmediately = true)
        {
            if (Repository.CharacterByName.ContainsKey(DefaultAbilities.CharacterName))
            {

                AnimatedAbility defaultAbility = Repository?.CharacterByName?[DefaultAbilities.CharacterName]
                    ?.Abilities?[defaultState];

                if (defaultAbility != null)
                {
                    AnimatableCharacterState state = new AnimatableCharacterStateImpl(defaultAbility, this);
                    AddState(state);
                }
            }

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
            state.Ability.Stop(this);
            state.AbilityAlreadyPlayed = false;
            var remove = state.Ability.StopAbility;
            remove.Play(this);
        }

        public void ResetAllAbiltitiesAndState()
        {
            var states = ActiveStates.ToList();
            foreach (var state in states)
                RemoveState(state);
        }

        public void RemoveStateByName(string name)
        {
            var state = (from s in ActiveStates where s.StateName == name select s).FirstOrDefault();
            if (state != null)
                ActiveStates.Remove(state);
        }

        public void TurnTowards(Position position)
        {
            Position.TurnTowards(position);
        }

        public void loadDefaultAbilities()
        {
            if (Repository != null)
                if (Repository.CharacterByName.ContainsKey(DefaultAbilities.CharacterName))
                {
                    var defaultCharacter = Repository.CharacterByName[DefaultAbilities.CharacterName];
                    foreach (var defaultAbility in defaultCharacter.Abilities.Values)
                        if (Abilities.ContainsKey(defaultAbility.Name) == false)
                            if (defaultCharacter.Abilities.ContainsKey(defaultAbility.Name))
                                Abilities[defaultAbility.Name] = defaultCharacter.Abilities[defaultAbility.Name];

                    //to do load the rest of the default abilities
                }
        }
    }

    internal class AnimatableCharacterStateImpl : AnimatableCharacterState
    {
        public AnimatableCharacterStateImpl(AnimatedAbility ability, AnimatedCharacter target)
        {
            StateName = ability.Name;
            Target = target;
            Ability = ability;
        }

        public AnimatedAbility Ability { get; set; }

        public AnimatedCharacter Target { get; set; }

        public bool Rendered { get; set; }

        public string StateName { get; set; }

        public bool AbilityAlreadyPlayed { get; set; }

        public void AddToCharacter(AnimatedCharacter character)
        {
            character.AddState(this);
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