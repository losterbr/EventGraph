using System;

namespace EventGraph.Tests
{
    public class MarketClockTests
    {
        [Fact]
        public void MarketClockDefaultsToToday()
        {
            var clock = new MarketClock();

            Assert.Equal(DateTime.Today, clock.Today);
        }

        [Fact]
        public void MarketClockAcceptsCustomInitialDate()
        {
            var customDate = new DateTime(2026, 1, 15);
            var clock = new MarketClock(customDate);

            Assert.Equal(customDate, clock.Today);
        }

        [Fact]
        public void SetDateUpdatesDateAndFiresEvent()
        {
            var initialDate = new DateTime(2026, 1, 1);
            var clock = new MarketClock(initialDate);
            DateTime? eventDate = null;
            clock.DateChanged += (_, date) => eventDate = date;

            var newDate = new DateTime(2026, 1, 2);
            clock.SetDate(newDate);

            Assert.Equal(newDate, clock.Today);
            Assert.Equal(newDate, eventDate);
        }

        [Fact]
        public void SetDateDoesNotFireEventWhenDateIsUnchanged()
        {
            var initialDate = new DateTime(2026, 1, 1);
            var clock = new MarketClock(initialDate);
            var eventFired = false;
            clock.DateChanged += (_, _) => eventFired = true;

            clock.SetDate(initialDate);

            Assert.False(eventFired);
        }

        [Fact]
        public void AdvanceDaysAdvancesBySpecifiedNumberOfDays()
        {
            var initialDate = new DateTime(2026, 1, 1);
            var clock = new MarketClock(initialDate);
            DateTime? eventDate = null;
            clock.DateChanged += (_, date) => eventDate = date;

            clock.AdvanceDays(5);

            Assert.Equal(new DateTime(2026, 1, 6), clock.Today);
            Assert.Equal(new DateTime(2026, 1, 6), eventDate);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AdvanceDaysRejectsNonPositiveDays(int days)
        {
            var clock = new MarketClock();

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => clock.AdvanceDays(days));
        }

        [Fact]
        public void AdvanceDateAdvancesByTimeSpan()
        {
            var initialDate = new DateTime(2026, 1, 1);
            var clock = new MarketClock(initialDate);
            DateTime? eventDate = null;
            clock.DateChanged += (_, date) => eventDate = date;

            clock.AdvanceDate(TimeSpan.FromDays(3));

            Assert.Equal(new DateTime(2026, 1, 4), clock.Today);
            Assert.Equal(new DateTime(2026, 1, 4), eventDate);
        }

        [Fact]
        public void AdvanceDateRejectsNonPositiveTimeSpan()
        {
            var clock = new MarketClock();

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => clock.AdvanceDate(TimeSpan.Zero));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => clock.AdvanceDate(TimeSpan.FromDays(-1)));
        }
    }
}
