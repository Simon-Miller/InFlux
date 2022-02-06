namespace InFluxTests
{
    [TestClass]
    public class EventChainListTests
    {
        internal class commonEventChainListTestHelper
        {
            public commonEventChainListTestHelper()
            {
                targetList.OnListChanged.Subscribe(chain =>
                {
                    listChangeCalls++;
                    chain.callbackWhenDone();
                });
                targetList.OnListChanged.OnEventCompleted.Subscribe(() => listChangeEventCompleteCalls++);
            }

            public readonly EventChainList<int> targetList = new();

            public int listChangeCalls = 0;
            public int listChangeEventCompleteCalls = 0;
        }

        [TestMethod]
        public void can_add_item_correctly()
        {
            // Arrange
            var common = new commonEventChainListTestHelper();

            var addItemCalls = 0;
            var addItemEventCompleteCalls = 0;

            common.targetList.OnItemAdded.Subscribe(chain =>
            {
                addItemCalls++;
                chain.callbackWhenDone();
            });
            common.targetList.OnItemAdded.OnEventCompleted.Subscribe(() => addItemEventCompleteCalls++);

            // Act
            common.targetList.Add(123);

            // Assert            
            Assert.AreEqual(1, common.targetList.Count);
            Assert.AreEqual(123, common.targetList[0]);

            Assert.AreEqual(1, common.listChangeCalls);
            Assert.AreEqual(1, common.listChangeEventCompleteCalls);
            Assert.AreEqual(1, addItemCalls);
            Assert.AreEqual(1, addItemEventCompleteCalls);
        }

        [TestMethod]
        public void can_add_range_correctly()
        {
            // Arrange
            var common = new commonEventChainListTestHelper();

            var rangeAddedCalls = 0;
            var addRangeAddedEventCompleteCalls = 0;
            common.targetList.OnRangeAdded.Subscribe(chain => { rangeAddedCalls++; chain.callbackWhenDone(); });
            common.targetList.OnRangeAdded.OnEventCompleted.Subscribe(() => addRangeAddedEventCompleteCalls++);

            // Act
            common.targetList.AddRange(new[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, common.listChangeCalls);
            Assert.AreEqual(1, common.listChangeEventCompleteCalls);
            Assert.AreEqual(1, rangeAddedCalls);
            Assert.AreEqual(1, addRangeAddedEventCompleteCalls);

            Assert.AreEqual(3, common.targetList.Count);
        }

        [TestMethod]
        public void can_remove_item_correctly()
        {
            // Arrange
            var common = new commonEventChainListTestHelper();
            common.targetList.Add(123);

            var removeItemCalls = 0;
            var removeItemEventCompleteCalls = 0;

            common.targetList.OnItemRemoved.Subscribe(chain =>
            {
                removeItemCalls++;
                chain.callbackWhenDone();
            });
            common.targetList.OnItemRemoved.OnEventCompleted.Subscribe(() => removeItemEventCompleteCalls++);

            // Act
            common.targetList.Remove(123);

            // Assert            
            Assert.AreEqual(0, common.targetList.Count);

            Assert.AreEqual(2, common.listChangeCalls);
            Assert.AreEqual(2, common.listChangeEventCompleteCalls);
            Assert.AreEqual(1, removeItemCalls);
            Assert.AreEqual(1, removeItemEventCompleteCalls);
        }

        [TestMethod]
        public void can_remove_item_at_index_correctly()
        {
            // Arrange
            var common = new commonEventChainListTestHelper();
            common.targetList.Add(123);

            var removeItemCalls = 0;
            var removeItemEventCompleteCalls = 0;

            common.targetList.OnItemRemoved.Subscribe(chain =>
            {
                removeItemCalls++;
                chain.callbackWhenDone();
            });
            common.targetList.OnItemRemoved.OnEventCompleted.Subscribe(() => removeItemEventCompleteCalls++);

            // Act
            common.targetList.RemoveAt(0);

            // Assert
            Assert.AreEqual(0, common.targetList.Count);

            Assert.AreEqual(2, common.listChangeCalls);
            Assert.AreEqual(2, common.listChangeEventCompleteCalls);
            Assert.AreEqual(1, removeItemCalls);
            Assert.AreEqual(1, removeItemEventCompleteCalls);
        }

        [TestMethod]
        public void can_change_item_correctly()
        {
            // Arrange
            var common = new commonEventChainListTestHelper();
            common.targetList.Add(123);

            var changeItemCalls = 0;
            var changeItemEventCompleteCalls = 0;

            common.targetList.OnItemChanged.Subscribe(chain =>
            {
                changeItemCalls++;
                chain.callbackWhenDone();
            });
            common.targetList.OnItemChanged.OnEventCompleted.Subscribe(() => changeItemEventCompleteCalls++);

            // Act
            common.targetList[0] = 321;

            // Assert
            Assert.AreEqual(1, common.targetList.Count);
            Assert.AreEqual(321, common.targetList[0]);

            Assert.AreEqual(2, common.listChangeCalls);
            Assert.AreEqual(2, common.listChangeEventCompleteCalls);
            Assert.AreEqual(1, changeItemCalls);
            Assert.AreEqual(1, changeItemEventCompleteCalls);
        }

        [TestMethod]
        public void can_clear_items_correctly()
        {
            // Arrange
            var common = new commonEventChainListTestHelper();
            common.targetList.AddRange(new[] { 1, 2, 3 });

            var listClearedCalls = 0;
            var listClearedEventCompleteCalls = 0;

            common.targetList.OnListCleared.Subscribe(chain =>
            {
                listClearedCalls++;
                chain.callbackWhenDone();
            });
            common.targetList.OnListCleared.OnEventCompleted.Subscribe(() => listClearedEventCompleteCalls++);

            // Act
            common.targetList.Clear();

            // Assert
            Assert.AreEqual(0, common.targetList.Count);

            Assert.AreEqual(2, common.listChangeCalls);
            Assert.AreEqual(2, common.listChangeEventCompleteCalls);
            Assert.AreEqual(1, listClearedCalls);
            Assert.AreEqual(1, listClearedEventCompleteCalls);
        }
    }
}
