using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared;
using Module.Shared.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Framework.WPF.Extensions;

namespace Module.HeroVirtualTabletop.HCSIntegration
{
    public interface IHCSIntegrator
    {
        event EventHandler<CustomEventArgs<Object>> SequenceUpdated;
        event EventHandler<CustomEventArgs<Object>> ActiveCharacterUpdated;
        event EventHandler<CustomEventArgs<Object>> AttackResultsUpdated;
        event EventHandler<CustomEventArgs<Object>> EligibleCombatantsUpdated;
        List<Character> InGameCharacters { get; set; }
        void StartIntegration();
        void StopIntegration();
        object GetLatestSequenceInfo();
        void ActivateHeldCharacter(Character heldCharacter);
        void ConfigureAttack(Attack attack, List<Character> attackers, List<Character> defenders);
        void PlaySimpleAbility(Character target, AnimatedAbility ability);
        void ConfirmAttack();
        void CancelAttack();
        void ResumeAttack();
        void NotifyStopMovement(CharacterMovement characterMovement, double distanceTravelled);
        float GetMovementDistanceLimit(CharacterMovement activeMovement);
        void AbortAction(List<Character> abortingCharacters);
        AttackInfo GetAttackInfo(string powerName);
    }
    public class HCSIntegrator : IHCSIntegrator
    {
        private CollisionEngine collisionEngine;
        private Timer timer;
        private object lockObjFileWatcher = new object();
        private object lockObjAttackSensor = new object();
        private string currentToken = null;
        private static FileSystemWatcher HCSIntegratorFileWatcher;
        private bool deckUpdatePending = false;
        private bool eligibleCombatantsUpdatePending = false;
        private bool attackInfoUpdatedForCurrentAttack = false;
        public List<Character> InGameCharacters { get; set; }
        public HCSIntegrationAction LastIntegrationAction { get; set; }
        public ActiveCharacterInfo CurrentActiveCharacterInfo { get; set; }
        public AttackResponseBase CurrentAttackResult { get; set; }
        public string CurrentAttackResultFileContents { get; set; }
        public string CurrentOnDeckCombatantsFileContents { get; set; }
        public string CurrentChronometerFileContents { get; set; }
        public string CurrentActiveCharacterInfoFileContents { get; set; }
        public string CurrentEligibleCombatantsFileContents { get; set; }
        public HCSAttackType CurrentAttackType { get; set; }
        public HCSIntegrationStatus CurrentIntegrationStatus { get; set; }

        public event EventHandler<CustomEventArgs<Object>> SequenceUpdated;
        public event EventHandler<CustomEventArgs<Object>> ActiveCharacterUpdated;
        public event EventHandler<CustomEventArgs<Object>> AttackResultsUpdated;
        public event EventHandler<CustomEventArgs<Object>> EligibleCombatantsUpdated;
        private void OnSequenceUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (SequenceUpdated != null)
                SequenceUpdated(sender, e);
        }
        private void OnActiveCharacterUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (ActiveCharacterUpdated != null)
                ActiveCharacterUpdated(sender, e);
        }
        private void OnAttackResultsUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (AttackResultsUpdated != null)
                AttackResultsUpdated(sender, e);
        }
        private void OnEligibleCombatantsUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (EligibleCombatantsUpdated != null)
                EligibleCombatantsUpdated(sender, e);
        }

        private string eventInfoDirectoryPath;
        public string EventInfoDirectoryPath
        {
            get
            {
                if (string.IsNullOrEmpty(eventInfoDirectoryPath))
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    string dir = Path.GetDirectoryName(path);
                    string eventInfoDir = Path.Combine(dir, "EventInfo");
                    if (Directory.Exists(eventInfoDir))
                        eventInfoDirectoryPath = eventInfoDir;
                }

                return eventInfoDirectoryPath;
            }
        }

        public HCSIntegrator()
        {
            collisionEngine = new AnimatedAbilities.CollisionEngine();
            if (HCSIntegratorFileWatcher == null)
            {
                HCSIntegratorFileWatcher = new FileSystemWatcher();
                HCSIntegratorFileWatcher.Path = string.Format("{0}\\", EventInfoDirectoryPath);
                HCSIntegratorFileWatcher.IncludeSubdirectories = false;
                //HCSIntegratorFileWatcher.Filter = "*.info";
                //HCSIntegratorFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                HCSIntegratorFileWatcher.Changed += HCSIntegratorFileWatcher_Changed;
                HCSIntegratorFileWatcher.Created += HCSIntegratorFileWatcher_Changed;
                HCSIntegratorFileWatcher.Renamed += HCSIntegratorFileWatcher_Changed;
            }
            HCSIntegratorFileWatcher.EnableRaisingEvents = false;
            timer = new Timer(timer_elapsed);
            CurrentIntegrationStatus = HCSIntegrationStatus.Stopped;
        }

        public void StartIntegration()
        {
            CurrentIntegrationStatus = HCSIntegrationStatus.Started;
            if (EventInfoDirectoryPath != null)
                HCSIntegratorFileWatcher.EnableRaisingEvents = true;
            timer.Change(5, 2000);
        }

        public void StopIntegration()
        {
            CurrentIntegrationStatus = HCSIntegrationStatus.Stopped;
            HCSIntegratorFileWatcher.EnableRaisingEvents = false;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        private void HCSIntegratorFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (lockObjFileWatcher)
            {
                string fileExt = Path.GetExtension(e.FullPath);
                if ((e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created) && fileExt == ".info" || fileExt == ".event")
                {
                    if (e.Name == Constants.COMBATANTS_FILE_NAME || e.Name == Constants.CHRONOMETER_FILE_NAME)
                    {
                        if (this.LastIntegrationAction != HCSIntegrationAction.AttackInitiated)
                        {
                            UpdateSequence();
                        }
                        else
                        {
                            deckUpdatePending = true;
                        }
                    }
                    else if (e.Name == Constants.ACTIVE_CHARACTER_FILE_NAME)
                    {
                        UpdateActiveCharacter();
                    }
                    //else if (e.Name == Constants.ATTACK_RESULT_FILE_NAME && this.CurrentAttackType != HCSAttackType.None)
                    //{
                    //    if (this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated || this.LastIntegrationAction == HCSIntegrationAction.AttackResultReceived)
                    //    {
                    //        string currentJson = this.GetAttackResultsFileContents();
                    //        if (currentJson != CurrentAttackResultFileContents)
                    //        {
                    //            ProcessAttackResults();
                    //        }
                    //        else
                    //            this.CurrentAttackResultFileContents = null;
                    //    }
                    //}
                    else if (e.Name == Constants.ELIGIBLE_COMBATANTS_FILE_NAME)
                    {
                        if (this.LastIntegrationAction != HCSIntegrationAction.AttackInitiated)
                        {

                            UpdateEligibleCombatantsInfo();
                        }
                        else
                        {
                            eligibleCombatantsUpdatePending = true;
                        }
                    }
                }
            };
        }

        private void timer_elapsed(object state)
        {
            if (CurrentIntegrationStatus != HCSIntegrationStatus.Stopped)
            {
                string pathAttackResult = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
                if (File.Exists(pathAttackResult))
                {
                    ProcessAttackResults();
                }
            }
        }

        private void UpdateSequence()
        {
            string currentCombatantsJson = this.GetCurrentCombatantsFileContents();
            string currentChronoMeterJson = this.GetCurrentChronometerFileContents();
            if (currentCombatantsJson != null && currentChronoMeterJson != null && this.CurrentChronometerFileContents != currentChronoMeterJson || this.CurrentOnDeckCombatantsFileContents != currentCombatantsJson)
            {
                this.LastIntegrationAction = HCSIntegrationAction.DeckUpdated;
                object sequenceInfo = this.GetLatestSequenceInfo();
                object[] seqArray = sequenceInfo as object[];
                if (seqArray != null && seqArray.Length == 4 && seqArray[0] != null && seqArray[1] != null && seqArray[2] != null && seqArray[3] != null)
                {
                    OnSequenceUpdated(null, new CustomEventArgs<object> { Value = seqArray });
                }
            }
        }

        private void UpdateActiveCharacter()
        {
            string currentActiveCharacterInfoJson = this.GetCurrentActiveCharacterInfoFileContents();
            if (this.CurrentActiveCharacterInfoFileContents != currentActiveCharacterInfoJson)
            {
                this.LastIntegrationAction = HCSIntegrationAction.ActiveCharacterUpdated;
                ActiveCharacterInfo activeCharacterInfo = GetCurrentActiveCharacterInfo();
                if (activeCharacterInfo != null)
                    OnActiveCharacterUpdated(null, new CustomEventArgs<object> { Value = activeCharacterInfo });
            }
        }

        private void UpdateEligibleCombatantsInfo()
        {
            string currentEligibleCombatantsJson = this.GetCurrentEligibleCombatantsFileContents();
            if (this.CurrentEligibleCombatantsFileContents != currentEligibleCombatantsJson)
            {
                this.LastIntegrationAction = HCSIntegrationAction.EligibleCombatantsUpdated;
                CombatantsCollection eligibleCombatants = GetCurrentEligibleCombatants();
                if (eligibleCombatants != null)
                    OnEligibleCombatantsUpdated(null, new CustomEventArgs<object> { Value = eligibleCombatants });
            }
        }

        public object GetLatestSequenceInfo()
        {
            Chronometer chronometer = GetCurrentChronometer();
            CombatantsCollection onDeckCombatants = GetCurrentCombatants();
            ActiveCharacterInfo activeCharacterInfo = GetCurrentActiveCharacterInfo();
            CombatantsCollection eligibleCombatants = GetCurrentEligibleCombatants();
            return new object[] { onDeckCombatants, chronometer, activeCharacterInfo, eligibleCombatants };
        }

        private CombatantsCollection GetCurrentCombatants()
        {
            CombatantsCollection onDeckCombatants = null;
            string json = GetCurrentCombatantsFileContents();
            this.CurrentOnDeckCombatantsFileContents = json;
            if (json != null)
                onDeckCombatants = JsonConvert.DeserializeObject<CombatantsCollection>(json);
            return onDeckCombatants;
        }

        int combatantsFileReadRetryCount = 5;
        private string GetCurrentCombatantsFileContents()
        {
            string pathCombatants = Path.Combine(EventInfoDirectoryPath, Constants.COMBATANTS_FILE_NAME);

            string json = null;
            try
            {
                if (File.Exists(pathCombatants))
                {
                    System.Threading.Thread.Sleep(500);
                    using (StreamReader r = new StreamReader(pathCombatants))
                    {
                        json = r.ReadToEnd();
                        combatantsFileReadRetryCount = 5;
                    }
                }
            }
            catch (Exception ex)
            {
                if (combatantsFileReadRetryCount-- > 0)
                {
                    json = GetCurrentCombatantsFileContents();
                }
            }
            return json;
        }

        private Chronometer GetCurrentChronometer()
        {
            Chronometer chronometer = null;
            string json = GetCurrentChronometerFileContents();
            this.CurrentChronometerFileContents = json;
            if (json != null)
                chronometer = JsonConvert.DeserializeObject<Chronometer>(json);
            return chronometer;
        }

        private string GetCurrentChronometerFileContents()
        {
            string pathChronometer = Path.Combine(EventInfoDirectoryPath, Constants.CHRONOMETER_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathChronometer))
                {
                    using (StreamReader r = new StreamReader(pathChronometer))
                    {
                        json = r.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return json;
        }

        private ActiveCharacterInfo GetCurrentActiveCharacterInfo()
        {
            ActiveCharacterInfo activeCharacterInfo = null;
            string json = this.GetCurrentActiveCharacterInfoFileContents();
            this.CurrentActiveCharacterInfoFileContents = json;
            if (json != null)
                activeCharacterInfo = JsonConvert.DeserializeObject<ActiveCharacterInfo>(json);
            this.CurrentActiveCharacterInfo = activeCharacterInfo;
            this.CurrentActiveCharacterInfo.AbilitiesEligibilityCollection = GetAbilityActivationEligibilityCollection();
            return activeCharacterInfo;
        }

        private string GetCurrentActiveCharacterInfoFileContents()
        {
            string pathActiveCharacter = Path.Combine(EventInfoDirectoryPath, Constants.ACTIVE_CHARACTER_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathActiveCharacter))
                {
                    System.Threading.Thread.Sleep(500);
                    using (StreamReader r = new StreamReader(pathActiveCharacter))
                    {
                        json = r.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return json;
        }
        private CombatantsCollection GetCurrentEligibleCombatants()
        {
            CombatantsCollection eligibleCombatants = null;
            string json = GetCurrentEligibleCombatantsFileContents();
            this.CurrentEligibleCombatantsFileContents = json;
            if (json != null)
                eligibleCombatants = JsonConvert.DeserializeObject<CombatantsCollection>(json);
            return eligibleCombatants;
        }
        int eligibleCombatantsFileReadRetryCount = 5;
        private string GetCurrentEligibleCombatantsFileContents()
        {
            string pathEligibleCombatants = Path.Combine(EventInfoDirectoryPath, Constants.ELIGIBLE_COMBATANTS_FILE_NAME);

            string json = null;
            try
            {
                if (File.Exists(pathEligibleCombatants))
                {
                    System.Threading.Thread.Sleep(500);
                    using (StreamReader r = new StreamReader(pathEligibleCombatants))
                    {
                        json = r.ReadToEnd();
                        eligibleCombatantsFileReadRetryCount = 5;
                    }
                }
            }
            catch (Exception ex)
            {
                if (eligibleCombatantsFileReadRetryCount-- > 0)
                {
                    json = GetCurrentEligibleCombatantsFileContents();
                }
            }
            return json;
        }

        private List<AbilityActivationEligibility> GetAbilityActivationEligibilityCollection()
        {
            List<AbilityActivationEligibility> eligibilityCollection = new List<HCSIntegration.AbilityActivationEligibility>();
            Dictionary<string, bool> abilityEligibilityDictionary = new Dictionary<string, bool>();
            if (this.CurrentActiveCharacterInfo.Powers != null)
            {
                string json = this.CurrentActiveCharacterInfo.Powers.ToString();
                abilityEligibilityDictionary = GetAbilityEligiblityDictionary(json);
            }
            if (this.CurrentActiveCharacterInfo.Defaults != null)
            {
                string json = this.CurrentActiveCharacterInfo.Defaults.ToString();
                var abilityEligibilityDictionaryOther = GetAbilityEligiblityDictionary(json);
                abilityEligibilityDictionary.AddRange(abilityEligibilityDictionaryOther);
            }
            foreach (var entry in abilityEligibilityDictionary)
            {
                if (!eligibilityCollection.Any(e => e.AbilityName == entry.Key))
                    eligibilityCollection.Add(new AbilityActivationEligibility { AbilityName = entry.Key, IsEnabled = entry.Value });
            }

            return eligibilityCollection;
        }

        private Dictionary<string, bool> GetAbilityEligiblityDictionary(string json)
        {
            Dictionary<string, bool> abilityEligibilityDictionary = new Dictionary<string, bool>();
            JToken outer = JToken.Parse(json);
            foreach (var obj in outer.Children())
            {
                JProperty jProp = obj as JProperty;
                if (jProp != null)
                {
                    JObject jObj = jProp.Value as JObject;
                    var values = jObj.Properties().Where(p => p.Name == "Is Enabled").Select(p => p.Value);
                    if (values.Count() > 0)
                    {
                        bool val = values.First().Value<bool>();
                        if (!abilityEligibilityDictionary.ContainsKey(jProp.Name))
                            abilityEligibilityDictionary.Add(jProp.Name, val);
                    }
                }
            }

            return abilityEligibilityDictionary;
        }

        private string GetAttackResultsFileContents()
        {
            string pathAttackResult = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathAttackResult))
                {
                    System.Threading.Thread.Sleep(1000);
                    using (StreamReader r = new StreamReader(pathAttackResult))
                    {
                        json = r.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return json;
        }
        private AttackResponseBase GetAttackResults()
        {
            AttackResponseBase attackResult = null;
            string json = GetAttackResultsFileContents();
            if (json != this.CurrentAttackResultFileContents)
                this.attackInfoUpdatedForCurrentAttack = true;
            this.CurrentAttackResultFileContents = json;
            switch (this.CurrentAttackType)
            {
                case HCSAttackType.Area:
                    attackResult = JsonConvert.DeserializeObject<AreaAttackResponse>(json);
                    break;
                case HCSAttackType.SingleTargetVanilla:
                    attackResult = JsonConvert.DeserializeObject<AttackResponse>(json);
                    break;
                default:
                    break;
            }

            return attackResult;
        }

        private bool ProcessAttackResults()
        {
            lock (lockObjAttackSensor)
            {
                AttackResponseBase attackResult = GetAttackResults();
                bool resultsReceivedWithToken = attackResult != null && attackResult.Token != null && attackResult.Token == this.currentToken;
                if (resultsReceivedWithToken)
                {
                    if (this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated)
                    {
                        this.LastIntegrationAction = HCSIntegrationAction.AttackResultReceived;
                        attackInfoUpdatedForCurrentAttack = false;
                        ProcessAttackResults(attackResult);
                    }
                    else if (this.attackInfoUpdatedForCurrentAttack)
                    {
                        attackInfoUpdatedForCurrentAttack = false;
                        ProcessAttackResults(attackResult);
                    }

                    if (deckUpdatePending)
                    {
                        deckUpdatePending = false;
                        UpdateSequence();
                    }
                    if (eligibleCombatantsUpdatePending)
                    {
                        eligibleCombatantsUpdatePending = false;
                        UpdateEligibleCombatantsInfo();
                    }
                }
                else if (attackResult != null && attackResult.Token == null)
                {
                    //DeleteCurrentResultFile();
                }
                else
                {

                }

                return resultsReceivedWithToken;
            }
        }

        private void ProcessAttackResults(AttackResponseBase attackResult)
        {
            this.CurrentAttackResult = attackResult;
            this.CurrentAttackResult = attackResult;
            List<Character> attackTargets = ParseAttackTargetsFromAttackResult(attackResult);
            //DeleteCurrentResultFile();
            OnAttackResultsUpdated(this, new CustomEventArgs<object> { Value = attackTargets });
        }

        int numDeleteRetry = 5;
        private void DeleteCurrentResultFile()
        {
            string pathAttackResult = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathAttackResult))
                {
                    File.Delete(pathAttackResult);
                    numDeleteRetry = 5;
                }
            }
            catch (IOException ioex)
            {
                numDeleteRetry--;
                if (numDeleteRetry > 0)
                {
                    Thread.Sleep(1000);
                    DeleteCurrentResultFile();
                }
            }
            catch
            {

            }
        }
        private void ParseEffects(ActiveAttackConfiguration attackConfig, List<string> effects)
        {
            if (effects != null)
            {
                if (effects.Contains("Stunned"))
                    attackConfig.IsStunned = true;
                if (effects.Contains("Unconscious"))
                    attackConfig.IsUnconcious = true;
                if (effects.Contains("Dying"))
                    attackConfig.IsDying = true;
                if (effects.Contains("Dead"))
                    attackConfig.IsDead = true;
                if (effects.Contains("Partially Destroyed"))
                    attackConfig.IsPartiallyDestryoed = true;
                if (effects.Contains("Destroyed"))
                    attackConfig.IsDestroyed = true;
            }
        }

        private List<Character> ParseAttackTargetsFromAttackResult(AttackResponseBase attackResult)
        {
            List<Character> targets = null;
            switch (this.CurrentAttackType)
            {
                case HCSAttackType.Area:
                    //targets = ParseAttackTargetsFromAreaAttackResult(attackResult as AttackAreaTargetResponse);
                    targets = ParseAttackTargetsFromAreaAttackResponse(attackResult as AreaAttackResponse);
                    break;
                case HCSAttackType.SingleTargetVanilla:
                    targets = ParseAttackTargetsFromAttackResult(attackResult as AttackResponse);
                    break;
                default:
                    break;
            }

            return targets;
        }

        private List<Character> ParseAttackTargetsFromAttackResult(AttackResponse attackResponse)
        {
            List<Character> attackTargets = new List<Character>();
            Character primaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResponse.Defender.Name);
            if (primaryTarget != null)
            {
                attackTargets.Add(primaryTarget);

                ActiveAttackConfiguration attackConfigPrimary = new ActiveAttackConfiguration();
                attackConfigPrimary.IsHit = attackResponse.IsHit;
                attackConfigPrimary.Body = attackResponse.Defender.Body.Current;
                attackConfigPrimary.Stun = attackResponse.Defender.Stun.Current;
                attackConfigPrimary.MoveAttackerToTarget = attackResponse.MoveBeforeAttackRequired;
                List<Character> secondaryTargets = new List<Character>();
                
                if (attackResponse.KnockbackResult != null && attackResponse.KnockbackResult.Distance != 0)
                {
                    attackConfigPrimary.IsKnockedBack = true;
                    attackConfigPrimary.KnockBackDistance = attackResponse.KnockbackResult.Distance;
                    
                    if (attackResponse.KnockbackResult.Collisions != null && attackResponse.KnockbackResult.Collisions.Count > 0)
                    {
                        attackConfigPrimary.ObstructingCharacters = new List<Characters.Character>();
                        foreach (var collision in attackResponse.KnockbackResult.Collisions)
                        {
                            var secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == collision.CollidingObject.Name);
                            ActiveAttackConfiguration attackConfigSecondary = new ActiveAttackConfiguration();
                            attackConfigSecondary.Body = collision.CollisionDamageResults.Body;
                            if (collision.CollidingObject.Effects != null
                            && collision.CollidingObject.Effects.Count > 0)
                            {
                                ParseEffects(attackConfigSecondary, collision.CollidingObject.Effects);
                            }
                            attackTargets.Add(secondaryTarget);
                            attackConfigSecondary.IsHit = true;
                            attackConfigSecondary.PrimaryTargetCharacter = primaryTarget;
                            attackConfigSecondary.ObstructingCharacters = null;
                            attackConfigSecondary.IsKnockbackObstruction = true;
                            secondaryTarget.ActiveAttackConfiguration = attackConfigSecondary;
                            attackConfigPrimary.ObstructingCharacters.Add(secondaryTarget);
                        }
                    }
                }

                if (attackResponse.ObstructionDamageResults != null && attackResponse.ObstructionDamageResults.Count > 0)
                {
                    if(attackConfigPrimary.ObstructingCharacters == null)
                        attackConfigPrimary.ObstructingCharacters = new List<Characters.Character>();
                    foreach (var obstruction in attackResponse.ObstructionDamageResults.Where(odr => odr.IsHit && odr.Defender != null))
                    {
                        List<Character> obstructingTargets = ParseAttackTargetsFromAttackResult(obstruction);
                        secondaryTargets.AddRange(obstructingTargets);
                    }
                    foreach(var secondaryTarget in secondaryTargets)
                    {
                        if (!attackTargets.Contains(secondaryTarget))
                        {
                            attackTargets.Add(secondaryTarget);
                            secondaryTarget.ActiveAttackConfiguration.PrimaryTargetCharacter = primaryTarget;
                            attackConfigPrimary.ObstructingCharacters.Add(secondaryTarget);
                        }
                    }
                }
                ParseEffects(attackConfigPrimary, attackResponse.Defender.Effects);
                primaryTarget.ActiveAttackConfiguration = attackConfigPrimary;
            }
            return attackTargets;
        }

        private List<Character> ParseAttackTargetsFromAreaAttackResponse(AreaAttackResponse areaAttackResponse)
        {
            List<Character> attackTargets = new List<Character>();

            foreach(var target in areaAttackResponse.Targets)
            {
                var targetsForThisResponse = ParseAttackTargetsFromAttackResult(target);
                foreach(var targetForThisResponse in targetsForThisResponse)
                {
                    if (target.MoveBeforeAttackRequired)
                    {
                        targetForThisResponse.ActiveAttackConfiguration.IsCenterTarget = false;
                        targetForThisResponse.ActiveAttackConfiguration.MoveAttackerToTarget = true;
                    }

                    if (!attackTargets.Contains(targetForThisResponse))
                        attackTargets.Add(targetForThisResponse);
                }
            }

            return attackTargets;
        }

        private List<Character> ParseAttackTargetsFromAreaAttackResult(AttackAreaTargetResponse attackResponse)
        {
            List<Character> attackTargets = new List<Characters.Character>();
            foreach (var target in attackResponse.Targets)
            {
                Character targetCharacter = this.InGameCharacters.FirstOrDefault(c => c.Name == target.Target.Name);
                if (targetCharacter != null)
                {
                    var result = target.Target.Result;
                    attackTargets.Add(targetCharacter);
                    ActiveAttackConfiguration attackConfigPrimary = new ActiveAttackConfiguration();
                    attackConfigPrimary.IsHit = result.Hit;
                    if (attackResponse.MoveBeforeAttackRequired)
                        attackConfigPrimary.IsCenterTarget = false; // No center target for this scenario
                    else
                        attackConfigPrimary.IsCenterTarget = targetCharacter.Name == attackResponse.Center;
                    attackConfigPrimary.Stun = result.DamageResults != null ? result.DamageResults.Stun : null;
                    attackConfigPrimary.Stun = result.DamageResults != null ? result.DamageResults.Stun : null;
                    Character secondaryTarget = null;
                    ActiveAttackConfiguration attackConfigSecondary = new ActiveAttackConfiguration();
                    if (result.Knockback != null && attackConfigPrimary.KnockBackDistance > 0)
                    {
                        attackConfigPrimary.IsKnockedBack = true;
                        attackConfigPrimary.IsHit = true;
                        attackConfigPrimary.KnockBackDistance = result.Knockback.Distance;
                        //if (result.Knockback.Collisions != null && result.Knockback.Collisions.Count > 0)
                        //{
                        //    secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == result.Knockback.ObstacleCollision.Name);
                        //    attackConfigSecondary.Body = result.Knockback.ObstacleCollision.Body.Current;
                        //    if (result.Knockback.ObstacleCollision.ObstacleDamageResults.Effects != null
                        //    && result.Knockback.ObstacleCollision.ObstacleDamageResults.Effects.Count > 0)
                        //    {
                        //        ParseEffects(attackConfigSecondary, result.Knockback.ObstacleCollision.ObstacleDamageResults.Effects);
                        //    }
                        //}
                    }
                    //if (secondaryTarget == null && attackResult.ObstructionDamageResults != null)
                    //{
                    //    secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResult.ObstructionDamageResults.ObstructionName);
                    //    if (attackResult.ObstructionDamageResults.Effects != null
                    //        && attackResult.ObstructionDamageResults.Effects.Count > 0)
                    //    {
                    //        ParseEffects(attackConfigSecondary, attackResult.ObstructionDamageResults.Effects);
                    //    }
                    //}
                    //if (secondaryTarget != null)
                    //{
                    //    attackTargets.Add(secondaryTarget);
                    //    attackConfigSecondary.IsHit = true;

                    //    attackConfigSecondary.ObstructingCharacter = null;

                    //    secondaryTarget.ActiveAttackConfiguration = attackConfigSecondary;

                    //    attackConfigPrimary.ObstructingCharacter = secondaryTarget;
                    //}
                    ParseEffects(attackConfigPrimary, target.Target.Result.Effects);
                    targetCharacter.ActiveAttackConfiguration = attackConfigPrimary;
                }
            }
            return attackTargets;
        }

        private float ParseDistanceLimitForMovement(string movementName, bool nonCombat)
        {
            string limitString = GetDistanceLimitString(movementName);
            float limit = 0;
            string[] tokens = limitString.Split(',');
            if (tokens.Length == 3)
            {
                string tokenToConsider = nonCombat ? tokens[2] : tokens[1];
                string[] numbers = Regex.Split(tokenToConsider, @"\D+");
                if (numbers.Length > 0)
                {
                    int k;
                    foreach (string n in numbers)
                    {
                        if (int.TryParse(n, out k))
                        {
                            limit = int.Parse(n);
                            break;
                        }
                    }
                }
            }

            return limit;
        }

        private string GetDistanceLimitString(string movementName)
        {
            string limitString = "";
            JToken outer = null;
            if (this.CurrentActiveCharacterInfo.Powers != null)
                outer = JToken.Parse(this.CurrentActiveCharacterInfo.Powers.ToString());
            bool movementExists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == movementName);
            if (!movementExists && this.CurrentActiveCharacterInfo.Defaults != null)
            {
                outer = JToken.Parse(this.CurrentActiveCharacterInfo.Defaults.ToString());
                movementExists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == movementName);
            }
            if (movementExists)
            {
                JObject inner = outer[movementName].Value<JObject>();
                dynamic d = inner;
                limitString = d.Description;
                // or the following works too
                //var values = inner.Properties().Where(p => p.Name == "Description").Select(p => p.Value);
                //foreach(var value in values)
                //{
                //    string val = value.Value<string>();
                //}
            }

            return limitString;
        }

        private AttackShape ParseAreaAttackShape(string shapeString)
        {
            AttackShape attackShape = AttackShape.None;

            if (shapeString.ToLower().Contains("radius"))
                attackShape = AttackShape.Radius;
            else if (shapeString.ToLower().Contains("one-hex"))
                attackShape = AttackShape.Line;
            else if (shapeString.ToLower().Contains("cone"))
                attackShape = AttackShape.Cone;

            return attackShape;
        }

        private void WaitForAttackUpdates()
        {
            //timer.Change(2000, Timeout.Infinite);
        }

        public void PlaySimpleAbility(Character target, AnimatedAbility ability)
        {
            GenerateSimpleAbilityMessage(target, ability);
        }

        public void NotifyStopMovement(CharacterMovement characterMovement, double distanceTravelled)
        {
            GenerateSimpleMovementMessage(characterMovement, distanceTravelled);
        }

        public float GetMovementDistanceLimit(CharacterMovement activeMovement)
        {
            float maxDistance = 0f;
            if (this.CurrentActiveCharacterInfo == null)
                this.GetCurrentActiveCharacterInfo();

            if (this.CurrentActiveCharacterInfo.Powers != null)
            {
                string movementName = GetMovementName(activeMovement.Name.ToLower());
                maxDistance = ParseDistanceLimitForMovement(movementName, activeMovement.IsNonCombatMovement);
            }

            return maxDistance;
        }

        public AttackInfo GetAttackInfo(string powerName)
        {
            AttackInfo attackInfo = new AttackInfo { AttackShape = AttackShape.None, AttackType = AttackType.None, Range = 0, TargetSelective = false};
            if (this.CurrentActiveCharacterInfo == null)
                this.GetCurrentActiveCharacterInfo();
            if (this.CurrentActiveCharacterInfo.Defaults != null)
            {
                JToken outer = JToken.Parse(this.CurrentActiveCharacterInfo.Defaults.ToString());
                bool powerExists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == powerName);
                if (powerExists)
                {
                    JObject inner = outer[powerName].Value<JObject>();
                    dynamic d = inner;
                    var description = d.Description;
                    if(description != null && description.ToString().Contains("Area Effect") && d.Advantages != null)
                    {
                        JObject advantagesObj = d.Advantages;
                        var areaEffectObj = advantagesObj.Properties().Where(p => p.Name == "Area Effect").Select(p => p.Value);
                        if (areaEffectObj != null && areaEffectObj.Count() > 0)
                        {
                            dynamic areaEffectInfo = areaEffectObj.First();
                            attackInfo.AttackType = AttackType.Area;
                            if (areaEffectInfo.Range != null)
                                attackInfo.Range = areaEffectInfo.Range;
                            if (areaEffectInfo.Type != null && areaEffectInfo.Type == "Selective")
                                attackInfo.TargetSelective = true;
                            if (areaEffectInfo.Shape != null && !string.IsNullOrEmpty(areaEffectInfo.Shape.ToString()))
                                attackInfo.AttackShape = ParseAreaAttackShape(areaEffectInfo.Shape.ToString());
                            else if (areaEffectInfo.Description != null && !string.IsNullOrEmpty(areaEffectInfo.Description.ToString()))
                                attackInfo.AttackShape = ParseAreaAttackShape(areaEffectInfo.Description.ToString());
                        }
                    }
                }
            }

            return attackInfo;
        }

        private void ConfigureVanillaAttack(Attack attack, Character attacker, Character defender)
        {
            this.CurrentAttackType = HCSAttackType.SingleTargetVanilla;
            GenerateAttackInitiationMessage(attack, attacker, defender);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            //WaitForAttackUpdates();
        }

        private void ConfigureAreaAttack(Attack attack, Character attacker, List<Character> defenders)
        {
            this.CurrentAttackType = HCSAttackType.Area;
            GenerateAttackAreaTargetInitiationMessage(attack, attacker, defenders);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            //WaitForAttackUpdates();
        }
        public void ConfigureAttack(Attack attack, List<Character> attackers, List<Character> defenders)
        {
            this.ResetAttackParameters();
            if (!attack.IsAreaEffect)
            {
                if (attackers.Count > 1)// Gang Attack
                {

                }
                else
                {
                    if (defenders.Count > 1) // Multi Target Vanilla Attack
                    {

                    }
                    else // Single Target Vanilla Attack
                    {
                        ConfigureVanillaAttack(attack, attackers[0], defenders[0]);
                    }
                }
            }
            else
            {
                if (attackers.Count > 1)// Gang Area Attack
                {

                }
                else// Minion Area Attack
                {
                    ConfigureAreaAttack(attack, attackers[0], defenders);
                }
            }
        }

        public void ConfirmAttack()
        {
            this.GenerateAttackConfirmationMessage(true);
            ResetAttackParameters();
        }

        public void CancelAttack()
        {
            this.GenerateAttackConfirmationMessage(false);
            ResetAttackParameters();
        }

        public void ResumeAttack()
        {
            this.CurrentAttackType = this.savedAttackType;
            var attackResults = this.GetAttackResults();
            if (attackResults != null)
                ProcessAttackResults(attackResults);
        }
        private HCSAttackType savedAttackType = HCSAttackType.None;
        public void AbortAction(List<Character> abortingCharacters)
        {
            foreach (Character character in abortingCharacters)
                GenerateAbortActionMessage(character);
            this.savedAttackType = this.CurrentAttackType;
        }

        public void ActivateHeldCharacter(Character heldCharacter)
        {
            GenerateActivateHeldCharacterMessage(heldCharacter);
            this.savedAttackType = this.CurrentAttackType;
        }

        private void ResetAttackParameters()
        {
            this.CurrentAttackResult = null;
            this.CurrentAttackResultFileContents = null;
            this.CurrentAttackType = HCSAttackType.None;
        }
        private void WriteToAbilityActivatedFile(object jsonObject)
        {
            Action d = delegate ()
            {
                string pathAttackActivated = Path.Combine(EventInfoDirectoryPath, Constants.ABILITY_ACTIVATED_FILE_NAME);
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Formatting = Formatting.Indented;
                using (StreamWriter streamWriter = new StreamWriter(pathAttackActivated))
                {
                    using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        serializer.Serialize(jsonWriter, jsonObject);
                        streamWriter.Flush();
                    }
                }
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 500);
            adex.ExecuteAsyncDelegate();
        }

        private void GenerateSimpleAbilityMessage(Character target, AnimatedAbility ability)
        {
            SimpleAbility simpleAbility = new SimpleAbility { Ability = ability.Name, Type = Constants.SIMPLE_ABILITY_TYPE_NAME };
            WriteToAbilityActivatedFile(simpleAbility);
        }

        private void GenerateAbortActionMessage(Character target)
        {
            SimpleAbility abortMessage = new SimpleAbility { Type = Constants.ABORT_ACTION_TYPE_NAME, Character = target.Name };
            WriteToAbilityActivatedFile(abortMessage);
            Thread.Sleep(1000);
        }

        private void GenerateActivateHeldCharacterMessage(Character target)
        {
            SimpleAbility abortMessage = new SimpleAbility { Type = Constants.ACTIVATE_HELD_CHARACTER_TYPE_NAME, Character = target.Name };
            WriteToAbilityActivatedFile(abortMessage);
            Thread.Sleep(1000);
        }

        private void GenerateSimpleMovementMessage(CharacterMovement characterMovement, double distanceTravelled)
        {
            SimpleMovement simpleMovement = new SimpleMovement { Movement = GetMovementName(characterMovement.Name), Type = Constants.MOVEMENT_TYPE_NAME, Distance = (int)distanceTravelled };
            WriteToAbilityActivatedFile(simpleMovement);
        }

        private string GetMovementName(string characterMovementName)
        {
            string movementName = "";
            switch (characterMovementName.ToLower())
            {
                case "running":
                case "run":
                    movementName = "Running";
                    break;
                case "walking":
                case "walk":
                    movementName = "Walking";
                    break;
                case "swimming":
                case "swim":
                    movementName = "Swimming";
                    break;
                case "leaping":
                case "leap":
                    movementName = "Leaping";
                    break;
                case "flying":
                case "fly":
                    movementName = "Flying";
                    break;
                case "jumping":
                case "jump":
                    movementName = "Leaping";
                    break;
            }
            return movementName;
        }

        private void GenerateAttackInitiationMessage(Attack attack, Character attacker, Character defender)
        {
            this.CurrentAttackResult = null;
            this.currentToken = Guid.NewGuid().ToString();
            float range = Vector3.Distance(attacker.CurrentPositionVector, defender.CurrentPositionVector);
            var obstructions = collisionEngine.FindObstructingObjects(attacker, defender, this.InGameCharacters.Where(c => c != attacker && c != defender).ToList());
            AttackRequest attackSingleTarget = new AttackRequest
            {
                Type = Constants.ATTACK_INITIATION_TYPE_NAME,
                Token = this.currentToken,
                Ability = attack.Name,
                Target = defender.Name,
                Range = (int)Math.Round((range) / 8f, 2)
            };
            if (obstructions != null && obstructions.Count > 0)
            {
                attackSingleTarget.Obstructions = new List<string>();
                foreach (var obstruction in obstructions)
                {
                    if(obstruction.CollidingObject is Character)
                    {
                        attackSingleTarget.Obstructions.Add((obstruction.CollidingObject as Character).Name);
                    }
                    else
                    {
                        attackSingleTarget.Obstructions.Add(obstruction.CollidingObject.ToString());
                    }
                }
            }
            List<Character> otherCharacters = this.InGameCharacters.Where(c => c != attacker && c != defender).ToList();
            var knockbackObstacles = collisionEngine.CalculateKnockbackObstructions(attacker, defender, 50, otherCharacters);
            if (knockbackObstacles != null && knockbackObstacles.Count > 0)
            {
                attackSingleTarget.PotentialKnockbackCollisions = new List<HCSIntegration.PotentialKnockbackCollision>();
                foreach(var knockbackObstacle in knockbackObstacles)
                {
                    attackSingleTarget.PotentialKnockbackCollisions.Add(
                        new PotentialKnockbackCollision
                        {
                            CollisionObject = knockbackObstacle.CollidingObject is Character ? (knockbackObstacle.CollidingObject as Character).Name : knockbackObstacle.CollidingObject.ToString(),
                            CollisionDistance = (int)Math.Round((knockbackObstacle.CollisionDistance) / 8f, 2)
                        }
                        );
                }

            }

            WriteToAbilityActivatedFile(attackSingleTarget);
        }

        private void GenerateAttackConfirmationMessage(bool isConfirm)
        {
            AttackConfirmation attackConfirmation = new HCSIntegration.AttackConfirmation
            {
                Type = Constants.ATTACK_CONFIRMATION_TYPE_NAME,
                ConfirmationStatus = isConfirm ? Constants.ATTACK_CONFIRMED_STATUS : Constants.ATTACK_CANCELLED_STATUS
            };
            WriteToAbilityActivatedFile(attackConfirmation);
        }
        private void GenerateAttackAreaTargetInitiationMessage(Attack attack, Character attacker, List<Character> defenders)
        {
            this.CurrentAttackResult = null;
            this.currentToken = Guid.NewGuid().ToString();
            AreaAttackRequest areaAttackRequest = new AreaAttackRequest
            {
                Type = Constants.AREA_ATTACK_INITIATION_TYPE_NAME,
                Token = this.currentToken,
                Ability = attack.Name,
            };
            
            areaAttackRequest.Targets = new List<AttackRequest>();
            foreach (Character defender in defenders)
            {
                AttackRequest areaEffectTarget = new HCSIntegration.AttackRequest();
                areaEffectTarget.Target = defender.Name;
                float range = Vector3.Distance(attacker.CurrentPositionVector, defender.CurrentPositionVector);
                areaEffectTarget.Range = (int)Math.Round((range) / 8f, 2);
                List<Character> otherCharacters = this.InGameCharacters.Where(c => c != attacker && c != defender).ToList();
                var obstructions = collisionEngine.FindObstructingObjects(attacker, defender, otherCharacters);
                if (obstructions != null && obstructions.Count > 0)
                {
                    areaEffectTarget.Obstructions = new List<string>();
                    foreach (var obstruction in obstructions)
                    {
                        if (obstruction.CollidingObject is Character)
                            areaEffectTarget.Obstructions.Add((obstruction.CollidingObject as Character).Name);
                        else
                            areaEffectTarget.Obstructions.Add(obstruction.CollidingObject.ToString());
                    }
                }
                
                var knockbackObstacles = collisionEngine.CalculateKnockbackObstructions(attacker, defender, 50, otherCharacters);
                if (knockbackObstacles != null && knockbackObstacles.Count > 0)
                {
                    areaEffectTarget.PotentialKnockbackCollisions = new List<HCSIntegration.PotentialKnockbackCollision>();
                    foreach (var knockbackObstacle in knockbackObstacles)
                    {
                        areaEffectTarget.PotentialKnockbackCollisions.Add(
                            new PotentialKnockbackCollision
                            {
                                CollisionObject = knockbackObstacle.CollidingObject is Character ? (knockbackObstacle.CollidingObject as Character).Name : knockbackObstacle.CollidingObject.ToString(),
                                CollisionDistance = (int)Math.Round((knockbackObstacle.CollisionDistance) / 8f, 2)
                            }
                            );
                    }

                }
                areaAttackRequest.Targets.Add(areaEffectTarget);
            }

            WriteToAbilityActivatedFile(areaAttackRequest);
            //GenerateSampleAreaAttackResult(areaAttackRequest);
        }
        private void GenerateSampleAreaAttackResult(AreaAttackRequest areaAttackRequest)
        {
            AreaAttackResponse attackResponse = new AreaAttackResponse();
            attackResponse.Ability = areaAttackRequest.Ability;
            attackResponse.IsHit = true;
            attackResponse.MoveBeforeAttackRequired = false;
            attackResponse.Type = areaAttackRequest.Type;
            attackResponse.Token = attackResponse.Token;
            attackResponse.Targets = new List<AttackResponse>();
            foreach (var target in areaAttackRequest.Targets)
            {
                AttackResponse targetResponse = new HCSIntegration.AttackResponse();
                targetResponse.Ability = areaAttackRequest.Ability;
                targetResponse.IsHit = true;
                targetResponse.MoveBeforeAttackRequired = false;
                targetResponse.Defender = new HCSIntegration.TargetObject
                {
                    Name = target.Target,
                    Body = new HealthMeasures { Name = "BODY", Starting = 70, Current = 35 },
                    Stun = new HealthMeasures { Name = "Stun", Starting = 40, Current = 15 },
                    Endurance = new HealthMeasures { Name = "End", Starting = 90, Current = 25 },
                    Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                };

                if (target.Obstructions != null && target.Obstructions.Count > 0)
                {
                    targetResponse.ObstructionDamageResults = new List<HCSIntegration.AttackResponse>();
                    foreach (var obstr in target.Obstructions)
                    {
                        AttackResponse obsResponse = new HCSIntegration.AttackResponse();
                        obsResponse.IsHit = true;
                        obsResponse.Ability = targetResponse.Ability;
                        obsResponse.Defender = new TargetObject
                        {
                            Name = obstr,
                            Body = new HealthMeasures { Name = "BODY", Starting = 70, Current = 5 },
                            Stun = new HealthMeasures { Name = "Stun", Starting = 20, Current = 5 },
                            Endurance = new HealthMeasures { Name = "End", Starting = 50, Current = 15 },
                            Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                        };
                        obsResponse.DamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15 };
                        targetResponse.ObstructionDamageResults.Add(obsResponse);
                    }
                }

                targetResponse.DamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15 };

                if (target.PotentialKnockbackCollisions != null && target.PotentialKnockbackCollisions.Count > 0)
                {
                    targetResponse.KnockbackResult = new KnockbackResult
                    {
                        Distance = 10,
                        Collisions = new List<KnockbackCollision>
                        {
                            new KnockbackCollision
                            {
                                CollidingObject = new TargetObject
                                {
                                    Name = target.PotentialKnockbackCollisions.First(kd => kd.CollisionDistance ==  target.PotentialKnockbackCollisions.Min(kds => kds.CollisionDistance)).CollisionObject,
                                    Body = new HealthMeasures { Name = "BODY", Starting = 50, Current = 15 },
                                    Stun = new HealthMeasures { Name = "Stun", Starting = 20, Current = 5 },
                                    Endurance = new HealthMeasures { Name = "End", Starting = 50, Current = 15 },
                                    Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                                },
                                CollisionDamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15}
                            }
                        }
                    };
                }

                attackResponse.Targets.Add(targetResponse);
            }
            string pathAttackRes = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter streamWriter = new StreamWriter(pathAttackRes))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(jsonWriter, attackResponse);
                    streamWriter.Flush();
                }
            }
        }
    }
}
