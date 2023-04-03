using System;

namespace InFlux.Attributes
{
    /// <summary>
    /// Similar to <see cref="AutoWireupAttribute"/>.  
    /// However, this version requires an <see cref="IntentProcessor"/> injected in.
    /// This means you'll likely want to use a factory to create instances of your entities?
    /// (Something else I can put into InFlux ?)
    /// <para>
    /// With an IntentProcessor common to all your entities, it could lead to a sinle UI feature
    /// that asks the user for permission to make a change (if blocked by moderating code).
    /// This is an experimental idea, as I've found adding an intent process to existing code to be
    /// painful, and am trying to find an easy solution for the future!
    /// I'd love some feedback on this.
    /// </para>
    /// <para>
    /// This attribute was introduced with the release of <a href="https://www.nuget.org/packages/Influx.CodeGenerators.AutoWireup/"/> version 1.1.0
    /// </para>
    /// </summary>
    public class AutoWireupWithIntentAttribute : Attribute
    {
    }
}
