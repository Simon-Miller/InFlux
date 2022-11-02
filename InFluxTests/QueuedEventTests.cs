using InFlux.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

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

        [TestMethod]
        public void can_subscribe_once()
        {
            // Arrange
            var target1 = new QueuedEvent();
            var target2 = new QueuedEvent<int>();

            var calls1 = 0;
            var calls2 = 0;
            int valueFromEvent = 0;

            target1.SubscribeOnce(() => calls1++);
            target2.SubscribeOnce((O, N) => { calls2++; valueFromEvent = N; });

            // Act: By triggering event twice we prove by calls1/2 and valueFromEvent, that we heard it only once.
            target1.FireEvent();
            target2.FireEvent(0, 123);

            target1.FireEvent();
            target2.FireEvent(123,234);

            // Assert
            Assert.AreEqual(1, calls1);
            Assert.AreEqual(1, calls2);
            Assert.AreEqual(123, valueFromEvent);
        }

        [TestMethod]
        public void PrioritySubscribe_works()
        {
            // Arrange
            var target = new QueuedEvent<int>();
            var called = new List<int>();

            target.Subscribe((O, N) => called.Add(1));
            target.PrioritySubscribe((O, N) => called.Add(2)); // NOTE: This should appear first in called list

            // Act
            target.FireEvent(0, 123); // we don't care about this value, just the order of events firing.

            // Assert
            Assert.AreEqual(2, called.Count);
            Assert.AreEqual(2, called[0]); // proves the priority subscription is at front of cal queue.
            Assert.AreEqual(1, called[1]); // proves first subscription was forced into 2nd place.
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

        #region generic version of queued event tests

        [TestMethod]
        public void can_unsunscribe_by_generic_action()
        {
            // Arrange -- create two subscribers, and remove one.  Other should still get called.
            var var1 = 0;
            var var2 = 0;

            void action1(bool o, bool n) { var1++; }
            void action2(bool o, bool n) { var2++; }

            var testEvent = new QueuedEvent<bool>();

            testEvent.Subscribe(action1);
            testEvent.Subscribe(action2);

            // Act
            testEvent.FireEvent(false, true);

            // Arrange 2
            testEvent.UnSubscribe(action1);

            // Act 2
            testEvent.FireEvent(false, true);

            // Assert
            Assert.AreEqual(1, var1);
            Assert.AreEqual(2, var2);
        }

        [TestMethod]
        public void Ordering_of_generic_events_is_more_predictable()
        {
            // Arrange -- NOTE how similar this is to setup in EventsSuckTests
            var hotel = new Hotel2();
            var fireBrigade = new FireBrigade2();
            var reporter = new Reporter2();

            hotel.KitchenOnFire.Subscribe((O, N) => fireBrigade.PutOutFire(N));
            hotel.KitchenOnFire.Subscribe((O, N) => reporter.ReportOnHotelFire(N));
            fireBrigade.PuttingOutAFire.Subscribe((O, N) => reporter.ReportOnFireBrigade(N));

            // Act
            hotel.StartFire(); // you arsonist!

            // Assert - NOTE: Events should now be fully processed before next 'event' is processed.
            Assert.IsTrue(Events2.List[0] == "hotel: Someone help! There's a fire!"); // info, not event.   
            Assert.IsTrue(Events2.List[1] == "fire brigade: On our way! 123"); // first event listener on KitchenOnFire
            Assert.IsTrue(Events2.List[2] == "News flash! Hotel on fire! 123");// second event listener on KitchenOnFire

            // only listener: NOTE - although called during first event handler,
            // the event firing queues the subscribers, so existing queued subscribers will be dealt with first.
            Assert.IsTrue(Events2.List[3] == "News flash! Fire fighters fight fire! 345");
        }


        class Hotel2
        {
            public readonly QueuedEvent<int> KitchenOnFire = new();
            public void StartFire()
            {
                Events2.List.Add("hotel: Someone help! There's a fire!");
                KitchenOnFire.FireEvent(0, 123);
            }
        }
        class FireBrigade2
        {
            public readonly QueuedEvent<int> PuttingOutAFire = new();
            public void PutOutFire(int number)
            {
                Events2.List.Add($"fire brigade: On our way! {number}");
                PuttingOutAFire.FireEvent(0, 345);
            }
        }
        class Reporter2
        {
            public void ReportOnHotelFire(int number) => Events2.List.Add($"News flash! Hotel on fire! {number}");
            public void ReportOnFireBrigade(int number) => Events2.List.Add($"News flash! Fire fighters fight fire! {number}");
        }
        static class Events2
        {
            public static readonly List<string> List = new();
        }

        #endregion


        [TestMethod]
        public void MyTestMethod()
        {
            var inst = new testClass();
            var count = 0;

            inst.AgeInsight.ValueChanged.Subscribe((O, N) => { count++; });
            inst.Age++; // should trigger event.

            //inst.

            Assert.AreEqual(1, count);
        }

    }

    [AutoWireup]
    public partial class testClass
    {
        [Required]
        [Range(minimum:16, maximum:21, ErrorMessage ="Range must be between 16 and 21")]
        int age; // ** all generated code seeded from THIS field, once project builds. **

        [Required]
        [RegularExpression("^[A-Za-z-'`]{2,25}$", ErrorMessage ="Name is invalid.  Use letters only. hyphen and apostrophe accepted.")]
        string name;
    }
}
