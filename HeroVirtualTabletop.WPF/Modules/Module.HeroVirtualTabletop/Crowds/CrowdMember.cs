using Module.HeroVirtualTabletop.Characters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Framework.WPF.Extensions;
using Module.Shared.Enumerations;
using Module.Shared;

namespace Module.HeroVirtualTabletop.Crowds
{
    public interface ICrowdMember
    {
        string Name { get; set; }
        Crowd RosterCrowd { get; set; }
        ObservableCollection<ICrowdMember> CrowdMemberCollection { get; set; }

        //void Place(Position position);
        void SavePosition();
        //string Save(string filename = null);
        ICrowdMember Clone();
    }

    public interface ICrowdMemberModel : ICrowdMember
    {
        bool IsExpanded { get; set; }
        bool IsMatch { get; set; }
        void ApplyFilter(string filter);
        void ResetFilter();
    }

    public class CrowdMember : Character, ICrowdMember
    {
        private Crowd rosterCrowd;
        [JsonIgnore]
        public Crowd RosterCrowd
        {
            get
            {
                return rosterCrowd;
            }

            set
            {
                rosterCrowd = value;
                OnPropertyChanged("RosterCrowd");
            }
        }

        private ObservableCollection<ICrowdMember> crowdMemberCollection;
        [JsonIgnore]
        public ObservableCollection<ICrowdMember> CrowdMemberCollection
        {
            get
            {
                return crowdMemberCollection;
            }
            set
            {
                crowdMemberCollection = value;
                OnPropertyChanged("CrowdMemberCollection");
            }
        }
        [JsonConstructor]
        public CrowdMember(): base()
        { 
            
        }
        public CrowdMember(string name): base(name)
        {
            
        }

        public virtual ICrowdMember Clone()
        {
            CrowdMember crowdMember = this.DeepClone() as CrowdMember;
            return crowdMember;
        }

        public virtual void SavePosition()
        { 
            
        }

        protected override string GetLabel()
        {
            if (gamePlayer != null && gamePlayer.IsReal)
            {
                return gamePlayer.Label;
            }
            else
            {
                string crowdLabel = string.Empty;
                if (RosterCrowd != null && RosterCrowd.Name != Constants.ALL_CHARACTER_CROWD_NAME && RosterCrowd.Name != Constants.NO_CROWD_CROWD_NAME)
                {
                    crowdLabel = " [" + RosterCrowd.Name + "]";
                }
                return Name + crowdLabel;
            }
        }
    }
    public class CrowdMemberModel : CrowdMember, ICrowdMemberModel
    {
        private bool isExpanded;
        [JsonIgnore]
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

        private bool isMatch = true;
        [JsonIgnore]
        public bool IsMatch
        {
            get
            {
                return isMatch;
            }
            set
            {
                isMatch = value;
                OnPropertyChanged("IsMatch");
            }
        }

        public void ApplyFilter(string filter)
        {
            if (alreadyFiltered == true && isMatch == true)
            {
                return;
            }
            if (string.IsNullOrEmpty(filter))
            {
                IsMatch = true;
            }
            else
            {
                Regex re = new Regex(filter, RegexOptions.IgnoreCase);
                IsMatch = re.IsMatch(Name);
            }
            IsExpanded = IsMatch;
            alreadyFiltered = true;
        }

        private bool alreadyFiltered = false;
        public void ResetFilter()
        {
            alreadyFiltered = false;
        }

        public override ICrowdMember Clone()
        {
            CrowdMemberModel crowdMemberModel = this.DeepClone() as CrowdMemberModel;
            return crowdMemberModel;
        }
        [JsonConstructor]
        public CrowdMemberModel() : base() { }
        public CrowdMemberModel(string name): base(name) { }
    }
}
