namespace InFlux
{
    /// <summary>
    /// Frustrating as it is, neither traditional events, or QueuedEvents offer solid knowledge to the 
    /// developer of the order that events are handled in, or in the case of QueuedEvents, whether
    /// event subscritions called on have actually processed the event.  
    /// <para>This approach seeks to remedy this.  It uses the QueuedEvents to ensure the ordering,
    /// but also counts all the subscription calls, and passes code that needs to be called when done,
    /// which is tracked as a count.  When all subscriptions report back, it calls its own callback. </para>
    /// <para>In essense then, a chain of events can be triggered where the calls return when done.
    /// This should translate to a chain of callbacks when done - </para>
    /// <para>So this should give us the best of both worlds: Predictable event processing, and
    /// knowing when even complicated event chains complete.</para>
    /// </summary>
    public class EventChain<T>
    {
        private int subId = 0;
        private Dictionary<int, Action<ChainLink<T>>> subscriptions = new();
        private List<Action<ChainLink<T>>> oneOffSubscriptions = new();

        /// <summary>
        /// called when the <see cref="FireEvent(T, Action)"/> method has completed the chain.
        /// </summary>
        public readonly QueuedEvent OnEventCompleted = new();

        public int Subscribe(Action<ChainLink<T>> callback)
        {
            var key = subId++;
            this.subscriptions.Add(key, callback);
            return key;
        }

        public void SubscribeOnce(Action<ChainLink<T>> callback) =>
            this.oneOffSubscriptions.Add(callback);

        public bool Unsubscribe(int subscriptionKey) =>
            this.subscriptions.Remove(subscriptionKey);

        public bool Unsubscribe(Action<ChainLink<T>> callbackCode)
        {
            foreach (var key in this.subscriptions.Keys)
                if (this.subscriptions[key] == callbackCode)
                {
                    this.subscriptions.Remove(key);
                    return true;
                }

            return false;
        }

        /* both subscribing and unsubscribing during the firing of an event will change the 
         * subscriptions whilst we are possibly iterating over them, and cause an exception.
         * Do we duplicate the list before processing?  That sounds sensible.
         */

        private Dictionary<int, Action<ChainLink<T>>> copySubscriptions()
        {
            // Could have used Linq, but this surely would be quicker?
            // Not that I think performance would be an issue.
            // Likely only a small number of subscribers at most.
            var copy = new Dictionary<int, Action<ChainLink<T>>>(this.subscriptions.Count);
            foreach(var key in this.subscriptions.Keys)
                copy.Add(key, this.subscriptions[key]);

            return copy;
        }
        private List<Action<ChainLink<T>>> copyOneOffSubscriptions()
        {
            var copy = new List<Action<ChainLink<T>>>(this.oneOffSubscriptions.Count);
            foreach(var item in this.oneOffSubscriptions)
                copy.Add(item);

            return copy;
        }
        public record class DebugSubscription(int Id, bool IsOneOff, Action callSubscription);
        public readonly List<DebugSubscription> DebugSubscriptions = new();
        // POPULATE THIS ABOVE LIST (DebugSubscriptions).
        // I was thinking that with all subscriptions we generate an index as we fire the event
        // we generate a DebugSubscription and add to the collection.  As the respond,
        // we know their ID, and can remove them from the list.

        /// <summary>
        /// Processes subscriptions and one-off subscriptions, and generate debug inforation.
        /// Handle chain of events, and only call back the <paramref name="callbackWhenDone"/> action,
        /// when all subscribers each and all callback THIS method.
        /// <para>NOTE: I made the Action nullable because there are times you don't need to hear about it,
        /// but I still think you should explicity (hence, not an optional property) say so.
        /// If you want an event without the callbacks, try using a <see cref="QueuedEvent"/> instead.</para>
        /// </summary>
        public void FireEvent(T payload, Action? callbackWhenDone)
        {
            var subscriptionsCount = this.subscriptions.Count + this.oneOffSubscriptions.Count;
            if (subscriptionsCount <= 0)
            {
                callbackWhenDone?.Invoke();
                return;
            }

            this.DebugSubscriptions.Clear();
            var nextDebugId = 1;

            var actionsForQueue = new List<Action>(this.subscriptions.Count);
            foreach (var subscription in this.subscriptions)
            {
                ChainLink<T> subscriberCallback = generatePayloadForSubscriber(nextDebugId);
                var debugRow = new DebugSubscription(nextDebugId, false, 
                                    () => subscription.Value.Invoke(subscriberCallback));

                this.DebugSubscriptions.Add(debugRow);
                nextDebugId++;

                actionsForQueue.Add(() => subscription.Value.Invoke(subscriberCallback));
            }

            // by adding all at once, we don't give the queue a chance to start processing the first
            // item, and thereby call something that wants to queue up actions, which would break
            // the naturally perceived chronological order.
            QueuedActions.AddRange(actionsForQueue);

            actionsForQueue.Clear();
            foreach (var subscription in this.oneOffSubscriptions)
            {
                ChainLink<T> subscriberCallback = generatePayloadForSubscriber(nextDebugId);
                var debugRow = new DebugSubscription(nextDebugId, true,
                                    () => subscription.Invoke(subscriberCallback));

                this.DebugSubscriptions.Add(debugRow);
                nextDebugId++;

                actionsForQueue.Add(() => subscription.Invoke(subscriberCallback));
            }
            QueuedActions.AddRange(actionsForQueue);
            this.oneOffSubscriptions.Clear();

            ChainLink<T> generatePayloadForSubscriber(int debugId) => 
                new ChainLink<T>(payload, ()=> callbackFromSubscription(debugId));
            
            void callbackFromSubscription(int debugId)
            {            
                this.DebugSubscriptions.Remove(this.DebugSubscriptions.First(x => x.Id == debugId));
                subscriptionsCount--;

                if (subscriptionsCount <= 0)
                {
                    callbackWhenDone?.Invoke();
                    this.OnEventCompleted.FireEvent();
                }
            }
        }
    }
}
