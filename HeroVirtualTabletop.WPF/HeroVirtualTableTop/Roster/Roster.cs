using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.AnimatedAbility;
using HeroVirtualTableTop.Crowd;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.Common;
using Module.HeroVirtualTabletop.OptionGroups;

namespace HeroVirtualTableTop.Roster
{
    public class RosterImpl : Roster
    {
        
        public RosterImpl()
        {
            Groups = new OrderedCollectionImpl<RosterGroup>();
            SelectedParticipants = new List<RosterParticipant>();
        }

        public void SpawnToDesktop(bool completeEvent = true)
        {
            throw new NotImplementedException();
        }
        public void ClearFromDesktop(bool completeEvent = true)
        {
            throw new NotImplementedException();
        }
        public void MoveCharacterToCamera(bool completeEvent = true)
        {
            throw new NotImplementedException();
        }
        public void SaveCurrentTableTopPosition()
        {
            throw new NotImplementedException();
        }
        public void PlaceOnTableTop(Position position = null)
        {
            throw new NotImplementedException();
        }
        public void PlaceOnTableTopUsingRelativePos()
        {
            throw new NotImplementedException();
        }
        public void Activate()
        {
            throw new NotImplementedException();
        }
        public void DeActivate()
        {
            throw new NotImplementedException();
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

        public Dictionary<string, RosterGroup> GroupsByName { get; }
        public Dictionary<string, RosterParticipant> ParticipantsByName { get; }

        public List<RosterParticipant> SelectedParticipants { get; }

        public void SelectParticipant(RosterParticipant participant)
        {
            SelectedParticipants.Add(participant);
        }
        public void UnsSelectParticipant(RosterParticipant participant)
        {
            SelectedParticipants.Remove(participant);
        }
        public void AddCrowdMemberAsParticipant(CharacterCrowdMember participant)
        {
            var group = createRosterGroup(participant.Parent);
            group.InsertElement(participant);
            participant.PropertyChanged += EnsureOnlyOneActiveOrAttackingCharacterInRoster;

        }
        public void RemoveParticipant(RosterParticipant paticipant)
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

        private void EnsureOnlyOneActiveOrAttackingCharacterInRoster(object sender, PropertyChangedEventArgs e)
        {
            AnimatedCharacter characterThatChanged = sender as AnimatedCharacter;
            updateRosterCharacterStateBasedOnCharacterChange(e.PropertyName, characterThatChanged, "ActiveCharacter", "IsActive");
            updateRosterCharacterStateBasedOnCharacterChange(e.PropertyName, characterThatChanged, "TargetedCharacter","IsTargeted");
            updateRosterCharacterStateBasedOnCharacterChange(e.PropertyName, characterThatChanged, "LastSelectedCharacter", "IsSelected");
            if (e.PropertyName == "ActiveAttack")
            {
                if (characterThatChanged != AttackingCharacter)
                {
                    if (characterThatChanged.ActiveAttack != null)
                    {
                        if (AttackingCharacter != null) { 
                            ((AnimatedCharacter) AttackingCharacter).ActiveAttack?.Stop();                 
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
            bool changedVal = (bool) propertyInfoForCharacterThatchanged.GetValue(characterThatChanged);
            if (changedVal == true)
            {
                if (rosterStateToChange != characterThatChanged)
                {
                    if (rosterStateToChange != null)
                    {
                        propertyInfoForCharacterThatchanged.SetValue(rosterStateToChange,false);
                    }
                    rosterStateToChange = characterThatChanged as RosterParticipant;
                    propertyInfoforStateToChange.SetValue(this, characterThatChanged);
                }
            }
            else
            {
                if (rosterStateToChange == characterThatChanged)
                {
                    (characterThatChanged as CrowdMember).PropertyChanged -=EnsureOnlyOneActiveOrAttackingCharacterInRoster;
                    propertyInfoForCharacterThatchanged.SetValue(characterThatChanged, false);
                   // characterThatChanged.IsActive = false;
                    propertyInfoforStateToChange.SetValue(this, null);
                    (characterThatChanged as CrowdMember).PropertyChanged +=EnsureOnlyOneActiveOrAttackingCharacterInRoster;
                }
            }

        }


        private RosterGroup createRosterGroup(Crowd.Crowd crowd)
        {
            RosterGroup group = null;
            if (Groups.ContainsKey(crowd.Name) == false)
            {
                @group = new RostergroupImpl {Name = crowd.Name};
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
            List<RosterParticipant> sel = SelectedParticipants.ToList();
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

        public RosterParticipant ActiveCharacter { get; set; }
        public RosterParticipant AttackingCharacter { get; set; }
        public RosterParticipant TargetedCharacter { get; set; }
       
        public RosterParticipant LastSelectedCharacter {
            get { return SelectedParticipants.LastOrDefault(); }
            set
            {
              SelectParticipant(value);  
            } 
        }
        public List<AnimatedAbility.AnimatedAbility> CommonAbilitiesForActiveCharacters { get; }
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
}
