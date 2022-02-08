namespace InFlux
{
    /// <summary>
    /// A type used during debugging of events.
    /// It represents an entry in a list corresponding to all subscriptions being called.
    /// As the event hears back from subscribers about their chain completing, then the 
    /// corresponding Debug row is removed.  Therefore, if you find an instance or more
    /// of this class exists in an <see cref="EventChain{T}"/> when you believe it should
    /// be complete, then this row offers you an <see cref="Action"/> you can call,
    /// which will take you to the subscription code that did not respond.
    /// <para>How's that for a nice developer experience?</para>
    /// </summary>
    /// <param name="Id">unique identifier of a row of type <see cref="DebugSubscription"/></param>
    /// <param name="IsOneOff">Indicates if the <see cref="callSubscription"/> came from 
    /// the SubscribeOnce collection, or the more permanent Subscribe collection of event listeners.</param>
    /// <param name="callSubscription">Generated code that when called, should lead back to your code
    /// registered as a listener to the event.</param>
    public record class DebugSubscription(int Id, bool IsOneOff, Action callSubscription);
}
