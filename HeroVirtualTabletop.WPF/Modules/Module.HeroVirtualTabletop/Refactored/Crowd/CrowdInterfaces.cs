using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.ManagedCharacter;
using HeroVirtualTableTop.Desktop;
using Framework.WPF.Library;
using System.ComponentModel;
using HeroVirtualTabletop.AnimatedCharacter;
using HeroVirtualTabletop.MoveableCharacter;

namespace HeroVirtualTableTop.Crowd
{
    public static class CROWD_CONSTANTS
    {
        public const string ALL_CHARACTER_CROWD_NAME = "All Characters";

    }
    
    public interface CrowdRepository
    {
        HashedObservableCollection<Crowd, string> Crowds { get; set; }
        void AddCrowd(Crowd crowd);
        Crowd NewCrowd(Crowd parent=null);
        CharacterCrowdMember NewCharacterCrowdMember(Crowd parent=null, string name = "Character");

        Crowd AllMembersCrowd { get; set; }
        string CreateUniqueName(string name);
    }

    public interface Crowd : CrowdMember
    {
        bool UseRelativePositioning { get; set; }
        HashedObservableCollection<CrowdMembership, string> Members {get; set; }       
        void AddCrowdMember(CrowdMember member);
        void RemoveMember(CrowdMember member);
        bool IsExpanded { get; set; }
        CrowdRepository CrowdRepository { get; set; }

    }

    public interface CharacterCrowdMember: ManagedCharacter.ManagedCharacter, CrowdMember
    {
        new string Name { get; set; }
        CrowdMembership LoadedParentMembership { get; set; }
    }
    public interface CrowdMember : CrowdMemberCommands, INotifyPropertyChanged
    {
        bool MatchesFilter { get; set; }
        string OldName { get; set; }
        string Name { get; set; }

        CrowdMember Clone();
        void ApplyFilter(string filter);
        void ResetFilter();
        bool CheckIfNameIsDuplicate(string updatedName);
        
    }
    public interface CrowdMembership
    {
        int Order { get; set; }
        Crowd ParentCrowd { get; }
        CrowdMember Child { get; set; }
        Position SavedPosition { get; set; }
    }


    public interface CrowdMemberCommands
    {
        void SaveCurrentTableTopPosition();
        void PlaceOnTableTop(Position position = null);
        void PlaceOnTableTopUsingRelativePos();
        
    }

    public interface CharacterCrowd : CrowdMembership, ManagedCharacterCommands, CrowdMemberCommands
    {
        bool UseRelativePositioning { get; set; }
        Dictionary<string, CrowdMembership> Members { get; set; }
        CrowdMember AddCharacter(ManagedCharacter.ManagedCharacter character);
        CharacterCrowd AddCrowd(CharacterCrowd crowd);
    }

    

    public interface CrowdClipboard
    {
        void CopyToClipboard(CrowdMember member);
        void LinkToClipboard(CrowdMember member);
        void CutToClipboard(CrowdMember member);
        void PasteFromClipboard(CrowdMember member);
    }


    public interface Roster : ManagedCharacterCommands, CrowdMemberCommands, AnimatedCharacterCommands
    {
        Dictionary<string, CharacterCrowd> Crowds { get; set; }
        Dictionary<string, MoveableCharacter> Participants { get; set; }

        List<CrowdMembership> SelectedParticipants { get; set; }
        void AddMemberToSelection(CrowdMembership member);
        void RemoveMemberFromSelection(CrowdMembership member);
        void ClearMembersFromSelection();

        CrowdMember ActiveCharacter { get; set; }
        void ActivateCharacter(CrowdMember crowdMember);
        void DeactivateCharacter(CrowdMember crowdMember);
        void AddMember(MoveableCharacter member);
        void RemoveMember(MoveableCharacter member);

    }
}
