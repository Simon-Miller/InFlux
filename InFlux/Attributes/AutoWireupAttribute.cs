using System;
using static System.Net.WebRequestMethods;

namespace InFlux.Attributes
{
    /// <summary>
    /// By applying this attribute, you will invoke a C# Code Generator (analyzer)
    /// from <a href="https://www.nuget.org/packages/Influx.CodeGenerators.AutoWireup"/>.
    /// It will create a partial class (so yours MUST BE too) with properties derived from
    /// fields you define in the class on which this attribute sits.
    /// It works best when your field names start with lower-case, as it will generate 
    /// a first-letter capitalized property.  Each property has the ability to fire an 
    /// event each time you write to it.  Secondly, if your fields have attributes, these
    /// will be copied to the public property created!
    /// <para>
    /// Put simply, consider you provide backing fields in a partial class, along with any
    /// attributes you might like, suck as model validation attributes, and then let the
    /// code-generator build you an observable class, with observable properties which 
    /// retain the attributes.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AutoWireupAttribute : Attribute
    {
    }
}
