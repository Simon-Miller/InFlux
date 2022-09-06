namespace InFluxTests
{
    [TestClass]
    public class EventChainPropertyTests
    {
        [TestMethod]
        public void Property_change_fires_events()
        {
            // Arrange
            bool testCompleted = false;

            // Act
            QueuedActions.AddRange(
                () => property_change_fires_events_test(() => testCompleted = true),
                () => { /* queued work item, so any event handling will have to wait for me! */ }
            );

            // queue will complete before we get here.
            Assert.IsTrue(testCompleted);
        }

        private void property_change_fires_events_test(Action callbackToTest)
        {
            //Arrange
            var parent = new EventChainProperty<int>();
            var child = new EventChainProperty<bool>();

            var parentCount = 0;
            var childCount = 0;

            // hooking into the event chain
            parent.ValueChanged.Subscribe(chain =>
            {
                parentCount++;

                // proves the old and new values come through.
                if (chain.Payload.oldValue != chain.Payload.newValue)
                {
                    // tests method with callback which uses SubscribeOnce
                    child.SetValue(true, () => chain.CallbackWhenDone());
                }
            });
            child.ValueChanged.Subscribe(chain => { childCount++; chain.CallbackWhenDone(); });

            bool isComplete = false;
            parent.ValueChangedEventCompleted.Subscribe(() => isComplete = true);

            // Act
            parent.Value = 123;// fire off events, but we're not listening directly.

            // Assert

            /* IMPORTANT:
             * You have to realise that at this point, although the property has changed,
             * the event chain most likely has not.  So if you need to know events have
             * been processed, we have to hook into another event: ValueChangedEventCompleted
             */
            Assert.IsFalse(isComplete);

            parent.ValueChangedEventCompleted.SubscribeOnce(() => 
            {
                Assert.AreEqual(1, parentCount);
                Assert.AreEqual(1, childCount);

                callbackToTest();
            });
        }
    }
}

    
