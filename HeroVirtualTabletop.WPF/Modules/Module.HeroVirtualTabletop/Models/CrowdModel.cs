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
