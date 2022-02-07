namespace InFluxTests
{
    [TestClass]
    public class EventsEntityBaseTests
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
            inst.ListOfBools.Add(true);
            inst.ECPropField.Value = 1234;
            inst.ECListField.Add(4321);
            inst.ECPropProp.Value = 111;
            inst.ECListProp.Add(true);

            // assert
            Assert.AreEqual(7,calledCount);
        }
    }

    /// <summary>
    /// This is likely how each entity will set themselves up as values change.
    /// This has nothing to do with validation, but could trigger validation?
    /// A shouty model like this won't work with model validation.
    /// That means you'd need to define a getter / setter property of your own,
    /// and have the value itself validate but get its value from an event property?
    /// </summary>
    class Entity : EventsEntityBase<Entity>
    {
        // field
        public readonly QueuedEventProperty<int> Age = new(initialValue: 0, onlyFireOnValueChanges: true);

        // field
        public readonly QueuedEventList<int> ItemCodes = new();

        // property
        public QueuedEventList<bool> ListOfBools { get; init; } = new();

        // field
        public readonly EventChainProperty<int> ECPropField = new();

        // field
        public readonly EventChainList<int> ECListField = new();

        // property
        public EventChainProperty<int> ECPropProp { get; set; } = new();

        // property
        public EventChainList<bool> ECListProp { get; set; } = new();
    }
}
