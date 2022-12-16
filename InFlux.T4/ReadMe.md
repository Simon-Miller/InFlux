# T4 Template Generated code

_(16th December 2022)_

The provided template needs its "properties" changes from "content" to "none".
This will be enough to allow you to edit the file!

Open the T4 template file "ModelAdditionalCode.tt".

```
// PLEASE FILL THIS IN:
// --------------------
var RELATIVE_PATH_FROM_THIS_FILE = @"\TestModels\";
// ----------------------------------
```

It wants to point to a folder in which any model you want to enhance lives.
The default folder name ```\TestModels\``` is unlikely what you need!

Its a good idea to create a folder to isolate your partial classes that will be
enhanced, away from other code where the code generator can't work on more things
than you intend it to.

- T4 templates can trigger code generation from visual studio when ever you save a change.
- Secondly, you can right-click on a "*.tt" file and choose the "Run Custom Tool" option.

Code generation assumes you've already got InFlux as a project NuGet dependency.
If not, you can get it here:
<a href="https://www.nuget.org/packages/InFlux/">InFlux on NuGet</a>

## future?

I don't know if I'll try to improve upon this with a code generator that integrated with
Visual Studio.  I have done that once already, and in my opinion, is not as easy to do,
and perhaps is less maintainable.  Then again, maybe that's because I've spend 12 years
occassionally playing with T4 templates. 
I do like the convenience that the new code generators bring.  
Perhaps some day?

