namespace InFlux
{

    public class ChainLink<T>
    {
        internal ChainLink(T payload, Action callbackWhenDone)
        {
            this.payload = payload;
            this.callbackWhenDone = callbackWhenDone;
        }

        public readonly T payload;
        public readonly Action callbackWhenDone;
    }

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
        int subId = 0;
        Dictionary<int, Action<ChainLink<T>>> subscriptions = new();
        List<Action<ChainLink<T>>> oneOffSubscriptions = new();

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
        // TODO: POPULATE THIS ABOVE LIST.
        // I was thinking that with all subscriptions we generate an index as we fire the event
        // we generate a DebugSubscription and add to the collection.  As the respond,
        // we know their ID, and can remove them from the list.

        public void FireEvent(T payload, Action callbackWhenDone)
        {
            var subscriptionsCount = this.subscriptions.Count + this.oneOffSubscriptions.Count;
            if (subscriptionsCount <= 0)
            {
                callbackWhenDone();
                return;
            }

            this.DebugSubscriptions.Clear();
            var nextDebugId = 1;

            var iterableSubscriptions = this.copySubscriptions();
            foreach (var subscription in iterableSubscriptions)
            {
                ChainLink<T> subscriberCallback = generatePayloadForSubscriber(nextDebugId);
                var debugRow = new DebugSubscription(nextDebugId, false, 
                                    () => subscription.Value.Invoke(subscriberCallback));

                this.DebugSubscriptions.Add(debugRow);
                nextDebugId++;

                QueuedActions.Add(() => subscription.Value.Invoke(subscriberCallback));
            }

            var iterableOneOffSubscriptions = this.copyOneOffSubscriptions();
            foreach (var subscription in iterableOneOffSubscriptions)
            {
                ChainLink<T> subscriberCallback = generatePayloadForSubscriber(nextDebugId);
                var debugRow = new DebugSubscription(nextDebugId, true,
                                    () => subscription.Invoke(subscriberCallback));

                this.DebugSubscriptions.Add(debugRow);
                nextDebugId++;

                QueuedActions.Add(() => subscription.Invoke(subscriberCallback));
            }

            this.oneOffSubscriptions.Clear();

            ChainLink<T> generatePayloadForSubscriber(int debugId) => 
                new ChainLink<T>(payload, ()=> callbackFromSubscription(debugId));
            
            void callbackFromSubscription(int debugId)
            {            
                this.DebugSubscriptions.Remove(this.DebugSubscriptions.First(x => x.Id == debugId));
                subscriptionsCount--;

                if (subscriptionsCount <= 0)
                    callbackWhenDone();
            }
        }
    }
}
