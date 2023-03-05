using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Influx.Indexing.Tests
{
    [TestClass]
    public class SortedLinkedItemsTests
    {
        [TestMethod]
        public void Methodical_test_of_small_number_of_items()
        {
            // Arrange
            var collection = new SortedLinkedItems<int>((L, R) =>
            {
                if (L < R) return Comparison.LeftIsSmaller;
                if (L == R) return Comparison.AreEqual;
                return Comparison.LeftIsLarger;
            });

            var collection2 = new SortedLinkedItems<int>((L, R) =>
            {
                if (L < R) return Comparison.LeftIsSmaller;
                if (L == R) return Comparison.AreEqual;
                return Comparison.LeftIsLarger;
            });

            // Act
            collection.Add(1);
            collection.Add(3);
            collection.Add(2);
            collection.Add(4);

            collection2.Add(2);
            collection2.Add(-1);

            // Act - iterate over collection
            var total = 0;
            var count = 0;
            foreach (var item in collection)
            {
                total += item;
                count++;
            }

            // Act - convert to list.
            var sorted = collection.ToList();
        }

        [TestMethod]
        public void TestMethod1()
        {
            // Arrange
            var collection = new SortedLinkedItems<int>((L, R) =>
            {
                if (L < R) return Comparison.LeftIsSmaller;
                if (L == R) return Comparison.AreEqual;
                return Comparison.LeftIsLarger;
            });

            var random = new Random();

            // Act - adding 1000000 items;
            for (int i = 0; i < 100000; i++)
            {
                collection.Add(random.Next());
            }

            // Act - iterate over collection
            var total = 0;
            var count = 0;
            foreach (var item in collection)
            {
                total += item;
                count++;
            }

            // Act - convert to list.
            var sorted = collection.ToList();

            // Assert
            Assert.AreEqual(1000000, count);
        }
    }
}