using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.ManagedCharacter;
using HeroVirtualTableTop.Desktop;
using Framework.WPF.Library;
using System.ComponentModel;

namespace HeroVirtualTableTop.Crowd
{
    public static class CROWD_CONSTANTS
    {
        public const string ALL_CHARACTER_CROWD_NAME = "All Characters";

    }
   
   

    public interface CrowdRepository
    {

        Dictionary<string, Crowd> CrowdsByName { get;}
        List <Crowd> Crowds { get; set; }
        Crowd NewCrowd(Crowd parent = null, string name = "Character");
        CharacterCrowdMember NewCharacterCrowdMember(Crowd parent = null, string name = "Character");

        Crowd AllMembersCrowd { get;  }
        string CreateUniqueName(string name,CrowdMember member);
    }

    public interface Crowd : CrowdMember
    {
        bool UseRelativePositioning { get; set; }
        List<CrowdMemberShip> MemberShips { get;}
        List<CrowdMember> Members { get; }
        Dictionary<string, CrowdMember> MembersByName { get; }
        void MoveCrowdMemberAfter(CrowdMember destination, CrowdMember crowdToMove);
        void AddCrowdMember(CrowdMember member);
        void RemoveMember(CrowdMember member);
        bool IsExpanded { get; set; }
        CrowdRepository CrowdRepository { get; set; }
        

    }

    public interface CharacterCrowdMember : ManagedCharacter.ManagedCharacter, CrowdMember
    {
        new string Name { get; set; }
        
    }
    public interface CrowdMember : CrowdMemberCommands, INotifyPropertyChanged
    {
        int Order { get; set; }
        bool MatchesFilter { get; set; }
        string OldName { get; set; }

        string Name { get; set; }

        CrowdMember Clone();
        void ApplyFilter(string filter);
        void ResetFilter();
        bool CheckIfNameIsDuplicate(string updatedName, List<Crowd> members);
        List<CrowdMemberShip> AllCrowdMembershipParents { get; }
        Crowd Parent { get; set; }

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


