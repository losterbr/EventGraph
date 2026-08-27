using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Backward-compatible alias for EquitySource. Prefer EquitySource for new code.
    /// </summary>
    public class SimulatedAssetSource : EquitySource
    {
        public SimulatedAssetSource(IReadOnlyDictionary<string, JsonElement> definition)
            : base(definition)
        {
        }

        public SimulatedAssetSource(
            string name,
            double spot,
            double vol,
            double meanTickTimeSeconds = 1.0,
            string currency = "USD")
            : base(name, spot, vol, meanTickTimeSeconds, currency)
        {
        }

        public override string Type => nameof(SimulatedAssetSource);
    }
}
