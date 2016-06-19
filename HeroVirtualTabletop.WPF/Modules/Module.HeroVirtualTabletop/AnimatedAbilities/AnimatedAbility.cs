using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimatedAbility : SequenceElement, ICharacterOption
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
                OnPropertyChanged("IsAttack");
            }
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
            this.OnHitAnimation = new AnimatedAbility(this.Name + " - DefenderHit", Keys.None, AnimationSequenceType.And, false, 1, this.Owner);
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

        public string InitiateAttack(bool persistent = false, Character target = null)
        {
            var character = target ?? this.Owner;
            Stop(character);
            if (this.Persistent || persistent)
                IsActive = true;
            // Change the costume to Complementary color - Chris to do
            
            // FIRE AN EVENT TO UPDATE ROSTER AND DECIDE ABOUT TARGET
            OnAttackInitiated(character, new CustomEventArgs<Attack> { Value = this });

            return null;
        }

        public string AnimateHit(ActiveAttackConfiguration attackConfiguration, Character target = null)
        {
            return null;
        }

        public void AnimateAttack(AttackDirection direction, Character attacker = null)
        {
            foreach(var animation in this.AnimationElements)
            {
                if(animation is FXEffectElement)
                {
                    (animation as FXEffectElement).AttackDirection = direction;
                }
            }
            base.Play(false, attacker); // TODISCUSS: Can an attack actually be persistent and should we allow playing it as persistent?
            // Restore FX direction as attack is complete
            foreach (var animation in this.AnimationElements)
            {
                if (animation is FXEffectElement)
                {
                    (animation as FXEffectElement).AttackDirection = null;
                    // TODO: Restore Secondary colors for costume of the attacker - Chris
                }
            }
        }

        public void AnimateMiss(ActiveAttackConfiguration attackConfiguration, Character target = null)
        {

        }

        public void AnimateKnockBack(ActiveAttackConfiguration attackConfiguration, Character target = null)
        {

        }

        public void AnimateAttackSequence(Character attackingCharacter, Character defendingCharacter, ActiveAttackConfiguration attackConfiguration)
        {
            AttackDirection direction = new AttackDirection();
            if(attackConfiguration.AttackResult == AttackResultOption.Hit)
            {
                direction.AttackDirectionX = defendingCharacter.Position.X;
                direction.AttackDirectionY = defendingCharacter.Position.Y + 4.0d;// Aim at the Chest :p
                direction.AttackDirectionZ = defendingCharacter.Position.Z;
            }
            else
            {
                Random rand = new Random();
                int randomOffset = rand.Next(4, 10);
                direction.AttackDirectionX = defendingCharacter.Position.X + randomOffset;
                direction.AttackDirectionY = defendingCharacter.Position.Y + 6.0d + randomOffset; // Might need to modify in future, not sure how tall these guys can grow to
                direction.AttackDirectionZ = defendingCharacter.Position.Z + randomOffset;
            }
            
            AnimateAttack(direction, attackingCharacter);
            if (attackConfiguration.AttackResult == AttackResultOption.Hit)
                AnimateHit(attackConfiguration, defendingCharacter);
            else
                AnimateMiss(attackConfiguration, defendingCharacter);
        }
        public override string Play(bool persistent = false, Character target = null)
        {
            if (this.IsAttack)
                return this.InitiateAttack(persistent, target);
            else
                return base.Play(persistent, target);
        }
        
    }

    public class AreaEffectAttack : Attack
    {
        [JsonConstructor]
        private AreaEffectAttack() : base(string.Empty) { }
        public AreaEffectAttack(string name, Keys activateOnKey = Keys.None, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, activateOnKey, seqType, persistent, order, owner)
        {

        }
        public List<AttackEffect> AttackEffects
        {
            get;
            set;
        }

        public void AnimateAttackEffects()
        {

        }
    }
    public class ActiveAttackConfiguration : NotifyPropertyChanged
    {
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
