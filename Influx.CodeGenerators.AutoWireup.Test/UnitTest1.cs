using InFlux;
using InFlux.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Influx.CodeGenerators.AutoWireup.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arrange

            // It is likely that there is only one IntentProcessor for the GUI.
            // It should be shared by all models that need to ask for permission to change a value.
            bool askedForPermission = false;
            var askUserForPermission = new SimpleQueuedEvent<Action<bool>>();
            askUserForPermission.Subscribe(callback => 
            {
                askedForPermission = true;
                callback(true); 
            });

            // above won't be called unless a moderator says permission needs to be asked for.
            var intentProcessor = new IntentProcessor(askUserForPermission);
            intentProcessor.SubscribeToIntentProcess<int?>(nameof(Testable), nameof(Testable.Value), intent => 
            {
                if (intent.NewValue == 123)
                    intent.AskUserForPermission();
            });


            var target = new Testable(intentProcessor);

            var informed = false;

            // look, Mum!  I can get events on fields, now!...  yes, Son, - that's nice. (without a clue on what was just said)
            target.ValueInsights.OnValueChanged.Subscribe((O, N) => { informed = true; });

            // Act

            target.TrySetValue(newValue: 123); // ignoring possibility of executing code based on change in value being allowed.

            // Assert
            Assert.IsTrue(informed);
            Assert.IsTrue(askedForPermission);
            Assert.AreEqual(123, target.Value);
        }
    }

    // GOTCHA!  don't forget it MUST be partial.
    [AutoWireup]
    partial class Testable
    {
        [Required]
        int? value;
    }
}