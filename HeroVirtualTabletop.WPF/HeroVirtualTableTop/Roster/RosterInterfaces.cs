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
    public interface Roster : ManagedCharacterCommands, CrowdMemberCommands, AnimatedCharacterCommands
    {
        string Name { get; set; }
        RosterCommandMode ComandMode { get; set; }
        OrderedCollection<RosterGroup> Groups { get; }
        List<RosterParticipant> Participants { get; }
        Dictionary<string, RosterGroup> GroupsByName { get; }
        Dictionary<string, RosterParticipant> ParticipantsByName { get; }

        // List<CharacterCrowdMember> SelectedParticipants { get; set; }
        List<RosterParticipant> SelectedParticipants { get; }
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

        List<AnimatedAbility.AnimatedAbility> CommonAbilitiesForActiveCharacters { get; }
        RosterParticipant TargetedCharacter { get; set; }

        void GroupSelectedParticpants();



    }

    public interface RosterGroup: OrderedElement, OrderedCollection<RosterParticipant>
    {

    }

    public interface RosterParticipant: OrderedElement, INotifyPropertyChanged

    {
       
    }


}
