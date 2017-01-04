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
                OldName = _name;
                _name = value;
                OnPropertyChanged("Name");
            }
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
            _members = new HashedObservableCollection<CrowdMemberShip, string>(x => x.Child.Name, x => x.Order, x => x.Child.Name);
        }

        private HashedObservableCollection<CrowdMemberShip, string> _members;
        public HashedObservableCollection<CrowdMemberShip, string> Members { get { return _members; } set { Members = value; } }
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
            CrowdMemberShip membership = new CrowdMemberShipImpl(this, member);
            this.Members.Add(membership);
            member.PropertyChanged += Member_PropertyChanged;
        }

        public void SaveCurrentTableTopPosition() {
            foreach (CrowdMemberShip crowdMembership in Members)
            {
                crowdMembership.Child.SaveCurrentTableTopPosition();
            }
        }
        public void PlaceOnTableTop(Position pos =null)
        {
            foreach (CrowdMemberShip crowdMembership in Members)
            {
                crowdMembership.Child.PlaceOnTableTop();
            }
        }
        public void PlaceOnTableTopUsingRelativePos()
        {
            foreach (CrowdMemberShip crowdMember in Members)
            {
                crowdMember.Child.PlaceOnTableTopUsingRelativePos();
            }
        }
        public CrowdMember Clone()
        {
            Crowd clone = new CrowdImpl(Name);
            clone.UseRelativePositioning = UseRelativePositioning;
            foreach (CrowdMemberShip membership in this._members)
            {
                CrowdMember member = membership.Child;
                clone.AddCrowdMember(member);
            }
            return clone;
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
            foreach (CrowdMemberShip crowdMemberShip in Members)
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

    public class CrowdMemberShipImpl: NotifyPropertyChanged, CrowdMemberShip
    {
        public CrowdMemberShipImpl(Crowd parent, CrowdMember child)
        {
            ParentCrowd = parent;
            Child = child;
        }
        public int Order { get; set; }
        public Crowd ParentCrowd { get; }
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
    }


    public class CharacterCrowdMemberImpl : ManagedCharacterImpl, CharacterCrowdMember
    {
        CharacterCrowdMemberImpl(CrowdMemberShip loadedParent, DesktopCharacterTargeter targeter, KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities) : base(targeter, generator, camera, identities)
        {
            LoadedParentMembership = loadedParent;
        }
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
                OldName = _name;
                _name = value;
                OnPropertyChanged("Name");
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

        public CrowdMemberShip LoadedParentMembership { get; set; }
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
            CrowdMember clone = new CharacterCrowdMemberImpl(null, Targeter, Generator, Camera, Identities.Clone());
            clone.Name = this.Name;

            return clone;
        }
    }

        
}
