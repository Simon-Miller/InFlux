namespace InFluxTests
{
    [TestClass]
    public class SimpleQueuedEventTests
    {
        [TestMethod]
        public void can_subscribe()
        {
            // Arrange
            var testEvent = new SimpleQueuedEvent<bool>();

            bool called = false;
            Action<bool> code = x => called = true;

            // Act
            testEvent.Subscribe(code);

            testEvent.FireEvent(true);

            // Assert
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void can_unsunscribe_by_key()
        {
            // Arrange -- create two subscribers, and remove one.  Other should still get called.
            var var1 = 0;
            var var2 = 0;

            Action<bool> act1 = x => var1++;
            Action<bool> act2 = x => var2++;

            var testEvent = new SimpleQueuedEvent<bool>();

            var key1 = testEvent.Subscribe(act1);
            var key2 = testEvent.Subscribe(act2);

            // Act
            testEvent.FireEvent(true);

            // Arrange 2
            testEvent.UnSubscribe(key1);

            // Act 2
            testEvent.FireEvent(true);

            // Assert
            Assert.AreEqual(1, var1);
            Assert.AreEqual(2, var2);
        }

        [TestMethod]
        public void can_unsunscribe_by_action()
        {
            // Arrange -- create two subscribers, and remove one.  Other should still get called.
            var var1 = 0;
            var var2 = 0;

            Action<bool> act1 = x => var1++;
            Action<bool> act2 = y => var2++;

            var testEvent = new SimpleQueuedEvent<bool>();

            testEvent.Subscribe(act1);
            testEvent.Subscribe(act2);

            // Act
            testEvent.FireEvent(true);

            // Arrange 2
            testEvent.UnSubscribe(act1);

            // Act 2
            testEvent.FireEvent(true);

            // Assert
            Assert.AreEqual(1, var1);
            Assert.AreEqual(2, var2);
        }

        [TestMethod]
        public void can_subscribe_once()
        {
            // Arrange
            var target1 = new SimpleQueuedEvent<bool>();
            var target2 = new SimpleQueuedEvent<int>();

            var calls1 = 0;
            var calls2 = 0;
            int valueFromEvent = 0;

            target1.SubscribeOnce(x => calls1++);
            target2.SubscribeOnce(n => { calls2++; valueFromEvent = n; });

            // Act: By triggering event twice we prove by calls1/2 and valueFromEvent, that we heard it only once.
            target1.FireEvent(true);
            target2.FireEvent(123);

            target1.FireEvent(true);
            target2.FireEvent(13);

            // Assert
            Assert.AreEqual(1, calls1);
            Assert.AreEqual(1, calls2);
            Assert.AreEqual(123, valueFromEvent);
        }

        [TestMethod]
        public void PrioritySubscribe_works()
        {
            // Arrange
            var target = new SimpleQueuedEvent<int>();
            var called = new List<int>();

            target.Subscribe(N => called.Add(1));
            target.PrioritySubscribe(N => called.Add(2)); // NOTE: This should appear first in called list

            // Act
            target.FireEvent(123); // we don't care about this value, just the order of events firing.

            // Assert
            Assert.AreEqual(2, called.Count);
            Assert.AreEqual(2, called[0]); // proves the priority subscription is at front of cal queue.
            Assert.AreEqual(1, called[1]); // proves first subscription was forced into 2nd place.
        }
    }
}
