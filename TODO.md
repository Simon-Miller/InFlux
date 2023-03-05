# Grumbling, I have decided we need to make the T4 template into a source generator.

I've commented out the original source generator for the moment,
so it can't get in the way of testing.  So for the moment, its going
to use the same **AutoWireup** attribute.

But for backwards compatibility, we want to use a different attribute?
Or add a parameter to the attribute?


## DONE:
I somehow got my attention diverted, and refactored all the code generation
into many smaller more easily understood methods.
This means the original code should now be easier to maintain.

## TODO:
- Need to test the refactored code!
- Need to cleanup, and commit that.  It won't be necessary to release it.
- Need to START on the T4 template conversion.
- 