namespace BinaryDocumentDb.Tests.UnitTestHelpers
{
    internal class IEnumerableComparer
    {
        /// <summary>
        /// Compare two IEnumerables of the same type.
        /// Returns FALSE if values are not identical in the same sequence, or if one of the collections are null.
        /// Returns TRUE if both collections are null, or have the same length, and same values in the same sequence
        /// </summary>
        public bool AreEqual<T>(IEnumerable<T> left, IEnumerable<T> right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;

            try
            {
                var leftEnumerator = left.GetEnumerator();
                var rightEnumerator = right.GetEnumerator();

                var comparer = Comparer<T>.Default;

                while (leftEnumerator.MoveNext() || rightEnumerator.MoveNext())
                {
                    // could be null or exception?
                    var leftValue = leftEnumerator.Current;

                    // could be null or exception?
                    var rightValue = rightEnumerator.Current;

                    if (comparer.Compare(leftValue, rightValue) != 0) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
