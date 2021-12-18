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

        /// <summary>
        /// Add a subscriber to the collection of listeners.  You are returned a KEY you
        /// may later use to more easily <see cref="UnSubscribe(int)"/> from should you need to.
        /// </summary>
        public int Subscribe(Action code)
        {
            var key = ++NextKey;
            subscribers.Add(key, code);

            return key;
        }

        /// <summary>
        /// The quick method of unscubscribing from an event which means you need to provide the KEY
        /// you would have been given when you originally subscribed.
        /// <para>If you no longer have the key, its still possible to unsubscribe by Action using 
        /// <see cref="UnSubscribe(Action)"/> however, that removed all keys that call the same code.</para>
        /// </summary>
        public bool UnSubscribe(int key) => subscribers.Remove(key);

        /// <summary>
        /// Looks through all subscriptions looking for any keys internally that relate to the code you provide.
        /// Therefore, if you know more than one subscription calls the same code, then more than one KEY will
        /// be removed from the internal collection of subscriptions.
        /// Ideally, you should avoid using this method, and instead use <see cref="UnSubscribe(int)"/>.
        /// </summary>
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
        public void FireEvent() =>
            QueuedActions.AddRange(this.subscribers.Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// represents an Event that has no payload data, and the event itself is the message that subscribers will want to hear about.
    /// When <see cref="FireEvent"/> is called, all the subscriber code to be called is added to the central event handling <see cref="QueuedActions"/>.
    /// This enforces the order that events are queued, and thus fired, making for a far more predictable sequence in firing of events.
    /// </summary>
    public class QueuedEvent<T>
    {
        private int NextKey = 0;
        private Dictionary<int, Action<T>> subscribers = new();

        /// <summary>
        /// Add a subscriber to the collection of listeners.  You are returned a KEY you
        /// may later use to more easily <see cref="UnSubscribe(int)"/> from should you need to.
        /// </summary>
        public int Subscribe(Action<T> code)
        {
            var key = ++NextKey;
            subscribers.Add(key, code);

            return key;
        }

        /// <summary>
        /// The quick method of unscubscribing from an event which means you need to provide the KEY
        /// you would have been given when you originally subscribed.
        /// <para>If you no longer have the key, its still possible to unsubscribe by Action using 
        /// <see cref="UnSubscribe(Action)"/> however, that removed all keys that call the same code.</para>
        /// </summary>
        public bool UnSubscribe(int key) => subscribers.Remove(key);

        /// <summary>
        /// Looks through all subscriptions looking for any keys internally that relate to the code you provide.
        /// Therefore, if you know more than one subscription calls the same code, then more than one KEY will
        /// be removed from the internal collection of subscriptions.
        /// Ideally, you should avoid using this method, and instead use <see cref="UnSubscribe(int)"/>.
        /// </summary>
        public bool UnSubscribe(Action<T> code)
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
        public void FireEvent(T payload) =>
            QueuedActions.AddRange(this.subscribers.Select<KeyValuePair<int, Action<T>>, Action>(x => () => x.Value(payload)).ToArray());
    }
}
