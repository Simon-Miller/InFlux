namespace InFlux
{
    /// <summary>
    /// provides the basic event notification of a value changing, without you knowing (or caring?) about that value.
    /// </summary>
    public abstract class QueuedEventPropertyBase : IQueuedEventProperty
    {
        /// <summary>
        /// When the property value changes, this non generic notification is fired.
        /// </summary>
        public QueuedEvent ValueChangedNotification { get; private set; } = new QueuedEvent();

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
