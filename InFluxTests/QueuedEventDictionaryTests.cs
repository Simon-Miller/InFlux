namespace InFluxTests
{
    [TestClass]
    public class QueuedEventDictionaryTests
    {
        [TestMethod]
        public void QueuedEventDictionary_works()
        {
            // Arrange
            var prop = new QueuedEventDictionary<int, bool>();

            var addCalls = 0;
            var removeCalls = 0;
            var changeCalls = 0;
            var dictionaryChangedCalls = 0;
            var clearCalls = 0;

            prop.OnChanged.Subscribe(() => dictionaryChangedCalls++);
            prop.OnDictionaryCleared.Subscribe((O,N) => clearCalls++);
            prop.OnItemAdded.Subscribe((O,N)=> addCalls++);
            prop.OnItemChanged.Subscribe((O,N)=> changeCalls++);
            prop.OnItemRemoved.Subscribe((O, N) => removeCalls++);

            // Act
            prop.Add(1, true);
            var addsCalled = addCalls;
            var dictChangesCalled = dictionaryChangedCalls;
            var addValue = prop[1];

            // Assert
            Assert.AreEqual(1, addsCalled);
            Assert.AreEqual(1, dictChangesCalled);
            Assert.IsTrue(addValue);

            // Act
            prop.Add(2, false); // add 2, dictionaryChangedCalls = 2;
            var beforeAlterValue = prop[2];
            prop[2] = true;
            var afterAlterValue = prop[2]; // dictionaryChangedCalls =3;

            // Assert
            Assert.AreEqual(false, beforeAlterValue);
            Assert.AreEqual(3, dictionaryChangedCalls);
            Assert.AreEqual(1, changeCalls);
            Assert.AreEqual(true, afterAlterValue);

            // Act
            var dictCountBefore = prop.Count;
            bool removeSuccess =prop.Remove(2); // dictionaryChangedCalls =4;
            var dictCountAfter = prop.Count;

            // Assert
            Assert.AreEqual(2, dictCountBefore);
            Assert.AreEqual(1, dictCountAfter);
            Assert.AreEqual(4, dictionaryChangedCalls);
            Assert.IsTrue(removeSuccess);

            // Act
            prop.Clear(); // dictionaryChangedCalls =5;

            // Assert
            Assert.AreEqual(0, prop.Count);
            Assert.AreEqual(5, dictionaryChangedCalls);
        }
    }
}
