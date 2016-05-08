using Module.HeroVirtualTabletop.Characters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Crowds
{
    public interface ICrowdMember
    {
        string Name { get; set; }
        Crowd Parent { get; set; }
        ObservableCollection<ICrowdMember> CrowdMemberCollection { get; set; }

        // Following methods would be added as necessary. These can be virtual or abstract. Will decide later.
        //void Place(Position position);
        //void SavePosition();
        //string Save(string filename = null);
        //ICrowdMember Clone();
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
        private Crowd parent;
        public Crowd Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
                OnPropertyChanged("Parent");
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

        public CrowdMember():this("")
        { 
            
        }
        public CrowdMember(string name): base(name)
        {
            //this.Name = name; //ALREADY HANDLED BY BASE CLASS
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

        public CrowdMemberModel() : base() { }
        public CrowdMemberModel(string name): base(name) { }
    }
}
