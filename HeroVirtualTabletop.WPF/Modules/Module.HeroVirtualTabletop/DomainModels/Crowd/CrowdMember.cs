using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.DomainModels
{
    public class CrowdMember : Character, ICrowdMember
    {
        public string Name
        {
            get
            {
                return base.Name; // Name should be the Character's name
            }
            set
            {
                base.Name = value;
                OnPropertyChanged("Name");
            }
        }

        private ICrowdMember parent;
        public ICrowdMember Parent
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
    }
}
