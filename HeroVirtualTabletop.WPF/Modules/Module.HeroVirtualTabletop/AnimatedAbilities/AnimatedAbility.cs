using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
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

        public string AnimateHit(List<Character> targets)
        {
            // Play the attack's on hit ability if there exists one
            if (this.OnHitAnimation != null && this.OnHitAnimation.AnimationElements != null && this.OnHitAnimation.AnimationElements.Count > 0)
            {
                this.OnHitAnimation.PlayGrouped(targets);
            }
            else // Or play the default hit ability
            {
                if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
                {
                    var globalHitAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.HIT_ABITIY_NAME);
                    if (globalHitAbility != null && globalHitAbility.AnimationElements != null && globalHitAbility.AnimationElements.Count > 0)
                    {
                        globalHitAbility.PlayGrouped(targets).RunSynchronously();
                    }
                }
            }
            // TODO: Animate Knockback
            this.AnimateKnockBack(targets);
            System.Threading.Thread.Sleep(1000);
            // Now play most severe of effects
            this.AnimateAttackEffects(targets);
            return null;
        }
        public string AnimateAttackEffects(List<Character> targets)
        {
            if (Helper.GlobalCombatAbilities != null && Helper.GlobalCombatAbilities.Count > 0)
            {
                var globalDeadAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DEAD_ABITIY_NAME);
                globalDeadAbility.PlayGrouped(targets.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Dead ).ToList()).RunSynchronously();
                        
                var globalDyingAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DYING_ABILITY_NAME);
                globalDyingAbility.PlayGrouped(targets.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Dying).ToList()).RunSynchronously();

                var globalUnconciousAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.UNCONCIOUS_ABITIY_NAME);
                globalUnconciousAbility.PlayGrouped(targets.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Unconcious).ToList()).RunSynchronously();

                var globalStunnedAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.STUNNED_ABITIY_NAME);
                globalStunnedAbility.PlayGrouped(targets.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Stunned).ToList()).RunSynchronously();
            }
            return null;
        }
        public void AnimateAttack(AttackDirection direction, Character attacker)
        {
            foreach (var animation in this.AnimationElements)
            {
                if (animation is FXEffectElement)
                {
                    (animation as FXEffectElement).AttackDirection = direction;
                }
            }
            base.Play(false, attacker); // TODISCUSS: Can an attack actually be persistent and should we allow playing it as persistent?
            // Restet FX direction
            foreach (var animation in this.AnimationElements)
            {
                if (animation is FXEffectElement)
                {
                    (animation as FXEffectElement).AttackDirection = null;
                }
            }
            //System.Threading.Thread.Sleep(2000); // Wait for attacker to finish moves before deactivating

        }

        public void AnimateMiss(List<Character> targets)
        {
            // Just play the default miss ability on the defender
            if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
            {
                var globalMissAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.MISS_ABITIY_NAME);
                if (globalMissAbility != null && globalMissAbility.AnimationElements != null && globalMissAbility.AnimationElements.Count > 0)
                {
                    globalMissAbility.PlayGrouped(targets).RunSynchronously();
                }
            }
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
                        direction.AttackDirectionY = centerTargetCharacter.Position.Y + 4.0d;
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
                        direction.AttackDirectionY = centerTargetCharacter.Position.Y + 5.0d + randomOffset * multiplyFactorY;
                        multiplyOffset = rand.Next(11, 20);
                        int multiplyFactorZ = multiplyOffset % 2 == 0 ? 1 : -1;
                        direction.AttackDirectionZ = centerTargetCharacter.Position.Z + randomOffset * multiplyFactorZ;
                    }
                }

            }
            AnimateAttack(direction, attackingCharacter);
            System.Threading.Thread.Sleep(1000); // Delay between attack and on hit animations

            if (defendingCharacters != null && defendingCharacters.Count > 0)
            {
                // Executing the attack on parallel thread actually works, but it sends the keybinds way too fast to the game to process, thus missing a few animations
                // Therefore commenting out the parallel code and executing synchronous loop
                //Parallel.ForEach(defendingCharacters, (targetCharacter) =>
                //{
                //    ActiveAttackConfiguration attackConfiguration = targetCharacter.ActiveAttackConfiguration;
                //    if (attackConfiguration.AttackResult == AttackResultOption.Hit)
                //        AnimateHit(attackConfiguration, targetCharacter);
                //    else if (attackConfiguration.AttackResult == AttackResultOption.Miss && targetCharacter != null && attackConfiguration.AttackEffectOption != AttackEffectOption.None)
                //        AnimateMiss(attackConfiguration, targetCharacter);
                //});

                AnimateHit(defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackResult == AttackResultOption.Hit).ToList());
                AnimateMiss(defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackResult == AttackResultOption.Miss).ToList());
                
            }
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
        public double AttackDirectionX
        {
            get;
            set;
        }
        public double AttackDirectionY
        {
            get;
            set;
        }
        public double AttackDirectionZ
        {
            get;
            set;
        }
    }
}
