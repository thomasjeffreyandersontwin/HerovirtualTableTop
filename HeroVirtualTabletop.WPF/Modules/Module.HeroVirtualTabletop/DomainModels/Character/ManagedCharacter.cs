using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.DomainModels
{
    public class ManagedCharacter : Character
    {
        private Identity _activeIdentity;

        public Identity ActiveIdentity
        {
            get
            {
                return _activeIdentity;
            }

            set
            {
                _activeIdentity = value;
                OnPropertyChanged("ActiveIdentity");
            }
        }
    }
}
