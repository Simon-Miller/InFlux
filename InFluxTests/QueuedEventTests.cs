﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace InFluxTests
{
    [TestClass]
    public class QueuedEventTests
    {
        [TestMethod]
        public void can_subscribe()
        {
            // Arrange
            var testEvent = new QueuedEvent();

            bool called = false;
            Action code = () => called = true;

            // Act
            testEvent.Subscribe(code);

            testEvent.FireEvent();

            // Assert
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void can_unsunscribe_by_key()
        {
            // Arrange -- create two subscribers, and remove one.  Other should still get called.
            var var1 = 0;
            var var2 = 0;

            Action act1 = () => var1++;
            Action act2 = () => var2++;

            var testEvent = new QueuedEvent();

            var key1 = testEvent.Subscribe(act1);
            var key2 = testEvent.Subscribe(act2);

            // Act
            testEvent.FireEvent();

            // Arrange 2
            testEvent.UnSubscribe(key1);

            // Act 2
            testEvent.FireEvent();

            // Assert
            Assert.AreEqual(1, var1);
            Assert.AreEqual(2, var2);
        }

        [TestMethod]
        public void can_unsunscribe_by_action()
        {
            // Arrange -- create two subscribers, and remove one.  Other should still get called.
            var var1 = 0;
            var var2 = 0;

            Action act1 = () => var1++;
            Action act2 = () => var2++;

            var testEvent = new QueuedEvent();

            testEvent.Subscribe(act1);
            testEvent.Subscribe(act2);

            // Act
            testEvent.FireEvent();

            // Arrange 2
            testEvent.UnSubscribe(act1);

            // Act 2
            testEvent.FireEvent();

            // Assert
            Assert.AreEqual(1, var1);
            Assert.AreEqual(2, var2);
        }

        #region using QueuedEvents gives a more predictable behaviour to events

        [TestMethod]
        public void Ordering_of_events_is_more_predictable()
        {
            // Arrange -- NOTE how similar this is to setup in EventsSuckTests
            var hotel = new Hotel();
            var fireBrigade = new FireBrigade();
            var reporter = new Reporter();

            hotel.KitchenOnFire.Subscribe(() => fireBrigade.PutOutFire());
            hotel.KitchenOnFire.Subscribe(() => reporter.ReportOnHotelFire());
            fireBrigade.PuttingOutAFire.Subscribe(() => reporter.ReportOnFireBrigade());

            // Act
            hotel.StartFire(); // you arsonist!

            // Assert - NOTE: Events should now be fully processed before next 'event' is processed.
            Assert.IsTrue(Events.List[0] == "hotel: Someone help! There's a fire!"); // info, not event.   
            Assert.IsTrue(Events.List[1] == "fire brigade: On our way!"); // first event listener on KitchenOnFire
            Assert.IsTrue(Events.List[2] == "News flash! Hotel on fire!");// second event listener on KitchenOnFire

            // only listener: NOTE - although called during first event handler,
            // the event firing queues the subscribers, so existing queued subscribers will be dealt with first.
            Assert.IsTrue(Events.List[3] == "News flash! Fire fighters fight fire!"); 
        }

        class Hotel
        {
            public readonly QueuedEvent KitchenOnFire = new();
            public void StartFire()
            {
                Events.List.Add("hotel: Someone help! There's a fire!");
                KitchenOnFire.FireEvent();
            }
        }
        class FireBrigade
        {
            public readonly QueuedEvent PuttingOutAFire = new();
            public void PutOutFire()
            {
                Events.List.Add("fire brigade: On our way!");
                PuttingOutAFire.FireEvent();
            }
        }
        class Reporter
        {
            public void ReportOnHotelFire() => Events.List.Add("News flash! Hotel on fire!");
            public void ReportOnFireBrigade() => Events.List.Add("News flash! Fire fighters fight fire!");
        }
        static class Events
        {
            public static readonly List<string> List = new();
        }

        #endregion
    }
}
