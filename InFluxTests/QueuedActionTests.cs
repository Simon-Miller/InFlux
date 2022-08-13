using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace InFluxTests
{
    [TestClass]
    public class QueuedActionTests
    {
        [TestMethod]
        public void Can_add_actions_and_they_fire_immediately()
        {
            // Arrange
            var logs = new List<string>();
            var code1 = new ActionWrap(() => { logs.Add("A"); });
            var code2 = new ActionWrap(() => { logs.Add("B"); });

            // Act
            QueuedActions.Add(code1.DoSomething);
            logs.Add("-");
            QueuedActions.Add(code2.DoSomething);

            // Assert
            Assert.AreEqual("A", logs[0]);
            Assert.AreEqual("-", logs[1]);
            Assert.AreEqual("B", logs[2]);
            Assert.AreEqual(1, code1.CallCount);
            Assert.AreEqual(1, code2.CallCount);

            // Act
            logs.Clear();
            QueuedActions.AddRange(code1.DoSomething, code2.DoSomething);

            // Assert
            Assert.AreEqual("A", logs[0]);
            Assert.AreEqual("B", logs[1]);
            Assert.AreEqual(2, code1.CallCount);
            Assert.AreEqual(2, code2.CallCount);
        }

        [TestMethod]
        public void weakly_refenced_event_means_disposal_possible_and_no_unsubscribe_necessary()
        {
            // Arrange
            var eventSource = new ThingFiringEvent();
            var counter = 0;
            var listener = new ThingToDispose(eventSource, ()=> counter++);

            // Act
            eventSource.Increment();

            listener = null; // free up for garbage collection

            GC.Collect();

            eventSource.Increment(); // second time means counter could be 2, but with disposed weak link, it shoudl remain 1.

            Assert.AreEqual(1, counter);
        }
    }

    class ActionWrap
    {
        public ActionWrap(Action action)
        {
            this.action = action;
        }

        private readonly Action action;
        public int CallCount { get; private set; } = 0;

        public void DoSomething()
        {
            this.action();
            this.CallCount++;
        }
    }

    #region helpers for weak reference test

    class ThingFiringEvent
    {
        public QueuedEventProperty<int> Counter = new(initialValue: 0);

        public void Increment() => this.Counter.Value++;
    }

    class ThingToDispose
    {
        public ThingToDispose(ThingFiringEvent thingToListenTo, Action counterUpdatedCallback)
        {
            thingToListenTo.Counter.ValueChanged.Subscribe((O, N) => counterUpdatedCallback());
        }
    }

    #endregion
}