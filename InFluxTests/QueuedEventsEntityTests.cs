namespace InFluxTests
{
    [TestClass]
    public class QueuedEventsEntityTests
    {
        [TestMethod]
        public void Can_listen_to_entity_changed()
        {
            // arrange
            var inst = new Entity();

            int calledCount = 0;

            inst.EntityChanged.Subscribe((O, N) => calledCount++);

            // act
            inst.Age.Value = 103;
            inst.ItemCodes.Add(123);

            // assert
            Assert.AreEqual(2,calledCount);
        }
    }

    /// <summary>
    /// This is likely how each entity will set themselves up as values change.
    /// This has nothing to do with validation, but could trigger validation?
    /// A shouty model like this won't work with model validation.
    /// That means you'd need to define a getter / setter property of your own,
    /// and have the value itself validate but get its value from an event property?
    /// </summary>
    class Entity : QueuedEventsEntity<Entity>
    {
        public readonly QueuedEventProperty<int> Age = new(initialValue: 0, onlyFireOnValueChanges: true);

        public readonly QueuedEventList<int> ItemCodes = new();
    }
}
