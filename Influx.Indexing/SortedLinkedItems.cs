using System;
using System.Collections;
using System.Collections.Generic;

namespace Influx.Indexing
{
    /// <summary>
    /// Can represent many thousands of items.
    /// Doesn't suffer from progressive slow-downs of inserting in the middle of a list.
    /// However, finding the position in the list to insert is still an iteration from the beginning.
    /// So this is simple code, but will get slow with high numbers of items.
    /// It can be converted back to a List at ant time.
    /// Its better than List with high number sof records, now numbers, such as only hundreds or records,
    /// you may well be better off with a List, and then sort the list.
    /// </summary>
    public class SortedLinkedItems<T> : IEnumerable<T>
    {
        public SortedLinkedItems(Func<T, T, Comparison> comparer)
        {
            this.comparer = comparer;
        }

        private readonly Func<T, T, Comparison> comparer;

        private int count = 0;

        public Link<T>? First { get; private set; }

        public Link<T> Add(T item)
        {
            count++;

            // start at first, and keep going, looking for first place where previous is smaller, and current is larger.
            if (this.First is null)
            {
                this.First = new Link<T>(item);

                return this.First;
            }
            else
            {
                var comparison = comparer(this.First.Value, item);
                if (comparison == Comparison.LeftIsLarger || comparison == Comparison.AreEqual)
                {
                    // no iteration needed.
                    var newFirst = new Link<T>(item, previous: null, next: this.First);
                    this.First.Previous = newFirst;
                    this.First = newFirst;

                    return newFirst;
                }
                else
                {
                    // we know the First is smaller.
                    var previous = this.First;
                    var current = previous.Next;

                    do
                    {
                        if (current != null)
                        {
                            var previousComparison = comparer(previous.Value, item);
                            var currentComparison = comparer(current.Value, item);

                            if (previousComparison == Comparison.LeftIsSmaller &&
                                (currentComparison == Comparison.AreEqual || currentComparison == Comparison.LeftIsLarger))
                            {
                                // slot in between these two.
                                var middle = new Link<T>(item, previous: previous, next: current);
                                previous.Next = middle;
                                current.Previous = middle;

                                return middle;
                            }
                        }
                        else
                        {
                            // we got to the end, so add to end.
                            var newLast = new Link<T>(item, previous: previous, next: null);
                            previous.Next = newLast;

                            return newLast;
                        }

                        previous = current;
                        current = previous.Next;
                    }
                    while (true);
                }
            }
        }

        public void Remove(Link<T> item)
        {
            count--;

            var prev = item.Previous;
            var next = item.Next;

            if (prev != null)
                prev.Next = next;

            if (next != null)
                next.Previous = prev;
        }

        /// <summary>
        /// Returns the collection as a list, which should be in sorted order.
        /// </summary>
        public List<T> ToList()
        {
            var list = new List<T>(count);

            Link<T>? current = this.First;

            while (current != null)
            {
                list.Add(current.Value);
                current = current.Next;
            }

            return list;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new TEnumerator(new LinkedItemsEnumerator(this.First!));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new TEnumerator(new LinkedItemsEnumerator(this.First!));
        }

        class LinkedItemsEnumerator : IEnumerator<Link<T>>
        {
            public LinkedItemsEnumerator(Link<T> first)
            {
                this.first = first;
                this.Current = first;
            }

            private bool areBeforeFirst = true;
            private Link<T> first;

            public Link<T> Current { get; set; }

            object IEnumerator.Current => (object)Current!;

            public void Dispose()
            {
                Current = null!;
                this.first = null!;
            }

            public bool MoveNext()
            {
                if(areBeforeFirst)
                {
                    areBeforeFirst = false;
                    Current = this.first;

                    return (Current.Next != null);
                }

                if (Current != null)
                {
                    Current = Current.Next!;

                    return (Current.Next != null);
                }

                return false;
            }

            public void Reset()
            {
                this.Current = first;
            }
        }
        class TEnumerator : IEnumerator<T>
        {
            public TEnumerator(LinkedItemsEnumerator enumerator)
            {
                this.enumerator = enumerator;
            }

            private LinkedItemsEnumerator enumerator;

            public T Current => enumerator.Current.Value;

            object IEnumerator.Current => (object)enumerator.Current.Value!;

            public void Dispose()
            {
                enumerator = null!;
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }
    }

    public enum Comparison
    {
        LeftIsSmaller = -1,
        AreEqual = 0,
        LeftIsLarger = 1
    }

    public class Link<T>
    {
        public Link(T value, Link<T>? previous = null, Link<T>? next = null)
        {
            Value = value;
            Previous = previous;
            Next = next;
        }

        public readonly T Value;

        public Link<T>? Previous;
        public Link<T>? Next;
    }
}
