using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Library
{
    public class SortableObservableCollection<T, TKey> : ObservableCollection<T>
    {
        private Func<T, TKey> keySelector;
        private bool keepSorted;
        private ListSortDirection sortOrder;

        public SortableObservableCollection(Func<T, TKey> keySelector, bool keepSorted = true, ListSortDirection sortOrder = ListSortDirection.Ascending)
           : base()
        {
            this.keySelector = keySelector;
            this.keepSorted = keepSorted;
            this.sortOrder = sortOrder;
        }

        public SortableObservableCollection(List<T> list, Func<T, TKey> keySelector, bool keepSorted = true, ListSortDirection sortOrder = ListSortDirection.Ascending)
           : base(list)
        {
            this.keySelector = keySelector;
            this.keepSorted = keepSorted;
            this.sortOrder = sortOrder;
        }

        public SortableObservableCollection(IEnumerable<T> collection, Func<T, TKey> keySelector, bool keepSorted = true, ListSortDirection sortOrder = ListSortDirection.Ascending)
           : base(collection)
        {
            this.keySelector = keySelector;
            this.keepSorted = keepSorted;
            this.sortOrder = sortOrder;
        }

        public void Sort(Func<T, TKey> keySelector = null)
        {
            if (keySelector == null)
            {
                keySelector = this.keySelector;
            }
            switch (sortOrder)
            {
                case ListSortDirection.Ascending:
                    {
                        ApplySort(Items.OrderBy(keySelector));
                        break;
                    }
                case ListSortDirection.Descending:
                    {
                        ApplySort(Items.OrderByDescending(keySelector));
                        break;
                    }
            }
        }

        public void Sort(IComparer<TKey> comparer, Func<T, TKey> keySelector = null)
        {
            if (keySelector == null)
                keySelector = this.keySelector;
            ApplySort(Items.OrderBy(keySelector, comparer));
        }

        private void ApplySort(IEnumerable<T> sortedItems)
        {
            var sortedItemsList = sortedItems.ToList();

            foreach (var item in sortedItemsList)
            {
                Move(IndexOf(item), sortedItemsList.IndexOf(item));
            }
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            if (keepSorted)
                Sort();
            if (item is INotifyPropertyChanged)
                (item as INotifyPropertyChanged).PropertyChanged += Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name") //Absolutely not the right way but works for now
                if (sender.GetType().GetProperty(e.PropertyName).GetValue(sender) == (object)keySelector((T)sender))
                    Sort();
        }
    }
}
