using System;

namespace EventGraph
{
    /// <summary>
    /// Defines immutable inputs for an equity option.
    /// </summary>
    public sealed class EquityOptionDefinition
    {
        public EquityOptionDefinition(string name, string underlyer, string maturity, double strike, string optionType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Option name cannot be empty.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(underlyer))
            {
                throw new ArgumentException("Option underlyer cannot be empty.", nameof(underlyer));
            }

            if (string.IsNullOrWhiteSpace(maturity))
            {
                throw new ArgumentException("Option maturity cannot be empty.", nameof(maturity));
            }

            if (strike <= 0.0 || double.IsNaN(strike) || double.IsInfinity(strike))
            {
                throw new ArgumentOutOfRangeException(nameof(strike), "Option strike must be a positive finite number.");
            }

            if (string.IsNullOrWhiteSpace(optionType))
            {
                throw new ArgumentException("Option type cannot be empty.", nameof(optionType));
            }

            Name = name;
            Underlyer = underlyer;
            Maturity = maturity;
            Strike = strike;
            OptionType = optionType;
        }

        public string Name { get; }

        public string Underlyer { get; }

        public string Maturity { get; }

        public double Strike { get; }

        public string OptionType { get; }
    }
}