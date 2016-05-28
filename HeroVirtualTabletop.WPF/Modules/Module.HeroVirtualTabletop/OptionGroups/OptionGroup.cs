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
using Module.Shared.Messages;

namespace Module.HeroVirtualTabletop.OptionGroups
{

    public class OptionGroup<T> : HashedObservableCollection<T, string> where T : ICharacterOption
    {
        public OptionGroup(): base(opt => { return opt.Name; })
        {

        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            item.PropertyChanged += Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                string key = this.GetKeyFromItem((T)sender);
                if (!this.UpdateKey(key, ((T)sender).Name))
                {
                    System.Windows.MessageBox.Show(Messages.DUPLICATE_NAME_MESSAGE, "Rename", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    ((T)sender).Name = key;
                }               
            }
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
