using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;
using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Library.Utility;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace HeroVirtualTableTop.Crowd
{





    public class CrowdRepositoryImpl : CrowdRepository
    {


        public Crowd AllMembersCrowd
        {
            get
            {
                if (Crowds == null)
                {
                    Crowds = new List<Crowd>();
                }
                Dictionary<string, Crowd> crowdsbyname = CrowdsByName;
                if (crowdsbyname.Keys.Contains(CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME) == false)
                {
                    createAllmembersCrowd();
                }
                return CrowdsByName[CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME];
            }
        }

        private void createAllmembersCrowd()
        {
            Crowd allMembersCrowd = new CrowdImpl(this);
            allMembersCrowd.Name = CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME;
            Crowds.Add(allMembersCrowd);
        }

        public Dictionary<string, Crowd> CrowdsByName
        {
            get
            {
                Dictionary<string, Crowd> memDict = new Dictionary<string, Crowd>();
                if (Crowds != null)
                {
                    foreach (Crowd crowd in Crowds)
                    {
        
                       memDict.Add(crowd.Name, crowd);
                        crowd.Parent = null;
                    }
                }
                return memDict;
            }
        }
        public List<Crowd> Crowds { get; set; }

        public CrowdRepositoryImpl()
        {
            Crowds = new List<Crowd>();
            createAllmembersCrowd();
        }

        public Crowd NewCrowdInstance { get; set; } //for DI to insert
        public Crowd NewCrowd(Crowd parent = null, string name = "Crowd")
        {
            if (name == "Character") {
                name = "Crowd";
            }
            Crowd newCrowd = this.NewCrowdInstance;
            if (!UsingDependencyInjection)
            {
                newCrowd = new CrowdImpl(this);
                newCrowd.Name = "Crowd";
            }

            // Crowds.Add(newCrowd);


            if (parent != null)
            {
                newCrowd.Name = CreateUniqueName(name, newCrowd, parent.Members);
                parent.AddCrowdMember(newCrowd);
                
            }
            else {
                newCrowd.Name = CreateUniqueName(name, newCrowd, null);
            }
            return newCrowd;
        }

        public CharacterCrowdMember NewCharacterCrowdMemberInstance { get; set; } //for DI to insert
        public bool UsingDependencyInjection { get;set; }

        public CharacterCrowdMember NewCharacterCrowdMember(Crowd parent = null, string name = "Character") 
        {
            CharacterCrowdMember newCharacter = NewCharacterCrowdMemberInstance;

            if (!UsingDependencyInjection )
            {
                newCharacter = new CharacterCrowdMemberImpl(parent, null, null, null, null, this);
            }
            else
            {
                newCharacter.Parent = parent;
                newCharacter.CrowdRepository = this;
               
            }
            
            newCharacter.Name = CreateUniqueName(name, newCharacter, AllMembersCrowd.Members);
            AllMembersCrowd.AddCrowdMember(newCharacter);
            if (parent != null)
            {
                parent.AddCrowdMember(newCharacter);
            }
            return newCharacter;
        }

        public string CreateUniqueName(string name, CrowdMember member, List<CrowdMember> context)
        {
            string suffix = string.Empty;
            string rootName = name;
            int i = 0;
            Regex reg = new Regex(@"\((\d+)\)");
            MatchCollection matches = reg.Matches(name);
            if (matches.Count > 0)
            {
                int k;
                Match match = matches[matches.Count - 1];
                if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out k))
                {
                    i = k + 1;
                    suffix = string.Format(" ({0})", i);
                    rootName = name.Substring(0, match.Index).TrimEnd();
                }
            }
            string newName = rootName + suffix;
            if (context != null)
            {
                List<CrowdMember> allMatches = (from other in context where other.Name == newName select other).ToList();
                while(allMatches.Count>0)
                {
                    suffix = string.Format(" ({0})", ++i);
                    newName = rootName + suffix;
                    allMatches = ( from other in context where other.Name == newName select other).ToList();

                }
            }      
            return newName;
        }
    }


    public class CrowdMemberComparer : IComparer<CrowdMemberShip>
    {
        public int Compare(CrowdMemberShip cmm1, CrowdMemberShip cmm2)
        {
            if (cmm1.Order != cmm2.Order)
                return cmm1.Order.CompareTo(cmm2.Order);
            string s1 = cmm1.Child.Name;
            string s2 = cmm2.Child.Name;

            return Helper.CompareStrings(s1, s2);
        }
    }


    public class CrowdImpl : NotifyPropertyChanged, Crowd
    {
        public bool IsExpanded { get; set; }
        public string OldName { get; set; }
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (Parent != null)
                {
                    if (CheckIfNameIsDuplicate(value, Parent.Members) == true)
                    {
                        throw new DuplicateKeyException(value);
                    }
                }
                OldName = value;
                _name = value;
                OnPropertyChanged("Name");

            }
        }
        public bool CheckIfNameIsDuplicate(string updatedName, List<CrowdMember> crowdlist )
        {
            foreach (CrowdMember member in crowdlist)
            {
                if (member is Crowd)
                {
                    if (member.Name == updatedName) return true;
                    Crowd crowd = member as Crowd;
                    List<CrowdMember> list = new List<CrowdMember>();
                    List<CrowdMember> members= (from crowdMember in crowd.Members where crowdMember is Crowd select crowdMember).ToList();
                    foreach(CrowdMember m in members)
                    {
                        list.Add((Crowd)m);
                    }

                    if (CheckIfNameIsDuplicate(updatedName, list) == true) return true;
                }
            }
            return false;

        }
        public CrowdRepository CrowdRepository { get; set; }
        public bool UseRelativePositioning { get; set; }

        public CrowdImpl(CrowdRepository repo)
        {
            CrowdRepository = repo;
            MemberShips = new List<CrowdMemberShip>();
            AllCrowdMembershipParents = new List<CrowdMemberShip>();

        }

        private CrowdMemberShip _loadedParentMembership;
        public List<CrowdMemberShip> AllCrowdMembershipParents { get; }
        public Crowd Parent
        {
            get
            {
                if (_loadedParentMembership == null)
                {
                    return null;
                }
                else {
                    return _loadedParentMembership.ParentCrowd;
                }
            }
            set
            {
                if (value != null)
                {
                    Dictionary<string, CrowdMemberShip> parents = AllCrowdMembershipParents.ToDictionary(x => x.ParentCrowd.Name);
                    if (parents.ContainsKey(value.Name) == false)
                    {
                        CrowdMemberShip parent = new CrowdMemberShipImpl(value, this);
                        AllCrowdMembershipParents.Add(parent);
                        _loadedParentMembership = parent;
                        _loadedParentMembership.Child = this;
                    }
                    else
                    {
                        _loadedParentMembership = parents[value.Name];
                    }
                    
                }
                else
                {
                    _loadedParentMembership = null;
                }
                
            }
        }

        public List<CrowdMemberShip> MemberShips { get; }
        public List<CrowdMember> Members
        {
            get
            {
                return MemberShips.Select(i =>
                    {
                        i.Child.Parent = this;
                        return i.Child; }
                    ).OrderBy(x=>x.Order).ToList();
            }
        }
        public Dictionary<string, CrowdMember> MembersByName
        {
            get
            {
                Dictionary<string, CrowdMember> memDict = new Dictionary<string, CrowdMember>();
                foreach (CrowdMemberShip ship in MemberShips)
                {
                    memDict.Add(ship.Child.Name, ship.Child);
                    ship.Child.Parent = ship.ParentCrowd;
                }
                return memDict;
            }
        }
        private List<CrowdMember> GetFlattenedMemberList(List<CrowdMember> list)
        {
            List<CrowdMember> flattened = new List<CrowdMember>();
            foreach (CrowdMember crowdMember in list)
            {
                if (crowdMember is Crowd)
                {
                    Crowd crowd = (crowdMember as Crowd);
                    if (crowd.Members != null && crowd.Members.Count > 0)

                        flattened.AddRange(GetFlattenedMemberList(crowd.Members));
                }
                flattened.Add(crowdMember);
            }
            return flattened;
        }
        public void AddCrowdMember(CrowdMember member)
        {
            CrowdMemberShip membership = new CrowdMemberShipImpl(this, member);
            this.MemberShips.Add(membership);
            member.Order = Members.Count;
            member.Parent = this;
            member.PropertyChanged += Member_PropertyChanged;
        }
        public void AddManyCrowdMembers(List<CrowdMember> members)
        {
            foreach (CrowdMember member in Members)
            {
                AddCrowdMember(member);
            }
        }
        public int Order
        {
            get
            {
                if (_loadedParentMembership != null)
                    return _loadedParentMembership.Order;
                else
                    return 0;
            }
            set
            {
                if (_loadedParentMembership != null)
                    _loadedParentMembership.Order = value;
                OnPropertyChanged("Order");
            }
        }
        public void RemoveParent(CrowdMember parent)
        { 
           CrowdMemberShip shipToKill = this.AllCrowdMembershipParents.Where(y => y.ParentCrowd.Name == parent.Name).FirstOrDefault();
            if (shipToKill != null)
            {
                this.AllCrowdMembershipParents.Remove(shipToKill);
            }
        }
        public void RemoveMember(CrowdMember child)
        {
            int removeOrder = child.Order;
            CrowdMemberShip ship = getMembershipThatAssociatesChildMemberToThis(child);
            removeMembershipFromThisAndChild(child, ship);
            decrementOrderForAllMembersAfterTheDeletedOrder(removeOrder);
            if (child is Crowd)
            {
                Crowd crowdBeingDeleted = child as Crowd;
                deleteAllChildrenIfThisDoesntHaveAnyOtherParentsAndChildrenDoNotHaveOtherParents(crowdBeingDeleted);
            }
            child.PropertyChanged -= Member_PropertyChanged;
        }
        private void decrementOrderForAllMembersAfterTheDeletedOrder(int removeOrder)
        {
            foreach (CrowdMember m in from m in Members orderby m.Order where m.Order > removeOrder select m)
            {
                m.Order = m.Order - 1;
            }
        }
        private void removeMembershipFromThisAndChild(CrowdMember member, CrowdMemberShip ship)
        {
            if (ship != null)
            {
                MemberShips.Remove(ship);
                member.RemoveParent(this);
            }
        }
        private static void deleteAllChildrenIfThisDoesntHaveAnyOtherParentsAndChildrenDoNotHaveOtherParents(Crowd crowdBeingDeleted)
        {
            if (crowdBeingDeleted.AllCrowdMembershipParents.Count == 0)
            {
                foreach (CrowdMember childOfCrowdBeingDeleted in crowdBeingDeleted.Members)
                {
                    if (childOfCrowdBeingDeleted.AllCrowdMembershipParents.Count <= 2) //parent from this crowd and allcrowds is all thats left
                    {
                        crowdBeingDeleted.RemoveMember(childOfCrowdBeingDeleted);
                    }
                }
            }
        }
        private CrowdMemberShip getMembershipThatAssociatesChildMemberToThis(CrowdMember member)
        {
            return (from membership in MemberShips where member.Name == membership.Child.Name select membership).FirstOrDefault();
        }
        public void MoveCrowdMemberAfter(CrowdMember destination, CrowdMember memberToMove)
        {
            if (destination.Parent == memberToMove.Parent)
            {
                int orginalOrder = memberToMove.Order;
                foreach (CrowdMember member in from member in Members orderby member.Order where member.Order > orginalOrder select member)
                {
                    member.Order = member.Order - 1;

                }
                int destinationOrder = destination.Order + 1;
                foreach (CrowdMember member in from member in Members orderby member.Order descending where member.Order >= destinationOrder select member)
                {
                    member.Order = member.Order + 1;

                }
                memberToMove.Order = destinationOrder;
            }
            else
            {
                memberToMove.Parent.RemoveMember(memberToMove);
                AddCrowdMember(memberToMove);
                MoveCrowdMemberAfter(destination, memberToMove);
            }



        }

        public void SaveCurrentTableTopPosition()
        {
            foreach (CrowdMember crowdMembers in Members)
            {
                crowdMembers.SaveCurrentTableTopPosition();
            }
        }
        public void PlaceOnTableTop(Position pos = null)
        {
            foreach (CrowdMember crowdMember in Members)
            {
                crowdMember.PlaceOnTableTop();
            }
        }
        public void PlaceOnTableTopUsingRelativePos()
        {
            foreach (CrowdMember crowdMember in Members)
            {
                crowdMember.PlaceOnTableTopUsingRelativePos();
            }
        }

        public CrowdMember Clone()
        {
            Crowd clone = CrowdRepository.NewCrowd();

            List<CrowdMember> crowds = (from crowd in CrowdRepository.Crowds select crowd as CrowdMember).ToList();
            clone.Name = CrowdRepository.CreateUniqueName(Name, clone, crowds);
            
            clone.UseRelativePositioning = UseRelativePositioning;
            foreach (CrowdMember member in this.Members)
            {
                CrowdMember cloneMember = member.Clone();
                clone.AddCrowdMember(cloneMember);
            }
            //EliminateDuplicateName();
            return clone;
        }
        private void EliminateDuplicateName()
        {
            if (Members != null)
            {
                List<CrowdMember> models = GetFlattenedMemberList(Members.ToList());
                foreach (var member in models)
                {
                    member.Name = CrowdRepository.CreateUniqueName(member.Name , member, member.Parent.Members);
                }
            }
        }

        private bool _matchedFilter = false;
        public bool MatchesFilter
        {
            get
            {
                return _matchedFilter;
            }
            set
            {
                _matchedFilter = value;
                OnPropertyChanged("IsMatched");
            }
        }
        public bool FilterApplied { get; set; }
        public void ApplyFilter(string filter)
        {
            if (FilterApplied == true && MatchesFilter == true)
            {
                return;
            }
            if (string.IsNullOrEmpty(filter))
            {
                MatchesFilter = true;
            }
            else
            {
                Regex re = new Regex(filter, RegexOptions.IgnoreCase);
                MatchesFilter = re.IsMatch(Name);
            }
            IsExpanded = MatchesFilter;
            FilterApplied = true;
        }
        public void ResetFilter()
        {
            FilterApplied = false;
            foreach (CrowdMember crowdMember in Members)
            {
                crowdMember.ResetFilter();
            }
        }

        private void Member_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                //to do MemberShips.Keys.UpdateKey((sender as CrowdMember).OldName, (sender as CrowdMember).Name);
            }
            if (e.PropertyName == "Name" || e.PropertyName == "Order")
            {
                //to do MemberShips.Sort(ListSortDirection.Ascending, new CrowdMemberComparer());
            }
        }
        public void SpawnToDesktop(bool completeEvent = true)
        {
            foreach (CrowdMember crowdMember in Members)
            {
                crowdMember.SpawnToDesktop(completeEvent);
            }
        }
        public void ClearFromDesktop(bool completeEvent = true)
        {
            foreach (CrowdMember crowdMember in Members)
            {
                crowdMember.ClearFromDesktop(completeEvent);
            }
        }
        public void MoveCharacterToCamera(bool completeEvent = true)
        {
            foreach (CrowdMember crowdMember in Members)
            {
                crowdMember.MoveCharacterToCamera(completeEvent);
            }
        }
    }

    public class CrowdMemberShipImpl : NotifyPropertyChanged, CrowdMemberShip
    {
        public CrowdMemberShipImpl(Crowd parent, CrowdMember child)
        {
            ParentCrowd = parent;
            Child = child;
        }
        public int Order { get; set; }
        public Crowd ParentCrowd { get; set; }
        public CrowdMember Child { get; set; }
        Position _savedPosition;
        public Position SavedPosition
        {
            get
            {
                return _savedPosition; ;
            }
            set
            {
                _savedPosition = value;
                OnPropertyChanged("SavedPosition");
            }
        }
        /*
       public void RemoveMemberFromParent(bool nested = false)
       {
           
           if (Child is CharacterCrowdMember)
           {
               // Check if the Character is in All Characters. If so, prompt
               if (ParentCrowd == ParentCrowd.CrowdRepository.AllMembersCrowd)
               {
                   DeleteCrowdMemberFromAllCrowdsByName(Child.Name);
               }
               else
               {
                   // Delete the Character from all occurances of this crowd
                   DeleteCrowdMemberFromCrowdByName(ParentCrowd, Child.Name);
               }
           }
           else // Delete Crowd
           {
               //If it is a nested crowd, just delete it from the parent
               if (Child != null)
               {
                   string nameOfDeletingCrowd = ParentCrowd.Name;
                   DeleteNestedCrowdFromCrowdByName(ParentCrowd, nameOfDeletingCrowd);
               }
               // Check if there are containing characters. If so, prompt
               else if (ParentCrowd.MemberShips != null && ParentCrowd.MemberShips.Where(cm => cm is CrowdMember).Count() > 0)
               {
                   string nameOfDeletingCrowd = "";
                   if (nested)
                   {
                       // Delete crowd specific characters from All Characters and this crowd
                       List<CrowdMember> crowdSpecificCharacters = FindCrowdSpecificCrowdMembers((Crowd)this.Child);
                       nameOfDeletingCrowd = Child.Name;
                       DeleteCrowdMembersFromAllCrowdsByList(crowdSpecificCharacters);
                       DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowd);
                       DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowd);
                       return;
                   }
                   else {
                       nameOfDeletingCrowd = Child.Name;
                       DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowd);
                       DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowd);
                       return;
                   }
               }
               // or just delete the crowd from crowd collection and other crowds
               else
               {
                   string nameOfDeletingCrowd = Child.Name;
                   DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowd);
                   DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowd);
               }
           }
       }
      
         private void DeleteCrowdMemberFromAllCrowdsByName(string nameOfDeletingCrowdMember)
        {
            foreach (CrowdMemberShip membership in ParentCrowd.CrowdRepository.AllMembersCrowd.MemberShips)
            {
                if (membership.Child is Crowd)
                {
                    DeleteCrowdMemberFromCrowdByName((Crowd)membership.Child, nameOfDeletingCrowdMember);
                    DeleteCrowdMemberFromNestedCrowdByName((Crowd)membership.Child, nameOfDeletingCrowdMember);
                }
            }
            DeleteCrowdMemberFromCharacterCollectionByName(nameOfDeletingCrowdMember);
        }
        private void DeleteCrowdMemberFromNestedCrowdByName(Crowd crowd, string nameOfDeletingCrowdMember)
        {
            if (crowd.MemberShips != null && crowd.MemberShips.Count > 0)
            {
                foreach (var membership in crowd.MemberShips)
                {
                    if (membership.Child is Crowd)
                    {
                        var membershipChild = membership as Crowd;
                        if (membershipChild.MemberShips != null)
                        {
                            var crm = membershipChild.MemberShips.Where(cmmm => cmmm.Child.Name == nameOfDeletingCrowdMember).FirstOrDefault();
                            if (crm != null)
                                membershipChild.RemoveMember(crm.Child);
                            DeleteCrowdMemberFromNestedCrowdByName(membershipChild, nameOfDeletingCrowdMember);
                        }
                    }
                }
            }
        }
        private void DeleteCrowdMemberFromCrowdByName(Crowd Crowd, string nameOfDeletingCrowdMember)
        {
            if (Crowd.MemberShips != null)
            {
                var crm = Crowd.MemberShips.Where(cm => cm.Child.Name == nameOfDeletingCrowdMember).FirstOrDefault();
                Crowd.RemoveMember(crm.Child);
            }
        }
        private void DeleteCrowdMemberFromCharacterCollectionByName(string nameOfDeletingCrowdMember)
        {
            var charFromAllCrowd = ParentCrowd.CrowdRepository.AllMembersCrowd.MemberShips.Where(c => c.Child.Name == nameOfDeletingCrowdMember).FirstOrDefault();
            ParentCrowd.CrowdRepository.AllMembersCrowd.MemberShips.Remove(charFromAllCrowd);
        }
        private void DeleteCrowdMemberFromCharacterCollectionByList(List<CrowdMember> crowdMembersToDelete)
        {
            foreach (var crowdMemberToDelete in crowdMembersToDelete)
            {
                var deletingCrowdMember = ParentCrowd.CrowdRepository.AllMembersCrowd.MemberShips.Where(c => c.Child.Name == crowdMemberToDelete.Name).FirstOrDefault();
                ParentCrowd.CrowdRepository.AllMembersCrowd.MemberShips.Remove(deletingCrowdMember);
            }
        }
        private void DeleteCrowdMembersFromAllCrowdsByList(List<CrowdMember> crowdMembersToDelete)
        {
            if (Child is Crowd)
            {
                foreach (Crowd crowd in ((Crowd)Child).MemberShips)
                {
                    DeleteCrowdMembersFromCrowdByList(crowd, crowdMembersToDelete);
                }
                DeleteCrowdMemberFromCharacterCollectionByList(crowdMembersToDelete);
            }
        }
        private void DeleteCrowdMembersFromCrowdByList(Crowd Crowd, List<CrowdMember> crowdMembersToDelete)
        {
            if (Crowd.MemberShips != null)
            {
                foreach (var crowdMemberToDelete in crowdMembersToDelete)
                {
                    var deletingCrowdMemberFromModel = Crowd.MemberShips.Where(cm => cm.Child.Name == crowdMemberToDelete.Name).FirstOrDefault();
                    Crowd.RemoveMember(deletingCrowdMemberFromModel.Child);
                }
            }
        }
        private void DeleteNestedCrowdFromAllCrowdsByName(string nameOfDeletingCrowd)
        {
            if (Child is Crowd)
            {
                foreach (Crowd crowd in ((Crowd)Child).MemberShips)
                {
                    DeleteNestedCrowdFromCrowdByName(crowd, nameOfDeletingCrowd);
                }
            }
        }
        private void DeleteNestedCrowdFromCrowdByName(Crowd Crowd, string nameOfDeletingCrowd)
        {
            if (Crowd.MemberShips != null)
            {
                var CrowdToDelete = Crowd.MemberShips.Where(cm => cm.Child.Name == nameOfDeletingCrowd).FirstOrDefault();
                if (CrowdToDelete != null)
                    Crowd.RemoveMember(CrowdToDelete.Child);
            }
        }
        private void DeleteCrowdFromCrowdCollectionByName(string nameOfDeletingCrowd)
        {
            var crowdToDelete = ((Crowd)Child).MemberShips.Where(cr => cr.Child.Name == nameOfDeletingCrowd).FirstOrDefault();
            ((Crowd)Child).MemberShips.Remove(crowdToDelete);
        }
        private List<CrowdMember> FindCrowdSpecificCrowdMembers(Crowd crowdModel)
        {
            List<CrowdMember> crowdSpecificCharacters = new List<CrowdMember>();
            foreach (CrowdMemberShip cMember in crowdModel.MemberShips)
            {
                if (cMember.Child is CharacterCrowdMember)
                {
                    CharacterCrowdMember currentCharacter = cMember as CharacterCrowdMember;
                    foreach (Crowd crowd in ParentCrowd.CrowdRepository.AllMembersCrowd.MemberShips.Where(cm => cm.Child.Name != ParentCrowd.Name))
                    {
                        var crm = crowd.MemberShips.Where(cm => cm is CharacterCrowdMember && cm.Child.Name == currentCharacter.Name).FirstOrDefault();
                        if (crm == null || crowd.Name == ParentCrowd.CrowdRepository.AllMembersCrowd.Name)
                        {
                            if (crowdSpecificCharacters.Where(csc => csc.Name == currentCharacter.Name).FirstOrDefault() == null)
                                crowdSpecificCharacters.Add(currentCharacter);
                        }
                    }
                }
            }
            return crowdSpecificCharacters;
        }
        */
    }

    public class CharacterCrowdMemberImpl : ManagedCharacterImpl, CharacterCrowdMember
    {
        public CharacterCrowdMemberImpl(Crowd parent, DesktopCharacterTargeter targeter, KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities, CrowdRepository repo) : base(targeter, generator, camera, identities)
        {
            AllCrowdMembershipParents = new List<CrowdMemberShip>();
            Parent = parent;
            CrowdRepository = repo;
        }
     

        private CrowdMemberShip _loadedParentMembership;

        public List<CrowdMemberShip> AllCrowdMembershipParents { get; }

        public Crowd Parent
        {
            get
            {
                return _loadedParentMembership.ParentCrowd;
            }
            set
            {
                if (value == null)
                {
                    _loadedParentMembership = null;
                    return;
                }
                Dictionary<string, CrowdMemberShip> parents = AllCrowdMembershipParents.ToDictionary(x => x.ParentCrowd.Name);
                if (!parents.ContainsKey(value.Name))
                {
                    CrowdMemberShip parent = new CrowdMemberShipImpl(value, this);
                    AllCrowdMembershipParents.Add(parent);
                    _loadedParentMembership = parent;
                }
                else {
                    _loadedParentMembership = parents[value.Name];
                }
                _loadedParentMembership.Child = this;
            }
        }
        public void RemoveParent(CrowdMember parent)
        {
            CrowdMemberShip shipToKill = this.AllCrowdMembershipParents.Where(y => y.ParentCrowd.Name == parent.Name).First();
            this.AllCrowdMembershipParents.Remove(shipToKill);
            if (this.AllCrowdMembershipParents.Count == 1)//remove from Allmembers crowd
            {
                this.CrowdRepository.AllMembersCrowd.RemoveMember(this);
            }
        }

        public int Order
        {
            get
            {
                if (_loadedParentMembership != null)
                    return _loadedParentMembership.Order;
                else
                    return 0;
            }
            set
            {
                if (_loadedParentMembership != null)
                    _loadedParentMembership.Order = value;
                OnPropertyChanged("Order");
            }
        }
        public CrowdRepository CrowdRepository { get; set;}

        public string OldName { get; set; }
        private string _name;
        public new string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (CheckIfNameIsDuplicate(value,null) == true)
                {
                    throw new DuplicateKeyException(value);
                }
                else {
                    OldName = _name;
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }
        public bool CheckIfNameIsDuplicate(string updatedName, List<CrowdMember> members)
        {
            if (CrowdRepository.AllMembersCrowd != null)
            {
                return CrowdRepository.AllMembersCrowd.MembersByName.ContainsKey(updatedName);
            }

            else
            {
                return false;
            }
        }


        private bool isExpanded;
        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }


        public void SaveCurrentTableTopPosition()
        {
            _loadedParentMembership.SavedPosition = Position.Duplicate();
        }
        public void PlaceOnTableTop(Position position = null)
        {
            if (!this.IsSpawned)
            {
                this.SpawnToDesktop();
            }
            if (position != null)
            {
                Position.MoveTo(position);
            }
            else
            {
                Position.MoveTo(_loadedParentMembership.SavedPosition);
            }
        }
        public void PlaceOnTableTopUsingRelativePos() { }

        public override string DesktopLabel
        {
            get
            {

                if (MemoryInstance != null)
                {
                    return MemoryInstance.Label;
                }
                else
                {
                    return null;
                }
            }
        }


        private bool _matchedFilter = false;
        public bool MatchesFilter
        {
            get
            {
                return _matchedFilter;
            }
            set
            {
                _matchedFilter = value;
                OnPropertyChanged("IsMatched");
            }
        }
        public bool FilterApplied { get; set; }
        public void ApplyFilter(string filter)
        {
            if (FilterApplied == true && MatchesFilter == true)
            {
                return;
            }
            if (string.IsNullOrEmpty(filter))
            {
                MatchesFilter = true;
            }
            else
            {
                Regex re = new Regex(filter, RegexOptions.IgnoreCase);
                MatchesFilter = re.IsMatch(Name);
            }
            IsExpanded = MatchesFilter;
            FilterApplied = true;
        }
        public void ResetFilter()
        {
            FilterApplied = false;
        }

        public CrowdMember Clone()
        {
            CharacterCrowdMemberImpl clone = (CharacterCrowdMemberImpl)CrowdRepository.NewCharacterCrowdMember();

            clone.Name = CrowdRepository.CreateUniqueName(this.Name, clone, CrowdRepository.AllMembersCrowd.Members);
            foreach( Identity id  in Identities.Values)
            {
                clone.Identities.Insert((Identity)id.Clone());

            }

            clone.Generator = Generator;
            clone.Targeter = Targeter;
            clone.Camera = Camera;


            return clone;
        }

    }
}