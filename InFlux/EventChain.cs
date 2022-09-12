using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace InFlux
{
    /// <summary>
    /// Frustrating as it is, neither traditional events, or QueuedEvents offer solid knowledge to the 
    /// developer of the order that events are handled in, or in the case of QueuedEvents, whether
    /// event subscritions called on have actually processed the event.  
    /// <para>This approach seeks to remedy this.  It uses a <see cref="ChainLink{T}"/> to ensure the ordering,
    /// but also counts all the subscription calls, and passes code that needs to be called when done,
    /// which is tracked as a count.  When all subscriptions report back, it calls its own callback. </para>
    /// <para>In essense then, a chain of events can be triggered where the calls return when done.</para>
    /// <para>So this should give us the best of both worlds: Predictable event processing, and
    /// knowing when even complicated event chains complete.</para>
    /// </summary>
    public partial class EventChain<T>
    {
        private int subId = 0;
        private readonly Dictionary<int, Action<ChainLink<T>>> subscriptions = new Dictionary<int, Action<ChainLink<T>>>();
        private readonly List<Action<ChainLink<T>>> oneOffSubscriptions = new List<Action<ChainLink<T>>>();

        /// <summary>
        /// Called when the <see cref="FireEvent(T, Action)"/> method is about to process the event the chain.
        /// </summary>
        public readonly QueuedEvent OnBeforeEvent = new QueuedEvent();

        /// <summary>
        /// Called when the <see cref="FireEvent(T, Action)"/> method has completed the chain.
        /// </summary>
        public readonly QueuedEvent OnEventCompleted = new QueuedEvent();

        /// <summary>
        /// Add a subscriber to the collection of listeners.  You are returned a KEY you
        /// may later use to more easily <see cref="UnSubscribe(int)"/> from should you need to.
        /// </summary>
        [DebuggerStepThrough]
        public int Subscribe(Action<ChainLink<T>> callback)
        {
            var key = subId++;
            this.subscriptions.Add(key, callback);
            return key;
        }

        /// <summary>
        /// Subscribe to the event, where after the next time it fires, your code will be automatically
        /// unsubscribed.  So your code will at most be called once only.
        /// </summary>
        [DebuggerStepThrough]
        public void SubscribeOnce(Action<ChainLink<T>> callback) =>
            this.oneOffSubscriptions.Add(callback);

        /// <summary>
        /// The quick method of unscubscribing from an event which means you need to provide the KEY
        /// you would have been given when you originally subscribed.
        /// <para>If you no longer have the key, its still possible to unsubscribe by Action using 
        /// <see cref="UnSubscribe(Action{ChainLink{T}})"/> however, that removes all keys that 
        /// call the same code. YOU HAVE BEEN WARNED!</para>
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(int subscriptionKey) =>
            this.subscriptions.Remove(subscriptionKey);

        /// <summary>
        /// Looks through all subscriptions looking for any keys internally that relate to the code you provide.
        /// Therefore, if you know more than one subscription calls the same code, then more than one KEY will
        /// be removed from the internal collection of subscriptions.
        /// Ideally, you should avoid using this method, and instead use <see cref="UnSubscribe(int)"/>.
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(Action<ChainLink<T>> callbackCode)
        {
            foreach (var key in this.subscriptions.Keys)
                if (this.subscriptions[key] == callbackCode)
                {
                    this.subscriptions.Remove(key);
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Helper collection of information about subscriber code.  These are members of the chain that have not yet reported
        /// back that they have completed.  Completed chains are removed from this list.  Therefore, if you're wondering why your
        /// chain event has not complete, it will be because these entries have not responded to say they have completed.
        /// </summary>
        public readonly List<DebugSubscription> DebugSubscriptions = new List<DebugSubscription>();
        // POPULATE THIS ABOVE LIST (DebugSubscriptions).
        // I was thinking that with all subscriptions we generate an index as we fire the event
        // we generate a DebugSubscription and add to the collection.  As they respond,
        // we know their ID, and can remove them from the list.

        /// <summary>
        /// Processes subscriptions and one-off subscriptions, and generate debug inforation.
        /// Handle chain of events, and only call back the <paramref name="callbackWhenDone"/> action
        /// when all subscribers callback THIS method.
        /// <para>NOTE: I made the Action nullable because there are times you don't need to hear about it,
        /// but I still think you should explicity (hence, not an optional property) say so.
        /// If you want an event without the callbacks, try using a <see cref="QueuedEvent"/> instead.</para>
        /// </summary>
        [DebuggerStepThrough]
        public void FireEvent(T payload, Action callbackWhenDone)
        {
            this.OnBeforeEvent.FireEvent();

            var subscriptionsCount = this.subscriptions.Count + this.oneOffSubscriptions.Count;
            if (subscriptionsCount <= 0)
            {
                callbackWhenDone?.Invoke();
                this.OnEventCompleted.FireEvent();
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
                new ChainLink<T>(payload, () => callbackFromSubscription(debugId));

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
