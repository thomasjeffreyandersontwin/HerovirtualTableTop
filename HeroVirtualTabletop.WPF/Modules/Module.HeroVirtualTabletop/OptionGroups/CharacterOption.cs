using Framework.WPF.Library;
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
