using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.DomainModels
{
    /// <summary>
    /// Base class for character items such as Identities, Abilities and Movements
    /// </summary>
    public class BaseCharacterProperty : NotifyPropertyChanged
    {
        /// <param name="name">The name of the item</param>
        public BaseCharacterProperty(string name)
        {
            Name = name;
        }

        private string name;

        /// <summary>The Name property represents the item's name.</summary>
        /// <value>The Name property gets/sets the value of the string field _name.</value>
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
