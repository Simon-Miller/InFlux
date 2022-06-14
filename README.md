# InFlux

Ever wonder why things are so difficult, when in your mind they seem to be so easy?
Take the very idea of an event.  Something happens, and you're informed of it!  Sounds so simple!  What could possibly go wrong?
Well, events can trigger other events.  You updated, and shouted about it, and not I'm updating and shouting about it, 
and perhaps something else will update as a result, and shout about it!

In your mind its so simple! There's a definite sequence.  But you're about to have your world shattered:
Think of a simple scenario - a hotel, a fire department, and a reporter.  You get to be the arsonist!
The hotel kitchen is on fire!  The hotel informs the fire department, which dutifully somes to put the fire out.
The reporter in your mind has an informant at the hotel that phones him up to say the place is gonna burn down!
He reports a news flash! But then he also hears about the fire brigade, so another news flash "Fire fighters fight fire, feroucioucly!"

If you have a class "Hotel"
if you have a class "FireBridade"
if you have a class "Reporter"
if you assume the event of the "BuildingOnFire" belongs to the "Hotel"
if you assume the "FireBrigade" have an event for "PutsOutFire"
If you assume the wire-up is the "FireBrigade" listens to the "BuildingOnFire" event
if you assume the "Reporter" listens to the "BuildingOnFire" event
if you assume the "Reporter" listens to the "PutsOutFire" event too, we're all set.

If you did things in this order, you'll find the reporter will report that on "PutsOutFire BEFORE reporing on "BuildingOnFire"!!
Yet the events will fire in the order of the building on fire, and the fire brigade will put the fire out.
**What went wrong?**

The issue is, that processing events is a recursive bit of code written by the compiler.  It calls each subscriber to an event in turn.
The **call**  part is the issue.  This means executing code, and in the processing of that code, other events may be "fired".
Therefore, before the first event is fully processed, other events begin to trigger.
This doesn't always cause issues.  But this is the simplest example I can think of that illustrates the problem.

So if we could have a single chain of "calls", which when processing an event, a list of all the calls (to subscribers) is added to a single list,
and if all events were processed by this single list, in the same way - we've more control over the order in which event handling occurs.

But it doesn't end there.  In a synchronous world, where code is so much simpler, and the order of things is predictable, you'd be ok.
So what about the long sdtanding asynchronous code?  How do we ever really know when that's finished?
Unless we expect event handlers to respond not just when they're complete, but when their children are complete too, we don't stand a chance!
This in the world of InFlux is the reals of a "chain event".

Beyond this, I tend to find the most common need for events, is knowing when a property or an entity has changed.  
This kind of thinking is great for MVVM, where we can update the UI in response to a model change, but conversely, update a model from a UI change.
Then anyone who cares to hear about that change can respond to it.  Most commonly, for realtime validation purposes.
These validations can determine in a model is valid.  There could be many models on a complex page, and something might listen for all the models validations?
This could enable a navigation button on screen, as an example.

All this stems from a model you can change, and it shouts about it!
But do you really want the pain of wiring up every property to some event?
Do you want the bigger pain of ensuring you're listening to EVERY property event in order to have a more general "Model changed" event?

That's what this library is about!  Solving these issues.
There's a number of unit tests you can investigate to get a better sense of how it all works.

I've decided there's enough to truly open the project up now, as I'd released it on NuGet but not made the code repository public.
I Hope this helps people make an easier to manage universe for their code to live in.
