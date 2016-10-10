<<<<<<< HEAD
﻿using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.OptionGroups
{
    public interface ICharacterOption
    {
        string Name { get; set; }
    }

    public abstract class CharacterOption : NotifyPropertyChanged, ICharacterOption
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
    }
}
=======
﻿using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.OptionGroups
{
    public interface ICharacterOption
    {
        string Name { get; set; }
    }

    public abstract class CharacterOption : NotifyPropertyChanged, ICharacterOption
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
    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
