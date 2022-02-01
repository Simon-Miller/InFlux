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
    /// developer of all the order that events are handled in, of in the case of QueuedEvents, whether
    /// event subscritions called on have actually processed the event.  
    /// <para>This approach seeks to remedy this.  It uses the QueuedEvents to ensure the ordering,
    /// but also counts all the subscription calls, and passes code that needs to be called when done,
    /// which is tracked as a count.  When all subscriptions report back, it calls its own callback. </para>
    /// <para>In essense then, a chain of events can be triggered where the calls return when done.
    /// This should translate to a chain of callback when done - </para>
    /// <para>So this should give us the best of both worlds: Predictable event processing, and
    /// knowing when even complicated event chains complete.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

        public void FireEvent(T payload, Action callbackWhenDone)
        {
            int handlersCount = this.subscriptions.Count + this.oneOffSubscriptions.Count;
            if (handlersCount <= 0)
            {
                callbackWhenDone();
                return;
            }

            foreach (var subscription in this.subscriptions)
            {
                QueuedActions.Add(() => 
                {
                    subscription.Value.Invoke(new ChainLink<T>(payload, callbackFromSubscription));
                });
            }

            foreach (var subscription in this.oneOffSubscriptions)
            {
                QueuedActions.Add(() => 
                {
                    subscription.Invoke(new ChainLink<T>(payload, callbackFromSubscription));
                });
            }

            this.oneOffSubscriptions.Clear();

            void callbackFromSubscription()
            {
                handlersCount--;

                if (handlersCount <= 0)
                    callbackWhenDone();
            }
        }
    }
}
