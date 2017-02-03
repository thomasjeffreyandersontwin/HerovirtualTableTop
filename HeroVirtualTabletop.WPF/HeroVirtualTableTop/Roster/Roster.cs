using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            AnimatedCharacter p = sender as AnimatedCharacter;
            if (e.PropertyName == "IsActive")
            {
                if (p.IsActive == true)
                {
                    if (ActiveCharacter != p)
                    {
                        if (ActiveCharacter != null)
                        {
                            ((AnimatedCharacter) ActiveCharacter).IsActive = false;
                        }
                        ActiveCharacter = p as RosterParticipant;
                    }
                }
                else
                {
                    if (ActiveCharacter == p)
                    {
                        p.IsActive = false;
                        ActiveCharacter = null;
                    }
                }
            }
            if (e.PropertyName == "IsAttacking")
            {
                if (p != AttackingCharacter)
                {
                    if (p.ActiveAttack != null)
                    {
                        ((AnimatedCharacter) AttackingCharacter).ActiveAttack.Stop();
                        AttackingCharacter = p as RosterParticipant;
                    }
                }
                else
                {
                    if (AttackingCharacter == p)
                    {
                        p.ActiveAttack = null;
                        AttackingCharacter = null;
                    }
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

        public void SaveAsCrowd()
        {
            throw new NotImplementedException();
        }

        public RosterParticipant ActiveCharacter { get; set; }
        public RosterParticipant AttackingCharacter { get; set; }
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
