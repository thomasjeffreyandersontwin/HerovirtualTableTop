﻿using Framework.WPF.Library;
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
using System.Text.RegularExpressions;
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

        public static string GetAppropriateAnimationName(AnimationType animationType, List<AnimationElement> collection)
        {
            string name = "";
            switch (animationType)
            {
                case AnimationType.Movement:
                    name = "Mov Element";
                    break;
                case AnimationType.FX:
                    name = "FX Element";
                    break;
                case AnimationType.Pause:
                    name = "Pause Element";
                    break;
                case AnimationType.Sequence:
                    name = "Seq Element";
                    break;
                case AnimationType.Sound:
                    name = "Sound Element";
                    break;
                case AnimationType.Reference:
                    name = "Ref Element";
                    break;
            }

            string suffix = " 1";
            string rootName = name;
            int i = 1;
            Regex reg = new Regex(@"\d+");
            MatchCollection matches = reg.Matches(name);
            if (matches.Count > 0)
            {
                int k;
                Match match = matches[matches.Count - 1];
                if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out k))
                {
                    i = k + 1;
                    suffix = string.Format(" {0}", i);
                    rootName = name.Substring(0, match.Index).TrimEnd();
                }
            }
            string newName = rootName + suffix;
            while (collection.Where(a => a.Name == newName).FirstOrDefault() != null)
            {
                suffix = string.Format(" {0}", ++i);
                newName = rootName + suffix;
            }
            return newName;
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
            set
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
                if (Helper.GlobalCombatAbilities != null && Helper.GlobalCombatAbilities.Count > 0)
                {
                    var globalHitAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.HIT_ABITIY_NAME);
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

        public void AnimateAttackSequence(Character attackingCharacter, List<Character> defendingCharacters)
        {
            AttackDirection direction = new AttackDirection();
            int attackDelay = 0;
            float distance = 0;
            PauseElement unitPauseElement = this.AnimationElements.LastOrDefault(a => a.Type == AnimationType.Pause && (a as PauseElement).IsUnitPause) as PauseElement;
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

                    distance = GetClosestTargetDistance(attackingCharacter, defendingCharacters);
                }
                else
                {
                    Vector3 vAttacker = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
                    Vector3 vCenterTarget = new Vector3(centerTargetCharacter.Position.X, centerTargetCharacter.Position.Y, centerTargetCharacter.Position.Z);
                    distance = Vector3.Distance(vAttacker, vCenterTarget);

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

            if (unitPauseElement != null)
            {
                DelayManager delayManager = new DelayManager(unitPauseElement);
                attackDelay = (int)delayManager.GetDelayForDistance(distance);
            }
            AnimateAttack(direction, attackingCharacter);
            System.Threading.Thread.Sleep(attackDelay); // Delay between attack and on hit animations

            AnimateHitAndMiss(defendingCharacters);
            AnimateAttackEffects(defendingCharacters);
            AnimateKnockBack(attackingCharacter, defendingCharacters);

            // Reset FX direction
            this.SetAttackDirection(null);
            attackingCharacter.Deactivate();
        }

        private float GetClosestTargetDistance(Character attackingCharacter, List<Character> defendingCharacters)
        {
            float minDistance = 0;
            Vector3 vAttacker = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
            foreach (Character defendingCharacter in defendingCharacters)
            {
                Vector3 vDefender = new Vector3(defendingCharacter.Position.X, defendingCharacter.Position.Y, defendingCharacter.Position.Z);
                var distance = Vector3.Distance(vAttacker, vDefender);
                minDistance = minDistance == 0 ? distance : distance < minDistance ? distance : minDistance;
            }

            return minDistance;
        }

        private SequenceElement GetSequenceToPlay(SequenceElement sequenceElement)
        {
            SequenceElement sequenceToPlay = new SequenceElement("SequenceToPlay", AnimationSequenceType.And);
            if (sequenceElement.SequenceType == AnimationSequenceType.And)
            {
                foreach (AnimationElement element in sequenceElement.AnimationElements)
                {
                    sequenceToPlay.AddAnimationElement(element);
                }
            }

            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(0, sequenceElement.AnimationElements.Count);
                sequenceToPlay.AddAnimationElement(sequenceElement.AnimationElements[chosen]);
            }
            return sequenceToPlay;
        }
        [Obsolete]
        private void AnimateHitAndMissAndEffectsAsChained(List<Character> defendingCharacters)
        {
            SequenceElement attackChainSequenceElement = new SequenceElement("attackChainSequence", AnimationSequenceType.And);
            Dictionary<AnimationElement, List<Character>> characterAnimationMappingDictionary = new Dictionary<AnimationElement, List<Character>>();

            if (defendingCharacters != null && defendingCharacters.Count > 0)
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
            //IconInteractionUtility.ExecuteCmd(new KeyBindsGenerator().PopEvents());
            attackChainSequenceElement.PlayGrouped(characterAnimationMappingDictionary).RunSynchronously();
        }

        private void AnimateHitAndMiss(List<Character> defendingCharacters)
        {
            SequenceElement hitMissSequenceElement = new SequenceElement("HitMissSequence", AnimationSequenceType.And);
            Dictionary<AnimationElement, List<Character>> characterAnimationMappingDictionary = new Dictionary<AnimationElement, List<Character>>();
            // find the flattened list of hit and miss animations and their targets
            var hitAbility = this.GetHitAbility();
            var hitAbilityToPlay = this.GetSequenceToPlay(hitAbility);
            var hitAbilityFlattened = hitAbilityToPlay.GetFlattenedAnimationList();
            List<Character> hitTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackResult == AttackResultOption.Hit).ToList();
            var missAbility = this.GetMissAbility();
            var missAbilityToPlay = this.GetSequenceToPlay(missAbility);
            var missAbilityFlattened = missAbilityToPlay.GetFlattenedAnimationList();
            List<Character> missTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackResult == AttackResultOption.Miss).ToList();

            // add all the hit animations in proper order
            foreach (var anim in hitAbilityFlattened)
            {
                AnimationElement hitElement = anim.Clone();
                hitElement.Name = GetAppropriateAnimationName(hitElement.Type, hitMissSequenceElement.AnimationElements.ToList());
                hitMissSequenceElement.AddAnimationElement(hitElement);
                characterAnimationMappingDictionary.Add(hitElement, hitTargets);
            }
            // Now inject the miss animations where appropriate
            List<AnimationElement> prependMissAnimations = new List<AnimationElement>(); // this list will keep all the non-fx/mov elements from the miss that appear before any mov/fx in the miss
            int missAnimationInjectCurrentPosition = -1, missAnimationInjectionInitialPosition = -1;
            if (missTargets.Count > 0)
            {
                foreach (var anim in missAbilityFlattened)
                {
                    AnimationElement missElement = anim.Clone();
                    missElement.Name = AnimatedAbility.GetAppropriateAnimationName(missElement.Type, hitMissSequenceElement.AnimationElements.ToList());
                    characterAnimationMappingDictionary.Add(missElement, missTargets);
                    if (missAnimationInjectCurrentPosition >= 0) // already found injection position, so add to the next position
                    {
                        hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                    }
                    else // find appropriate position if miss animation is mov or fx, else prepend
                    {
                        if (missElement is MOVElement || missElement is FXEffectElement) // need to chain it to the first mov/fx of the hit
                        {
                            var movOrFxToChain = hitMissSequenceElement.AnimationElements.FirstOrDefault(a => a is MOVElement || a is FXEffectElement);
                            if (movOrFxToChain != null)
                            {
                                if (movOrFxToChain.PlayWithNext) // chain at the end of the playwithnext sequence
                                {
                                    movOrFxToChain = hitMissSequenceElement.AnimationElements.FirstOrDefault(a => a.Order > movOrFxToChain.Order && !(a is MOVElement || a is FXEffectElement));
                                    if (movOrFxToChain == null) // add at the end of the hit animations
                                    {
                                        movOrFxToChain = hitMissSequenceElement.AnimationElements.Last();
                                        movOrFxToChain.PlayWithNext = true;
                                        missAnimationInjectCurrentPosition = movOrFxToChain.Order;
                                        hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                        missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                                    }
                                    else // add before this element in the hit animations
                                    {
                                        movOrFxToChain.PlayWithNext = true;
                                        missAnimationInjectCurrentPosition = movOrFxToChain.Order - 1;
                                        hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                        missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                                    }

                                }
                                else // chain with the current mov/fx in the hit animation
                                {
                                    missAnimationInjectCurrentPosition = movOrFxToChain.Order;
                                    movOrFxToChain.PlayWithNext = true;
                                    hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                    missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                                }
                            }
                            else // add it to first of hit animation, we don't care as there is nothing to chain
                            {
                                hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                            }
                        }
                        else // add it to prepend list, we'll add it later
                        {
                            prependMissAnimations.Add(missElement);
                        }
                    }
                }

                if (prependMissAnimations.Count > 0) // add these before where we appended the first miss mov/fx
                {
                    missAnimationInjectCurrentPosition = missAnimationInjectionInitialPosition - 1;
                    foreach (AnimationElement anim in prependMissAnimations)
                    {
                        hitMissSequenceElement.AddAnimationElement(anim, ++missAnimationInjectCurrentPosition);
                    }
                }
            }

            // Now we have the flattened sequence ready with character mapping, so play each of them in proper order on respective targets
            hitMissSequenceElement.PlayFlattenedAnimationsOnTargeted(characterAnimationMappingDictionary);
        }

        private void AnimateAttackEffects(List<Character> defendingCharacters)
        {
            SequenceElement attackEffectSequenceElement = new SequenceElement("AttackEffectSequence", AnimationSequenceType.And);
            Dictionary<AnimationElement, List<Character>> characterAnimationMappingDictionary = new Dictionary<AnimationElement, List<Character>>();

            var globalDeadAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DEAD_ABITIY_NAME);
            List<Character> deadTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Dead).ToList();
            var globalDyingAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DYING_ABILITY_NAME);
            List<Character> dyingTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Dying).ToList();
            var globalUnconciousAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.UNCONCIOUS_ABITIY_NAME);
            List<Character> unconciousTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Unconcious).ToList();
            var globalStunnedAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.STUNNED_ABITIY_NAME);
            List<Character> stunnedTargets = defendingCharacters.Where(t => t.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Stunned).ToList();

            if (deadTargets.Count > 0 && globalDeadAbility != null)
            {
                var deadAbilityToPlay = this.GetSequenceToPlay(globalDeadAbility);
                var deadAbilityFlattened = deadAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in deadAbilityFlattened)
                {
                    AnimationElement deadAnimation = animation.Clone();
                    deadAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(deadAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(deadAnimation);
                    characterAnimationMappingDictionary.Add(deadAnimation, deadTargets);
                }
            }

            if (dyingTargets.Count > 0 && globalDyingAbility != null)
            {
                var dyingAbilityToPlay = this.GetSequenceToPlay(globalDyingAbility);
                var dyingAbilityFlattened = dyingAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in dyingAbilityFlattened)
                {
                    AnimationElement dyingAnimation = animation.Clone();
                    dyingAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(dyingAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(dyingAnimation);
                    characterAnimationMappingDictionary.Add(dyingAnimation, dyingTargets);
                }
            }

            if (unconciousTargets.Count > 0 && globalUnconciousAbility != null)
            {
                var unconciousAbilityToPlay = this.GetSequenceToPlay(globalUnconciousAbility);
                var unconciousAbilityFlattened = unconciousAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in unconciousAbilityFlattened)
                {
                    AnimationElement unconciousAnimation = animation.Clone();
                    unconciousAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(unconciousAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(unconciousAnimation);
                    characterAnimationMappingDictionary.Add(unconciousAnimation, unconciousTargets);
                }

            }

            if (stunnedTargets.Count > 0 && globalStunnedAbility != null)
            {
                var stunnedAbilityToPlay = this.GetSequenceToPlay(globalStunnedAbility);
                var stunnedAbilityFlattened = stunnedAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in stunnedAbilityFlattened)
                {
                    AnimationElement stunnedAnimation = animation.Clone();
                    stunnedAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(stunnedAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(stunnedAnimation);
                    characterAnimationMappingDictionary.Add(stunnedAnimation, stunnedTargets);
                }
            }

            // now make all MOVs and FXs to play with next, except the last one so that they play together
            AnimationElement lastMovOrFx = null;
            foreach (var movOrFxElement in attackEffectSequenceElement.AnimationElements.Where(a => a.Type == AnimationType.FX || a.Type == AnimationType.Movement))
            {
                movOrFxElement.PlayWithNext = true;
                lastMovOrFx = movOrFxElement;
            }
            if (lastMovOrFx != null)
                lastMovOrFx.PlayWithNext = false;

            attackEffectSequenceElement.PlayFlattenedAnimationsOnTargeted(characterAnimationMappingDictionary);
        }

        private void AnimateKnockBack(Character attackingCharacter, List<Character> defendingCharacters)
        {
            foreach (Character character in defendingCharacters.Where(c => c.ActiveAttackConfiguration != null && c.ActiveAttackConfiguration.AttackResult == AttackResultOption.Hit && c.ActiveAttackConfiguration.KnockBackOption == KnockBackOption.KnockBack))
            {
                int knockbackDistance = character.ActiveAttackConfiguration.KnockBackDistance;
                if (knockbackDistance > 0)
                {
                    Vector3 vAttacker = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
                    Vector3 vTarget = new Vector3(character.Position.X, character.Position.Y, character.Position.Z);
                    Vector3 directionVector = vTarget - vAttacker;
                    directionVector.Normalize();
                    var destX = vTarget.X + directionVector.X * knockbackDistance;
                    var destY = vTarget.Y + directionVector.Y * knockbackDistance;
                    var destZ = vTarget.Z + directionVector.Z * knockbackDistance;
                    if (Math.Abs(vTarget.X - destX) < 1)
                        destX = vTarget.X;
                    if (Math.Abs(vTarget.Y - destY) < 1)
                        destY = vTarget.Y;
                    if (Math.Abs(vTarget.Z - destZ) < 1)
                        destZ = vTarget.Z;
                    var collisionInfo = IconInteractionUtility.GetCollisionInfo(vTarget.X, vTarget.Y, vTarget.Z, destX, destY, destZ);
                    Vector3 targetPosition = GetCollisionPoint(collisionInfo);
                    if(targetPosition.X == 0 && targetPosition.Y == 0 && targetPosition.Z == 0)
                    {
                        character.Position.X = destX;
                        character.Position.Y = destY;
                        character.Position.Z = destZ;
                    }
                    else
                    {
                        character.Position.X = targetPosition.X;
                        character.Position.Y = targetPosition.Y;
                        character.Position.Z = targetPosition.Z;
                    }
                }
            }
        }
        public override void Play(bool persistent = false, Character target = null, bool forcePlay = false)
        {
            if (this.IsAttack)
                this.InitiateAttack(persistent, target);
            else
                base.Play(persistent, target, forcePlay);
        }

        //X:[126.30] Y:[-0.50] Z:[-60.09] D:[0.00]
        private Vector3 GetCollisionPoint(string collisionInfo)
        {
            int indexXStart = collisionInfo.IndexOf("[");
            int indexXEnd = collisionInfo.IndexOf("]");
            string xStr = collisionInfo.Substring(indexXStart + 1, indexXEnd - indexXStart - 1);
            float X = float.Parse(xStr);

            int indexYStart = collisionInfo.IndexOf("[", indexXEnd);
            int indexYEnd = collisionInfo.IndexOf("]", indexYStart);
            string yStr = collisionInfo.Substring(indexYStart + 1, indexYEnd - indexYStart - 1);
            float Y = float.Parse(yStr);

            int indexZStart = collisionInfo.IndexOf("[", indexYEnd);
            int indexZEnd = collisionInfo.IndexOf("]", indexZStart);
            string zStr = collisionInfo.Substring(indexZStart + 1, indexZEnd - indexZStart - 1);
            float Z = float.Parse(zStr);

            return new Vector3(X, Y, Z);
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

        private int knockBackDistance;
        public int KnockBackDistance
        {
            get
            {
                return knockBackDistance;
            }
            set
            {
                knockBackDistance = value;
                OnPropertyChanged("KnockBackDistance");
            }
        }

        private bool isStunned;
        public bool IsStunned
        {
            get
            {
                return isStunned;
            }
            set
            {
                isStunned = value;
                OnPropertyChanged("IsStunned");
            }
        }

        private bool isUnconcious;
        public bool IsUnconcious
        {
            get
            {
                return isUnconcious;
            }
            set
            {
                isUnconcious = value;
                OnPropertyChanged("IsUnconcious");
            }
        }

        private bool isDying;
        public bool IsDying
        {
            get
            {
                return isDying;
            }
            set
            {
                isDying = value;
                OnPropertyChanged("IsDying");
            }
        }

        private bool isDead;
        public bool IsDead
        {
            get
            {
                return isDead;
            }
            set
            {
                isDead = value;
                OnPropertyChanged("IsDead");
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
