using System;

namespace EventGraph
{
    /// <summary>
    /// Provides the market valuation date and signals when the calendar advances.
    /// </summary>
    public interface IMarketClock
    {
        DateTime Today { get; }

        event EventHandler<DateTime> DateChanged;

        void SetDate(DateTime valuationDate);

        void AdvanceDays(int days = 1);

        void AdvanceDate(TimeSpan timeSpan);
    }

    /// <summary>
    /// Controllable market valuation clock that can advance time and notify subscribers.
    /// </summary>
    public sealed class MarketClock : IMarketClock
    {
        private DateTime today;

        public MarketClock(DateTime? initialDate = null)
        {
            today = (initialDate ?? DateTime.Today).Date;
        }

        public DateTime Today => today;

        public event EventHandler<DateTime> DateChanged;

        public void SetDate(DateTime valuationDate)
        {
            var newDate = valuationDate.Date;
            if (newDate != today)
            {
                today = newDate;
                DateChanged?.Invoke(this, today);
            }
        }

        public void AdvanceDays(int days = 1)
        {
            if (days <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(days), "Days to advance must be positive.");
            }

            SetDate(today.AddDays(days));
        }

        public void AdvanceDate(TimeSpan timeSpan)
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan), "Time span to advance must be positive.");
            }

            SetDate(today.Add(timeSpan));
        }
    }
}
