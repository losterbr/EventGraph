using System;

namespace EventGraph
{
    public class SpotMessage : EventArgs
    {
        private readonly string name;
        private double value;

        public SpotMessage(string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Have to provide name for SpotMessage.", nameof(name));
            }

            this.name = name;
            this.value = value;
        }

        public string Name => name;

        public double Value
        {
            get => value;
            set => this.value = value;
        }
    }
}
