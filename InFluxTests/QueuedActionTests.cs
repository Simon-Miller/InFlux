using InFlux;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

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
}