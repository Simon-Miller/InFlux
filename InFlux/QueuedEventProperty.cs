using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InFlux
{
    /// <summary>
    /// Represents a property who's value type is <see cref="T"/>.
    /// This property contains an event that is fired when the <see cref="Value"/> of this property changes.
    /// You are free to subscribe to <see cref="ValueChanged"/> which is a queued event.
    /// </summary>
    public class QueuedEventProperty<T>
    {
        /// <summary>
        /// Define an initial starting value for this property, and behaviour with relation to when
        /// events are fired.  Often it is preferable to only fire the <see cref="ValueChanged"/> event
        /// when the new value differs from the current value.
        /// </summary>
        public QueuedEventProperty(T initialValue, bool onlyFireOnValueChanges = false)
        {
            this.value = initialValue;
            this.onlyFireOnValueChanges = onlyFireOnValueChanges;
        }

        private readonly bool onlyFireOnValueChanges;

        public readonly QueuedEvent<(T oldValue,T newValue)> ValueChanged = new();

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
                if(onlyFireOnValueChanges == false || object.Equals(currentValue, value) == false)
                {
                    this.value = value;
                    this.ValueChanged.FireEvent((currentValue, value));
                }
            }
        }

        public static implicit operator T (QueuedEventProperty<T> source) => source.Value;
    }
}
