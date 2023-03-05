using Moq;

namespace InFluxTests
{
    [TestClass]
    public class IntentProcessorTests
    {
        public interface IAleModel
        {
            IAleShared sharedEvents { get; }
        }

        public interface IAleShared
        {
            SimpleQueuedEvent<Action<bool>> AskUserForPermission { get; }
        }

        [TestMethod]
        public void Can_moderate_intent_and_user_reject_change()
        {
            // ARRANGE
            var askUserForPermission = new SimpleQueuedEvent<Action<bool>>();

            var faker = new Mock<IAleModel>();
            faker.Setup(m => m.sharedEvents.AskUserForPermission).Returns(askUserForPermission);

            // NOTE: THIS IS SUPER IMPORTANT TO UNDERSTAND! - you're effectively passing through the desire to ask for permission
            // from the intent process to the user (via an ALE) which itself is required to be able to take an Action<bool>
            // as a parameter, with the idea that when the user responds, that this very action will be called with true/false from
            // the user.  In effect, the Intent processor will hear back, and can react appropriately to the user response.

            var intentProcessor = new IntentProcessor(faker.Object.sharedEvents.AskUserForPermission);

            // this works too.
            //var intentProcessor = new IntentProcessor(callback => faker.Object.sharedEvents.AskUserForPermission.FireEvent(callback));

            // ----------
            // CHANGE ME!
            // ----------
            bool detailsHaveBeenModified = true;

            // checked in Asserts
            string message = "not modified";

            // components or code that want to get involved in the chain of moderators for a given intent,
            // need to register their interest.
            intentProcessor.SubscribeToIntentProcess<int>(
                callerType: "DetailsComponent",
                callerName: "ManageExamsScreen",
                intent =>
                {
                    // block changes to the selected exam from the drop-down list, if the current details have been modified.
                    if (intent.OldValue != intent.NewValue)
                        if (detailsHaveBeenModified == true)
                            intent.AskUserForPermission();
                });

            // fake a response from a blazor UI interaction with the user.
            askUserForPermission.Subscribe(callback =>
            {
                // UI gets shows.
                // We hold the callback as a private property.
                // model popup window closes, and user chooses to deny the change.
                // callback is called with the user's decision.

                // ----------
                // CHANGE ME!
                // ----------
                callback(false);
            });

            bool codeAllowedCalled = false;
            bool codeRejectedCalled = false;

            // ACT
            /* This looks complicated!  But essentially, if the drop-down-list is disciplined about making the change to the
             * selected exam, then it should first ask if there's any reason for it not to. (The intent to change a value).
             * So by calling the FireIntent message, anyon listening for changes to all "DetailsComponent" types, 
             * or more specifically one instance on the "ManageExamsScreen", then it can decide if it should block the change.
             * What I mean by that, is that the change should not happen without first asking the user for permission to do so.
             * If the user accepts the change, then the real code that performs the change can be called, otherwise an optional
             * bit of code can run that might want to do something like highlight the SAVE button in response to the user
             * rejecting the change?  
             */
            intentProcessor.FireIntent<int>(
                callerType: "DetailsComponent",
                callerName: "ManageExamsScreen",
                oldValue: 1, 
                newValue: 2,
                codeIfAllowed: codeIfAllowed,
                callbackIfNotAllowed: callbackIfNotAllowed
            );

            // ASSERT
            Assert.IsFalse(codeAllowedCalled);
            Assert.IsTrue(codeRejectedCalled);
            Assert.AreEqual("not modified", message);

            void codeIfAllowed()
            {
                message = "modified";
                codeAllowedCalled = true;
            }

            void callbackIfNotAllowed()
            {
                codeRejectedCalled = true;
            }
        }
    }
}
