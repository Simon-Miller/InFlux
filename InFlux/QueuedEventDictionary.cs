using System.Diagnostics.CodeAnalysis;

namespace InFlux
{
    /// <summary>
    /// Acts as a dictionary that will inform you when an item is added, removed, or replaced within it.
    /// (You subscribe to the <see cref="OnChanged"/> event)
    /// Works best when your <typeparamref name="K"/> is a record type, or some other immutable type.
    /// This dictionary doesn't attempt to duplicate entries as you read them.  
    /// Therefore, this dictionary is only safe if your types are immutable.
    /// </summary>
    public class QueuedEventDictionary<K, V> :
            IDictionary<K, V>,
            ICollection<KeyValuePair<K, V>>,
            IEnumerable<KeyValuePair<K, V>>,
            IEnumerable,
            IReadOnlyCollection<KeyValuePair<K, V>>,
            IReadOnlyDictionary<K, V>,
            ICollection,
            IDictionary
        where K : notnull
    {
        /// <summary>
        /// Sets up code that listens for actions that alter this collection, so after those events fire,
        /// it leads to a more generic <see cref="OnChanged"/> event firing automatically.
        /// </summary>
        public QueuedEventDictionary()
        {
            this.OnItemAdded.Subscribe((O, N) => this.OnChanged.FireEvent());
            this.OnItemChanged.Subscribe((O, N) => this.OnChanged.FireEvent());
            this.OnItemRemoved.Subscribe((O, N) => this.OnChanged.FireEvent());
            this.OnDictionaryCleared.Subscribe((O, N) => this.OnChanged.FireEvent());
        }

        private Dictionary<K, V> dictionary = new();

        /// <summary>
        /// Subscribe or unsubscribe from this event to be informed of any changes to this dictionary,
        /// including Adds, Removes, and Updates to entries in this dictionary.
        /// Call either: Subscribe(ValueChangedResponse{IEnumerable{KeyValuePair{K,V}}})
        /// or: UnSubscribe(Action{ValueChangedResponse{IEnumerable{KeyValuePair{K,V}}})
        /// </summary>
        public readonly QueuedEvent OnChanged = new();

        /// <summary>
        /// informs you about any added items to this <see cref="QueuedEventDictionary{K, V}"/>.
        /// Also fires <see cref="OnChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<KeyValuePair<K, V>> OnItemAdded = new();

        /// <summary>
        /// informs you about any removed items from this <see cref="QueuedEventDictionary{K, V}"/>.
        /// Also fires <see cref="OnChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<KeyValuePair<K, V>> OnItemRemoved = new();

        /// <summary>
        /// informs you about any items with a given Key being swapped (changed) for a different item
        /// on this <see cref="QueuedEventDictionary{K, V}"/>.
        /// Also fires <see cref="OnChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<KeyValuePair<K, V>> OnItemChanged = new();

        /// <summary>
        /// informs you if this <see cref="QueuedEventDictionary{K, V}"/> has all items removed, specifically
        /// to clear the list.  Also fires <see cref="OnChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<IEnumerable<KeyValuePair<K, V>>> OnDictionaryCleared = new();

        /// <summary>
        /// Gets or sets the element at the specified keyin this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public V this[K key]
        {
            get => this.dictionary[key];
            set
            {
                var oldValue = this.dictionary[key];
                this.dictionary[key] = value;
                this.OnItemChanged.FireEvent(
                    new KeyValuePair<K, V>(key, oldValue),
                    new KeyValuePair<K, V>(key, value)
                );
            }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{K}"/> containing the keys of the <see cref="Dictionary{K, V}"/>
        /// </summary>
        public ICollection<K> Keys => this.dictionary.Keys;

        /// <summary>
        /// Gets an <see cref="ICollection{V}"/> containing the values of the <see cref="Dictionary{K, V}"/>
        /// </summary>
        public ICollection<V> Values => this.dictionary.Values;

        /// <summary>
        /// Gets the number of elements contained in this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public int Count => this.dictionary.Count;

        /// <summary>
        /// Gets a value indicating whether this <see cref="QueuedEventDictionary{K, V}"/> is read-only.
        /// </summary>
        public bool IsReadOnly => ((IDictionary)this.dictionary).IsReadOnly;

        IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => this.dictionary.Keys;

        IEnumerable<V> IReadOnlyDictionary<K, V>.Values => this.dictionary.Values;

        public bool IsSynchronized => ((ICollection)this.dictionary).IsSynchronized;

        public object SyncRoot => ((ICollection)this.dictionary).SyncRoot;

        public bool IsFixedSize => ((IDictionary)this.dictionary).IsFixedSize;

        ICollection IDictionary.Keys => ((IDictionary)this.dictionary).Keys;

        ICollection IDictionary.Values => ((IDictionary)this.dictionary).Values;

        /// <summary>
        /// Gets or sets the element at the specified keyin this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public object? this[object key]
        {
            get => ((IDictionary)this.dictionary)[key];
            set
            {
                var k = (K)key;
                var v = (V)value!;
                this[k] = v;
            }
        }

        /// <summary>
        /// Adds an item to this <see cref="QueuedEventDictionary{K, V}"/> with given key.
        /// </summary>
        public void Add(K key, V value)
        {
            this.dictionary.Add(key, value);
            this.OnItemAdded.FireEvent(
                new KeyValuePair<K, V>(
                    key, default(V)!),
                new KeyValuePair<K, V>(key, value)
            );
        }

        /// <summary>
        /// Adds an item to this <see cref="QueuedEventDictionary{K, V}"/> with given key.
        /// </summary>
        public void Add(KeyValuePair<K, V> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Removes all elements from this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public void Clear()
        {
            var oldValues = this.dictionary.ToList();// copy all name/value pairs
            this.dictionary.Clear();
            this.OnDictionaryCleared.FireEvent(oldValues, new List<KeyValuePair<K, V>>());
        }

        /// <summary>
        /// Determines whether an element is in this <see cref="QueuedEventDictionary{K, V}"/> with given key.
        /// </summary>
        public bool Contains(KeyValuePair<K, V> item) =>
            this.dictionary.Contains(item);

        /// <summary>
        /// Determines whether the given <paramref name="key"/> exists in 
        /// this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public bool ContainsKey(K key) =>
            this.dictionary.ContainsKey(key);

        /// <summary>
        /// Copies the entire <see cref="QueuedEventDictionary{K, V}"/> to a compatible one-dimensional
        /// array, starting at the specified <paramref name="arrayIndex"/> of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="System.Array"/> that is the destination 
        /// of the elements copied from this <see cref="QueuedEventDictionary{K, V}"/>. 
        /// The <see cref="System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) =>
            ((IDictionary<K, V>)this.dictionary).CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates through this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>  
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() =>
            this.dictionary.GetEnumerator();

        /// <summary>
        ///  Removes the first occurrence of a specific object from this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public bool Remove(K key)
        {
            var oldValue = this.dictionary[key];
            bool result = this.dictionary.Remove(key);

            if (result)
                this.OnItemRemoved.FireEvent(
                    new KeyValuePair<K, V>(key, oldValue),
                    new KeyValuePair<K, V>(key, default!)
                );

            return result;
        }

        /// <summary>
        ///  Removes the first occurrence of a specific object from this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public bool Remove(KeyValuePair<K, V> item) =>
            this.Remove(item.Key);

        /// <summary>
        /// Tries to get an item from this <see cref="QueuedEventDictionary{K, V}"/> 
        /// using provided <paramref name="key"/>.  Returns TRUE if successful in getting a value.
        /// </summary>
        public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) =>
            this.dictionary.TryGetValue(key, out value);

        /// <summary>
        /// Returns an enumerator that iterates through this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>  
        IEnumerator IEnumerable.GetEnumerator() =>
            this.dictionary.GetEnumerator();

        /// <summary>
        /// Copies the entire <see cref="QueuedEventDictionary{K, V}"/> to a compatible one-dimensional
        /// array, starting at the specified <paramref name="index"/> of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="System.Array"/> that is the destination 
        /// of the elements copied from this <see cref="QueuedEventDictionary{K, V}"/>. 
        /// The <see cref="System.Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(Array array, int index) =>
            ((IDictionary)this.dictionary).CopyTo(array, index);

        /// <summary>
        /// Adds an item to this <see cref="QueuedEventDictionary{K, V}"/> with given key.
        /// </summary>
        public void Add(object key, object? value) =>
            this.Add(new KeyValuePair<K, V>((K)key, (V)value!));

        /// <summary>
        /// Determines whether an element is in this <see cref="QueuedEventDictionary{K, V}"/> with given key.
        /// </summary>
        public bool Contains(object key) =>
            ((IDictionary)this.dictionary).Contains(key);

        /// <summary>
        /// Returns an enumerator that iterates through this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>  
        IDictionaryEnumerator IDictionary.GetEnumerator() =>
            ((IDictionary)this.dictionary).GetEnumerator();

        /// <summary>
        ///  Removes the first occurrence of a specific object from this <see cref="QueuedEventDictionary{K, V}"/>.
        /// </summary>
        public void Remove(object key) =>
            this.Remove((K)key);

    }
}
