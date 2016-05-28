using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.OptionGroups
{
    public interface ICharacterOption: INotifyPropertyChanged
    {
        string Name { get; set; }
    }
}
