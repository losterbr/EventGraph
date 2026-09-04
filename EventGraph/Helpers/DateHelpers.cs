using System;
using System.Globalization;

namespace EventGraph
{
    /// <summary>
    /// Shared date and tenor helpers used across graph nodes.
    /// </summary>
    public static class DateHelpers
    {
        private const double DaysPerYear = 365.0;

        /// <summary>
        /// Converts a tenor such as "1M", "6M", or "5Y" into an Act/365 fraction of a year.
        /// </summary>
        public static double ToYearFraction(string tenor)
        {
            if (!TryParseTenor(tenor, out var amount, out var unit))
            {
                throw new FormatException($"'{tenor}' is not a valid tenor. Expected a number followed by D, W, M, or Y.");
            }

            return unit switch
            {
                'D' => amount / DaysPerYear,
                'W' => amount * 7.0 / DaysPerYear,
                'M' => amount / 12.0,
                'Y' => amount,
                _ => throw new FormatException($"'{tenor}' is not a valid tenor. Expected a number followed by D, W, M, or Y.")
            };
        }

        /// <summary>
        /// Adds a calendar-accurate tenor such as "1M", "6M", or "5Y" to <paramref name="date"/>.
        /// Returns false if <paramref name="tenor"/> is not a recognized tenor.
        /// </summary>
        public static bool TryAddTenor(DateTime date, string tenor, out DateTime result)
        {
            if (!TryParseTenor(tenor, out var amount, out var unit))
            {
                result = default;
                return false;
            }

            result = unit switch
            {
                'D' => date.AddDays(amount),
                'W' => date.AddDays(amount * 7),
                'M' => date.AddMonths(amount),
                _ => date.AddYears(amount)
            };
            return true;
        }

        /// <summary>
        /// Computes the Act/365 year fraction between two dates.
        /// </summary>
        public static double YearFraction(DateTime start, DateTime end)
        {
            return (end - start).TotalDays / DaysPerYear;
        }

        private static bool TryParseTenor(string tenor, out int amount, out char unit)
        {
            amount = 0;
            unit = default;
            if (string.IsNullOrWhiteSpace(tenor) || tenor.Length < 2)
            {
                return false;
            }

            unit = char.ToUpperInvariant(tenor[^1]);
            return unit is 'D' or 'W' or 'M' or 'Y'
                && int.TryParse(tenor.AsSpan(0, tenor.Length - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out amount);
        }
    }
}
