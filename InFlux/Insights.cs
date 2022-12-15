namespace InFlux
{
    /// <summary>
    /// Indirect metadata about a property associated with this instance.  Likey similarly named.
    /// </summary>
    public class Insights<T>
    {
        /// <summary>
        /// To construct an Insights instance, you need to provide the <paramref name="intent"/> and a starting value
        /// from which any further metadata can be derived.
        /// </summary>
        public Insights(IntentProcessor intent, T startValue)
        {
            Intent = intent;
            OriginalValueHash = startValue?.GetHashCode() ?? 0;
        }

        internal T PreviousValue = default;
        internal int OriginalValueHash;

        /// <summary>
        /// Indicates if the target property's value ever changes.  It it does, it's considered to 
        /// be Touched upon by the user or system.  Can be reset by calling <see cref="ResetToPristine"/>
        /// </summary>
        public bool IsTouched { get; internal set; } = false;

        /// <summary>
        /// Indicates is the current value for the target property differs from the original value.
        /// Its possible for the value to change, and then change back to the original value, in which case
        /// this property would consider the current value to be 'clean', where as any change from the 
        /// original value implies it is not 'dirty' as a result of the change.  This can be reset by
        /// calling <see cref="ResetToPristine"/>
        /// </summary>
        public bool IsDirty { get; internal set; } = false;

        /// <summary>
        /// If you want to be informed when the target property's value changes, this is the event for you!
        /// </summary>
        public QueuedEvent<T> OnValueChanged = new QueuedEvent<T>();

        /// <summary>
        /// Fires when the anticipated change to the value was cancelled by the user via an Intent.
        /// </summary>
        public QueuedEvent<T> OnValueUnChanged = new QueuedEvent<T>();

        internal readonly IntentProcessor Intent;

        /// <summary>
        /// You might want to cause this Insight to reset after performing an action such as saving data,
        /// where the current data should now be considered a source of truth.  This means the target property's
        /// value will be considered both untouched, and clean. <see cref="IsTouched"/> will report FALSE,
        /// and <see cref="IsDirty"/> will also report FALSE.
        /// </summary>
        public void ResetToPristine()
        {
            IsTouched = false;
            IsDirty = false;
        }
    }
}
