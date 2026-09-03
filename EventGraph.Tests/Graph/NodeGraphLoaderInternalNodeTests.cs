namespace EventGraph.Tests
{
    public class NodeGraphLoaderInternalNodeTests
    {
        [Fact]
        public void LoadGraphCreatesExactlyOneNodePerStandaloneSourceDefinition()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "aapl.json"), /*lang=json,strict*/ """
                {
                  "type": "EquitySource",
                  "name": "AAPL",
                  "spot": 225.0,
                  "volatility": 0.28,
                  "meanTickTimeSeconds": 4.5
                }
                """);
                File.WriteAllText(Path.Combine(directory, "msft.json"), /*lang=json,strict*/ """
                {
                  "type": "EquitySource",
                  "name": "MSFT",
                  "spot": 400.0,
                  "volatility": 0.25,
                  "meanTickTimeSeconds": 3.0
                }
                """);
                File.WriteAllText(Path.Combine(directory, "usd.json"), /*lang=json,strict*/ """
                {
                  "type": "CurrencyRateSource",
                  "name": "USD",
                  "interestRate": 0.02
                }
                """);

                var graph = NodeGraphLoader.LoadGraph(directory);

                Assert.Equal(3, graph.Nodes.Count);
                Assert.Contains(graph.Nodes, node => node is EquitySource && node.Name == "AAPL");
                Assert.Contains(graph.Nodes, node => node is EquitySource && node.Name == "MSFT");
                Assert.Contains(graph.Nodes, node => node is CurrencyRateSource && node.Name == "USD");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphAutoMaterializesInternalNodesFromReferences()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "aapl.json"), /*lang=json,strict*/ """
                {
                  "type": "EquitySource",
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

                Assert.Equal(7, graph.Nodes.Count);
                Assert.Contains(graph.Nodes, node => node is SpotNode);
                Assert.Contains(graph.Nodes, node => node is VolatilityNode);
                Assert.Contains(graph.Nodes, node => node is RateCurveNode);
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
                  "type": "EquitySource",
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
                  "discountCurve": "RateCurveNode::USD"
                }
                """);

                var graph = NodeGraphLoader.LoadGraph(directory);

                Assert.Contains(graph.Nodes, node => node is ForwardCurve);
                Assert.Contains(graph.Nodes, node => node is SpotNode);
                Assert.Contains(graph.Nodes, node => node is RateCurveNode);
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
