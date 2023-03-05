using InFlux.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Influx.CodeGenerators.AutoWireup.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arrange
            var target = new TestClass();
            var informed = false;

            // look, Mum!  I can get events on fields, now!...  yes, Son, - that's nice. (without a clue on what was just said)
            target.ValueInsight.ValueChanged.Subscribe((O, N) => { informed = true; });

            // Act
            target.Value = 123;

            // Assert
            Assert.IsTrue(informed);
        }
    }

    // GOTCHA!  don't forget it MUST be partial.
    [AutoWireup]
    partial class TestClass
    {
        [Required]
        int? value;
    }
}