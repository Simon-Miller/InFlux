namespace InFlux
{
    internal static class Common
    {
        /// <summary>
        /// reduces dictionary to an array of type <see cref="Action"/>, 
        /// where the weak references are checked to have instances of code referenced,
        /// before being added to the result set returned.
        /// </summary>
        internal static IEnumerable<T> GetAllLiveActions<T>(this Dictionary<int, WeakReference<T>> dictionary)
            where T : class
        {
            var output = new List<T>();

            foreach (var entry in dictionary)
                if (entry.Value.TryGetTarget(out T? value) && value != null)
                    output.Add(value);

            return output;
        }

        /// <summary>
        /// reduces a list to an array of type <see cref="Action"/>,
        /// where the weak references are checked to have instances of code referenced,
        /// before being added to the result set returned.
        /// </summary>
        internal static IEnumerable<Action> GetAllLiveActions(this IEnumerable<WeakReference<Action>> collection)
        {
            var output = new List<Action>();

            foreach (var reference in collection)
                if (reference.TryGetTarget(out Action? code) && code is not null)
                    output.Add(code);

            return output;
        }

        /// <summary>
        /// reduces dictionary to an array of type <see cref="WeakReference{T}"/> that only contains live referenced objects.
        /// </summary>
        internal static IEnumerable<KeyValuePair<int, WeakReference<T>>> GetAllLiveEntries<T>(this Dictionary<int, WeakReference<T>> dictionary)
            where T : class =>
                dictionary.Where(entry=> entry.Value.TryGetTarget(out T? _));
      
        /// <summary>
        /// returns a filtered collection, but you'll want to consider only keeping weak references to the actions returned.
        /// </summary>
        internal static FilterResult FilterSubscriptions<T>(this Dictionary<int, WeakReference<ValueChangedResponse<T>>> subscriptions, Func<ValueChangedResponse<T>, Action> codeWrapper)
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
        internal static List<Action> FilterSubscriptions<T>(this List<WeakReference<ValueChangedResponse<T>>>subscriptions, Func<ValueChangedResponse<T>, Action> codeWrapper)
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

    internal record struct FilterResult(List<int> DeadSubscriptionIDs, List<Action> LiveSubscriptions);
}
