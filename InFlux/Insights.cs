namespace InFlux
{
    /// <summary>
    /// In an effort to limit who can change the state information in the intent,
    /// whilst leaving the intent itself very publicly visible, the implementation of this
    /// interface provides access methods that will change the state on demand.
    /// Hence, this is considered 'managing' state - hence refering to an instance as a 'manager'.
    /// </summary>
    public interface IOwnInsight
    {
        /// <summary>
        /// Provide access to the hashcode value of the original (or reset) property value
        /// this Insight refers to.
        /// </summary>
        int OriginalValueHash { get; }

        /// <summary>
        /// force the state in the Insight to become what ever you set it to.
        /// </summary>
        public void SetIsTouched(bool newState);

        /// <summary>
        /// force the state in the Insight to become what ever you set it to.
        /// </summary>
        public void SetIsDirty(bool newState);
    }

    /// <summary>
    /// Allows you to provide an instance of an Intent Processor once (could come from DI)
    /// which you can then use this factory to create all instances of an Insight.
    /// This guarantees all insights will refer to the same instance of the Intent Processor.
    /// </summary>
    public class InsightsFactory
    {
        /// <summary>
        /// Provide (through DI?) an instance of an Intent Processor which will be shared by
        /// all instances of an <see cref="Insights{T}"/> that this factory instance creates.
        /// </summary>
        public InsightsFactory(IntentProcessor intentProcessor)
        {
            this.intentProcessor = intentProcessor;
        }

        readonly IntentProcessor intentProcessor;

        /// <summary>
        /// Make an instance of an Insight based on this factory's intent processor.
        /// NOTE: Also returns an <see cref="IOwnInsight"/> manager instance for you
        /// to control the state.
        /// </summary>
        public (Insights<T> insight, IOwnInsight manager) Make<T>(T startValue)
        {
            var instance = new Insights<T>(this.intentProcessor, startValue);
            var manager = new InsightsManager<T>(instance);

            return (instance, manager);
        }
    }

    internal class InsightsManager<T> : IOwnInsight
    {
        internal InsightsManager(Insights<T> insights)
        {
            this.insights = insights;
        }

        private readonly Insights<T> insights;

        public int OriginalValueHash => this.insights.OriginalValueHash;

        public void SetIsTouched(bool newState)
        {
            this.insights.IsTouched = newState;
        }

        public void SetIsDirty(bool newState)
        {
            this.insights.IsDirty = newState;
        }
    }

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

            this.OriginalValueHash = PreviousValue?.GetHashCode() ?? 0;
        }
    }
}
