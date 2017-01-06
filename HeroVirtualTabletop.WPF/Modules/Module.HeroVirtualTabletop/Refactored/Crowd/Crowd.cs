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
       
        public Crowd AllMembersCrowd { get; set; }

        public HashedObservableCollection<Crowd, string> Crowds { get; set; }

        public void AddCrowd(Crowd crowd) { }
        public Crowd NewCrowd(Crowd parent = null)
        {
            Crowd newCrowd=null;
            AllMembersCrowd.AddCrowdMember(newCrowd);
            parent.AddCrowdMember(newCrowd);
            return newCrowd;
        }
        public CharacterCrowdMember NewCharacterCrowdMember(Crowd parent = null, string name = "Character")
        {
            CharacterCrowdMember newCharacter = null;
            newCharacter.Name = CreateUniqueName(name);
            AllMembersCrowd.AddCrowdMember(newCharacter);
            if (parent != null)
            {
                parent.AddCrowdMember(newCharacter);
            }
           
            return newCharacter;
        }

        public string CreateUniqueName(string name)
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
            while (Crowds.ContainsKey(newName))
            {
                suffix = string.Format(" ({0})", ++i);
                newName = rootName + suffix;
            }
            return newName;
        }

        
    }
    


    public class CrowdMemberComparer : IComparer<CrowdMembership>
    {
        public int Compare(CrowdMembership cmm1, CrowdMembership cmm2)
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
                if (CheckIfNameIsDuplicate(value) == false)
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
        public bool CheckIfNameIsDuplicate(string updatedName)
        {
            return Members.ContainsKey(updatedName);
        }

        public CrowdRepository CrowdRepository { get; set; }
        public CrowdImpl(CrowdRepository repo)
        {
            CrowdRepository = repo;
        }
        public bool UseRelativePositioning { get; set; }

        private int _order;
        public int Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;
                OnPropertyChanged("Order");
            }
        }

        CrowdImpl(string name) {
            Name = name;
            _members = new HashedObservableCollection<CrowdMembership, string>(x => x.Child.Name, x => x.Order, x => x.Child.Name);
        }

        private HashedObservableCollection<CrowdMembership, string> _members;
        public HashedObservableCollection<CrowdMembership, string> Members { get { return _members; } set { Members = value; } }
        public void RemoveMember(CrowdMember member)
        {
            if (_members.ContainsKey(member.Name))
            {
                _members.Remove(member.Name);
                member.PropertyChanged -= Member_PropertyChanged;
            }

        }
        public void AddCrowdMember(CrowdMember member)
        {
            CrowdMembership membership = new CrowdMemberShipImpl(this, member);
            this.Members.Add(membership);
            member.PropertyChanged += Member_PropertyChanged;
        }

        public void SaveCurrentTableTopPosition() {
            foreach (CrowdMembership crowdMembership in Members)
            {
                crowdMembership.Child.SaveCurrentTableTopPosition();
            }
        }
        public void PlaceOnTableTop(Position pos =null)
        {
            foreach (CrowdMembership crowdMembership in Members)
            {
                crowdMembership.Child.PlaceOnTableTop();
            }
        }
        public void PlaceOnTableTopUsingRelativePos()
        {
            foreach (CrowdMembership crowdMember in Members)
            {
                crowdMember.Child.PlaceOnTableTopUsingRelativePos();
            }
        }

        public CrowdMember Clone()
        {
            Crowd clone = CrowdRepository.NewCrowd();
            clone.Name = CrowdRepository.CreateUniqueName(Name);
            EliminateDuplicateName();
            clone.UseRelativePositioning = UseRelativePositioning;
            foreach (CrowdMembership membership in this._members)
            {
                CrowdMember member = membership.Child;
                clone.AddCrowdMember(member);
            }
            return clone;
        }
        private void EliminateDuplicateName()
        {
            if (Members != null)
            {
                List<CrowdMember> models = GetFlattenedMemberList(Members.ToList());
                foreach (var member in models)
                {
                    member.Name = CrowdRepository.CreateUniqueName(member.Name);
                }
            }
        }
        private List<CrowdMember> GetFlattenedMemberList(List<CrowdMembership> list)
        {
            List<CrowdMember> flattened = new List<CrowdMember>();
            foreach (CrowdMembership crowdMembership in list)
            {
                if (crowdMembership.Child is Crowd)
                {
                    Crowd crowd = (crowdMembership as Crowd);
                    if (crowd.Members != null && crowd.Members.Count > 0)

                        flattened.AddRange(GetFlattenedMemberList(crowd.Members.ToList()));
                }
                flattened.Add(crowdMembership.Child);
            }
            return flattened;
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
            foreach (CrowdMembership crowdMemberShip in Members)
            {
                CrowdMember member = crowdMemberShip.Child;
                member.ResetFilter();
            }
        }

        private void Member_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                Members.UpdateKey((sender as CrowdMember).OldName, (sender as CrowdMember).Name);
            }
            if (e.PropertyName == "Name" || e.PropertyName == "Order")
            {
                Members.Sort(ListSortDirection.Ascending, new CrowdMemberComparer());
            }
        }
    }

    public class CrowdMemberShipImpl : NotifyPropertyChanged, CrowdMembership
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
                else if (ParentCrowd.Members != null && ParentCrowd.Members.Where(cm => cm is CrowdMember).Count() > 0)
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
            foreach (CrowdMembership membership in ParentCrowd.CrowdRepository.AllMembersCrowd.Members)
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
            if (crowd.Members != null && crowd.Members.Count > 0)
            {
                foreach (var membership in crowd.Members)
                {
                    if (membership.Child is Crowd)
                    {
                        var membershipChild = membership as Crowd;
                        if (membershipChild.Members != null)
                        {
                            var crm = membershipChild.Members.Where(cmmm => cmmm.Child.Name == nameOfDeletingCrowdMember).FirstOrDefault();
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
            if (Crowd.Members != null)
            {
                var crm = Crowd.Members.Where(cm => cm.Child.Name == nameOfDeletingCrowdMember).FirstOrDefault();
                Crowd.RemoveMember(crm.Child);
            }
        }
        private void DeleteCrowdMemberFromCharacterCollectionByName(string nameOfDeletingCrowdMember)
        {
            var charFromAllCrowd = ParentCrowd.CrowdRepository.AllMembersCrowd.Members.Where(c => c.Child.Name == nameOfDeletingCrowdMember).FirstOrDefault();
            ParentCrowd.CrowdRepository.AllMembersCrowd.Members.Remove(charFromAllCrowd);
        }
        private void DeleteCrowdMemberFromCharacterCollectionByList(List<CrowdMember> crowdMembersToDelete)
        {
            foreach (var crowdMemberToDelete in crowdMembersToDelete)
            {
                var deletingCrowdMember = ParentCrowd.CrowdRepository.AllMembersCrowd.Members.Where(c => c.Child.Name == crowdMemberToDelete.Name).FirstOrDefault();
                ParentCrowd.CrowdRepository.AllMembersCrowd.Members.Remove(deletingCrowdMember);
            }
        }
        private void DeleteCrowdMembersFromAllCrowdsByList(List<CrowdMember> crowdMembersToDelete)
        {
            if (Child is Crowd)
            {
                foreach (Crowd crowd in ((Crowd)Child).Members)
                {
                    DeleteCrowdMembersFromCrowdByList(crowd, crowdMembersToDelete);
                }
                DeleteCrowdMemberFromCharacterCollectionByList(crowdMembersToDelete);
            }
        }
        private void DeleteCrowdMembersFromCrowdByList(Crowd Crowd, List<CrowdMember> crowdMembersToDelete)
        {
            if (Crowd.Members != null)
            {
                foreach (var crowdMemberToDelete in crowdMembersToDelete)
                {
                    var deletingCrowdMemberFromModel = Crowd.Members.Where(cm => cm.Child.Name == crowdMemberToDelete.Name).FirstOrDefault();
                    Crowd.RemoveMember(deletingCrowdMemberFromModel.Child);
                }
            }
        }
        private void DeleteNestedCrowdFromAllCrowdsByName(string nameOfDeletingCrowd)
        {
            if (Child is Crowd)
            {
                foreach (Crowd crowd in ((Crowd)Child).Members)
                {
                    DeleteNestedCrowdFromCrowdByName(crowd, nameOfDeletingCrowd);
                }
            }
        }
        private void DeleteNestedCrowdFromCrowdByName(Crowd Crowd, string nameOfDeletingCrowd)
        {
            if (Crowd.Members != null)
            {
                var CrowdToDelete = Crowd.Members.Where(cm => cm.Child.Name == nameOfDeletingCrowd).FirstOrDefault();
                if (CrowdToDelete != null)
                    Crowd.RemoveMember(CrowdToDelete.Child);
            }
        }
        private void DeleteCrowdFromCrowdCollectionByName(string nameOfDeletingCrowd)
        {
            var crowdToDelete = ((Crowd)Child).Members.Where(cr => cr.Child.Name == nameOfDeletingCrowd).FirstOrDefault();
            ((Crowd)Child).Members.Remove(crowdToDelete);
        }
        private List<CrowdMember> FindCrowdSpecificCrowdMembers(Crowd crowdModel)
        {
            List<CrowdMember> crowdSpecificCharacters = new List<CrowdMember>();
            foreach (CrowdMembership cMember in crowdModel.Members)
            {
                if (cMember.Child is CharacterCrowdMember)
                {
                    CharacterCrowdMember currentCharacter = cMember as CharacterCrowdMember;
                    foreach (Crowd crowd in ParentCrowd.CrowdRepository.AllMembersCrowd.Members.Where(cm => cm.Child.Name != ParentCrowd.Name))
                    {
                        var crm = crowd.Members.Where(cm => cm is CharacterCrowdMember && cm.Child.Name == currentCharacter.Name).FirstOrDefault();
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


        public class CharacterCrowdMemberImpl : ManagedCharacterImpl, CharacterCrowdMember
        {
            public CharacterCrowdMemberImpl(CrowdMembership loadedParent, DesktopCharacterTargeter targeter, KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities, CrowdRepository repo) : base(targeter, generator, camera, identities)
            {
                LoadedParentMembership = loadedParent;
                CrowdRepository = repo;
            }

            CrowdRepository CrowdRepository { get; set; }

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
                    if (CheckIfNameIsDuplicate(value) == false)
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
            public bool CheckIfNameIsDuplicate(string updatedName)
            {
                return CrowdRepository.AllMembersCrowd.Members.ContainsKey(updatedName);
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

            public CrowdMembership LoadedParentMembership { get; set; }
            public void SaveCurrentTableTopPosition()
            {
                LoadedParentMembership.SavedPosition = Position.Duplicate();
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
                    Position.MoveTo(LoadedParentMembership.SavedPosition);
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

                clone.Name = CrowdRepository.CreateUniqueName(this.Name);
                clone.Identities = Identities.Clone();
                clone.Generator = Generator;
                clone.Targeter = Targeter;
                clone.Camera = Camera;


                return clone;
            }

        }
    }

}
