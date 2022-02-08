namespace InFluxTests
{
    [TestClass]
    public class EventChainTests
    {
        [TestMethod]
        public void MyTestMethod()
        {
            // Arrange
            var outerCalls = 0;
            var innerCalls = 0;
            var oneOff = 0;
            var inst1 = new testClass();
            var inst2 = new testClass();
            inst1.Event.Subscribe(cl =>
            {
                inst2.Event.FireEvent(123, () =>
                {
                    cl.callbackWhenDone(); 
                });
            });
            var subKey = inst2.Event.Subscribe(cb =>
            {
                innerCalls++;
                cb.callbackWhenDone();
            });
            inst2.Event.SubscribeOnce(cb =>
            {
                oneOff++;
                cb.callbackWhenDone();
            });

            // Act

            // FAIL!!!   This isn't calling outercalls++, but I can't see why.
            inst1.Event.FireEvent(321, () => outerCalls++);

            if (inst1.Event.DebugSubscriptions.Count > 0)
            {
                // debug showing some kind of error in chain.
                inst1.Event.DebugSubscriptions[0].callSubscription();

                /* There's no way of knowing exactly, and repeated calls to the event causes
                 * it to reset each time.  Therefore you can only test a chain immediately after
                 * it supposedly completes. It won't build up a memory of all the subscriptions ever
                 * called, and not heard back from!  Just over the firing of the event.
                 * But still, having the ability to call the code that didn't respond, and see it 
                 * and debug it, is a big help!
                 */
            }

            inst1.Event.FireEvent(432, () => outerCalls++);

            inst2.Event.UnSubscribe(subKey);
            inst1.Event.FireEvent(543, () => outerCalls++);

            // Assert
            Assert.AreEqual(3, outerCalls);
            Assert.AreEqual(2, innerCalls);
            Assert.AreEqual(1, oneOff);
        }
    }

    class testClass
    {
        public EventChain<int> Event = new();
    }
}
