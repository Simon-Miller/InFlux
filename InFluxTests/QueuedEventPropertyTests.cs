namespace InFluxTests
{
    [TestClass]
    public class QueuedEventPropertyTests
    {
        class test
        {
            public readonly QueuedEventProperty<int> Age = new(initialValue: 0, onlyFireOnValueChanges: true);
        }

        [TestMethod]
        public void can_subscribe_and_fire_and_unsubscribe()
        {
            // Arrange
            var target = new test();

            var calls = 0;
            var oldValue = 0;
            var newValue = 10;

            var key = target.Age.ValueChanged.Subscribe(x => 
            {
                if (x.oldValue != oldValue || x.newValue != newValue)
                    throw new ArgumentException();
                calls++;
            });

            // act
            target.Age.Value = oldValue; // shouldn't fire event.

            target.Age.Value = newValue;

            target.Age.ValueChanged.UnSubscribe(key);

            target.Age.Value = 20;

            // Assert
            Assert.IsTrue(target.Age == 20); // casting works?
            Assert.AreEqual(1, calls); // would be 2 if subscription was still active.
        }
    }
}
