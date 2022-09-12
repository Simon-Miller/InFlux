using System;

namespace InFlux
{
    /// <summary>
    /// Represents the payload of an event, and a callback that a subscriber must call when they're finished 
    /// processing their own code.  The concept is for a ChainLink event to know not only that it called back to
    /// all the subscribers, but the code executed by the subscribers is also considered complete.
    /// It then can fire another kind of event signalling this event has completed.
    /// <para>Therefore, within the context of Firing an event that uses the chain concept, a developer needs to
    /// be conscious of when they consider their code be complete.  If they call other events, should they
    /// also wait for a response from all their subscribers before deeming their own code complete, and thus
    /// calling the <see cref="CallbackWhenDone"/> action - signalling to the creator of this instance of 
    /// a <see cref="ChainLink{T}"/> that their code is completed.</para>
    /// </summary>
    public class ChainLink<T>
    {
        internal ChainLink(T payload, Action callbackWhenDone)
        {
            this.Payload = payload;
            this.CallbackWhenDone = callbackWhenDone;
        }

        /// <summary>
        /// data being passed around by an event.
        /// </summary>
        public readonly T Payload;
        
        /// <summary>
        /// Action you need to call when you are sure all your event listeners have processed your own events that may be raised during
        /// the processing of your event handler for this event.
        /// </summary>
        public readonly Action CallbackWhenDone;
    }
}
