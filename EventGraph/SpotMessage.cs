using System;

namespace EventGraph
{
    public class SpotMessage : EventArgs
    {
        private double spot;
        private string name;
        public SpotMessage(string name, double spot)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Have to provide name for SpotMessage.");
            this.name = name;
            this.spot = spot;
        }
        public string Name
        {
            get { return name; }
        }
        public double Value
        {
            set { spot = value; }
            get { return spot; }
        }
    }
}
