using InFlux.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static InFlux.QueuedEventHelperExtensions;

namespace InFlux
{
    /// <summary>
    /// represents an Event that has payload data, but does not incluse an OLD value for your consideration, only a NEW value.
    /// When <see cref="FireEvent"/> is called, all the subscriber code to be called is added to the central event handling <see cref="QueuedActions"/>.
    /// This enforces the order that events are queued, and thus fired, making for a far more predictable sequence in firing of events.
    /// </summary>
    public class SimpleQueuedEvent<T>
    {
        private int NextKey = 0;
        private Dictionary<int, WeakReference<Action<T>>> subscribers = new Dictionary<int, WeakReference<Action<T>>>();
        private readonly List<WeakReference<Action<T>>> oneOffSubscribers = new List<WeakReference<Action<T>>>();

        /// <summary>
        /// Add a subscriber to the collection of listeners.  You are returned a KEY you
        /// may later use to more easily <see cref="UnSubscribe(int)"/> from should you need to.
        /// </summary>
        [DebuggerStepThrough]
        public int Subscribe(Action<T> code)
        {
            var key = ++NextKey;
            subscribers.Add(key, new WeakReference<Action<T>>(code));

            return key;
        }

        /// <summary>
        /// Add a subscriber to the collection but ensure its (currently) first in the queued to be called.
        /// This pushes ahead of all other subscriptions.  Very specialised case!  Otherwise don't use this!
        /// You are returned a KEY you may later use to more easily <see cref="UnSubscribe(int)"/> from 
        /// should you need to.
        /// </summary>
        [DebuggerStepThrough]
        public int PrioritySubscribe(Action<T> code)
        {
            var key = ++NextKey;
            var currentDictionaryValues = subscribers;

            // jiggle dictionary so new code is first item in collection.
            subscribers = new Dictionary<int, WeakReference<Action<T>>>();
            subscribers.Add(key, new WeakReference<Action<T>>(code));

            foreach (var oldKey in currentDictionaryValues.Keys)
                subscribers.Add(oldKey, currentDictionaryValues[oldKey]);

            return key;
        }

        /// <summary>
        /// The quick method of unscubscribing from an event which means you need to provide the KEY
        /// you would have been given when you originally subscribed.
        /// <para>If you no longer have the key, its still possible to unsubscribe by Action using 
        /// <see cref="UnSubscribe(Action{T})"/> however, that removed all keys that call the same code.</para>
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(int key) =>
            subscribers.Remove(key);

        /// <summary>
        /// Looks through all subscriptions looking for any keys internally that relate to the code you provide.
        /// Therefore, if you know more than one subscription calls the same code, then more than one KEY will
        /// be removed from the internal collection of subscriptions.
        /// Ideally, you should avoid using this method, and instead use <see cref="UnSubscribe(int)"/>.
        /// </summary>
        [DebuggerStepThrough]
        public bool UnSubscribe(Action<T> code)
        {
            bool removed = false;

            subscribers.GetAllLiveEntries().Each(entry =>
            {
                //subscribers.Remove(entry.Key);
                //removed = true;

                if (entry.Value.TryGetTarget(out var target))
                {
                    if (target == code)
                    {
                        subscribers.Remove(entry.Key);
                        removed = true;
                    }
                }
            });

            return removed;
        }

        /// <summary>
        /// cause all subscribers to hear about this event. In queued order.
        /// </summary>
        [DebuggerStepThrough]
        public void FireEvent(T value)
        {
            var result = subscribers.FilterSubscriptions(codeWrapper: eventHandler => invokeThe(eventHandler));

            // add loyal subscribers before the one-offs, just because we like them :-)
            QueuedActions.AddRange(result.LiveSubscriptions);

            var deadSubscriptions = result.DeadSubscriptionIDs;

            // cleanup dead subscribers
            if (deadSubscriptions.Count > 0)
                deadSubscriptions.Each(deadSubscription => subscribers.Remove(deadSubscription));

            // now deal with one-off subscriptions .
            var filteredOneOffSubscriptions = oneOffSubscribers.FilterSubscriptions(codeWrapper: eventHandler => invokeThe(eventHandler));
            if (filteredOneOffSubscriptions.Count > 0)
                QueuedActions.AddRange(filteredOneOffSubscriptions);

            // clear one-offs, 
            oneOffSubscribers.Clear();

            // create an action to capture and 
            Action invokeThe(Action<T> handler) => () =>
            {
                handler.Invoke(value);
            };
        }

        /// <summary>
        /// Subscribe to the event, where after the next time it fires, your code will be automatically
        /// unsubscribed.  So your code will at most be called once only.
        /// </summary>
        [DebuggerStepThrough]
        public void SubscribeOnce(Action<T> code) =>
            oneOffSubscribers.Add(new WeakReference<Action<T>>(code));
    }

    internal static class SimpleQueuedEventHelperExtensions
    {
        /// <summary>
        /// returns a filtered collection, but you'll want to consider only keeping weak references to the actions returned.
        /// </summary>
        internal static FilterResult FilterSubscriptions<T>(this Dictionary<int, WeakReference<Action<T>>> subscriptions, Func<Action<T>, Action> codeWrapper)
        {
            var deadSubscriptions = new List<int>();
            var liveSubscriptions = new List<Action>();

            subscriptions.Each(sub =>
            {
                if (sub.Value.TryGetTarget(out var code) && code != null)
                    liveSubscriptions.Add(() =>
                    {
                        var codeToExecute = codeWrapper(code);
                        codeToExecute.Invoke();
                    });
                else
                    deadSubscriptions.Add(sub.Key);
            });

            return new FilterResult(deadSubscriptions, liveSubscriptions);
        }

        /// <summary>
        /// returns a filtered <see cref="List{Action}"/>, but you'll want to consider only keeping weak references to the actions returned.
        /// </summary>
        internal static List<Action> FilterSubscriptions<T>(this List<WeakReference<Action<T>>> subscriptions, Func<Action<T>, Action> codeWrapper)
        {
            var liveSubscriptions = new List<Action>();

            subscriptions.Each(sub =>
            {
                if (sub.TryGetTarget(out var code) && code != null)
                    liveSubscriptions.Add(() =>
                    {
                        var codeToExecute = codeWrapper(code);
                        codeToExecute.Invoke();
                    });
            });

            return liveSubscriptions;
        }
    }
}
