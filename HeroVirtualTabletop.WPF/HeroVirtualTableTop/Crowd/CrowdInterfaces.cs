using System.Collections.Generic;
using System.ComponentModel;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;

namespace HeroVirtualTableTop.Crowd
{
    public static class CROWD_CONSTANTS
    {
        public const string ALL_CHARACTER_CROWD_NAME = "All Characters";
    }


    public interface CrowdRepository
    {
        Dictionary<string, Crowd> CrowdsByName { get; }
        List<Crowd> Crowds { get; set; }

        Crowd AllMembersCrowd { get; }
        Crowd NewCrowd(Crowd parent = null, string name = "Character");
        CharacterCrowdMember NewCharacterCrowdMember(Crowd parent = null, string name = "Character");
        string CreateUniqueName(string name, CrowdMember member, List<CrowdMember> context);
    }

    public interface Crowd : CrowdMember
    {
        bool UseRelativePositioning { get; set; }
        List<CrowdMemberShip> MemberShips { get; }
        List<CrowdMember> Members { get; }
        Dictionary<string, CrowdMember> MembersByName { get; }
        bool IsExpanded { get; set; }

        void MoveCrowdMemberAfter(CrowdMember destination, CrowdMember crowdToMove);
        void AddManyCrowdMembers(List<CrowdMember> member);

        void AddCrowdMember(CrowdMember member);
        void RemoveMember(CrowdMember member);
    }

    public interface CharacterCrowdMember : ManagedCharacter.ManagedCharacter, CrowdMember
    {
        new string Name { get; set; }
    }

    public interface CrowdMember : CrowdMemberCommands, INotifyPropertyChanged, ManagedCharacterCommands
    {
        int Order { get; set; }
        bool MatchesFilter { get; set; }
        string OldName { get; set; }

        string Name { get; set; }
        List<CrowdMemberShip> AllCrowdMembershipParents { get; }
        Crowd Parent { get; set; }
        CrowdRepository CrowdRepository { get; set; }

        CrowdMember Clone();
        void ApplyFilter(string filter);
        void ResetFilter();
        bool CheckIfNameIsDuplicate(string updatedName, List<CrowdMember> members);

        void RemoveParent(CrowdMember crowdMember);
    }

    public interface CrowdMemberShip
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


    public interface CrowdClipboard
    {
        void CopyToClipboard(CrowdMember member);
        void LinkToClipboard(CrowdMember member);
        void CutToClipboard(CrowdMember member);
        void PasteFromClipboard(CrowdMember member);
    }
}