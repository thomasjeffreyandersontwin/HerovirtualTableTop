using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimatedAbility : SequenceElement
    {
        [JsonConstructor]
        private AnimatedAbility() : base(string.Empty) { }

        public AnimatedAbility(string name, Keys activateOnKey = Keys.None, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, seqType, persistent, order, owner)
        {
            this.ActivateOnKey = activateOnKey;
        }

        private Keys activateOnKey;
        public Keys ActivateOnKey
        {
            get
            {
                return activateOnKey;
            }
            set
            {
                activateOnKey = value;
                OnPropertyChanged("ActivateOnKey");
            }
        }

        private bool isAttack;
        public bool IsAttack
        {
            get
            {
                return isAttack;
            }
            set
            {
                isAttack = value;
                if (!value)
                    this.IsAreaEffect = false;
                OnPropertyChanged("IsAttack");
            }
        }

        private bool isAreaEffect;
        public bool IsAreaEffect
        {
            get
            {
                return isAreaEffect;
            }
            set
            {
                isAreaEffect = value;
                OnPropertyChanged("IsAreaEffect");
            }
        }

        public override AnimationElement Clone()
        {
            AnimatedAbility clonedAbility = new AnimatedAbility(this.Name, Keys.None, this.SequenceType, this.Persistent);
            clonedAbility.DisplayName = this.DisplayName;
            foreach (var element in this.AnimationElements)
            {
                var clonedElement = (element as AnimationElement).Clone() as AnimationElement;
                clonedAbility.AddAnimationElement(clonedElement);
            }
            clonedAbility.animationElements = new HashedObservableCollection<AnimationElement, string>(clonedAbility.AnimationElements, x => x.Name, x => x.Order);
            clonedAbility.AnimationElements = new ReadOnlyHashedObservableCollection<AnimationElement, string>(clonedAbility.animationElements);
            clonedAbility.ActivateOnKey = this.ActivateOnKey;
            clonedAbility.IsAreaEffect = this.IsAreaEffect;
            clonedAbility.IsAttack = this.IsAttack;
            clonedAbility.Name = this.Name;
            return clonedAbility;
        }
    }

    public class AttackEffect : AnimatedAbility
    {
        [JsonConstructor]
        private AttackEffect() : base(string.Empty) { }
        public AttackEffect(string name, Keys activateOnKey = Keys.None, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, activateOnKey, seqType, persistent, order, owner)
        {

        }
        public AttackEffectOption Effect
        {
            get;
            set;
        }

        public double KnockBackDistance
        {
            get;
            set;

        }
        public KnockBackOption KnockbackEffect
        {
            get;
            set;
        }

        public Movement KnockBack
        {
            get;
            set;
        }

        public void Render()
        {
            // should probably call Move() of its knockBack effects (Movements). Will be implemented later
        }
    }

    public class Attack : AnimatedAbility
    {
        #region Events

        public event EventHandler AttackInitiated;
        public void OnAttackInitiated(object sender, CustomEventArgs<Attack> e)
        {
            if (AttackInitiated != null)
                AttackInitiated(sender, e);
        }

        #endregion

        [JsonConstructor]
        private Attack() : base(string.Empty) { }
        public Attack(string name, Keys activateOnKey = Keys.None, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, activateOnKey, seqType, persistent, order, owner)
        {
            this.OnHitAnimation = new AnimatedAbility(this.Name + " - OnHit", Keys.None, AnimationSequenceType.And, false, 1, this.Owner);
        }

        private AnimatedAbility onHitAnimation;
        public AnimatedAbility OnHitAnimation
        {
            get
            {
                return onHitAnimation;
            }
            set
            {
                onHitAnimation = value;
                OnPropertyChanged("OnHitAnimation");
            }
        }

        private AttackEffect attackEffect;
        public AttackEffect AttackEffect
        {
            get
            {
                return attackEffect;
            }
            set
            {
                attackEffect = value;
                OnPropertyChanged("AttackEffect");
            }
        }

        private bool isActive;
        public override bool IsActive
        {
            get
            {
                return isActive || base.IsActive;
            }
            protected set
            {
                isActive = value;
                OnPropertyChanged("IsActive");
            }
        }
        public override void Stop(Character Target = null)
        {
            IsActive = false;
            base.Stop(Target);
        }

        public string InitiateAttack(bool persistent = false, Character target = null)
        {
            var character = target ?? this.Owner;
            Stop(character);
            //if (this.Persistent || persistent)
            IsActive = true;
            // Change the costume to Complementary color - CHRIS to do
            character.Activate();
            // Fire event to update Roster and select target
            OnAttackInitiated(character, new CustomEventArgs<Attack> { Value = this });

            return null;
        }

        private AnimatedAbility GetHitAbility()
        {
            AnimatedAbility ability = null;
            if (this.OnHitAnimation != null && this.OnHitAnimation.AnimationElements != null && this.OnHitAnimation.AnimationElements.Count > 0)
            {
                ability = this.OnHitAnimation;
            }
            else 
            {
                if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
                {
                    var globalHitAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.HIT_ABITIY_NAME);
                    if (globalHitAbility != null && globalHitAbility.AnimationElements != null && globalHitAbility.AnimationElements.Count > 0)
                    {
                        ability = globalHitAbility;
                    }
                }
            }
            return ability;
        }

        public void AnimateAttack(AttackDirection direction, Character attacker)
        {
            this.SetAttackDirection(direction);
            this.SetAttackerFacing(direction, attacker);
            base.Play(false, attacker); 
            // Reset FX direction
            this.SetAttackDirection(null);
        }

        private void SetAttackDirection(AttackDirection direction)
        {
            foreach (var animation in this.AnimationElements)
            {
                if (animation is FXEffectElement)
                {
                    (animation as FXEffectElement).AttackDirection = direction;
                }
            }
        }

        private void SetAttackerFacing(AttackDirection direction, Character attacker)
        {
            Vector3 facingVector = new Vector3(direction.AttackDirectionX, direction.AttackDirectionY, direction.AttackDirectionZ);
            (attacker.Position as Position).SetTargetFacing(facingVector);
        }

        private AnimatedAbility GetMissAbility()
        {
            AnimatedAbility ability = null;
            if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
            {
                var globalMissAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.MISS_ABITIY_NAME);
                if (globalMissAbility != null && globalMissAbility.AnimationElements != null && globalMissAbility.AnimationElements.Count > 0)
                {
                    ability = globalMissAbility;
                }
            }
            return ability;
        }

        public void AnimateKnockBack(List<Character> targets)
        {

        }

        public void AnimateAttackSequence(Character attackingCharacter, List<Character> defendingCharacters)
        {
            AttackDirection direction = new AttackDirection();
            if (defendingCharacters == null || defendingCharacters.Count == 0)
            {
                var targetInFacingDirection = (attackingCharacter.Position as Module.HeroVirtualTabletop.Library.ProcessCommunicator.Position).GetTargetInFacingDirection();
                direction.AttackDirectionX = targetInFacingDirection.X;
                direction.AttackDirectionY = targetInFacingDirection.Y;
                direction.AttackDirectionZ = targetInFacingDirection.Z;
            }
            else
            {
                Character centerTargetCharacter = defendingCharacters.Where(dc => dc.ActiveAttackConfiguration.IsCenterTarget).FirstOrDefault();
                if (centerTargetCharacter == null)
                {
                    var targetInFacingDirection = (attackingCharacter.Position as Module.HeroVirtualTabletop.Library.ProcessCommunicator.Position).GetTargetInFacingDirection();
                    direction.AttackDirectionX = targetInFacingDirection.X;
                    direction.AttackDirectionY = targetInFacingDirection.Y;
                    direction.AttackDirectionZ = targetInFacingDirection.Z;
                }
                else
                {
                    if (centerTargetCharacter.ActiveAttackConfiguration.AttackResult == AttackResultOption.Hit)
                    {
                        direction.AttackDirectionX = centerTargetCharacter.Position.X;
                        direction.AttackDirectionY = centerTargetCharacter.Position.Y + 4.0f;
                        direction.AttackDirectionZ = centerTargetCharacter.Position.Z;
                    }
                    else
                    {
                        Random rand = new Random();
                        int randomOffset = rand.Next(2, 7);
                        int multiplyOffset = rand.Next(11, 20);
                        int multiplyFactorX = multiplyOffset % 2 == 0 ? 1 : -1;
                        direction.AttackDirectionX = centerTargetCharacter.Position.X + randomOffset * multiplyFactorX;
                        multiplyOffset = rand.Next(11, 20);
                        int multiplyFactorY = multiplyOffset % 2 == 0 ? 1 : -1;
                        direction.AttackDirectionY = centerTargetCharacter.Position.Y + 5.0f + randomOffset * multiplyFactorY;
                        multiplyOffset = rand.Next(11, 20);
                        int multiplyFactorZ = multiplyOffset % 2 == 0 ? 1 : -1;
                        direction.AttackDirectionZ = centerTargetCharacter.Position.Z + randomOffset * multiplyFactorZ;
                    }
                }
            }
            //AnimateAttack(direction, attackingCharacter);
            //System.Threading.Thread.Sleep(1000); // Delay between attack and on hit animations
            this.SetAttackDirection(direction);
            this.SetAttackerFacing(direction, attackingCharacter);

            AnimationElement lastKeybindAnimation = this.AnimationElements.LastOrDefault(a => a.Type == AnimationType.Movement || a.Type == AnimationType.FX);

            SequenceElement attackSequenceMain = new SequenceElement("attackSequenceMain", AnimationSequenceType.And);
            attackSequenceMain.Owner = attackingCharacter;
            foreach (AnimationElement element in this.AnimationElements.Except(new List<AnimationElement> { lastKeybindAnimation }))
            {
                attackSequenceMain.AddAnimationElement(element);
            }
            attackSequenceMain.Play(false, attackingCharacter);

            SequenceElement attackChainSequenceElement = new SequenceElement("attackChainSequence", AnimationSequenceType.And);
            Dictionary<AnimationElement, List<Character>> characterAnimationMappingDictionary = new Dictionary<AnimationElement, List<Character>>();
            if(lastKeybindAnimation != null)
            {
                attackChainSequenceElement.AddAnimationElement(lastKeybindAnimation);
                characterAnimationMappingDictionary.Add(lastKeybindAnimation, new List<Character> { attackingCharacter });
            }
            
            if(defendingCharacters != null && defendingCharacters.Count > 0)
            {
                // Attack results
                var hitAbility = this.GetHitAbility();
                List<Character> hitTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackResult == AttackResultOption.Hit).ToList();
                var missAbility = this.GetMissAbility();
                List<Character> missTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackResult == AttackResultOption.Miss).ToList();

                if (hitTargets.Count > 0)
                {
                    attackChainSequenceElement.AddAnimationElement(hitAbility);
                    characterAnimationMappingDictionary.Add(hitAbility, hitTargets);
                }

                if (missTargets.Count > 0)
                {
                    attackChainSequenceElement.AddAnimationElement(missAbility);
                    characterAnimationMappingDictionary.Add(missAbility, missTargets);
                }

                // Effects
                var globalDeadAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DEAD_ABITIY_NAME);
                List<Character> dyingTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Dying).ToList();
                var globalDyingAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DYING_ABILITY_NAME);
                List<Character> deadTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Dead).ToList();
                var globalUnconciousAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.UNCONCIOUS_ABITIY_NAME);
                List<Character> unconciousTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Unconcious).ToList();
                var globalStunnedAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.STUNNED_ABITIY_NAME);
                List<Character> stunnedTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Stunned).ToList();

                if (deadTargets.Count > 0)
                {
                    attackChainSequenceElement.AddAnimationElement(globalDeadAbility);
                    characterAnimationMappingDictionary.Add(globalDeadAbility, deadTargets);
                }

                if (dyingTargets.Count > 0)
                {
                    attackChainSequenceElement.AddAnimationElement(globalDyingAbility);
                    characterAnimationMappingDictionary.Add(globalDyingAbility, dyingTargets);
                }

                if (unconciousTargets.Count > 0)
                {
                    attackChainSequenceElement.AddAnimationElement(globalUnconciousAbility);
                    characterAnimationMappingDictionary.Add(globalUnconciousAbility, unconciousTargets);
                }

                if (stunnedTargets.Count > 0)
                {
                    attackChainSequenceElement.AddAnimationElement(globalStunnedAbility);
                    characterAnimationMappingDictionary.Add(globalStunnedAbility, stunnedTargets);
                }
            }
            

            // Finally play as chained 
            IconInteractionUtility.ExecuteCmd(new KeyBindsGenerator().PopEvents());
            attackChainSequenceElement.PlayGrouped(characterAnimationMappingDictionary).RunSynchronously();

            // Reset FX direction
            this.SetAttackDirection(null);
            attackingCharacter.Deactivate();
        }

        public override void Play(bool persistent = false, Character target = null, bool forcePlay = false)
        {
            if (this.IsAttack)
                this.InitiateAttack(persistent, target);
            else
                base.Play(persistent, target, forcePlay);
        }

        public override AnimationElement Clone()
        {
            Attack clonedAttack = new Attack(this.Name, Keys.None, this.SequenceType, this.Persistent);
            clonedAttack.DisplayName = this.DisplayName;
            foreach (var element in this.AnimationElements)
            {
                var clonedElement = (element as AnimationElement).Clone() as AnimationElement;
                clonedAttack.AddAnimationElement(clonedElement);
            }
            clonedAttack.animationElements = new HashedObservableCollection<AnimationElement, string>(clonedAttack.AnimationElements, x => x.Name, x => x.Order);
            clonedAttack.AnimationElements = new ReadOnlyHashedObservableCollection<AnimationElement, string>(clonedAttack.animationElements);
            clonedAttack.ActivateOnKey = this.ActivateOnKey;
            clonedAttack.IsAreaEffect = this.IsAreaEffect;
            clonedAttack.IsAttack = this.IsAttack;
            clonedAttack.Name = this.Name;
            clonedAttack.OnHitAnimation = this.OnHitAnimation.Clone() as AnimatedAbility;
            return clonedAttack;
        }
    }
    public class ActiveAttackConfiguration : NotifyPropertyChanged
    {
        private bool isCenterTarget;
        public bool IsCenterTarget
        {
            get
            {
                return isCenterTarget;
            }
            set
            {
                isCenterTarget = value;
                OnPropertyChanged("IsCenterTarget");
            }
        }
        private AttackMode attackMode;
        public AttackMode AttackMode // None/Attack/Defend
        {
            get
            {
                return attackMode;
            }
            set
            {
                attackMode = value;
                OnPropertyChanged("AttackMode");
            }
        }

        private KnockBackOption knockBackOption; // Knockback/KnockDown
        public KnockBackOption KnockBackOption
        {
            get
            {
                return knockBackOption;
            }
            set
            {
                knockBackOption = value;
                OnPropertyChanged("KnockBackOption");
            }
        }
        private AttackResultOption attackResult;
        public AttackResultOption AttackResult // Miss/Hit
        {
            get
            {
                return attackResult;
            }
            set
            {
                attackResult = value;
                OnPropertyChanged("AttackResult");
            }
        }

        private AttackEffectOption attackEffectOption;
        public AttackEffectOption AttackEffectOption // None/Stunned/Unconcious/Dying/Dead
        {
            get
            {
                return attackEffectOption;
            }
            set
            {
                attackEffectOption = value;
                OnPropertyChanged("AttackEffectOption");
            }
        }
    }
    public class AttackDirection
    {
        public float AttackDirectionX
        {
            get;
            set;
        }
        public float AttackDirectionY
        {
            get;
            set;
        }
        public float AttackDirectionZ
        {
            get;
            set;
        }
        public AttackDirection()
        {

        }
        public AttackDirection(Vector3 direction)
        {
            this.AttackDirectionX = direction.X;
            this.AttackDirectionY = direction.Y;
            this.AttackDirectionZ = direction.Z;
        }
    }
}
