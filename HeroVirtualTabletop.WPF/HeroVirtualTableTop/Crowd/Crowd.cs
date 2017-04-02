using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Framework.WPF.Library;
using HeroVirtualTableTop.AnimatedAbility;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;
using HeroVirtualTableTop.Roster;
using System.Collections.ObjectModel;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using HeroVirtualTableTop.Common;
using Module.Shared;
using Caliburn.Micro;

namespace HeroVirtualTableTop.Crowd
{
    public class CrowdRepositoryImpl : AnimatedCharacterRepositoryImpl, CrowdRepository
    {
        private string crowdRepositoryPath;
        public CrowdRepositoryImpl()
        {
            crowdRepositoryPath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_CROWD_REPOSITORY_FILENAME);
            Crowds = new List<Crowd>();
            createAllmembersCrowd();
        }

        public Crowd NewCrowdInstance { get; set; } //for DI to insert

        public CharacterCrowdMember NewCharacterCrowdMemberInstance { get; set; } //for DI to insert
        public bool UsingDependencyInjection { get; set; }


        public Crowd AllMembersCrowd
        {
            get
            {
                if (Crowds == null)
                    Crowds = new List<Crowd>();
                var crowdsbyname = CrowdsByName;
                if (crowdsbyname.Keys.Contains(CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME) == false)
                    createAllmembersCrowd();
                return CrowdsByName[CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME];
            }
        }

        public Dictionary<string, Crowd> CrowdsByName
        {
            get
            {
                var memDict = new Dictionary<string, Crowd>();
                if (Crowds != null)
                    foreach (var crowd in Crowds)
                    {
                        memDict.Add(crowd.Name, crowd);
                        crowd.Parent = null;
                    }
                return memDict;
            }
        }

        public List<Crowd> Crowds { get; set; }

        public Crowd NewCrowd(Crowd parent = null, string name = "Crowd")
        {
            if (name == "Character") name = "Crowd";
            var newCrowd = NewCrowdInstance;
            if (!UsingDependencyInjection)
            {
                newCrowd = new CrowdImpl(this) {Name = "Crowd"};
            }

            // Crowds.Add(newCrowd);


            if (parent != null)
            {
                newCrowd.Name = CreateUniqueName(name, parent.Members);
                parent.AddCrowdMember(newCrowd);
            }
            else
            {
                newCrowd.Name = CreateUniqueName(name, null);
            }
            return newCrowd;
        }

        public CharacterCrowdMember NewCharacterCrowdMember(Crowd parent = null, string name = "Character")
        {
            var newCharacter = NewCharacterCrowdMemberInstance;

            if (!UsingDependencyInjection)
            {
                newCharacter = new CharacterCrowdMemberImpl(parent, null, null, null, null, this);
            }
            else
            {
                newCharacter.Parent = parent;
                newCharacter.CrowdRepository = this;
            }

            newCharacter.Name = CreateUniqueName(name, AllMembersCrowd.Members);
            AllMembersCrowd.AddCrowdMember(newCharacter);
            parent?.AddCrowdMember(newCharacter);
            return newCharacter;
        }

        public string CreateUniqueName(string name, List<CrowdMember> context)
        {
            var suffix = string.Empty;
            var rootName = name;
            var i = 0;
            var reg = new Regex(@"\((\d+)\)");
            var matches = reg.Matches(name);
            if (matches.Count > 0)
            {
                int k;
                var match = matches[matches.Count - 1];
                if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out k))
                {
                    i = k + 1;
                    suffix = $" ({i})";
                    rootName = name.Substring(0, match.Index).TrimEnd();
                }
            }
            var newName = rootName + suffix;
            if (context != null)
            {
                var allMatches = (from other in context where other.Name == newName select other).ToList();
                while (allMatches.Count > 0)
                {
                    suffix = $" ({++i})";
                    newName = rootName + suffix;
                    allMatches = (from other in context where other.Name == newName select other).ToList();
                }
            }
            return newName;
        }

        private void createAllmembersCrowd()
        {
            Crowd allMembersCrowd = new CrowdImpl(this);
            allMembersCrowd.Name = CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME;
            Crowds.Add(allMembersCrowd);
        }

        public void AddDefaultCharacters()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            List<Crowd> crowdCollection = GetDefaultCrowdCollectionFromResource();
            foreach (Crowd cr in crowdCollection)
                this.Crowds.Add(cr);
        }

        private List<Crowd> GetDefaultCrowdCollectionFromResource()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            List<Crowd> crowdCollection = new List<Crowd>();
            string resName = "HeroVirtualTabletop.Resources.DefaultCharactersWithAbilities.data"; // TODO: Need to add this resource later
            using (StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.Formatting = Formatting.Indented;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;

                    crowdCollection = serializer.Deserialize<List<Crowd>>(reader);
                }
            }
            return crowdCollection;
        }

        private System.Action getCrowdCollectionCompleted;
        public void LoadCrowdsAsync(System.Action getCrowdCollectionCompletedCallback)
        {
            this.getCrowdCollectionCompleted = getCrowdCollectionCompletedCallback;

            ThreadPool.QueueUserWorkItem
                (new WaitCallback(
                    delegate (object state)
                    {
                        this.Crowds = CommonLibrary.GetDeserializedJSONFromFile<List<Crowd>>(crowdRepositoryPath);
                        if (this.Crowds == null)
                            this.Crowds = new List<Crowd>();
                        //TakeBackup(); // Take backup of valid data file from last execution
                        this.getCrowdCollectionCompleted();
                    }));
        }

        private object lockObj = new object();

        private System.Action saveCrowdCollectionCompleted;
        public void SaveCrowdsAsync(System.Action saveCrowdCollectionCompletedCallback)
        {
            this.saveCrowdCollectionCompleted = saveCrowdCollectionCompletedCallback;

            ThreadPool.QueueUserWorkItem
                (new WaitCallback(
                    delegate (object state)
                    {
                        lock (lockObj)
                        {
                            CommonLibrary.SerializeObjectAsJSONToFile(crowdRepositoryPath, this.Crowds);
                            this.saveCrowdCollectionCompleted();
                        }
                    }));
        }
    }
    public enum ClipboardAction
    {
        Clone,
        Cut,
        Link,
        CloneLink
    }

    public class CrowdImpl : NotifyPropertyChanged, Crowd
    {
        public CrowdImpl(CrowdRepository repo)
        {
            CrowdRepository = repo;
            MemberShips = new List<CrowdMemberShip>();
            AllCrowdMembershipParents = new List<CrowdMemberShip>();
        }

        private bool _matchedFilter;
        public bool FilterApplied { get; set; }
        public bool IsExpanded { get; set; }
        public bool MatchesFilter
        {
            get { return _matchedFilter; }
            set
            {
                _matchedFilter = value;
                OnPropertyChanged("IsMatched");
            }
        }
        public void ApplyFilter(string filter)
        {
            if (FilterApplied && MatchesFilter)
                return;
            if (string.IsNullOrEmpty(filter))
            {
                MatchesFilter = true;
            }
            else
            {
                var re = new Regex(filter, RegexOptions.IgnoreCase);
                MatchesFilter = re.IsMatch(Name);
            }
            IsExpanded = MatchesFilter;
            FilterApplied = true;
        }
        public void ResetFilter()
        {
            FilterApplied = false;
            foreach (var crowdMember in Members)
                crowdMember.ResetFilter();
        }

        private string _name;
        public string OldName { get; set; }
        public string Name
        {
            get { return _name; }

            set
            {
                if (Parent != null)
                    if (CheckIfNameIsDuplicate(value, Parent.Members))
                        throw new DuplicateKeyException(value);
                OldName = value;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public bool CheckIfNameIsDuplicate(string updatedName, List<CrowdMember> crowdlist)
        {
            foreach (var member in crowdlist)
                if (member is Crowd)
                {
                    if (member.Name == updatedName) return true;
                    var crowd = member as Crowd;
                    var list = new List<CrowdMember>();
                    var members =
                        (from crowdMember in crowd.Members where crowdMember is Crowd select crowdMember).ToList();
                    foreach (var m in members)
                        list.Add((Crowd) m);

                    if (CheckIfNameIsDuplicate(updatedName, list)) return true;
                }
            return false;
        }

        public CrowdRepository CrowdRepository { get; set; }
        
        public List<CrowdMemberShip> AllCrowdMembershipParents { get; }
        private CrowdMemberShip _loadedParentMembership;
        public Crowd Parent
        {
            get
            {
                return _loadedParentMembership?.ParentCrowd;
            }
            set
            {
                if (value != null)
                {
                    var parents = AllCrowdMembershipParents.ToDictionary(x => x.ParentCrowd.Name);
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
        public int Order
        {
            get
            {
                if (_loadedParentMembership != null)
                    return _loadedParentMembership.Order;
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
            var shipToKill = AllCrowdMembershipParents.FirstOrDefault(y => y.ParentCrowd.Name == parent.Name);
            if (shipToKill != null)
                AllCrowdMembershipParents.Remove(shipToKill);
        }
        public RosterGroup RosterParent { get; set; }
        public List<CrowdMemberShip> MemberShips { get; }
        public List<CrowdMember> Members
        {
            get
            {
                return MemberShips.Select(i =>
                    {
                        i.Child.Parent = this;
                        return i.Child;
                    }
                ).OrderBy(x => x.Order).ToList();
            }
        }
        public Dictionary<string, CrowdMember> MembersByName
        {
            get
            {
                var memDict = new Dictionary<string, CrowdMember>();
                foreach (var ship in MemberShips)
                {
                    memDict.Add(ship.Child.Name, ship.Child);
                    ship.Child.Parent = ship.ParentCrowd;
                }
                return memDict;
            }
        }
        public void AddCrowdMember(CrowdMember member)
        {
            CrowdMemberShip membership = new CrowdMemberShipImpl(this, member);
            MemberShips.Add(membership);
            member.Order = Members.Count;
            member.Parent = this;
            member.PropertyChanged += Member_PropertyChanged;
        }
        public void AddManyCrowdMembers(List<CrowdMember> members)
        {
            foreach (var member in Members)
                AddCrowdMember(member);
        }
        public void RemoveMember(CrowdMember child)
        {
            var removeOrder = child.Order;
            var ship = getMembershipThatAssociatesChildMemberToThis(child);
            removeMembershipFromThisAndChild(child, ship);
            decrementOrderForAllMembersAfterTheDeletedOrder(removeOrder);
            if (child is Crowd)
            {
                var crowdBeingDeleted = child as Crowd;
                deleteAllChildrenIfThisDoesntHaveAnyOtherParentsAndChildrenDoNotHaveOtherParents(crowdBeingDeleted);
            }
            child.PropertyChanged -= Member_PropertyChanged;
        }
        public void MoveCrowdMemberAfter(CrowdMember destination, CrowdMember memberToMove)
        {
            if (destination.Parent == memberToMove.Parent)
            {
                var orginalOrder = memberToMove.Order;
                foreach (
                    var member in
                    from member in Members orderby member.Order where member.Order > orginalOrder select member)
                    member.Order = member.Order - 1;
                var destinationOrder = destination.Order + 1;
                foreach (
                    var member in
                    from member in Members
                    orderby member.Order descending
                    where member.Order >= destinationOrder
                    select member)
                    member.Order = member.Order + 1;
                memberToMove.Order = destinationOrder;
            }
            else
            {
                memberToMove.Parent.RemoveMember(memberToMove);
                AddCrowdMember(memberToMove);
                MoveCrowdMemberAfter(destination, memberToMove);
            }
        }
        private void decrementOrderForAllMembersAfterTheDeletedOrder(int removeOrder)
        {
            foreach (var m in from m in Members orderby m.Order where m.Order > removeOrder select m)
                m.Order = m.Order - 1;
        }
        private void removeMembershipFromThisAndChild(CrowdMember member, CrowdMemberShip ship)
        {
            if (ship != null)
            {
                MemberShips.Remove(ship);
                member.RemoveParent(this);
            }
        }
        private static void deleteAllChildrenIfThisDoesntHaveAnyOtherParentsAndChildrenDoNotHaveOtherParents(
            Crowd crowdBeingDeleted)
        {
            if (crowdBeingDeleted.AllCrowdMembershipParents.Count == 0)
                foreach (var childOfCrowdBeingDeleted in crowdBeingDeleted.Members)
                    if (childOfCrowdBeingDeleted.AllCrowdMembershipParents.Count <= 2)
                        //parent from this crowd and allcrowds is all thats left
                        crowdBeingDeleted.RemoveMember(childOfCrowdBeingDeleted);
        }
        private CrowdMemberShip getMembershipThatAssociatesChildMemberToThis(CrowdMember member)
        {
            return
                (from membership in MemberShips where member.Name == membership.Child.Name select membership)
                .FirstOrDefault();
        }

        public bool UseRelativePositioning { get; set; }
        public void SaveCurrentTableTopPosition()
        {
            foreach (var crowdMembers in Members)
                crowdMembers.SaveCurrentTableTopPosition();
        }
        public void PlaceOnTableTop(Position pos = null)
        {
            foreach (var crowdMember in Members)
                crowdMember.PlaceOnTableTop();
        }
        public void PlaceOnTableTopUsingRelativePos()
        {
            foreach (var crowdMember in Members)
                crowdMember.PlaceOnTableTopUsingRelativePos();
        }

        public void SpawnToDesktop(bool completeEvent = true)
        {
            foreach (var crowdMember in Members)
                crowdMember.SpawnToDesktop(completeEvent);
        }
        public void ClearFromDesktop(bool completeEvent = true)
        {
            foreach (var crowdMember in Members)
                crowdMember.ClearFromDesktop(completeEvent);
        }
        public void MoveCharacterToCamera(bool completeEvent = true)
        {
            foreach (var crowdMember in Members)
                crowdMember.MoveCharacterToCamera(completeEvent);
        }
        public Dictionary<string, Identity> IdentitiesList
        {
            get
            {
                var i = new Dictionary<string, Identity>();
                foreach (var member in Members)
                {
                    var idList = member.IdentitiesList;
                    foreach (var id in idList)
                    {
                        i[id.Key] = id.Value;
                    }
                }
                return i;
            }
        }
        public Identity DefaultIdentity {
            get
            {
                List<CharacterAction> iList = new List<CharacterAction>();       
                Members.ForEach(x=> iList.Add(x.DefaultIdentity));
                return new RosterSelectionIdentityWrapper(null,iList);

            }
        }

        public void Activate()
        {
            foreach (var crowdMember in Members)
                crowdMember.Activate();
        }
        public void DeActivate()
        {
            foreach (var crowdMember in Members)
                crowdMember.DeActivate();
        }
        public Dictionary<string, AnimatedAbility.AnimatedAbility> AbilitiesList
        {
            get
            {
                var i = new Dictionary<string, AnimatedAbility.AnimatedAbility>();
                foreach (var member in Members)
                {
                    var idList = member.AbilitiesList;
                    foreach (var id in idList)
                    {
                        i[id.Key] = id.Value;
                    }
                }
                return i;
            }
        }

        public AnimatedAbility.AnimatedAbility DefaultAbility {
            get
            {
                List<CharacterAction> iList = new List<CharacterAction>();
                Members.ForEach(x => iList.Add(x.DefaultAbility));
                return new RosterSelectionAbilityWrapper(null, iList);
            }
        }
        public List<AnimatableCharacterState> ActiveStates {
            get
            {
                var commonStates = new List<AnimatableCharacterState>();
                var firstMember = Members.FirstOrDefault();
                foreach (var state in firstMember.ActiveStates)
                {
                    var found = Members.Where(x=>x.ActiveStates.Where(y=>y.StateName==state.StateName).Count()>0);
                    if (found.Count() == Members.Count())
                        commonStates.Add(state);
                }
              return commonStates;  
            }
        }
        public void RemoveStateByName(string stateName)
        {
            Members.ForEach(x => x.RemoveStateByName(stateName));

        }


        public CrowdMember Clone()
        {
            var clone = CrowdRepository.NewCrowd();

            var crowds = (from crowd in CrowdRepository.Crowds select crowd as CrowdMember).ToList();
            clone.Name = CrowdRepository.CreateUniqueName(Name, crowds);

            clone.UseRelativePositioning = UseRelativePositioning;
            foreach (var member in Members)
            {
                var cloneMember = member.Clone();
                clone.AddCrowdMember(cloneMember);
            }
            //EliminateDuplicateName();
            return clone;
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
        private List<CrowdMember> GetFlattenedMemberList(List<CrowdMember> list)
        {
            var flattened = new List<CrowdMember>();
            foreach (var crowdMember in list)
            {
                var crowd = crowdMember as Crowd;
                if (crowd?.Members != null && crowd.Members.Count > 0)

                    flattened.AddRange(GetFlattenedMemberList(crowd.Members));
                flattened.Add(crowdMember);
            }
            return flattened;
        }

        public void Rename(string newName)
        {
            throw new NotImplementedException();
        }
    }

    public class CrowdMemberShipImpl : NotifyPropertyChanged, CrowdMemberShip
    {
        private Position _savedPosition;

        public CrowdMemberShipImpl(Crowd parent, CrowdMember child)
        {
            ParentCrowd = parent;
            Child = child;
        }

        public int Order { get; set; }
        public Crowd ParentCrowd { get; set; }
        public CrowdMember Child { get; set; }

        public Position SavedPosition
        {
            get
            {
                return _savedPosition;
                
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

    public class CharacterCrowdMemberImpl : AnimatedCharacterImpl, CharacterCrowdMember
    {
        private CrowdMemberShip _loadedParentMembership;


        private bool _matchedFilter;
        private string _name;

        private bool isExpanded;

        public CharacterCrowdMemberImpl(Crowd parent, DesktopCharacterTargeter targeter,
            KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities,
            CrowdRepository repo) : base(targeter, generator, camera, identities, repo)
        {
            AllCrowdMembershipParents = new List<CrowdMemberShip>();
            Parent = parent;
            CrowdRepository = repo;
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        public bool FilterApplied { get; set; }

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
                var parents = AllCrowdMembershipParents.ToDictionary(x => x.ParentCrowd.Name);
                if (!parents.ContainsKey(value.Name))
                {
                    CrowdMemberShip parent = new CrowdMemberShipImpl(value, this);
                    AllCrowdMembershipParents.Add(parent);
                    _loadedParentMembership = parent;
                }
                else
                {
                    _loadedParentMembership = parents[value.Name];
                }
                _loadedParentMembership.Child = this;
            }
        }

        public void RemoveParent(CrowdMember parent)
        {
            var shipToKill = AllCrowdMembershipParents.First(y => y.ParentCrowd.Name == parent.Name);
            AllCrowdMembershipParents.Remove(shipToKill);
            if (AllCrowdMembershipParents.Count == 1) //remove from Allmembers crowd
                CrowdRepository.AllMembersCrowd.RemoveMember(this);
        }

        public int Order
        {
            get
            {
                if (_loadedParentMembership != null)
                    return _loadedParentMembership.Order;
                return 0;
            }
            set
            {
                if (_loadedParentMembership != null)
                    _loadedParentMembership.Order = value;
                OnPropertyChanged("Order");
            }
        }

        public CrowdRepository CrowdRepository { get; set; }

        public string OldName { get; set; }

        public new string Name
        {
            get { return _name; }

            set
            {
                if (CheckIfNameIsDuplicate(value, null))
                    throw new DuplicateKeyException(value);
                OldName = _name;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public bool CheckIfNameIsDuplicate(string updatedName, List<CrowdMember> members)
        {
            if (CrowdRepository.AllMembersCrowd != null)
                return CrowdRepository.AllMembersCrowd.MembersByName.ContainsKey(updatedName);

            return false;
        }


        public void SaveCurrentTableTopPosition()
        {
            _loadedParentMembership.SavedPosition = Position.Duplicate();
        }

        public void PlaceOnTableTop(Position position = null)
        {
            if (!IsSpawned)
                SpawnToDesktop();
            Position.MoveTo(position ?? _loadedParentMembership.SavedPosition);
        }

        public void PlaceOnTableTopUsingRelativePos()
        {
        }

        public override string DesktopLabel => MemoryInstance?.Label;

        public bool MatchesFilter
        {
            get { return _matchedFilter; }
            set
            {
                _matchedFilter = value;
                OnPropertyChanged("IsMatched");
            }
        }

        public void ApplyFilter(string filter)
        {
            if (FilterApplied && MatchesFilter)
                return;
            if (string.IsNullOrEmpty(filter))
            {
                MatchesFilter = true;
            }
            else
            {
                var re = new Regex(filter, RegexOptions.IgnoreCase);
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
            var clone = (CharacterCrowdMemberImpl) CrowdRepository.NewCharacterCrowdMember();

            clone.Name = CrowdRepository.CreateUniqueName(Name, CrowdRepository.AllMembersCrowd.Members);
            foreach (var id  in Identities.Values)
                clone.Identities.InsertElement((Identity) id.Clone());

            clone.Generator = Generator;
            clone.Targeter = Targeter;
            clone.Camera = Camera;


            return clone;
        }

        public void Rename(string newName)
        {
            this.OldName = this.Name;
            this.Name = newName;
        }

        public RosterGroup RosterParent { get; set; }
    }

    public class CrowdClipboardImpl : CrowdClipboard
    {
        private object clipboardObject;
        public ClipboardAction CurrentClipboardAction
        {
            get; set;
        }

        public void CopyToClipboard(CrowdMember member)
        {
            CurrentClipboardAction = ClipboardAction.Clone;
            clipboardObject = member;
        }

        public void CutToClipboard(CrowdMember member, Crowd parent)
        {
            this.CurrentClipboardAction = ClipboardAction.Cut;
            this.clipboardObject = new object[] { member, parent };
        }

        public void LinkToClipboard(CrowdMember member)
        {
            CurrentClipboardAction = ClipboardAction.Link;
            clipboardObject = member;
        }

        public void PasteFromClipboard(CrowdMember destinationMember)
        {
            switch (this.CurrentClipboardAction)
            {
                case ClipboardAction.Clone:
                    cloneAndPaste(destinationMember);
                    break;
                case ClipboardAction.Cut:
                    cutAndPaste(destinationMember);
                    break;
                case ClipboardAction.Link:
                    linkAndPaste(destinationMember);
                    break;
            }
        }

        private void cloneAndPaste(CrowdMember member)
        {
            var destCrowd = getDestinationCrowdForPaste(member);
            var cloningMember = clipboardObject as CrowdMember;
            var clonedMember = cloningMember?.Clone();
            destCrowd.AddCrowdMember(clonedMember);
        }

        private void cutAndPaste(CrowdMember member)
        {
            var destCrowd = getDestinationCrowdForPaste(member);
            var objs = this.clipboardObject as object[];
            var cuttingMember = objs[0] as CrowdMember;
            var cuttingMemberParent = objs[1] as Crowd;
            var parent = cuttingMemberParent ?? cuttingMember.Parent;
            if (parent != null && destCrowd != parent)
            {
                parent.RemoveMember(cuttingMember);
                destCrowd.AddCrowdMember(cuttingMember);
            }
        }

        private void linkAndPaste(CrowdMember member)
        {
            var destCrowd = getDestinationCrowdForPaste(member);
            var linkingMember = clipboardObject as CrowdMember;
            destCrowd.AddCrowdMember(linkingMember);
        }

        private Crowd getDestinationCrowdForPaste(CrowdMember member)
        {
            if (member is CharacterCrowdMember)
                return (member as CharacterCrowdMember).Parent;
            else return member as Crowd;
        }
    }

    public class CrowdMemberExplorerViewModelImpl : CrowdMemberExplorerViewModel
    {
        public IEventAggregator EventAggregator
        {
            get; set;
        }
        public CrowdClipboard CrowdClipboard
        {
            get;set;
        }
        private CrowdRepository crowdRepository;
        public CrowdRepository CrowdRepository
        {
            get
            {
                return crowdRepository;
            }
            set
            {
                crowdRepository = value;
            }
        }
        private CrowdMember selectedCrowdMember;
        public CrowdMember SelectedCrowdMember
        {
            get
            {
                return selectedCrowdMember;
            }

            set
            {
                selectedCrowdMember = value;
            }
        }

        public Crowd SelectedCrowdMemberParent
        {
            get
            {
                if (SelectedCrowdMember is Crowd)
                    return SelectedCrowdMember as Crowd;
                else return SelectedCrowdMember.Parent;
            }
        }

        private BindableCollection<CrowdMember> crowdMembers;
        public BindableCollection<CrowdMember> CrowdMembers
        {
            get
            {
                return crowdMembers;
            }
            set
            {
                //crowdMembers = new ObservableCollection<CrowdMember>(this.CrowdRepository.Crowds);
                crowdMembers = value;
            }
        }

        public CrowdMemberExplorerViewModelImpl(CrowdRepository repository, CrowdClipboard clipboard, IEventAggregator eventAggregator)
        {
            this.CrowdRepository = repository;
            this.CrowdClipboard = clipboard;
            this.EventAggregator = eventAggregator;
        }

        public void AddCharacterCrowd()
        {
            var charCrowd = this.CrowdRepository.NewCharacterCrowdMember();
            this.SelectedCrowdMemberParent.AddCrowdMember(charCrowd);
            this.CrowdRepository.SaveCrowdsAsync(null);
        }

        public void AddCrowd()
        {
            var charCrowd = this.CrowdRepository.NewCharacterCrowdMember();
            this.SelectedCrowdMemberParent.AddCrowdMember(charCrowd);
            this.CrowdRepository.SaveCrowdsAsync(null);
        }

        public void CreateCrowdFromModels()
        {
            throw new NotImplementedException();
        }

        public void AddCrowdMemberToRoster(CrowdMember member)
        {
            throw new NotImplementedException();
        }

        public void ApplyFilter(string filter)
        {
            foreach (var crowd in this.CrowdRepository.Crowds)
            {
                foreach (var mem in crowd.Members)
                {
                    mem.ApplyFilter(filter);
                }
            }
        }

        public void CloneCrowdMember(CrowdMember member)
        {
            this.CrowdClipboard.CopyToClipboard(this.SelectedCrowdMember);
        }

        public void CutCrowdMember(CrowdMember member)
        {
            this.CrowdClipboard.CutToClipboard(this.SelectedCrowdMember);
        }

        public void DeleteCrowdMember()
        {
            this.SelectedCrowdMemberParent.RemoveMember(this.SelectedCrowdMember);
            this.CrowdRepository.SaveCrowdsAsync(null);
        }

        public void LinkCrowdMember(CrowdMember member)
        {
            this.CrowdClipboard.LinkToClipboard(this.SelectedCrowdMember);
        }

        public void MoveCrowdMember(CrowdMember movingCrowdMember, CrowdMember targetCrowdMember, Crowd destinationCrowd)
        {
            destinationCrowd.MoveCrowdMemberAfter(targetCrowdMember, movingCrowdMember);
        }

        public void PasteCrowdMember(CrowdMember member)
        {
            this.CrowdClipboard.PasteFromClipboard(this.SelectedCrowdMember);
        }

        public void RenameCrowdMember(CrowdMember member, string newName)
        {
            IEnumerable<CrowdMember> allMembers = this.CrowdRepository.Crowds;
            var isDuplicate = member.CheckIfNameIsDuplicate(newName, allMembers.ToList());
            if(!isDuplicate)
            {
                member.Rename(newName);
            }
        }

        public void SortCrowds()
        {
            throw new NotImplementedException();
        }
    }
}