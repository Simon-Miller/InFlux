namespace InFluxTests
{
    [TestClass]
    public class QueuedEventListTests
    {
        [TestMethod]
        public void Add_works()
        {
            // Arrange
            var list = new QueuedEventList<Widget>()
            {
                new Widget(123) // supports this style of definition because of the Add method signature. 
            };

            int calls = 0;
            int addCalls = 0;

            var subKey = list.OnListChanged.Subscribe((O, N) => calls++);
            var subKey2 = list.OnItemAdded.Subscribe((O, N) => addCalls++);

            // Act
            list.Add(new Widget(234));

            list.OnListChanged.UnSubscribe(subKey);
            list.OnItemAdded.UnSubscribe(subKey2);

            list.Add(new Widget(345));

            // Assert
            Assert.AreEqual(calls, 1);
            Assert.AreEqual(addCalls, 1);
            Assert.AreEqual(list.Count, 3);
            Assert.AreEqual(list[0], new Widget(123));
            Assert.AreEqual(list[1], new Widget(234));
            Assert.AreEqual(list[2], new Widget(345));
        }

        [TestMethod]
        public void Remove_works()
        {
            // Arrange
            var one = new Widget(1);

            var list = new QueuedEventList<Widget>()
            {
                one,
                new Widget(2),
            };

            int calls = 0;
            list.OnListChanged.Subscribe((O, N) => calls++);
            int removeCalls = 0;
            list.OnItemRemoved.Subscribe((O,N) => removeCalls++);

            // Act
            var initialCount = list.Count;

            list.RemoveAt(1);
            list.Remove(one);

            // Assert
            Assert.AreEqual(calls, 2);
            Assert.AreEqual(removeCalls, 2);
            Assert.AreEqual(initialCount, 2);
            Assert.AreEqual(list.Count, 0);
        }

        [TestMethod]
        public void Indexer_works()
        {
            // Arrange
            var list = new QueuedEventList<Widget>()
            {
                new Widget(0),
                new Widget(3),
                new Widget(2)
            };

            var calls = 0;
            list.OnListChanged.Subscribe((O, N) => calls++);
            var indexerCalls = 0;
            list.OnItemChanged.Subscribe((O,N)=> indexerCalls++);   

            // Act
            var oldItem = list[1]!;
            list[1] = oldItem with { Value = 1 };

            // Assert
            Assert.AreEqual(calls, 1);
            Assert.AreEqual(indexerCalls,1);
            Assert.AreEqual(list[0], new Widget(0));
            Assert.AreEqual(list[1], new Widget(1));
            Assert.AreEqual(list[2], new Widget(2));
        }

        [TestMethod]
        public void AddRange_works()
        {
            // Arrange
            var list = new QueuedEventList<Widget>();
            var calls = 0;
            list.OnListChanged.Subscribe((O, N) => calls++);
            var rangeCalls = 0;
            list.OnItemAdded.Subscribe((O,N) => rangeCalls++);

            // Act
            list.AddRange(new Widget[]
            {
                new Widget(1),
                new Widget(2)
            });

            // Assert
            Assert.AreEqual(2, calls);
            Assert.AreEqual(2, rangeCalls);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(new Widget(1), list[0]);
            Assert.AreEqual(new Widget(2), list[1]);
        }
    }

    internal record class Widget(int Value);
}
