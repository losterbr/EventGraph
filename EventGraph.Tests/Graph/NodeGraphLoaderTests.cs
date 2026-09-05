namespace EventGraph.Tests
{
    public class NodeGraphLoaderTests
    {
        [Fact]
        public void LoadGraphCreatesNodesFromJson()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ """
            {
                            "type": "EquitySource",
              "name": "JSON",
              "currency": "USD",
              "spot": 123.0,
              "volatility": 0.15,
              "meanTickTimeSeconds": 2.0,
              "futureProperty": "owned by the source node"
            }
            """);

                var sources = NodeGraphLoader.LoadGraph(directory).QuoteNodes;

                var source = Assert.Single(sources);
                Assert.Equal("JSON", source.Name);
                Assert.IsNotAssignableFrom<IDefinitionProvider<SpotDefinition>>(source);
                Assert.Equal(nameof(SpotNode), source.Type);
                Assert.Equal(123.0, source.Spot);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphReadsFilesInStableOrder()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "b.json", "B");
                WriteDefinition(directory, "a.json", "A");

                var sources = NodeGraphLoader.LoadGraph(directory).QuoteNodes;

                Assert.Equal("A", sources[0].Name);
                Assert.Equal("B", sources[1].Name);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphCreatesBasketFromNamedSourcesAndWeights()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "a.json", "A");
                WriteDefinition(directory, "b.json", "B");
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ """
            {
              "type": "BasketDefinition",
              "name": "EquityBasket",
              "currency": "USD",
              "constituents": ["A", "B"],
              "weights": [0.25, 0.75]
            }
            """);

                var nodes = NodeGraphLoader.LoadGraph(directory).Nodes;

                var basket = Assert.Single(nodes.OfType<BasketSpotNode>());
                Assert.Equal("EquityBasket", basket.Name);
                Assert.Equal("A=0.25, B=0.75", basket.GetWeights());
                Assert.All(basket.Dependencies, dependency => Assert.IsType<SpotNode>(dependency));
                Assert.DoesNotContain(nodes, node => node.Type == nameof(BasketDefinition));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphAssignsStableNodeIndicesAndDependencyIndices()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "b.json", "B");
                WriteDefinition(directory, "a.json", "A");
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ """
            {
              "type": "BasketDefinition",
              "name": "EquityBasket",
              "currency": "USD",
              "constituents": ["A", "B"],
              "weights": [0.25, 0.75]
            }
            """);

                var graph = NodeGraphLoader.LoadGraph(directory);

                Assert.Equal(["A", "B", "A", "B", "EquityBasket"], graph.Nodes.Select(node => node.Name));
                Assert.Equal(0, graph.GetIndex("EquitySource::A"));
                Assert.Equal(1, graph.NodeIndexByName["EquitySource::B"]);
                Assert.Equal([2, 3], graph.DependenciesByNode[graph.GetIndex("BasketSpotNode::EquityBasket")]);
                Assert.Empty(graph.DependenciesByNode[graph.GetIndex("EquitySource::A")]);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsBasketWithUnknownSource()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ "{\"type\":\"BasketDefinition\",\"name\":\"B\",\"currency\":\"USD\",\"constituents\":[\"Missing\"],\"weights\":[1]}");

                _ = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsBasketWithConstituentInDifferentCurrency()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ "{\"type\":\"EquitySource\",\"name\":\"EUR_ASSET\",\"currency\":\"EUR\",\"spot\":100,\"volatility\":0.2,\"meanTickTimeSeconds\":1}");
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ "{\"type\":\"BasketDefinition\",\"name\":\"USD_BASKET\",\"currency\":\"USD\",\"constituents\":[\"EUR_ASSET\"],\"weights\":[1]}");

                var exception = Assert.Throws<ArgumentException>(() => NodeGraphLoader.LoadGraph(directory));

                Assert.Contains("same currency", exception.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphReportsMissingSourcePropertyFromSourceNode()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ "{\"type\":\"EquitySource\",\"name\":\"A\",\"spot\":100}");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));

                Assert.Contains("volatility", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphReportsMissingBasketPropertyFromBasketNode()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "source.json", "A");
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ "{\"type\":\"BasketDefinition\",\"name\":\"Basket\",\"currency\":\"USD\",\"constituents\":[\"A\"]}");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));

                Assert.Contains("weights", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphReportsOptionWhenItsEquitySourceIsMissing()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "option.json"), /*lang=json,strict*/ "{\"type\":\"EquityOptionNode\",\"name\":\"A_CALL\",\"underlyer\":\"A\",\"maturity\":\"1Y\",\"strike\":100,\"optionType\":\"Call\"}");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));

                Assert.Equal(
                    "Could not enrich graph definition 'EquityOptionNode::A_CALL': required graph definition 'EquitySource::A' was not found.",
                    exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsMissingDirectory()
        {
            _ = Assert.Throws<DirectoryNotFoundException>(() =>
                NodeGraphLoader.LoadGraph(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
        }

        [Fact]
        public void LoadGraphRejectsBlankDirectoryPath()
        {
            _ = Assert.Throws<ArgumentException>(() => NodeGraphLoader.LoadGraph(" "));
        }

        [Fact]
        public void LoadGraphRejectsDuplicateNames()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "a.json", "DUPLICATE");
                WriteDefinition(directory, "b.json", "duplicate");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));

                Assert.Contains("Duplicate graph node key", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsDefinitionsWithoutTypes()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ "{\"name\":\"A\"}");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));

                Assert.Contains("type", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsEmptyDirectory()
        {
            var directory = CreateDirectory();
            try
            {
                _ = Assert.Throws<InvalidOperationException>(() =>
                    NodeGraphLoader.LoadGraph(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsInvalidJson()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), "not json");

                _ = Assert.Throws<InvalidDataException>(() =>
                    NodeGraphLoader.LoadGraph(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsJsonArrays()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), "[]");

                var exception = Assert.Throws<InvalidDataException>(() =>
                    NodeGraphLoader.LoadGraph(directory));

                Assert.Contains("JSON object", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadGraphRejectsUnsupportedTypes()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ "{\"type\":\"UnknownNode\",\"name\":\"A\"}");

                _ = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadGraph(directory));
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

        private static void WriteDefinition(string directory, string fileName, string name)
        {
            File.WriteAllText(Path.Combine(directory, fileName), $"{{\"type\":\"EquitySource\",\"name\":\"{name}\",\"spot\":100,\"volatility\":0.2,\"meanTickTimeSeconds\":1}}");
        }
    }
}
