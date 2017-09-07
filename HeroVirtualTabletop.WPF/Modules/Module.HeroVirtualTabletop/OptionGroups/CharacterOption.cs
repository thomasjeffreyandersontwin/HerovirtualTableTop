using Framework.WPF.Library;
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
        string OptionTooltip { get; set; }
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

        private string optionTooltip;
        public virtual string OptionTooltip
        {
            get
            {
                if (string.IsNullOrEmpty(optionTooltip))
                    optionTooltip = this.Name;
                return optionTooltip;
            }

            set
            {
                optionTooltip = value;
                OnPropertyChanged("OptionTooltip");
            }
        }
    }
}
