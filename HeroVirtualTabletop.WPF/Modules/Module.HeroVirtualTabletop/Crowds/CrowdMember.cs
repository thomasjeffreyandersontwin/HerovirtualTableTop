using Module.HeroVirtualTabletop.Characters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        public CrowdMember(string name)
        {
            this.Name = name;
        }
    }
}
