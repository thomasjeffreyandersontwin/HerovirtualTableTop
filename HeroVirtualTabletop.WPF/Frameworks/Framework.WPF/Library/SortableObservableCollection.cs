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
        private Func<T, IComparable>[] keySelectors;

        public SortableObservableCollection(params Func<T, IComparable>[] keySelectors )
           : base()
        {
            this.keySelectors = keySelectors;
        }

        public SortableObservableCollection(List<T> list, params Func<T, IComparable>[] keySelectors)
           : base(list)
        {
            this.keySelectors = keySelectors;
        }

        public SortableObservableCollection(IEnumerable<T> collection, params Func<T, IComparable>[] keySelectors)
           : base(collection)
        {
            this.keySelectors = keySelectors;
        }

        public void Sort(ListSortDirection sortOrder = ListSortDirection.Ascending, params Func<T, IComparable>[] keySelectors)
        {
            if (keySelectors .Count() == 0)
            {
                keySelectors = this.keySelectors;
            }
            switch (sortOrder)
            {
                case ListSortDirection.Ascending:
                    {
                        for (int i = keySelectors.Count()-1; i >= 0; i--)
                            ApplySort(Items.OrderBy(keySelectors[i]));
                        break;
                    }
                case ListSortDirection.Descending:
                    {
                        for (int i = keySelectors.Count() - 1; i >= 0; i--)
                            ApplySort(Items.OrderByDescending(keySelectors[i]));
                        break;
                    }
            }
        }
        
        private void ApplySort(IEnumerable<T> sortedItems)
        {
            var sortedItemsList = sortedItems.ToList();

            foreach (var item in sortedItemsList)
            {
                Move(IndexOf(item), sortedItemsList.IndexOf(item));
            }
        }

    }
}
