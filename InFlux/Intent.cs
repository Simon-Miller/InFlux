namespace InFlux
{
    /// <summary>
    /// Represents the intent to change a value.
    /// The Intent Processor allows any subscribers to the intent,
    /// to participate in a find of 'validation' process in which you may need
    /// permission from the user to make the change.  If nothing listening to
    /// the intent thinks the user needs be be asked, (no unsaved changes?)
    /// then the intended change can be executed by the Intent Processor.
    /// <para>
    /// This object is intended to be passed around via the subscribed event,
    /// giving code change to call the <see cref="AskUserForPermission"/> methor,
    /// OR NOT.  
    /// </para>
    /// </summary>
    public class Intent<T>
    {
        /// <summary>
        /// construct an externally immutable instance containing the values provided.
        /// </summary>
        public Intent(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// The current value that may be lost if the intended change goes ahead
        /// </summary>
        public T OldValue { get; private set; }

        /// <summary>
        /// The new value that will replace the <see cref="OldValue"/> in the target object,
        /// should there be no objection from any subscribers to the Intent process, or
        /// from the user.
        /// </summary>
        public T NewValue { get; private set; }

        internal bool PermissionNeededForChange { get; private set; }

        /// <summary>
        /// If the intended change could cause the user to lose data (unsaved changes?)
        /// Then callis this will set an internal state that the Intent Processor will respond to,
        /// and in turn fire off a application level event that if implemented by the UI,
        /// will trigger a request from the user to verify that they are ok with losing any
        /// unsaved changes.  If they are not, they should have the option to cancel.
        /// The user response should feed back into the Intent Processor, and based on the user
        /// agreeing, the code that performs the intended change is executed OR NOT.
        /// </summary>
        public void AskUserForPermission()
        {
            PermissionNeededForChange = true;
        }

        /// <summary>
        /// Compares this instance to other object using VALUES.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as Intent<T>;
            if (other is null) return false;

            if (OldValue.Equals(other.OldValue) == false) return false;
            if (NewValue.Equals(other.NewValue) == false) return false;

            return true;
        }

        /// <summary>
        /// Generate a hashcode to represent this instance's values.
        /// </summary>
        public override int GetHashCode() =>
            OldValue.GetHashCode() ^ NewValue.GetHashCode();
    }
}
