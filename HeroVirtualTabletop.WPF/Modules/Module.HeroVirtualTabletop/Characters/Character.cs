using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Identities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Characters
{
    public class Character : NotifyPropertyChanged
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

        private Identity activeIdentity;
        public Identity ActiveIdentity
        {
            get
            {
                return activeIdentity;
            }

            set
            {
                activeIdentity = value;
                OnPropertyChanged("ActiveIdentity");
            }
        }
    }
}
