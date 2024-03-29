﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InFlux
{
    /// <summary>
    /// Acts as a list that will inform you when an item is added, removed, or replaced within this list.
    /// (You subscribe to the <see cref="OnListChanged"/> event)
    /// Works best when your <typeparamref name="T"/> is a record type, or some other immutable type.
    /// This list doesn't attempt to duplicate entries as you read them.  Therefore, this list is only safe
    /// if your types are immutable.
    /// </summary>
    public class QueuedEventList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        /// <summary>
        /// Sets up code that listens for actions that alter this collection, so after those events fire,
        /// it leads to a more generic <see cref="OnListChanged"/> event firing automatically.
        /// </summary>
        public QueuedEventList()
        {
            // listen to all changes, and fire the more general ListChanged event in response

            this.OnItemAdded.Subscribe((O, N) => 
                this.OnListChanged.FireEvent(new List<T> { O }, new List<T> { N }));

            this.OnItemRemoved.Subscribe((O, N) =>
                this.OnListChanged.FireEvent(new List<T> { O }, new List<T> { N }));

            this.OnItemChanged.Subscribe((O, N) =>
                this.OnListChanged.FireEvent(new List<T> { O }, new List<T> { N }));

            this.OnListCleared.Subscribe((O, N) => this.OnListChanged.FireEvent(O, N));
        }

        private readonly List<T> list = new List<T>();

        /// <summary>
        /// Subscribe or unsubscribe from this event to be informed of any changes to this list,
        /// including Adds, Removes, and Updates to entries in this list.
        /// Call either: UnSubscribe(ValueChangedResponse &lt; IEnumerable &lt; T &gt; &gt;)"/>
        /// or: UnSubscribe(Action{ValueChangedResponse &lt; IEnumerable &lt; T &gt; &gt;)"/>
        /// </summary>
        public readonly QueuedEvent<IEnumerable<T>> OnListChanged = new QueuedEvent<IEnumerable<T>>();

        /// <summary>
        /// informs you about any added items to this <see cref="QueuedEventList{T}"/>.
        /// Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<T> OnItemAdded = new QueuedEvent<T>();

        /// <summary>
        /// informs you about any removed items from this <see cref="QueuedEventList{T}"/>.
        /// Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<T> OnItemRemoved = new QueuedEvent<T>();

        /// <summary>
        /// informs you about any items at a given index being swapped (changed) for a different item
        /// on this <see cref="QueuedEventList{T}"/>.
        /// Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<T> OnItemChanged = new QueuedEvent<T>();

        /// <summary>
        /// informs you if this <see cref="QueuedEventList{T}"/> has all items removed, specifically
        /// to clear the list.  Also fires <see cref="OnListChanged"/> event.
        /// </summary>
        public readonly QueuedEvent<IEnumerable<T>> OnListCleared = new QueuedEvent<IEnumerable<T>>();

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
            this.OnItemAdded.FireEvent(default, item);
        }

        /// <summary>
        /// Removes all elements from the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public void Clear()
        {
            var currentList = this.list.ToList();
            this.list.Clear();
            this.OnListCleared.FireEvent(currentList, this.list);
        }

        /// <summary>
        ///  Removes the first occurrence of a specific object from the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public bool Remove(T item)
        {
            var result = this.list.Remove(item);
            this.OnItemRemoved.FireEvent(item, default);

            return result;
        }

        /// <summary>
        /// Inserts an element into the <see cref="QueuedEventList{T}"/> at the specified index.
        /// </summary>
        public void Insert(int index, T item)
        {
            var oldItem = this.list[index];
            this.list[index] = item;
            this.OnItemChanged.FireEvent(oldItem, item);
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="QueuedEventList{T}"/>.
        /// </summary>
        public void RemoveAt(int index)
        {
            var oldItem = this.list[index];
            this.list.RemoveAt(index);
            this.OnItemRemoved.FireEvent(oldItem, default);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public T this[int index]
        {
            get => this.list[index];
            set
            {
                var oldItem = this.list[index];
                this.list[index] = value;
                this.OnItemChanged.FireEvent(oldItem, value);
            }
        }

        /// <summary>
        /// Adds each item in the provided collection to this collection.
        /// </summary>
        /// <param name="collection"></param>
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (T item in collection)
                this.Add(item);     // each item trigger event.  But that's informative,
                                    // otherwise we'd need an event for the range of values added.
        }
    }
}
