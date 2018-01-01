using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.HCSIntegration
{
    public class CombatantsCollection
    {
        public List<Combatant> Combatants { get; set; }
    }

    public class Combatant : NotifyPropertyChanged, IComparable
    {
        public string Phase { get; set; }
        [JsonProperty("Character")]
        public string CharacterName { get; set; }
        [JsonIgnore]
        public Character CombatantCharacter { get; set; }
        [JsonIgnore]
        public int Order { get; set; }
        [JsonIgnore]
        private bool isActivePhase;
        public bool IsActivePhase
        {
            get
            {
                return isActivePhase;
            }
            set
            {
                isActivePhase = value;
                OnPropertyChanged("IsActivePhase");
            }
        }

        public int CompareTo(object cmb)
        {
            Combatant cmb2 = cmb as Combatant;
            string s1 = this.Phase;
            string s2 = cmb2.Phase;
            if (this.Phase == cmb2.Phase)
            {
                s1 = this.CharacterName;
                s2 = cmb2.CharacterName;
            }
            else if (this.Phase == "Not Found")
            {
                s2 = "_";
            }
            else if (cmb2.Phase == "Not Found")
            {
                s1 = "_";
            }

            return Helper.CompareStrings(s1, s2);
        }
    }

    public class ActiveCharacterInfo
    {
        public string Name { get; set; }
        public object Perks { get; set; }
        public object Disadvantages { get; set; }
        public object Talents { get; set; }
        public object Equipment { get; set; }
        public object Skills { get; set; }
        public object Defenses { get; set; }
        public object Stats { get; set; }
        public object Powers { get; set; }
        public object Defaults { get; set; }
        public object Effects { get; set; }
        [JsonProperty("States")]
        public CharacterStates CharacterStates { get; set; }
        [JsonIgnore]
        public  List<AbilityActivationEligibility> AbilitiesEligibilityCollection { get; set; }
    }

    public class CharacterStates
    {
        [JsonProperty("Is Holding For Dex")]
        public bool? IsHoldingForDex { get; set; }
        [JsonProperty("Is Dying")]
        public bool? IsDying { get; set; }
        [JsonProperty("Is Alive")]
        public bool? IsAlive { get; set; }
        [JsonProperty("Is Abortable")]
        public bool? IsAbortable { get; set; }
        [JsonProperty("Is Unconsious")] 
        public bool? IsUnconscious { get; set; }
        [JsonProperty("Is Dead")]
        public bool? IsDead { get; set; }
        [JsonProperty("Is Stunned")]
        public bool? IsStunned { get; set; }
    }

    public class Chronometer
    {
        public string CurrentPhase { get; set; }
    }

    public class AbilityActivationEligibility
    {
        public string AbilityName { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class SimpleAbility
    {
        public string Type { get; set; }
        public string Ability { get; set; }
        public string Character { get; set; }
    }

    public class SimpleMovement
    {
        public string Type { get; set; }
        public string Movement { get; set; }
        public int Distance { get; set; }
    }

    public class AttackRequest
    {
        public string Token { get; set; }
        public string Type { get; set; }
        public string Ability { get; set; }
        public string Obstruction { get; set; }
        [JsonProperty("Potential Collision")]
        public PotentialCollision PotentialCollision { get; set; }
    }

    public class AttackTargetRequest : AttackRequest
    {
        public string Target { get; set; }
        public int? Range { get; set; }
        public int? PushedStr { get; set; }
        public int? Generic { get; set; }
        [JsonProperty("Off Hand")]
        public bool? OffHand { get; set; }
        [JsonProperty("Unfamiliar Weapon")]
        public bool? UnfamiliarWeapon { get; set; }
        [JsonProperty("Surprise Move")]
        public int? SurpriseMove { get; set; }
        public int? Encumbrance { get; set; }
        [JsonProperty("From Behind")]
        public bool? FromBehind { get; set; }
        public bool? Surprised { get; set; }
    }

    public class PotentialCollision
    {
        public string Obstacle { get; set; }
        [JsonProperty("Distance From Target")]
        public int DistanceFromTarget { get; set; }
    }

    public class AreaEffectRequest : AttackRequest
    {
        public string Center { get; set; }
        public List<AttackTargetRequest> Targets { get; set; }
    }

    public class KnockbackCollisionSingleTarget
    {
        public string Type { get; set; }
        [JsonProperty("Knockback Target")]
        public string KnockbackTarget { get; set; }
    }

    public class KnockbackCollisionMultiTarget
    {
        public string Type { get; set; }
        [JsonProperty("Targets")]
        public List<KnockbackTargetObstruction> TargetObstructions { get; set; }
    }

    public class KnockbackTargetObstruction
    {
        public string Target { get; set; }
        [JsonProperty("Knockback Target")]
        public string ObstructingTarget { get; set; }
    }

    public class AttackResponse
    {
        public string Token { get; set; }
        public string Type { get; set; }
        public string Ability { get; set; }
    }

    public class AttackSingleTargetResponse : AttackResponse
    {
        public Target Target { get; set; }
        [JsonProperty("Obstruction Result")]
        public ObstructionResult ObstructionResult { get; set; }
        public Results Results { get; set; }
    }

    public class Target
    {
        public string Name { get; set; }
        [JsonProperty("STUN")]
        public HealthMeasures Stun { get; set; }
        [JsonProperty("BODY")]
        public HealthMeasures Body { get; set; }
        [JsonProperty("END")]
        public HealthMeasures Endurance { get; set; }
    }

    public class AttackAreaTargetResponse : AttackResponse
    {
        public string Center { get; set; }
        [JsonProperty("Obstruction Result")]
        public ObstructionResult ObstructionResult { get; set; }
        public List<AttackAreaTarget> Targets { get; set; }
    }

    public class AttackAreaTarget
    {
        public AttackAreaTargetIndividual Target { get; set; }
    }

    public class AttackAreaTargetIndividual
    {
        public string Name { get; set; }
        public string Crowd { get; set; }
        public Statistics Stats { get; set; }
        public Results Result { get; set; }
    }
    public class Knockback
    {
        [JsonProperty("Obstacle Collision")]
        public ObstacleCollision ObstacleCollision { get; set; }
        public int Distance { get; set; }
    }
    public class ObstacleCollision
    {
        public string Name { get; set; }
        [JsonProperty("Obstacle Damage Results")]
        public ObstacleDamageResults ObstacleDamageResults { get; set; }
        [JsonProperty("BODY")]
        public HealthMeasures Body { get; set; }
    }
    public class HealthMeasures
    {
        public int? Max { get; set; }
        public int? Current { get; set; }
    }
    public class ObstacleDamageResults
    {
        public List<string> Effects { get; set; }
    }

    public class ObstructionResult
    {
        [JsonProperty("Obstruction Name")]
        public string ObstructionName { get; set; }
        public List<string> Effects { get; set; }
    }

    public class Statistics
    {
        public HealthMeasures Stun { get; set; }
        public HealthMeasures Body { get; set; }
        [JsonProperty("End")]
        public HealthMeasures Endurance { get; set; }
    }

    public class DamageResults
    {
        public int? Stun { get; set; }
        public int? Body { get; set; }
    }

    public class Results
    {
        public bool Hit { get; set; }
        public Knockback Knockback { get; set; }
        public List<string> Effects { get; set; }
        public DamageResults DamageResults { get; set; }
    }

    public class AttackConfirmation
    {
        public string Type { get; set; }
        [JsonProperty("Status")]
        public string ConfirmationStatus { get; set; }
    }

    public enum HCSIntegrationStatus
    {
        Started,
        Stopped
    }
    public enum HCSIntegrationAction
    {
        DeckUpdated,
        ActiveCharacterUpdated,
        EligibleCombatantsUpdated,
        AttackInitiated,
        AttackCancelled,
        AttackConfirmed,
        AttackResultReceived
    }

    public enum HCSAttackType
    {
        None,
        SingleTargetVanilla,
        MultiTargetVanilla,
        GangVanilla,
        Area,
        GangArea
    }
}
