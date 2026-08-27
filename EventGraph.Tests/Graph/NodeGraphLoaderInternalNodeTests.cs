namespace EventGraph.Tests
{
    public class NodeGraphLoaderInternalNodeTests
    {
        [Fact]
        public void LoadGraphAutoMaterializesInternalNodesFromReferences()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "aapl.json"), /*lang=json,strict*/ """
                {
                  "type": "SimulatedAssetSource",
                  "name": "AAPL",
                  "currency": "USD",
                  "spot": 225.0,
                  "volatility": 0.28,
                  "meanTickTimeSeconds": 4.5
                }
                """);
                File.WriteAllText(Path.Combine(directory, "usd.json"), /*lang=json,strict*/ """
                {
                  "type": "CurrencyRateSource",
                  "name": "USD",
                  "interestRate": 0.02
                }
                """);
                File.WriteAllText(Path.Combine(directory, "option.json"), /*lang=json,strict*/ """
                {
                  "type": "EquityOption",
                  "name": "AAPL_1Y_CALL",
                  "underlyer": "AAPL",
                  "maturity": "1Y",
                  "strike": 225.0,
                  "optionType": "Call"
                }
                """);

                var graph = NodeGraphLoader.LoadGraph(directory);

                Assert.Equal(8, graph.Nodes.Count);
                Assert.Contains(graph.Nodes, node => node is SpotNode);
                Assert.Contains(graph.Nodes, node => node is VolatilitySource);
                Assert.Contains(graph.Nodes, node => node is RateNode);
                Assert.Contains(graph.Nodes, node => node is RateCurveSource);
                Assert.Contains(graph.Nodes, node => node is ForwardCurve);
                Assert.Contains(graph.Nodes, node => node is EquityOption);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphAutoMaterializesForwardCurveFromExplicitJson()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "aapl.json"), /*lang=json,strict*/ """
                {
                  "type": "SimulatedAssetSource",
                  "name": "AAPL",
                  "currency": "USD",
                  "spot": 225.0,
                  "volatility": 0.28,
                  "meanTickTimeSeconds": 4.5
                }
                """);
                File.WriteAllText(Path.Combine(directory, "usd.json"), /*lang=json,strict*/ """
                {
                  "type": "CurrencyRateSource",
                  "name": "USD",
                  "interestRate": 0.02
                }
                """);
                File.WriteAllText(Path.Combine(directory, "forward.json"), /*lang=json,strict*/ """
                {
                  "type": "ForwardCurve",
                  "spot": "SpotNode::AAPL",
                  "discountCurve": "RateCurveSource::USD"
                }
                """);

                var graph = NodeGraphLoader.LoadGraph(directory);

                Assert.Contains(graph.Nodes, node => node is ForwardCurve);
                Assert.Contains(graph.Nodes, node => node is SpotNode);
                Assert.Contains(graph.Nodes, node => node is RateCurveSource);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsInternalNodeWithoutNameWhenTypeCannotInferIt()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "bad.json"), /*lang=json,strict*/ """
                {
                  "type": "BasketAggregate",
                  "constituents": [],
                  "weights": []
                }
                """);

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));

                Assert.Contains("name", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static string CreateDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _ = Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
