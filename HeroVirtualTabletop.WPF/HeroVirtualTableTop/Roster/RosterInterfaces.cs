using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.WPF.Library;
using HeroVirtualTableTop.Crowd;
using HeroVirtualTableTop.AnimatedAbility;
using HeroVirtualTableTop.ManagedCharacter;
using HeroVirtualTableTop.Common;
namespace HeroVirtualTableTop.Roster
{
    public enum RosterCommandMode { Standard, CycleCharacter, OnRosterClick }
    public interface Roster 
    {
        string Name { get; set; }
        RosterCommandMode ComandMode { get; set; }
        OrderedCollection<RosterGroup> Groups { get; }
        List<RosterParticipant> Participants { get; }
        //Dictionary<string, RosterGroup> GroupsByName { get; }
       // Dictionary<string, RosterParticipant> ParticipantsByName { get; }

        // List<CharacterCrowdMember> Participants { get; set; }
        RosterSelection Selected { get; }
        void SelectParticipant(RosterParticipant participant);
        void UnsSelectParticipant(RosterParticipant participant);
        void AddCrowdMemberAsParticipant(CharacterCrowdMember participant);
        void RemoveParticipant(RosterParticipant participant);

        void CreateGroupFromCrowd(Crowd.Crowd crowd);
        void RemoveGroup(RosterGroup crowd);
        void SelectGroup(RosterGroup crowd);
        void UnSelectGroup(RosterGroup crowd);

        void ClearAllSelections();
        void SelectAllParticipants();

        Crowd.Crowd SaveAsCrowd();

        RosterParticipant ActiveCharacter { get; }
        RosterParticipant AttackingCharacter { get; }
        RosterParticipant LastSelectedCharacter { get; }

        RosterParticipant TargetedCharacter { get; set; }

        void GroupSelectedParticpants();



    }

    public interface RosterGroup: OrderedElement, OrderedCollection<RosterParticipant>
    {

    }

    public interface RosterParticipant: OrderedElement, ManagedCharacterCommands, AnimatedCharacterCommands
    {
       
    }

    public interface RosterSelection : CharacterActionContainer, ManagedCharacterCommands, AnimatedCharacterCommands
    {
        List<RosterParticipant> Participants { get;set; }

    }


}
