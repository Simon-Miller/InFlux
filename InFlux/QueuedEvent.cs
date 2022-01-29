using System.Diagnostics;

namespace InFlux
{
    /// <summary>
    /// represents an Event that has no payload data, and the event itself is the message that subscribers will want to hear about.
    /// When <see cref="FireEvent"/> is called, all the subscriber code to be called is added to the central event handling <see cref="QueuedActions"/>.
    /// This enforces the order that events are queued, and thus fired, making for a far more predictable sequence in firing of events.
    /// </summary>
    public class QueuedEvent
    {
        private int NextKey = 0;
        private Dictionary<int, Action> subscribers = new();

        private readonly List<Action> oneOffSubscribers = new();

        /// <summary>
        /// Add a subscriber to the collection of listeners.  You are returned a KEY you
        /// may later use to more easily <see cref="UnSubscribe(int)"/> from should you need to.
        /// </summary>
        [DebuggerStepThrough]
        public int Subscribe(Action code)
        {
            var key = ++NextKey;
            subscribers.Add(key, code);

            return key;
        }

        /// <summary>
        /// Add a subscriber to the collection but ensure its (currently) first in the queued to be called.
        /// This pushes ahead of all other subscriptions.  Very specialised case!  Otherwise don't use this!
        /// You are returned a KEY you may later use to more easily <see cref="UnSubscribe(int)"/> from 
        /// should you need to.
        /// </summary>
        [DebuggerStepThrough]
        public int PrioritySubscribe(Action code)
        {
            var key = ++NextKey;
            var currentDictionaryValues = this.subscribers;

            // jiggle dictionary so new code is first item in collection.
            this.subscribers = new();
            this.subscribers.Add(key, code);
            foreach (var oldKey in currentDictionaryValues.Keys)
                this.subscribers.Add(oldKey, currentDictionaryValues[oldKey]);

            return key;
        }

        /// <summary>
        /// The quick method of unscubscribing from an event which means you need to provide the KEY
        /// you would have been given when you originally subscribed.
        /// <para>If you no longer have the key, its still possible to unsubscribe by Action using 
        /// <see cref="UnSubscribe(Action)"/> however, that removed all keys that call the same code.</para>
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(int key) => subscribers.Remove(key);

        /// <summary>
        /// Looks through all subscriptions looking for any keys internally that relate to the code you provide.
        /// Therefore, if you know more than one subscription calls the same code, then more than one KEY will
        /// be removed from the internal collection of subscriptions.
        /// Ideally, you should avoid using this method, and instead use <see cref="UnSubscribe(int)"/>.
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(Action code)
        {
            bool removed = false;
            var removables = this.subscribers.Where(x => x.Value == code).ToArray();
            foreach (var removeable in removables)
            {
                this.subscribers.Remove(removeable.Key);
                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// cause all subscribers to hear about this event. In queued order.
        /// </summary>
        [DebuggerStepThrough]
        public void FireEvent()
        {
            QueuedActions.AddRange(this.subscribers.Select(x => x.Value).ToArray());

            QueuedActions.AddRange(this.oneOffSubscribers.ToArray());

            this.oneOffSubscribers.Clear();
        }

        [DebuggerStepThrough]
        public void SubscribeOnce(Action code)
        {
            this.oneOffSubscribers.Add(code);
        }
    }

    /// <summary>
    /// represents an Event that has no payload data, and the event itself is the message that subscribers will want to hear about.
    /// When <see cref="FireEvent"/> is called, all the subscriber code to be called is added to the central event handling <see cref="QueuedActions"/>.
    /// This enforces the order that events are queued, and thus fired, making for a far more predictable sequence in firing of events.
    /// </summary>
    public class QueuedEvent<T>
    {
        private int NextKey = 0;
        private Dictionary<int, ValueChangedResponse<T>> subscribers = new();

        private readonly List<ValueChangedResponse<T>> oneOffSubscribers = new();

        /// <summary>
        /// Add a subscriber to the collection of listeners.  You are returned a KEY you
        /// may later use to more easily <see cref="UnSubscribe(int)"/> from should you need to.
        /// </summary>
        [DebuggerStepThrough]
        public int Subscribe(ValueChangedResponse<T> code)
        {
            var key = ++NextKey;
            subscribers.Add(key, code);

            return key;
        }

        /// <summary>
        /// Add a subscriber to the collection but ensure its (currently) first in the queued to be called.
        /// This pushes ahead of all other subscriptions.  Very specialised case!  Otherwise don't use this!
        /// You are returned a KEY you may later use to more easily <see cref="UnSubscribe(int)"/> from 
        /// should you need to.
        /// </summary>
        [DebuggerStepThrough]
        public int PrioritySubscribe(ValueChangedResponse<T> code)
        {
            var key = ++NextKey;
            var currentDictionaryValues = this.subscribers;

            // jiggle dictionary so new code is first item in collection.
            this.subscribers = new();
            this.subscribers.Add(key, code);
            foreach(var oldKey in currentDictionaryValues.Keys)
                this.subscribers.Add(oldKey, currentDictionaryValues[oldKey]);

            return key;
        }

        /// <summary>
        /// The quick method of unscubscribing from an event which means you need to provide the KEY
        /// you would have been given when you originally subscribed.
        /// <para>If you no longer have the key, its still possible to unsubscribe by Action using 
        /// <see cref="UnSubscribe(Action)"/> however, that removed all keys that call the same code.</para>
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(int key) => subscribers.Remove(key);

        /// <summary>
        /// Looks through all subscriptions looking for any keys internally that relate to the code you provide.
        /// Therefore, if you know more than one subscription calls the same code, then more than one KEY will
        /// be removed from the internal collection of subscriptions.
        /// Ideally, you should avoid using this method, and instead use <see cref="UnSubscribe(int)"/>.
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(ValueChangedResponse<T> code)
        {
            bool removed = false;
            var removables = this.subscribers.Where(x => x.Value == code).ToArray();
            foreach (var removeable in removables)
            {
                this.subscribers.Remove(removeable.Key);
                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// cause all subscribers to hear about this event. In queued order.
        /// </summary>
        [DebuggerStepThrough]
        public void FireEvent(T? oldValue, T? newValue)
        {
            QueuedActions.AddRange(
                this.subscribers.Select<KeyValuePair<int, ValueChangedResponse<T>>, Action>(x =>
                                    () => x.Value(oldValue, newValue))
                                .ToArray());

            QueuedActions.AddRange(
                this.oneOffSubscribers.Select<ValueChangedResponse<T>, Action>(code =>
                                    () => code(oldValue, newValue))
                                .ToArray());

            this.oneOffSubscribers.Clear();
        }

        /// <summary>
        /// Subscribe to the event, where after the next time it fires, your code will be automatically
        /// unsubscribed.  So your code will at most be called once only.
        /// </summary>
        /// <param name="code"></param>
        [DebuggerStepThrough]
        public void SubscribeOnce(ValueChangedResponse<T> code)
        {
            this.oneOffSubscribers.Add(code);
        }
    }
}
