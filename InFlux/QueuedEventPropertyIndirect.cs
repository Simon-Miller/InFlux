using System;

namespace InFlux
{
    /// <summary>
    /// Developed for the [AutoWireup] attribute, but you can use it outside of this assembly.
    /// However, you will likely find <see cref="QueuedEventProperty{T}"/> is what you need.
    /// </summary>
    public class QueuedEventPropertyIndirect<T> : QueuedEventPropertyBase
    {
        /// <summary>
        /// Developed for the [AutoWireup] attribute, but you can use it outside of this assembly.
        /// However, you will likely find <see cref="QueuedEventProperty{T}"/> is what you need.
        /// </summary>
        /// <param name="getField">code returning a value of type <typeparamref name="T"/>.</param>
        /// <param name="setField">code setting your own field or property of type <typeparamref name="T"/>.</param>
        public QueuedEventPropertyIndirect(Func<T> getField, Action<T> setField)
            : base()
        {
            this.getField = getField;
            this.setField = setField;
        }

        private readonly Func<T> getField;
        private readonly Action<T> setField;

        /// <summary>
        /// By default (TRUE) will cause the <see cref="ValueChanged"/> event to fire only when
        /// the current value is considered different from the new value you are trying to set it to.
        /// If you want this event to fire every time, then set this field's value to FALSE.
        /// </summary>
        public bool OnlyFireOnValueChanges = true;

        /// <summary>
        /// When ever <see cref="Value"/> is set, and conforms to expected event behaviour,
        /// this event fires in a predicatable queued fashion.
        /// </summary>
        public readonly QueuedEvent<T> ValueChanged = new QueuedEvent<T>();

        /// <summary>
        /// setter of this property causes <see cref="ValueChanged"/> to fire based on your
        /// configuration in the constructor.
        /// </summary>
        public T Value
        {
            get => getField();
            set
            {
                var currentValue = getField();
                if (OnlyFireOnValueChanges == false || object.Equals(currentValue, value) == false)
                {
                    setField(value);
                    ValueChanged.FireEvent(currentValue, value);
                    base.ValueChangedNotification.FireEvent();
                }
            }
        }

        // used internally for auto-wire-up (EventsEntityBase).
        internal override void OnValueChanged(ValueChangedResponse code) =>
            ValueChanged.Subscribe((O, N) => code(O, N));
    }
}
