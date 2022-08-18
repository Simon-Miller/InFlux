namespace InFlux
{
    /// <summary>
    /// provides the basic event notification of a value changing, without you knowing (or caring?) about that value.
    /// </summary>
    public interface IQueuedEventProperty
    {
        /// <summary>
        /// When the property value changes, this non generic notification is fired.
        /// </summary>
        QueuedEvent ValueChangedNotification { get; }
    }
}
