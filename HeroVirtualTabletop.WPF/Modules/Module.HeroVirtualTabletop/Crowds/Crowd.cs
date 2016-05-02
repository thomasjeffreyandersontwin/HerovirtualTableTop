using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        public Crowd(string name) : base()
        {
            this.Name = name;
        }
        public Crowd()
        { 
        
        }
    }

    public class CrowdModel : Crowd
    {
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


        public CrowdModel()
        {
            this.CrowdMemberCollection = new ObservableCollection<ICrowdMember>();
        }
        public CrowdModel(string name)
        {
            this.Name = name;
        }
    }
}
