using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Utility
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
    public class HashedBindableCollection<TValue, TKey> : ObservableCollection<TValue>
    {
        public static bool IgnoreDuplicatesException = false;

        protected internal Dictionary<TKey, int> indecies = new Dictionary<TKey, int>();
        protected internal Func<TValue, TKey> _keySelector;

        /// <summary>
        /// Create new HashedBindableCollection
        /// </summary>
        /// <param name="keySelector">Selector function to create key from value</param>
        public HashedBindableCollection(Func<TValue, TKey> keySelector)
            : base()
        {
            if (keySelector == null) throw new ArgumentException("keySelector");
            _keySelector = keySelector;
        }

        #region Protected Methods
        protected override void InsertItem(int index, TValue item)
        {
            if (item == null)
                return;
            var key = _keySelector(item);
            if (indecies.ContainsKey(key))
            {
                if (IgnoreDuplicatesException)
                    return;
                throw new DuplicateKeyException(key.ToString());
            }

            if (index != this.Count)
            {
                foreach (var k in indecies.Keys.Where(k => indecies[k] >= index).ToList())
                {
                    indecies[k]++;
                }
            }

            base.InsertItem(index, item);
            indecies[key] = index;

        }

        protected override void ClearItems()
        {
            base.ClearItems();
            indecies.Clear();
        }


        protected override void RemoveItem(int index)
        {
            var item = this[index];
            var key = _keySelector(item);

            base.RemoveItem(index);

            indecies.Remove(key);

            foreach (var k in indecies.Keys.Where(k => indecies[k] > index).ToList())
            {
                indecies[k]--;
            }
        }
        #endregion

        public virtual bool ContainsKey(TKey key)
        {
            return indecies.ContainsKey(key);
        }

        /// <summary>
        /// Gets or sets the element with the specified key.  If setting a new value, new value must have same key.
        /// </summary>
        /// <param name="key">Key of element to replace</param>
        /// <returns></returns>
        public virtual TValue this[TKey key]
        {

            get { return this[indecies[key]]; }
            set
            {
                //confirm key matches
                if (!_keySelector(value).Equals(key))
                    throw new InvalidOperationException("Key of new value does not match");

                if (!indecies.ContainsKey(key))
                {
                    this.Add(value);
                }
                else
                {
                    this[indecies[key]] = value;
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
            if (!indecies.ContainsKey(key)) return false;
            //confirm key matches
            if (!_keySelector(value).Equals(key))
                throw new InvalidOperationException("Key of new value does not match");

            this[indecies[key]] = value;
            return true;

        }

        public virtual bool Remove(TKey key)
        {
            if (!indecies.ContainsKey(key)) return false;

            this.RemoveAt(indecies[key]);
            return true;

        }

    }
    public class DuplicateKeyException : Exception
    {

        public string Key { get; private set; }
        public DuplicateKeyException(string key)
            : base("Attempted to insert duplicate key " + key + " in collection")
        {
            Key = key;
        }
    }
}
