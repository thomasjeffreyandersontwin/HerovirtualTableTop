using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Module.HeroVirtualTabletop.Library.Utility;
using Framework.WPF.Library;

namespace Module.HeroVirtualTabletop.OptionGroups
{

    public class OptionGroup<T> : HashedObservableCollection<T, string> where T : ICharacterOption
    {
        public OptionGroup(): base(opt => { return opt.Name; })
        {

        }

        public string NewValidOptionName(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Option";
            }
            string suffix = string.Empty;
            int i = 0;
            while ((this.Cast<ICharacterOption>().Any((ICharacterOption opt) => { return opt.Name == name + suffix; })))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return string.Format("{0}{1}", name, suffix).Trim();
        }
    }
}
