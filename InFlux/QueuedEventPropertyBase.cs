namespace InFlux
{
    public abstract class QueuedEventPropertyBase : IQueuedEventProperty
    {
        /// <summary>
        /// When the property value changes, this non generic notification is fired.
        /// </summary>
        public QueuedEvent ValueChangedNotification { get; init; } = new();

        /// <summary>
        /// Allows a QueuedEventsEntity to listen in on children.
        /// Also fires the <see cref="ValueChangedNotification"/> event.
        /// </summary>
        internal virtual void OnValueChanged(ValueChangedResponse code)
        {
            this.ValueChangedNotification?.FireEvent();
        }
    }
}
