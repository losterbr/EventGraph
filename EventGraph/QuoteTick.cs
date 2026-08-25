using System;

namespace EventGraph
{
    /// <summary>
    /// Represents a single quote update carrying a symbol and value.
    /// </summary>
    public class QuoteTick : EventArgs
    {
        private readonly string name;
        private double value;

        public QuoteTick(string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Have to provide name for QuoteTick.", nameof(name));
            }

            this.name = name;
            this.value = value;
        }

        public string Name => name;

        public double Value
        {
            get => value;
            set
            {
                if (!double.IsFinite(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Spot values must be finite numbers.");
                }

                this.value = value;
            }
        }
    }
}
