using Framework.WPF.Library;
using Module.HeroVirtualTabletop.DomainModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Models
{
    public class CrowdModel : Crowd
    {
        private ObservableCollection<BaseCrowdMember> childCrowdCollection;
        public ObservableCollection<BaseCrowdMember> ChildCrowdCollection
        {
            get
            {
                return childCrowdCollection;
            }
            set
            {
                childCrowdCollection = value;
                OnPropertyChanged("ChildCrowdCollection");
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


        public CrowdModel()
        {
            this.ChildCrowdCollection = new ObservableCollection<BaseCrowdMember>();
        }
        public CrowdModel(string name)
        {
            this.Name = name;
        }
    }
}
