namespace InFlux
{
    /// <summary>
    /// Non-generic version of delegate representing a change in value from an <paramref name="oldValue" />
    /// to  <paramref name="newValue" />
    /// </summary>
    public delegate void ValueChangedResponse(object oldValue, object newValue);

    /// <summary>
    /// generic version of delegate representing a change in value from an <paramref name="oldValue"/>
    /// to  <paramref name="newValue" />
    /// </summary>
    public delegate void ValueChangedResponse<T>(T oldValue, T newValue);

    /// <summary>
    /// Represents a property who's value type is <typeparamref name="T"/>.
    /// This property contains an event that is fired when the <see cref="Value"/> of this property changes.
    /// You are free to subscribe to <see cref="ValueChanged"/> which is a queued event.
    /// </summary>
    public class QueuedEventProperty<T> : QueuedEventPropertyBase
    {
        /// <summary>
        /// Define an initial starting value for this property, and behaviour with relation to when
        /// events are fired.  Often it is preferable to only fire the <see cref="ValueChanged"/> event
        /// when the new value differs from the current value.
        /// </summary>
        public QueuedEventProperty(T initialValue, bool onlyFireOnValueChanges = false) 
            : base()
        {
            this.value = initialValue;
            this.onlyFireOnValueChanges = onlyFireOnValueChanges;
        }

        private readonly bool onlyFireOnValueChanges;

        /// <summary>
        /// When ever <see cref="Value"/> is set, and conforms to expected event behaviour,
        /// this event fires in a predicatable queued fashion.
        /// </summary>
        public readonly QueuedEvent<T> ValueChanged = new QueuedEvent<T>();

        private T value;

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
                    this.ValueChanged.FireEvent(currentValue, value);
                    base.ValueChangedNotification.FireEvent();
                }
            }
        }

        // used internally for auto-wire-up (EventsEntityBase).
        internal override void OnValueChanged(ValueChangedResponse code) =>
            this.ValueChanged.Subscribe((O, N) => code(O, N));

        /// <summary>
        /// Implicity extract the value of a QueuedEventProperty to the underlying type.
        /// </summary>
        public static implicit operator T(QueuedEventProperty<T> source) => source.Value;
    }
}
