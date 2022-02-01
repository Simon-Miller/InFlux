using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InFluxTests
{
    [TestClass]
    public class EventChainTests
    {
        [TestMethod]
        public void MyTestMethod()
        {
            // Arrange
            var outerCalls = 0;
            var innerCalls = 0;
            var oneOff = 0;
            var inst1 = new testClass();
            var inst2 = new testClass();
            inst1.Event.Subscribe(cl => 
            {
                inst2.Event.FireEvent(123, ()=> 
                {
                    cl.callbackWhenDone(); 
                });
            });
            var subKey = inst2.Event.Subscribe(cb => 
            {
                innerCalls++; 
                cb.callbackWhenDone(); 
            });
            inst2.Event.SubscribeOnce(cb=> 
            {
                oneOff++;
                cb.callbackWhenDone();
            });

            // Act

            // FAIL!!!   This isn't calling outercalls++, but I can't see why.
            inst1.Event.FireEvent(321, () => outerCalls++);
            inst1.Event.FireEvent(432, () => outerCalls++);

            inst2.Event.Unsubscribe(subKey);
            inst1.Event.FireEvent(543, () => outerCalls++);

            // Assert
            Assert.AreEqual(3, outerCalls);
            Assert.AreEqual(2, innerCalls);
            Assert.AreEqual(1, oneOff);
        }
    }

    class testClass
    {
        public EventChain<int> Event = new();
    }
}
