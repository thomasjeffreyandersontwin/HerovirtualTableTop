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

namespace Module.HeroVirtualTabletop.HCSIntegration
{
    public interface IHCSIntegrator
    {
        event EventHandler<CustomEventArgs<Object>> SequenceUpdated;
        event EventHandler<CustomEventArgs<Object>> ActiveCharacterUpdated;
        event EventHandler<CustomEventArgs<Object>> AttackResultsUpdated;
        event EventHandler<CustomEventArgs<Object>> AttackResultsNotFound;
        List<Character> InGameCharacters { get; set; }
        void StartIntegration();
        object GetLatestSequenceInfo();
        void ConfigureAttack(Attack attack, List<Character> attackers, List<Character> defenders);
        void PlaySimpleAbility(Character target, AnimatedAbility ability);
        void ConfirmAttack();
        void CancelAttack();
        void NotifyStopMovement(CharacterMovement characterMovement, double distanceTravelled);
        float GetMovementDistanceLimit(CharacterMovement activeMovement);
        void AbortAction(List<Character> abortingCharacters);
    }
    public class HCSIntegrator : IHCSIntegrator
    {
        private CollisionEngine collisionEngine;
        private Timer timer;
        private object lockObj = new object();
        private static FileSystemWatcher HCSIntegratorFileWatcher;
        public List<Character> InGameCharacters { get; set; }
        public HCSIntegrationAction LastIntegrationAction { get; set; }
        public ActiveCharacterInfo CurrentActiveCharacterInfo { get; set; }
        public AttackResultHCS CurrentAttackResult { get; set; }
        public string CurrentAttackResultFileContents { get; set; }
        public string CurrentOnDeckCombatantsFileContents { get; set; }
        public string CurrentChronometerFileContents { get; set; }
        public string CurrentActiveCharacterInfoFileContents { get; set; }
        public HCSAttackType CurrentAttackType { get; set; }

        public event EventHandler<CustomEventArgs<Object>> SequenceUpdated;
        public event EventHandler<CustomEventArgs<Object>> ActiveCharacterUpdated;
        public event EventHandler<CustomEventArgs<Object>> AttackResultsUpdated;
        public event EventHandler<CustomEventArgs<Object>> AttackResultsNotFound;
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
        private void OnAttackResultsNotFound(object sender, CustomEventArgs<Object> e)
        {
            if (AttackResultsNotFound != null)
                AttackResultsNotFound(sender, e);
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
        }

        public void StartIntegration()
        {
            if (EventInfoDirectoryPath != null)
                HCSIntegratorFileWatcher.EnableRaisingEvents = true;
        }

        private DateTime lastReadTime = DateTime.MinValue;
        private void HCSIntegratorFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (lockObj)
            {
                string fileExt = Path.GetExtension(e.FullPath);
                if ((e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created) && fileExt == ".info" || fileExt == ".event")
                {
                    if (e.Name == Constants.COMBATANTS_FILE_NAME || e.Name == Constants.CHRONOMETER_FILE_NAME)
                    {
                        string currentCombatantsJson = this.GetCurrentCombatantsFileContents();
                        string currentChronoMeterJson = this.GetCurrentChronometerFileContents();
                        if(currentCombatantsJson != null && currentChronoMeterJson != null && this.CurrentChronometerFileContents != currentChronoMeterJson || this.CurrentOnDeckCombatantsFileContents != currentCombatantsJson)
                        {
                            this.LastIntegrationAction = HCSIntegrationAction.DeckUpdated;
                            object sequenceInfo = this.GetLatestSequenceInfo();
                            object[] seqArray = sequenceInfo as object[];
                            if (seqArray != null && seqArray.Length == 3 && seqArray[0] != null && seqArray[1] != null && seqArray[2] != null)
                            {
                                OnSequenceUpdated(null, new CustomEventArgs<object> { Value = seqArray });
                            }
                        }
                    }
                    else if (e.Name == Constants.ACTIVE_CHARACTOR_FILE_NAME)
                    {
                        string currentActiveCharacterInfoJson = this.GetCurrentActiveCharacterInfoFileContents();
                        if(this.CurrentActiveCharacterInfoFileContents != currentActiveCharacterInfoJson)
                        {
                            this.LastIntegrationAction = HCSIntegrationAction.ActiveCharacterUpdated;
                            ActiveCharacterInfo activeCharacterInfo = GetCurrentActiveCharacterInfo();
                            if (activeCharacterInfo != null)
                                OnActiveCharacterUpdated(null, new CustomEventArgs<object> { Value = activeCharacterInfo });
                        }
                        
                    }
                    else if (e.Name == Constants.ATTACK_RESULT_FILE_NAME && this.CurrentAttackType != HCSAttackType.None)
                    {
                        if (this.LastIntegrationAction == HCSIntegrationAction.AttackInfoUpdatedWithPossibleCollision || this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated)
                        {
                            string currentJson = this.GetAttackResultsFileContents();
                            if (currentJson != CurrentAttackResultFileContents)
                            {
                                ProcessAttackResults();
                            }
                            else
                                this.CurrentAttackResultFileContents = null;
                        }
                    }
                }
            };
        }

        private void timer_elapsed(object state)
        {
            HCSIntegrationAction integrationAction = (HCSIntegrationAction)state;
            if (integrationAction == this.LastIntegrationAction)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                if (this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated || this.LastIntegrationAction == HCSIntegrationAction.AttackInfoUpdatedWithPossibleCollision)
                {
                    // First check if it has been modified in last 5 secs
                    DateTime now = DateTime.Now;
                    bool resultsReceived = false;
                    string pathAttackResult = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
                    if (File.Exists(pathAttackResult))
                    {
                        DateTime lastModTime = File.GetLastWriteTime(pathAttackResult);
                        if((now - lastModTime).Milliseconds < 5000)
                        {
                            string currentJson = this.GetAttackResultsFileContents();
                            if (currentJson != CurrentAttackResultFileContents)
                            {
                                resultsReceived = true;
                                ProcessAttackResults();
                            }
                        }
                    }
                    if (!resultsReceived)
                    {
                        var customEventArgs = new CustomEventArgs<object>();
                        if (this.CurrentAttackResult != null)
                            customEventArgs.Value = ParseAttackTargetsFromAttackResult(this.CurrentAttackResult);
                        else
                            customEventArgs.Value = null;
                        OnAttackResultsNotFound(this, customEventArgs);
                    }
                }
            }
        }

        public object GetLatestSequenceInfo()
        {
            OnDeckCombatants onDeckCombatants = GetCurrentCombatants();
            Chronometer chronometer = GetCurrentChronometer();
            ActiveCharacterInfo activeCharacterInfo = GetCurrentActiveCharacterInfo();
            return new object[] { onDeckCombatants, chronometer, activeCharacterInfo };
        }

        private OnDeckCombatants GetCurrentCombatants()
        {
            OnDeckCombatants onDeckCombatants = null;
            string json = GetCurrentCombatantsFileContents();
            this.CurrentOnDeckCombatantsFileContents = json;
            if(json != null)
                onDeckCombatants = JsonConvert.DeserializeObject<OnDeckCombatants>(json);
            return onDeckCombatants;
        }

        private string GetCurrentCombatantsFileContents()
        {
            string pathCombatants = Path.Combine(EventInfoDirectoryPath, Constants.COMBATANTS_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathCombatants))
                {
                    System.Threading.Thread.Sleep(1000);
                    using (StreamReader r = new StreamReader(pathCombatants))
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

        private Chronometer GetCurrentChronometer()
        {
            Chronometer chronometer = null;
            string json = GetCurrentChronometerFileContents();
            this.CurrentChronometerFileContents = json;
            if(json != null)
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
                    System.Threading.Thread.Sleep(1000);
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
            if(json != null)
                activeCharacterInfo = JsonConvert.DeserializeObject<ActiveCharacterInfo>(json);
            this.CurrentActiveCharacterInfo = activeCharacterInfo;
            this.CurrentActiveCharacterInfo.AbilitiesEligibilityCollection = GetAbilityActivationEligibilityCollection();
            return activeCharacterInfo;
        }

        private string GetCurrentActiveCharacterInfoFileContents()
        {
            string pathActiveCharacter = Path.Combine(EventInfoDirectoryPath, Constants.ACTIVE_CHARACTOR_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathActiveCharacter))
                {
                    System.Threading.Thread.Sleep(1000);
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

        private List<AbilityActivationEligibility> GetAbilityActivationEligibilityCollection()
        {
            List<AbilityActivationEligibility> eligibilityCollection = new List<HCSIntegration.AbilityActivationEligibility>();
            string json = this.CurrentActiveCharacterInfo.Powers.ToString();

            JToken outer = JToken.Parse(this.CurrentActiveCharacterInfo.Powers.ToString());
            foreach(var obj in outer.Children())
            {
                JProperty jProp = obj as JProperty;
                if(jProp != null)
                {
                    JObject jObj = jProp.Value as JObject;
                    var values = jObj.Properties().Where(p => p.Name == "Is Enabled").Select(p => p.Value);
                    if (values.Count() > 0)
                    {
                        bool val = values.First().Value<bool>();
                        eligibilityCollection.Add(new HCSIntegration.AbilityActivationEligibility { AbilityName = jProp.Name, IsEnabled = val });
                    }
                }
            }
            return eligibilityCollection;
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
        private AttackResultHCS GetAttackResults()
        {
            AttackResultHCS attackResult = null;
            string json = GetAttackResultsFileContents();
            this.CurrentAttackResultFileContents = json;
            switch (this.CurrentAttackType)
            {
                case HCSAttackType.Area:
                    attackResult = JsonConvert.DeserializeObject<AttackAreaTargetResult>(json);
                    break;
                case HCSAttackType.SingleTargetVanilla:
                    attackResult = JsonConvert.DeserializeObject<AttackSingleTargetResult>(json);
                    break;
                default:
                    break;
            }

            return attackResult;
        }

        private void ProcessAttackResults()
        {
            AttackResultHCS attackResult = GetAttackResults();

            if (this.LastIntegrationAction == HCSIntegrationAction.AttackInfoUpdatedWithPossibleCollision)
            {
                this.LastIntegrationAction = HCSIntegrationAction.AttackResultReceivedWithPossibleCollision;
                ProcessSecondaryAttackResults(attackResult);
            }
            else if (this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated)
            {
                this.LastIntegrationAction = HCSIntegrationAction.AttackResultReceived;
                ProcessPrimaryAttackResults(attackResult);
            }
        }

        private void ProcessPrimaryAttackResults(AttackResultHCS attackResult)
        {
            Action d = delegate ()
            {
                this.CurrentAttackResult = attackResult;
                switch (this.CurrentAttackType)
                {
                    case HCSAttackType.Area:
                        ProcessPrimaryAreaTargetResult(attackResult as AttackAreaTargetResult);
                        break;
                    case HCSAttackType.SingleTargetVanilla:
                        ProcessPrimarySingleTargetResult(attackResult as AttackSingleTargetResult);
                        break;
                    default:
                        break;
                }

            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 100);
            adex.ExecuteAsyncDelegate();
        }
        private void ProcessPrimaryAreaTargetResult(AttackAreaTargetResult attackResult)
        {
            //if (attackResult.Targets.Any(t => t.Target.Result.Knockback != null))
            //{
            //    Dictionary<Character, object> targetKnockbackObstructionDictionary = new Dictionary<Character, object>();
            //    foreach (var knockBackResult in attackResult.Targets.Where(t => t.Target.Result.Knockback != null))
            //    {
            //        int distance = knockBackResult.Target.Result.Knockback.Distance;
            //        if (this.CurrentActiveCharacterInfo == null)
            //            this.GetCurrentActiveCharacterInfo();
            //        Character attacker = this.InGameCharacters.FirstOrDefault(c => c.Name == this.CurrentActiveCharacterInfo.Name);
            //        Character target = this.InGameCharacters.FirstOrDefault(c => c.Name == knockBackResult.Target.Name);
            //        List<Character> otherCharacters = this.InGameCharacters.Where(c => c != attacker && !attackResult.Targets.Any(t => t.Target.Name == c.Name)).ToList();
            //        object obstruction = collisionEngine.CalculateKnockbackObstruction(attacker, target, distance, otherCharacters);
            //        if (obstruction != null)
            //        {
            //            targetKnockbackObstructionDictionary.Add(target, obstruction);
            //        }
            //    }
            //    if (targetKnockbackObstructionDictionary.Count > 0)
            //    {
            //        GenerateKnockbackMultiTargetMessage(targetKnockbackObstructionDictionary);
            //        this.LastIntegrationAction = HCSIntegrationAction.AttackInfoUpdatedWithPossibleCollision;
            //        WaitForAttackUpdates();
            //    }
            //    else
            //    {
            //        ProcessSecondaryAttackResults(attackResult);
            //    }
            //}
            //else
            {
                ProcessSecondaryAttackResults(attackResult);
            }
        }
        private void ProcessPrimarySingleTargetResult(AttackSingleTargetResult attackResult)
        {
            //if (attackResult.Results != null && attackResult.Results.Knockback != null)
            //{
            //    int distance = attackResult.Results.Knockback.Distance;
            //    if (this.CurrentActiveCharacterInfo == null)
            //        this.GetCurrentActiveCharacterInfo();
            //    Character attacker = this.InGameCharacters.FirstOrDefault(c => c.Name == this.CurrentActiveCharacterInfo.Name);
            //    Character target = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResult.Target.Name);
            //    List<Character> otherCharacters = this.InGameCharacters.Where(c => c != attacker && c != target).ToList();
            //    object obstruction = collisionEngine.CalculateKnockbackObstruction(attacker, target, distance, otherCharacters);
            //    if (obstruction != null)
            //    {
            //        GenerateKnockbackSingleTargetMessage(obstruction);
            //        this.LastIntegrationAction = HCSIntegrationAction.AttackInfoUpdatedWithPossibleCollision;
            //        WaitForAttackUpdates();
            //    }
            //    else
            //    {
            //        ProcessSecondaryAttackResults(attackResult);
            //    }
            //}
            //else
            {
                ProcessSecondaryAttackResults(attackResult);
            }
        }
        private void ProcessSecondaryAttackResults(AttackResultHCS attackResult)
        {
            this.CurrentAttackResult = attackResult;
            List<Character> attackTargets = ParseAttackTargetsFromAttackResult(attackResult);
            OnAttackResultsUpdated(this, new CustomEventArgs<object> { Value = attackTargets });
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

        private List<Character> ParseAttackTargetsFromAttackResult(AttackResultHCS attackResult)
        {
            List<Character> targets = null;
            switch (this.CurrentAttackType)
            {
                case HCSAttackType.Area:
                    targets = ParseAttackTargetsFromAttackAreaTargetResult(attackResult as AttackAreaTargetResult);
                    break;
                case HCSAttackType.SingleTargetVanilla:
                    targets = ParseAttackTargetsFromAttackSingleTargetResult(attackResult as AttackSingleTargetResult);
                    break;
                default:
                    break;
            }

            return targets;
        }

        private List<Character> ParseAttackTargetsFromAttackSingleTargetResult(AttackSingleTargetResult attackResult)
        {
            List<Character> attackTargets = new List<Character>();
            Character primaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResult.Target.Name);
            if (primaryTarget != null)
            {
                attackTargets.Add(primaryTarget);

                ActiveAttackConfiguration attackConfigPrimary = new ActiveAttackConfiguration();
                attackConfigPrimary.IsHit = attackResult.Results.Hit;
                attackConfigPrimary.Body = attackResult.Target.Body.Current;
                attackConfigPrimary.Stun = attackResult.Target.Stun.Current;
                Character secondaryTarget = null;
                ActiveAttackConfiguration attackConfigSecondary = new ActiveAttackConfiguration();
                if (attackResult.Results.Knockback != null && attackResult.Results.Knockback.Distance != 0)
                {
                    attackConfigPrimary.IsKnockedBack = true;
                    attackConfigPrimary.KnockBackDistance = attackResult.Results.Knockback.Distance;
                    
                    if (attackResult.Results.Knockback.ObstacleCollision != null)
                    {
                        secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResult.Results.Knockback.ObstacleCollision.Name);
                        attackConfigSecondary.Body = attackResult.Results.Knockback.ObstacleCollision.Body.Current;
                        if (attackResult.Results.Knockback.ObstacleCollision.ObstacleDamageResults != null
                        && attackResult.Results.Knockback.ObstacleCollision.ObstacleDamageResults.Effects != null
                        && attackResult.Results.Knockback.ObstacleCollision.ObstacleDamageResults.Effects.Count > 0)
                        {
                            ParseEffects(attackConfigSecondary, attackResult.Results.Knockback.ObstacleCollision.ObstacleDamageResults.Effects);
                        }
                    }
                }

                if (secondaryTarget == null && attackResult.ObstructionResult != null)
                {
                    secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResult.ObstructionResult.ObstructionName);
                    if (attackResult.ObstructionResult.Effects != null
                        && attackResult.ObstructionResult.Effects.Count > 0)
                    {
                        ParseEffects(attackConfigSecondary, attackResult.ObstructionResult.Effects);
                    }
                }

                if (secondaryTarget != null)
                {
                    attackTargets.Add(secondaryTarget);
                    attackConfigSecondary.IsHit = true;
                    attackConfigSecondary.PrimaryTargetCharacter = primaryTarget;
                    attackConfigSecondary.ObstructingCharacter = null;
                    secondaryTarget.ActiveAttackConfiguration = attackConfigSecondary;
                    attackConfigPrimary.ObstructingCharacter = secondaryTarget;
                }
                ParseEffects(attackConfigPrimary, attackResult.Results.Effects);
                primaryTarget.ActiveAttackConfiguration = attackConfigPrimary;
            }
            return attackTargets;
        }

        private List<Character> ParseAttackTargetsFromAttackAreaTargetResult(AttackAreaTargetResult attackResult)
        {
            List<Character> attackTargets = new List<Characters.Character>();
            foreach (var target in attackResult.Targets)
            {
                Character targetCharacter = this.InGameCharacters.FirstOrDefault(c => c.Name == target.Target.Name);
                if (targetCharacter != null)
                {
                    var result = target.Target.Result;
                    attackTargets.Add(targetCharacter);
                    ActiveAttackConfiguration attackConfigPrimary = new ActiveAttackConfiguration();
                    attackConfigPrimary.IsHit = result.Hit;
                    attackConfigPrimary.IsCenterTarget = targetCharacter.Name == attackResult.Center;
                    attackConfigPrimary.Stun = result.DamageResults != null ? result.DamageResults.Stun : null;
                    attackConfigPrimary.Stun = result.DamageResults != null ? result.DamageResults.Stun : null;
                    Character secondaryTarget = null;
                    ActiveAttackConfiguration attackConfigSecondary = new ActiveAttackConfiguration();
                    if (result.Knockback != null && attackConfigPrimary.KnockBackDistance > 0)
                    {
                        attackConfigPrimary.IsKnockedBack = true;
                        attackConfigPrimary.IsHit = true;
                        attackConfigPrimary.KnockBackDistance = result.Knockback.Distance;
                        if (result.Knockback.ObstacleCollision != null)
                        {
                            secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == result.Knockback.ObstacleCollision.Name);
                            attackConfigSecondary.Body = result.Knockback.ObstacleCollision.Body.Current;
                            if (result.Knockback.ObstacleCollision.ObstacleDamageResults.Effects != null
                            && result.Knockback.ObstacleCollision.ObstacleDamageResults.Effects.Count > 0)
                            {
                                ParseEffects(attackConfigSecondary, result.Knockback.ObstacleCollision.ObstacleDamageResults.Effects);
                            }
                        }
                    }
                    if (secondaryTarget == null && attackResult.ObstructionResult != null)
                    {
                        secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResult.ObstructionResult.ObstructionName);
                        if (attackResult.ObstructionResult.Effects != null
                            && attackResult.ObstructionResult.Effects.Count > 0)
                        {
                            ParseEffects(attackConfigSecondary, attackResult.ObstructionResult.Effects);
                        }
                    }
                    if (secondaryTarget != null)
                    {
                        attackTargets.Add(secondaryTarget);
                        attackConfigSecondary.IsHit = true;
                        
                        attackConfigSecondary.ObstructingCharacter = null;
                        
                        secondaryTarget.ActiveAttackConfiguration = attackConfigSecondary;

                        attackConfigPrimary.ObstructingCharacter = secondaryTarget;
                    }
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
            JToken outer = JToken.Parse(this.CurrentActiveCharacterInfo.Powers.ToString());
            bool movementExists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == movementName);
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

        private void WaitForAttackUpdates()
        {
            var integrationAction = this.LastIntegrationAction;
            timer = new Timer(timer_elapsed, integrationAction, Timeout.Infinite, Timeout.Infinite);
            timer.Change(5000, Timeout.Infinite);
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

        private void ConfigureSingleTargetVanillaAttack(Attack attack, Character attacker, Character defender)
        {
            this.CurrentAttackType = HCSAttackType.SingleTargetVanilla;
            GenerateAttackSingleTargetInitiationMessage(attack, attacker, defender);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            WaitForAttackUpdates();
        }

        private void ConfigureAreaAttack(Attack attack, Character attacker, List<Character> defenders)
        {
            this.CurrentAttackType = HCSAttackType.Area;
            GenerateAttackAreaTargetInitiationMessage(attack, attacker, defenders);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            WaitForAttackUpdates();
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
                        ConfigureSingleTargetVanillaAttack(attack, attackers[0], defenders[0]);
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
            if (this.LastIntegrationAction == HCSIntegrationAction.AttackInfoUpdatedWithPossibleCollision || this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated
                || this.LastIntegrationAction == HCSIntegrationAction.AttackResultReceived || this.LastIntegrationAction == HCSIntegrationAction.AttackResultReceivedWithPossibleCollision)
                this.GenerateAttackConfirmationMessage(false);
            ResetAttackParameters();
        }

        public void AbortAction(List<Character> abortingCharacters)
        {
            foreach (Character character in abortingCharacters)
                GenerateAbortActionMessage(character);
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
                    movementName = "Jumping";
                    break;
            }
            return movementName;
        }

        private void GenerateAttackSingleTargetInitiationMessage(Attack attack, Character attacker, Character defender)
        {
            this.CurrentAttackResult = null;
            float range = Vector3.Distance(attacker.CurrentPositionVector, defender.CurrentPositionVector);
            var obstruction = collisionEngine.FindObstructingObject(attacker, defender, this.InGameCharacters.Where(c => c != attacker && c != defender).ToList());
            AttackSingleTarget attackSingleTarget = new AttackSingleTarget
            {
                Type = Constants.ATTACK_SINGLE_TARGET_INITIATION_TYPE_NAME,
                Ability = attack.Name,
                Target = defender.Name,
                Range = (int)Math.Round((range - 5) / 8f, 2)
            };
            if (obstruction != null)
            {
                if (obstruction.CollidingObject is Character)
                    attackSingleTarget.Obstruction = (obstruction.CollidingObject as Character).Name;
                else
                    attackSingleTarget.Obstruction = obstruction.CollidingObject.ToString();
            }
            List<Character> otherCharacters = this.InGameCharacters.Where(c => c != attacker && c != defender).ToList();
            var knockbackObstacle = collisionEngine.CalculateKnockbackObstruction(attacker, defender, 200, otherCharacters);
            if (knockbackObstacle != null)
            {
                attackSingleTarget.PotentialCollision = new HCSIntegration.PotentialCollision
                {
                    Obstacle = knockbackObstacle.CollidingObject is Character ? (knockbackObstacle.CollidingObject as Character).Name : knockbackObstacle.CollidingObject.ToString(),
                    DistanceFromTarget = (int)Math.Round((knockbackObstacle.CollisionDistance - 5) / 8f, 2)
                };
            }

            WriteToAbilityActivatedFile(attackSingleTarget);
        }

        private void GenerateKnockbackSingleTargetMessage(object obstruction)
        {
            KnockbackCollisionSingleTarget knockbackSingleTarget = new HCSIntegration.KnockbackCollisionSingleTarget();
            knockbackSingleTarget.Type = Constants.KNOCKBACK_COLLISION_SINGLE_TARGET_TYPE_NAME;
            if (obstruction is Character)
                knockbackSingleTarget.KnockbackTarget = (obstruction as Character).Name;
            else
                knockbackSingleTarget.KnockbackTarget = obstruction.ToString();

            WriteToAbilityActivatedFile(knockbackSingleTarget);
        }

        private void GenerateKnockbackMultiTargetMessage(Dictionary<Character, object> knockbackTargetObstructionDictionary)
        {
            KnockbackCollisionMultiTarget knockbackMultiTarget = new HCSIntegration.KnockbackCollisionMultiTarget();
            knockbackMultiTarget.Type = Constants.KNOCKBACK_COLLISION_MULTI_TARGET_TYPE_NAME;
            knockbackMultiTarget.TargetObstructions = new List<KnockbackTargetObstruction>();
            foreach (Character keyCharacter in knockbackTargetObstructionDictionary.Keys)
            {
                object obstruction = knockbackTargetObstructionDictionary[keyCharacter];
                if (obstruction is Character)
                    knockbackMultiTarget.TargetObstructions.Add(new KnockbackTargetObstruction { Target = keyCharacter.Name, ObstructingTarget = (obstruction as Character).Name });
                else
                    knockbackMultiTarget.TargetObstructions.Add(new KnockbackTargetObstruction { Target = keyCharacter.Name, ObstructingTarget = obstruction.ToString() });
            }
            WriteToAbilityActivatedFile(knockbackMultiTarget);
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
            AreaEffectTargetCollection areaEffectTargetCollection = new AreaEffectTargetCollection
            {
                Type = Constants.ATTACK_AREA_TARGET_INITIATION_TYPE_NAME,
                Ability = attack.Name
            };
            areaEffectTargetCollection.Targets = new List<AreaEffectTarget>();
            foreach (Character defender in defenders)
            {
                AreaEffectTarget areaEffectTarget = new HCSIntegration.AreaEffectTarget();
                areaEffectTarget.Target = defender.Name;
                float range = Vector3.Distance(attacker.CurrentPositionVector, defender.CurrentPositionVector);
                areaEffectTarget.Range = (int)Math.Round((range - 5) / 8f, 2);
                var obstruction = collisionEngine.FindObstructingObject(attacker, defender, this.InGameCharacters.Where(c => c != attacker && !defenders.Contains(c)).ToList());
                if (obstruction != null)
                {
                    if (obstruction.CollidingObject is Character)
                        areaEffectTarget.Obstruction = (obstruction.CollidingObject as Character).Name;
                    else
                        areaEffectTarget.Obstruction = obstruction.CollidingObject.ToString();
                }
                areaEffectTargetCollection.Targets.Add(areaEffectTarget);
            }

            WriteToAbilityActivatedFile(areaEffectTargetCollection);
        }
    }
}
