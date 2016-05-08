using Framework.WPF.Library;
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
    public class Crowd : NotifyPropertyChanged, ICrowdMember
    {
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

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

        public Crowd()
        {
            this.CrowdMemberCollection = new ObservableCollection<ICrowdMember>();
        }

        public Crowd(string name) : this()
        {
            this.Name = name;
        }
    }

    public class CrowdModel : Crowd, ICrowdMemberModel
    {
        private SortableObservableCollection<ICrowdMemberModel, string> crowdMemberCollection;
        public new SortableObservableCollection<ICrowdMemberModel, string> CrowdMemberCollection
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
            if (IsMatch)
            {
                foreach (ICrowdMemberModel cm in CrowdMemberCollection)
                {
                    cm.ApplyFilter(string.Empty);
                }
            }
            else
            {
                foreach (ICrowdMemberModel cm in CrowdMemberCollection)
                {
                    cm.ApplyFilter(filter);
                }
                if (CrowdMemberCollection.Any(cm => { return (cm as ICrowdMemberModel).IsMatch; }))
                {
                    IsMatch = true;
                }
            }
            
            IsExpanded = IsMatch;
            alreadyFiltered = true;
        }

        private bool alreadyFiltered = false;
        public void ResetFilter()
        {
            alreadyFiltered = false;
            foreach (ICrowdMemberModel cm in CrowdMemberCollection)
            {
                cm.ResetFilter();
            }
        }

        public CrowdModel() : base()
        {
            this.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name);
        }
        public CrowdModel(string name) : base(name)
        {
            this.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name);
            //this.Name = name; Handled by base class
        }
    }
}
