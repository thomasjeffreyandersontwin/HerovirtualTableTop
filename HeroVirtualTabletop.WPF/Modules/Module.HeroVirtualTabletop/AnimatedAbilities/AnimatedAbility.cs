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

        //private AnimatedAbility attackAnimation;
        public AnimatedAbility AttackAnimation 
        {
            get
            {
                return this;
            }
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

        public string AnimateAttack(bool persistent = false, Character target = null)
        {
            var character = target ?? this.Owner;
            Stop(character);
            if (this.Persistent || persistent)
                IsActive = true;
            // Change the costume to Complementary color - Chris to do
            
            // FIRE AN EVENT TO UPDATE ROSTER AND DECIDE ABOUT TARGET
            OnAttackInitiated(character, new CustomEventArgs<Attack> { Value = this });
            // Wait for target updated event to fire the abilities
            // If hit is selected play AnimateHit
            // If miss is selected play AnimateMiss

            return null;
        }

        public string AnimateOnHit()
        {
            return null;
        }

        public void AnimateHit()
        {

        }

        public void AnimateMiss()
        {

        }

        public void AnimateKnockBack()
        {

        }
        public override string Play(bool persistent = false, Character target = null)
        {
            if (this.IsAttack)
                return this.AnimateAttack(persistent, target);
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

    public class AttackOption
    {
        public AttackMode AttackMode
        {
            get;
            set;
        }
        public AttackEffectOption AttackEffectOption
        {
            get;
            set;
        }
    }
}
