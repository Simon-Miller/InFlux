using System.Diagnostics;

namespace InFlux
{
    /// <summary>
    /// Gives you a really flexible and reliable property, where changes
    /// are chained together in an <see cref="EventChain{T}"/>.  Because of that,
    /// You can either use the <see cref="SetValue(T, Action)"/> method and get a callback
    /// when complete, or you can hook into the <see cref="ValueChangedEventCompleted"/> event.
    /// <para>So long as you wait for the response, all event processing in the chain
    /// should be coplete.</para>
    /// </summary>
    public class EventChainProperty<T>
    {
        public EventChainProperty(T? startValue = default, bool onlyFireOnValueChanges = true)
        {
            this.value = (startValue is not null) ? startValue : default!;
        }

        private readonly bool onlyFireOnValueChanges;
        private T value;

        public readonly EventChain<(T oldValue, T newValue)> ValueChanged = new();

        /// <summary>
        /// Fires then the event chain determines it has completed processing.
        /// </summary>
        public readonly QueuedEvent ValueChangedEventCompleted = new();

        /// <summary>
        /// setter of this property causes <see cref="ValueChanged"/> to fire based on your
        /// configuration in the constructor.
        /// </summary>
        public T Value
        {
            get => value;
            set
            {
                var currentValue = this.value;
                if (onlyFireOnValueChanges == false || object.Equals(currentValue, value) == false)
                {
                    this.value = value;

                    this.ValueChanged.FireEvent((currentValue, value), () =>
                    {
                        this.ValueChangedEventCompleted?.FireEvent();
                    });
                }
            }
        }

        /// <summary>
        /// So long as there's no problems with your event chain, this will fire off subscriptions
        /// in a predictable order, and callback your code when the event is considered complete.
        /// </summary>
        [DebuggerStepThrough]
        public void SetValue(T value, Action callbackWhenEventProcessingCompletes)
        {
            this.ValueChangedEventCompleted.SubscribeOnce(callbackWhenEventProcessingCompletes);
            this.Value = value;
        }
    }
}
