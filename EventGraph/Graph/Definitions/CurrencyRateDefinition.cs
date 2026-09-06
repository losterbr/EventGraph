using System;

namespace EventGraph
{
    /// <summary>
    /// Defines immutable inputs for a currency's flat interest rate.
    /// </summary>
    public sealed class CurrencyRateDefinition
    {
        public CurrencyRateDefinition(string name, double interestRate, string currency = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Currency rate name cannot be empty.", nameof(name));
            }

            if (double.IsNaN(interestRate) || double.IsInfinity(interestRate))
            {
                throw new ArgumentOutOfRangeException(nameof(interestRate), "Interest rate must be a finite number.");
            }

            if (currency != null && string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));
            }

            Name = name;
            InterestRate = interestRate;
            Currency = currency ?? name;
        }

        public string Name { get; }

        public double InterestRate { get; }

        public string Currency { get; }
    }
}
