namespace InFlux
{
    public abstract class QueuedEventPropertyBase
    {
        /// <summary>
        /// allows a QueuedEventsEntity to listen in on children.
        /// </summary>
        internal abstract void OnValueChanged(ValueChangedResponse code);
    }
}
