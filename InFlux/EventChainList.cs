namespace InFlux
{
    /// <summary>
    /// Like a <see cref="List{T}"/> but with events relating to state changes.
    /// Each event is based on an <see cref="EventChain{T}"/> so you can know when the events associated
    /// with the given <see cref="EventChain{T}"/> has completed.
    /// </summary>
    public class EventChainList<T> : ICollection<T?>, IEnumerable<T?>, IEnumerable, IList<T?>, IReadOnlyCollection<T?>, IReadOnlyList<T?>
    {
        /// <summary>
        /// Sets up code that listens for actions that alter this collection, so after those events fire,
        /// it leads to a more generic <see cref="OnListChanged"/> event firing automatically.
        /// </summary>
        public EventChainList()
        {
            // listen to all changes, and fire the more general ListChanged event in response

            setupChain(this.OnItemAdded, chain => (default(T?).AsList(), chain.Payload.AsList()));
            setupChain(this.OnRangeAdded, chain => chain.Payload);
            setupChain(this.OnItemRemoved, chain => (chain.Payload.AsList(), default(T).AsList()));
            setupChain(this.OnItemChanged, chain => (chain.Payload.oldValue.AsList(), chain.Payload.newValue.AsList()));
            setupChain(this.OnListCleared, chain => chain.Payload);

            // bubble up a change to THIS list, to the general OnListChanged event chain.
            void setupChain<TPayload>(EventChain<TPayload?> target, Func<ChainLink<TPayload?>, (IEnumerable<T?> oldValues, IEnumerable<T?> newValues)> mapToPayload)
            {
                target.Subscribe(chain =>
                {
                    // don't want to hold-up the chain, as we want to fire an event that occurs AFTER.
                    chain.CallbackWhenDone();

                    // this should fire AFTER the callback, so you hear about 'on something completed'
                    // BEFORE hearing about this list has changed!!
                    this.OnListChanged.FireEvent(mapToPayload(chain), null);
                });
            }
        }

        private readonly List<T?> list = new();

        /// <summary>
        /// By default we fire events whenever a change is detected.
        /// There may be times when you want to switch off this behaviour (temporarily?)
        /// In which case you can set this to TRUE.  When you are ready for normal behaviour,
        /// you can set this back to FALSE, and any new changes will fire their events once more.
        /// </summary>
        public readonly EventChainProperty<bool> SuppressEvents = new();

        /// <summary>
        /// Subscribe or unsubscribe from this event to be informed of any changes to this list,
        /// including Adds, Removes, and Updates to entries in this list.
        /// Call either: Subscribe(ValueChangedResponse &lt; IEnumerable &lt; T &gt; &gt;)"
        /// or: UnSubscribe(Action{ValueChangedResponse &lt; IEnumerable &lt; T &gt; &gt;)"
        /// </summary>
        public readonly EventChain<(IEnumerable<T?> oldValues, IEnumerable<T?> newValues)> OnListChanged = new();

        /// <summary>
        /// informs you about any added items to this <see cref="EventChainList{T}"/>.
        /// Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly EventChain<T?> OnItemAdded = new();

        /// <summary>
        /// informs you about a range of items added this <see cref="EventChainList{T}"/> 
        /// </summary>
        public readonly EventChain<(IEnumerable<T?> oldValues, IEnumerable<T?> newValues)> OnRangeAdded = new();

        /// <summary>
        /// informs you about any removed items from this <see cref="EventChainList{T}"/>.
        /// Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly EventChain<T?> OnItemRemoved = new();

        /// <summary>
        /// informs you about any items at a given index being swapped (changed) for a different item
        /// on this <see cref="EventChainList{T}"/>.
        /// Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly EventChain<(T? oldValue, T? newValue)> OnItemChanged = new();

        /// <summary>
        /// informs you if this <see cref="EventChainList{T}"/> has all items removed, specifically
        /// to clear the list.  Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly EventChain<(IEnumerable<T?> oldValues, IEnumerable<T?> newValues)> OnListCleared = new();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="EventChainList{T}"/>.
        /// </summary>
        public int Count => this.list.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="EventChainList{T}"/> is read-only.
        /// </summary>
        public bool IsReadOnly => ((ICollection<T?>)this.list).IsReadOnly;

        /// <summary>
        /// Determines whether an element is in the <see cref="EventChainList{T}"/>.
        /// </summary>
        public bool Contains(T? item) => this.list.Contains(item);

        /// <summary>
        /// Copies the entire <see cref="EventChainList{T}"/> to a compatible one-dimensional
        /// array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied
        /// from <see cref="EventChainList{T}"/>. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T?[] array, int arrayIndex) => this.list.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="EventChainList{T}"/>.
        /// </summary>     
        public IEnumerator<T?> GetEnumerator() => this.list.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="EventChainList{T}"/>.
        /// </summary> 
        IEnumerator IEnumerable.GetEnumerator() => this.list.GetEnumerator();

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the entire <see cref="EventChainList{T}"/>.
        /// </summary>
        public int IndexOf(T? item) => this.list.IndexOf(item);

        /// <summary>
        /// Calls provided code if <see cref="SuppressEvents"/> is FALSE.
        /// otherwise does nothing.
        /// </summary>
        /// <param name="fireEventsCode">Code to execute if events are not being suppressed.</param>
        [DebuggerStepThrough]
        protected void SuppressEventsCheck(Action fireEventsCode)
        {
            if(this.SuppressEvents.Value == false)
            {
                fireEventsCode();
            }
        }

        /// <summary>
        /// Adds an object to the end of the list.
        /// </summary>
        [DebuggerStepThrough]
        public void Add(T? item)
        {
            this.list.Add(item);
            this.SuppressEventsCheck(()=> this.OnItemAdded.FireEvent(item, null));
        }

        /// <summary>
        /// Removes all elements from the list.
        /// </summary>
        [DebuggerStepThrough]
        public void Clear()
        {
            var currentList = this.list.ToList();
            this.list.Clear();
            this.SuppressEventsCheck(() => this.OnListCleared.FireEvent((currentList, this.list), null));
        }

        /// <summary>
        ///  Removes the first occurrence of a specific object from the list.
        /// </summary>
        [DebuggerStepThrough]
        public bool Remove(T? item)
        {
            var result = this.list.Remove(item);
            this.SuppressEventsCheck(() => this.OnItemRemoved.FireEvent(item, null));

            return result;
        }

        /// <summary>
        /// Inserts an element into the list at the specified index.
        /// </summary>
        [DebuggerStepThrough]
        public void Insert(int index, T? item)
        {
            var oldItem = this.list[index];
            this.list[index] = item;
            this.SuppressEventsCheck(() => this.OnItemChanged.FireEvent((oldItem, item), null));
        }

        /// <summary>
        /// Removes the element at the specified index of this list.
        /// </summary>
        [DebuggerStepThrough]
        public void RemoveAt(int index)
        {
            var oldItem = this.list[index];
            this.list.RemoveAt(index);
            this.SuppressEventsCheck(() => this.OnItemRemoved.FireEvent(oldItem, null));
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public T? this[int index]
        {
            get => this.list[index];
            set
            {
                var oldItem = this.list[index];
                this.list[index] = value;
                this.SuppressEventsCheck(() => this.OnItemChanged.FireEvent((oldItem, value), null));
            }
        }

        /// <summary>
        /// Adds each item in the provided collection to this list.
        /// <para>Fires <see cref="OnRangeAdded"/> when done.</para>
        /// </summary>
        [DebuggerStepThrough]
        public void AddRange(IEnumerable<T?> collection)
        {
            this.list.AddRange(collection);

            this.SuppressEventsCheck(() => this.OnRangeAdded.FireEvent((default(T?).AsList(), collection), null));
        }
    }
}
