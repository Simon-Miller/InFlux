using InFlux.Attributes;

namespace InFlux
{
    /// <summary>
    /// Represents one of your class models with the <see cref="AutoWireupAttribute"/> applied.
    /// <para>This interface cannot represent all the generated properties, but the one thing
    /// they all have in common: <see cref="OnEntityChanged"/>.</para>
    /// <para>This is a useful hook for other concerns such as general validation, page state,
    /// or triggering checks for dirty properties.</para>
    /// </summary>
    public interface IAutoWireup
    {
        /// <summary>
        /// An eveny that is triggered when ever a property value changes on this model.
        /// </summary>
        QueuedEvent OnEntityChanged { get; }
    }
}
