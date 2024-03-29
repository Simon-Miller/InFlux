﻿namespace InFluxTests
{
    [TestClass]
    public class EventsSuckTests
    {
#if RELEASE
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
#endif
        //[TestMethod]
        public void Ordering_of_events_not_as_expected()
        {
            // Arrange
            Notes.List.Clear();

            var hotel = new Hotel();
            var fireBrigade = new FireBrigade();
            var reporter = new Reporter();

            hotel.KitchenOnFire += () => fireBrigade.PutOutFire();
            hotel.KitchenOnFire += () => reporter.ReportOnHotelFire();
            fireBrigade.PuttingOutAFire += () => reporter.ReportOnFireBrigade();

            // Act
            hotel.StartFire(); // you arsonist!

            // Assert
            // you would perhaps innocently expect the sequence to look like this:
            Assert.IsTrue(Notes.List[0] == "hotel: Someone help! There's a fire!");
            Assert.IsTrue(Notes.List[1] == "News flash! Hotel on fire!");
            Assert.IsTrue(Notes.List[2] == "fire brigade: On our way!");
            Assert.IsTrue(Notes.List[3] == "News flash! Fire fighters fight fire!");

            // Maybe you're expecting this sequence:
            Assert.IsTrue(Notes.List[0] == "hotel: Someone help! There's a fire!");
            Assert.IsTrue(Notes.List[2] == "fire brigade: On our way!");
            Assert.IsTrue(Notes.List[1] == "News flash! Hotel on fire!");
            Assert.IsTrue(Notes.List[3] == "News flash! Fire fighters fight fire!");

            // What you actually get, is this:
            Assert.IsTrue(Notes.List[0] == "hotel: Someone help! There's a fire!");
            Assert.IsTrue(Notes.List[2] == "fire brigade: On our way!");
            Assert.IsTrue(Notes.List[3] == "News flash! Fire fighters fight fire!");
            Assert.IsTrue(Notes.List[1] == "News flash! Hotel on fire!");

            // NOTE: The reporter reports on the fire being put out BEFORE reporting on the fire.
            // When we develop against events, this is the hardest thing to understand.
            // The built-in events system in C# can't solve this issue, despite being a very useful tool.
        }
    }

    class Hotel
    {
        public event Action? KitchenOnFire;
        public void StartFire()
        {
            Notes.Add("hotel: Someone help! There's a fire!");
            KitchenOnFire?.Invoke();
        }
    }
    class FireBrigade
    {
        public event Action? PuttingOutAFire;
        public void PutOutFire()
        {
            Notes.Add("fire brigade: On our way!");
            PuttingOutAFire?.Invoke();
        }
    }
    class Reporter
    {
        public void ReportOnHotelFire() => 
            Notes.Add("News flash! Hotel on fire!");

        public void ReportOnFireBrigade() => 
            Notes.Add("News flash! Fire fighters fight fire!");
    }
    static class Notes
    {
        public static readonly List<string> List = new();
        public static void Add(string note) => List.Add(note);
    }
}
