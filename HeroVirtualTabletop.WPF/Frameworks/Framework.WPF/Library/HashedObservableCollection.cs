using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Library
{
    /// <summary>
    /// Represents BindableCollection indexed by a dictionary to improve lookup/replace performance.
    /// </summary>
    /// <remarks>
    /// Assumes that the key will not change and is unique for each element in the collection.
    /// Collection is not thread-safe, so calls should be made single-threaded.
    /// </remarks>
    /// <typeparam name="TValue">The type of elements contained in the BindableCollection</typeparam>
    /// <typeparam name="TKey">The type of the indexing key</typeparam>
    public class HashedObservableCollection<TValue, TKey> : ObservableCollection<TValue>
    {
        public static bool IgnoreDuplicatesException = false;

        protected internal Dictionary<TKey, int> indices = new Dictionary<TKey, int>();
        protected internal Func<TValue, TKey> _keySelector;

        /// <summary>
        /// Create new HashedBindableCollection
        /// </summary>
        /// <param name="keySelector">Selector function to create key from value</param>
        public HashedObservableCollection(Func<TValue, TKey> keySelector)
            : base()
        {
            if (keySelector == null) throw new ArgumentException("keySelector");
            _keySelector = keySelector;
        }

        public HashedObservableCollection(IEnumerable<TValue> collection, Func<TValue, TKey> keySelector)
            : base(collection)
        {
            if (keySelector == null) throw new ArgumentException("keySelector");
            _keySelector = keySelector;
            InitializeWithCollection(collection);
        }

        private void InitializeWithCollection(IEnumerable<TValue> collection)
        {
            this.Clear();
            foreach (TValue item in collection)
            {
                this.Add(item);
            }
        }

        #region Protected Methods
        protected override void InsertItem(int index, TValue item)
        {
            if (item == null)
                return;
            var key = _keySelector(item);
            if (indices.ContainsKey(key))
            {
                if (IgnoreDuplicatesException)
                    return;
                throw new DuplicateKeyException(key.ToString());
            }

            if (index != this.Count)
            {
                foreach (var k in indices.Keys.Where(k => indices[k] >= index).ToList())
                {
                    indices[k]++;
                }
            }

            base.InsertItem(index, item);
            indices[key] = index;

        }

        protected override void ClearItems()
        {
            base.ClearItems();
            indices.Clear();
        }


        protected override void RemoveItem(int index)
        {
            var item = this[index];
            var key = _keySelector(item);

            base.RemoveItem(index);

            indices.Remove(key);

            foreach (var k in indices.Keys.Where(k => indices[k] > index).ToList())
            {
                indices[k]--;
            }
        }
        #endregion

        public virtual bool ContainsKey(TKey key)
        {
            return indices.ContainsKey(key);
        }

        /// <summary>
        /// Gets or sets the element with the specified key.  If setting a new value, new value must have same key.
        /// </summary>
        /// <param name="key">Key of element to replace</param>
        /// <returns></returns>
        public virtual TValue this[TKey key]
        {

            get 
            {
                if (indices.ContainsKey(key))
                    return this[indices[key]];
                else
                    return default(TValue);
            }
            set
            {
                //confirm key matches
                if (!_keySelector(value).Equals(key))
                    throw new InvalidOperationException("Key of new value does not match");

                if (!indices.ContainsKey(key))
                {
                    this.Add(value);
                }
                else
                {
                    this[indices[key]] = value;
                }
            }
        }

        /// <summary>
        /// Replaces element at given key with new value.  New value must have same key.
        /// </summary>
        /// <param name="key">Key of element to replace</param>
        /// <param name="value">New value</param>
        /// 
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns>False if key not found</returns>
        public virtual bool Replace(TKey key, TValue value)
        {
            if (!indices.ContainsKey(key)) return false;
            //confirm key matches
            if (!_keySelector(value).Equals(key))
                throw new InvalidOperationException("Key of new value does not match");

            this[indices[key]] = value;
            return true;

        }

        public virtual bool Remove(TKey key)
        {
            if (!indices.ContainsKey(key)) return false;

            this.RemoveAt(indices[key]);
            return true;

        }

        public virtual bool UpdateKey(TKey oldKey, TKey newKey)
        {
            if (!indices.ContainsKey(oldKey)) 
                return false;
            
            int x = indices[oldKey];
            indices.Remove(oldKey);
            TValue item = this[x];
            indices.Add(newKey, x);
            return true;
        }

    }
}
