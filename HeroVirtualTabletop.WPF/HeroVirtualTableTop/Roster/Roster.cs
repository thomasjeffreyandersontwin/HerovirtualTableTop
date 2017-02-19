using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Internal;
using HeroVirtualTableTop.AnimatedAbility;
using HeroVirtualTableTop.Attack;
using HeroVirtualTableTop.Crowd;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.Common;
using HeroVirtualTableTop.ManagedCharacter;
using Module.HeroVirtualTabletop.OptionGroups;
using Ploeh.AutoFixture;

namespace HeroVirtualTableTop.Roster
{
    public class RosterImpl : Roster
    {
        
        public RosterImpl()
        {
            Groups = new OrderedCollectionImpl<RosterGroup>();
            Selected = new RosterSelectionImpl();
        }

       

        public string Name { get; set; }
        public RosterCommandMode ComandMode { get; set; }

        public OrderedCollection<RosterGroup> Groups { get; }
        public List<RosterParticipant> Participants
        {
            get
            {
                List<RosterParticipant> participants = new List<RosterParticipant>();
                foreach (var group in Groups.Values)
                {
                    foreach (RosterParticipant p in group.Values)
                    {
                        participants.Add(p);
                    }
                }
                return participants;
            }
        }

       // public Dictionary<string, RosterGroup> GroupsByName { get; }
       // public Dictionary<string, RosterParticipant> ParticipantsByName { get; }
       
        public RosterSelection Selected { get;}
        public void SelectParticipant(RosterParticipant participant)
        {
            Selected.Participants.Add(participant);
        }
        public void UnsSelectParticipant(RosterParticipant participant)
        {
            Selected.Participants.Remove(participant);
        }
        public void AddCrowdMemberAsParticipant(CharacterCrowdMember participant)
        {
            var group = createRosterGroup(participant.Parent);
            group.InsertElement(participant);
            participant.PropertyChanged += EnsureOnlyOneActiveOrAttackingCharacterInRoster;

        }
        public void RemoveParticipant(RosterParticipant participant)
        {
            throw new NotImplementedException();
        }       

        public void CreateGroupFromCrowd(Crowd.Crowd crowd)
        {
            var group = createRosterGroup(crowd);
            foreach (CrowdMember member in crowd.Members)
            {
                if (member is Crowd.Crowd)
                {
                    CreateGroupFromCrowd(member as Crowd.Crowd);
                }
                else
                {
                    group.InsertElement(member);
                    member.PropertyChanged += EnsureOnlyOneActiveOrAttackingCharacterInRoster;
                }
            }
        }
        private RosterGroup createRosterGroup(Crowd.Crowd crowd)
        {
            RosterGroup group = null;
            if (Groups.ContainsKey(crowd.Name) == false)
            {
                @group = new RostergroupImpl { Name = crowd.Name };
            }
            else
            {
                @group = Groups[crowd.Name];
            }
            Groups.InsertElement(group);
            return @group;
        }
        public void RemoveGroup(RosterGroup group)
        {
            throw new NotImplementedException();
        }
        public void SelectGroup(RosterGroup group)
        {
            foreach (var p in group.Values)
            {
                SelectParticipant(p);
            }
        }
        public void UnSelectGroup(RosterGroup group)
        {
            foreach (var p in group.Values)
            {
                UnsSelectParticipant(p);
            }
        }
        public void ClearAllSelections()
        {
            List<RosterParticipant> sel = Selected.Participants.ToList();
            foreach (var p in sel)
            {
                UnsSelectParticipant(p);
            }
        }
        public void SelectAllParticipants()
        {
            foreach (RosterGroup g in Groups.Values)
            {
                foreach (RosterParticipant p in g.Values)
                {
                    SelectParticipant(p);
                    
                }
            }
        }

        public Crowd.Crowd SaveAsCrowd()
        {
            CrowdRepository repo = new CrowdRepositoryImpl();
            Crowd.Crowd rosterClone = repo.NewCrowd(null, Name);
            foreach (RosterGroup group in Groups.Values)
            {
                Crowd.Crowd groupClone = repo.NewCrowd(rosterClone, group.Name);
                groupClone.Order = group.Order;
                foreach(RosterParticipant participant in group.Values)
                {
                    groupClone.AddCrowdMember(participant as CrowdMember);
                }
            }
            return rosterClone;
        }

        //character event subscription methods
        private void EnsureOnlyOneActiveOrAttackingCharacterInRoster(object sender, PropertyChangedEventArgs e)
        {
            AnimatedCharacter characterThatChanged = sender as AnimatedCharacter;
            updateRosterCharacterStateBasedOnCharacterChange(e.PropertyName, characterThatChanged, "ActiveCharacter", "IsActive");
            updateRosterCharacterStateBasedOnCharacterChange(e.PropertyName, characterThatChanged, "TargetedCharacter", "IsTargeted");
            updateRosterCharacterStateBasedOnCharacterChange(e.PropertyName, characterThatChanged, "LastSelectedCharacter", "IsSelected");
            if (e.PropertyName == "ActiveAttack")
            {
                if (characterThatChanged != AttackingCharacter)
                {
                    if (characterThatChanged.ActiveAttack != null)
                    {
                        if (AttackingCharacter != null)
                        {
                            ((AnimatedCharacter)AttackingCharacter).ActiveAttack?.Stop();
                        }
                    }
                    AttackingCharacter = characterThatChanged as RosterParticipant;
                }
                else
                {
                    if (AttackingCharacter == characterThatChanged)
                    {
                        characterThatChanged.ActiveAttack = null;
                        AttackingCharacter = null;
                    }
                }
            }
        }
        private void updateRosterCharacterStateBasedOnCharacterChange(string propertyName, AnimatedCharacter characterThatChanged, string rosterStateToChangeProperty, string characterStateThatchanged)
        {
            PropertyInfo propertyInfoforStateToChange = this.GetType().GetProperty(rosterStateToChangeProperty);
            RosterParticipant rosterStateToChange = (RosterParticipant)
                propertyInfoforStateToChange.GetValue(this);

            PropertyInfo propertyInfoForCharacterThatchanged = characterThatChanged.GetType().GetProperty(characterStateThatchanged);
            bool changedVal = (bool)propertyInfoForCharacterThatchanged.GetValue(characterThatChanged);
            if (changedVal == true)
            {
                if (rosterStateToChange != characterThatChanged)
                {
                    if (rosterStateToChange != null)
                    {
                        propertyInfoForCharacterThatchanged.SetValue(rosterStateToChange, false);
                    }
                    rosterStateToChange = characterThatChanged as RosterParticipant;
                    propertyInfoforStateToChange.SetValue(this, characterThatChanged);
                }
            }
            else
            {
                if (rosterStateToChange == characterThatChanged)
                {
                    (characterThatChanged as CrowdMember).PropertyChanged -= EnsureOnlyOneActiveOrAttackingCharacterInRoster;
                    propertyInfoForCharacterThatchanged.SetValue(characterThatChanged, false);
                    // characterThatChanged.IsActive = false;
                    propertyInfoforStateToChange.SetValue(this, null);
                    (characterThatChanged as CrowdMember).PropertyChanged += EnsureOnlyOneActiveOrAttackingCharacterInRoster;
                }
            }

        }
        public RosterParticipant ActiveCharacter { get; set; }
        public RosterParticipant AttackingCharacter { get; set; }
        public RosterParticipant TargetedCharacter { get; set; }     
        public RosterParticipant LastSelectedCharacter {
            get { return Selected.Participants.LastOrDefault(); }
            set
            {
              SelectParticipant(value);  
            } 
        }
        
        public void GroupSelectedParticpants()
        {
            throw new NotImplementedException();
        }

    }
    class RostergroupImpl : OrderedCollectionImpl<RosterParticipant>, RosterGroup
    {
        public string Name { get; set; }
        public int Order { get; set; }
    }

    public class RosterSelectionImpl : RosterSelection
    {
        public RosterSelectionImpl()
        {
            Participants = new List<RosterParticipant>();
        }

           
        
       
        public Dictionary<CharacterActionType, Dictionary<string,CharacterAction>> CharacterActionGroups {
            get
            {
                var lists = new Dictionary<CharacterActionType, Dictionary<string,CharacterAction>>();
                lists[CharacterActionType.Ability] = getCommonCharacterActionsForSelectedParticipants(CharacterActionType.Ability);
                lists[CharacterActionType.Identity] = getCommonCharacterActionsForSelectedParticipants(CharacterActionType.Identity);
                return lists;


            }
        }
        private Dictionary<string, CharacterAction> getCommonCharacterActionsForSelectedParticipants(CharacterActionType type)
        {
            var returnList = new Dictionary<string, CharacterAction>();
            var participant = Participants.FirstOrDefault();

            //get collection of actions from first participant based on character action type
            var actionPropertyName = getActionCollectionPropertyNameForType(type);
            PropertyInfo actionProperty = participant.GetType().GetProperty(actionPropertyName);
            System.Collections.IDictionary participantActions = (IDictionary)actionProperty.GetValue(participant);

            foreach (CharacterAction action in participantActions.Values)
            {
                var commonActions = getActionsWithSameNameAcrossAllParticioants(action, actionPropertyName);
                CommonCharacterActionsWrapper commonWrapper;
                //add wrapper to common list
                switch (type)
                {
                    case CharacterActionType.Identity:
                        commonWrapper = new CommonIdentityWrapper(this, commonActions);
                        returnList[commonWrapper.Name] = commonWrapper;
                        break;
                    case CharacterActionType.Ability:
                        commonWrapper = new CommonAbilityWrapper(this, commonActions);
                        returnList[commonWrapper.Name] = commonWrapper;
                        break;
                    case CharacterActionType.Movement:
                        //   commonWrapper = new CommonWrapper(this, commonActions);

                        //   returnList.AddNew(commonWrapper);
                        break;
                }
            }
            return returnList;
        }
        private List<CharacterAction> getActionsWithSameNameAcrossAllParticioants(CharacterAction action, string actionPropertyName)
        {
            PropertyInfo actionProperty = Participants.FirstOrDefault().GetType().GetProperty(actionPropertyName);
            //check the other particpatns to see if they also have the action
            List<RosterParticipant> participantsWithCommonAction = Participants.Where(
                x => ((IDictionary)actionProperty.GetValue(x)).Contains(action.Name)).ToList();

            //add the matching action from each particpant to a wrapper class
            List<CharacterAction> withCommonName = new List<CharacterAction>();
            foreach (var x in Participants)
            {
                CharacterCrowdMember c = (CharacterCrowdMember)x;

                actionProperty = c.GetType().GetProperty(actionPropertyName);
                System.Collections.IDictionary potentialActions = (IDictionary)actionProperty.GetValue(c);
                CharacterAction commonAction = (CharacterAction)potentialActions[action.Name];

                if (commonAction != null)
                {
                    withCommonName.Add(commonAction);
                }
            }
            return withCommonName;
        }
        private static string getActionCollectionPropertyNameForType(CharacterActionType type)
        {
            string actionPropertyName = "";
            switch (type)
            {
                case CharacterActionType.Identity:
                    actionPropertyName = "Identities";
                    break;
                case CharacterActionType.Ability:
                    actionPropertyName = "Abilities";
                    break;
                case CharacterActionType.Movement:
                    actionPropertyName = "Movements";
                    break;
            }
            return actionPropertyName;
        }
        public List<RosterParticipant> Participants { get; set; }

        public Dictionary<string, Identity> IdentitiesList
        {
            get
            {
                var i = new Dictionary<string, Identity>();
                var actions = CharacterActionGroups[CharacterActionType.Identity];
                foreach (var characterAction in actions.Values)
                {
                    var id = (Identity)characterAction;
                    i[id.Name] = id;
                }
                return i;
            }



        }
        public void SpawnToDesktop(bool completeEvent = true)
        {
            foreach (var part in Participants)
            {
                part.SpawnToDesktop(completeEvent);
            }
        }
        public void ClearFromDesktop(bool completeEvent = true)
        {
            //foreach (var crowdMember in Members)
              //  crowdMember.ClearFromDesktop(completeEvent);
        }
        public void MoveCharacterToCamera(bool completeEvent = true)
        {
          //  foreach (var crowdMember in Members)
            //    crowdMember.MoveCharacterToCamera(completeEvent);
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }
        public void DeActivate()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, AnimatedAbility.AnimatedAbility> AbilitiesList
        {
            get
            {
                var i = new Dictionary<string, AnimatedAbility.AnimatedAbility>();
                var actions = CharacterActionGroups[CharacterActionType.Ability];
                foreach (var characterAction in actions.Values)
                {
                    var id = (AnimatedAbility.AnimatedAbility) characterAction;
                    i[id.Name] = id;
                }
                return i;
            }
        }
    }
    class CommonAbilityWrapper : CommonCharacterActionsWrapper, AnimatedAbility.AnimatedAbility
    {
        public CommonAbilityWrapper(RosterSelection selection, List<CharacterAction> list) : base(selection, list)
        {
        }

        public SequenceType Type { get; set; }
        public List<AnimationElement> AnimationElements { get; }
        public void InsertMany(List<AnimationElement> animationElements)
        {
            throw new NotImplementedException();
        }
        public void InsertElement(AnimationElement toInsert)
        {
            throw new NotImplementedException();
        }
        public void RemoveElement(AnimationElement animationElement)
        {
            throw new NotImplementedException();
        }
        public void InsertElementAfter(AnimationElement toInsert, AnimationElement moveAfter)
        {
            throw new NotImplementedException();
        }

        public void Stop(AnimatedCharacter target)
        {
            Stop();
        }
        public void Play(AnimatedCharacter target)
        {
            Play();
        }
        public void Play(List<AnimatedCharacter> targets)
        {
            Play();
        }

        public AnimatedCharacter Target { get; set; }
        public bool Persistant { get; set; }
        public AnimationSequencer Sequencer { get; }
        public AnimatedAbility.AnimatedAbility StopAbility { get; set; }

        public AnimatedAbility.AnimatedAbility Clone(AnimatedCharacter target)
        {
            throw new NotImplementedException();
        }
    }
    class CommonCharacterActionsWrapper : CharacterActionImpl
    {
        public CommonCharacterActionsWrapper(RosterSelection selection, List<CharacterAction> list)
        {

            CommonActions = list;
            Owner = selection;
        }
        private RosterSelection _selection;
        protected List<CharacterAction> CommonActions;
        public string Name
        {
            get
            {
                return CommonActions?.FirstOrDefault()?.Name;

            }
            set { }
        }
        public override int Order
        {
            get { return CommonActions.FirstOrDefault().Order; }
            set { }
        }
        public override CharacterActionContainer Owner
        {
            get
            {
                return _selection;

            }
            set { _selection = value as RosterSelection; }
        }
        public override KeyBindCommandGenerator Generator
        {
            get { return CommonActions?.FirstOrDefault()?.Generator; }
            set { }
        }
        public override void Play(bool completeEvent = true)
        {
            CommonActions.ForEach(action => action.Play(completeEvent));
        }
        public override void Stop(bool completeEvent = true)
        {
            CommonActions.ForEach(action => action.Stop(completeEvent));
        }
        public override CharacterAction Clone()
        {
            throw new NotImplementedException();
        }
    }
    class CommonIdentityWrapper : CommonCharacterActionsWrapper, Identity
    {
        public CommonIdentityWrapper(RosterSelection selection, List<CharacterAction> list) : base(selection, list)
        {
        }

        public string Surface
        {
            get
            {
                Identity i = (Identity)CommonActions?.FirstOrDefault();
                return i.Surface;
            }
            set { }
        }
        public SurfaceType Type { get; set; }
    }


   

    
}
