# Influx: AutoWireup

```
[AutoWireup]
public partial class testClass
{
    [Required]
    [Range(minimum:16, maximum:21, ErrorMessage ="Range must be between 16 and 21")]
    int age; // ** all generated code seeded from THIS field, once project builds. **
}
```

The ```AutoWireUp``` attribute, when applied to a class, can generate properties for you.
That may not sounds very useful, until you understand that when property values change,
an event is triggered.

That might sound more useful if you're looking for a reactive entity without the boilerplate!
You might also be thinking that if you wanted an event to fire when ever a property changes,
that you'd need to wire up an event listener to every property.
But you don't!

```
// ** AUTO GENERATED CODE: Last generated: 04/11/2022 09:46:42 **
// ***********************

using InFlux;
using InFlux.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

namespace InFluxTests 
{
	public partial class testClass
	{
		public testClass()
		{
			AgeInsight = new(() => age, value => age = value);
			AgeInsight.ValueChangedNotification.Subscribe(() => OnEntityChanged.FireEvent());
		}

		public readonly QueuedEvent OnEntityChanged = new();

		[Required]
		[Range(minimum:16, maximum:21, ErrorMessage ="Range must be between 16 and 21")]
		public int Age {get => AgeInsight.Value; set => AgeInsight.Value = value; }
		public readonly QueuedEventPropertyIndirect<int> AgeInsight;
	}
}
```

### GOTCHA!
Ensure your class is **partial**.  Otherwise you'll get build errors.

Every FIELD encountered will have its first letter CAPITALISED as a way of generating
the name for the property.

#### Attributes
Thankfully DataAnnotation attributes can be placed on fields too.
The idea being that you make private fields (lower case) and this will generate public
properties with an upper-cased first letter.  
- The attributes are duplicated. 
- The ```set``` code block of the property will check for changes, and fire an event to say so.
- The constructor generated will pick up on this, and fire off the generic ```OnEntityChanged```
event too.  

#### What if I don't want a property generated?

```
[AutoWireupIgnore]
int ignoreMe;
```

Need I say more?

### The BIG picture

In an MVC or Blazor style of application, you may come to depend on DataAnnotations for
model validation.  Especially where you use components (Blazor) and point to the property
being represented.  As such, the data annotations need to be on a simple property.
We can therefore only hide complexity in the setter of the property.

### How to access property Events?

Each property generated has a buddy property (**insights**) named the same as the property but
with "**Insight**" appended, such as "**IdInsight**".

This property leverages a special version of the ```QueuedEventProperty``` to provide you
with methods to subscribe to the ```ValueChanged``` event either once or indefinitely, as
well as being able to unsubscribe from the event.

Perhaps you're beginning to see there's quite a lot of consideration around interactions with 
such an entity, and its properties, and why leveraging auto-generated code will mean you
won't need to test the behaviour, or else you're testing Influx's AutoWireup!

Whilst I fully acknowledge that this generator is far from fully complete, its certainly
in a state I'm personally happy to make use of it, and hopefully you will too.

If you've any feedback, you can contact me through the Nuget package page: 
https://www.nuget.org/packages/InFlux
or via the GitHub project issues: 
https://github.com/Simon-Miller/InFlux/issues


**&hyphen; Simon Miller &hyphen;**
