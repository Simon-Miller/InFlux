using System.Collections;

namespace InFlux
{
    /// <summary>
    /// Acts as a list that will inform you when an item is added, removed, or replaced within this list.
    /// (You subscribe to the <see cref="ListChanged"/> event)
    /// Works best when your <see cref="T"/> is a record type, or some other immutable type.
    /// This list doesn't attempt to duplicate entries as you read them.  Therefore, this list is only safe
    /// if your types are immutable.
    /// </summary>
    public class QueuedEventList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        private readonly List<T> list = new();

        /// <summary>
        /// Subscribe or unsubscribe from this event to be informed of any changes to this list,
        /// including Adds, Removes, and Updates to entries in this list.
        /// Call Either: <seealso cref="QueuedEvent{T}.Subscribe(Action{T})"/> 
        ///  or: <seealso cref="QueuedEvent{T}.UnSubscribe(Action{T})"/>.
        /// </summary>
        public readonly QueuedEvent<QueuedEventList<T>> ListChanged = new();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public int Count => this.list.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="QueuedEventList{T}"/> is read-only.
        /// </summary>
        public bool IsReadOnly => ((ICollection<T>)this.list).IsReadOnly;

        /// <summary>
        /// Determines whether an element is in the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public bool Contains(T item) => this.list.Contains(item);

        /// <summary>
        /// Copies the entire <see cref="QueuedEventList{T}"/> to a compatible one-dimensional
        /// array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied
        /// from <see cref="QueuedEventList{T}"/>. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex) => this.list.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="QueuedEventList{T}"/>.
        /// </summary>     
        public IEnumerator<T> GetEnumerator() => this.list.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="QueuedEventList{T}"/>.
        /// </summary> 
        IEnumerator IEnumerable.GetEnumerator() => this.list.GetEnumerator();

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the entire <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public int IndexOf(T item) => this.list.IndexOf(item);

        /// <summary>
        /// Adds an object to the end of the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public void Add(T item)
        {
            this.list.Add(item);
            this.ListChanged.FireEvent(this);
        }

        /// <summary>
        /// Removes all elements from the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public void Clear()
        {
            this.list.Clear();
            this.ListChanged.FireEvent(this);
        }

        /// <summary>
        ///  Removes the first occurrence of a specific object from the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public bool Remove(T item)
        {
            var result = this.list.Remove(item);
            this.ListChanged.FireEvent(this);

            return result;
        }

        /// <summary>
        /// Inserts an element into the <see cref="QueuedEventList{T}"/> at the specified index.
        /// </summary>
        public void Insert(int index, T item)
        {
            this.list[index] = item;
            this.ListChanged.FireEvent(this);
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public void RemoveAt(int index)
        {
            this.list.RemoveAt(index);
            this.ListChanged.FireEvent(this);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public T this[int index]
        {
            get => this.list[index];
            set
            {
                this.list[index] = value;
                this.ListChanged.FireEvent(this);
            }
        }

        /// <summary>
        /// Adds each item in the provided collection to this collection.
        /// </summary>
        /// <param name="collection"></param>
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (T item in collection)
                this.list.Add(item);

            this.ListChanged.FireEvent(this);
        }
    }
}
