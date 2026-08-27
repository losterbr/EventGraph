namespace EventGraph.Tests
{
    public class NodeGraphLoaderTests
    {
        [Fact]
        public void LoadNodesCreatesNodesFromJson()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ """
            {
                            "type": "SimulatedAssetSource",
              "name": "JSON",
              "currency": "USD",
              "spot": 123.0,
              "volatility": 0.15,
              "meanTickTimeSeconds": 2.0,
              "futureProperty": "owned by the source node"
            }
            """);

                var sources = NodeGraphLoader.LoadNodes(directory);

                var source = Assert.Single(sources);
                Assert.Equal("JSON", source.Name);
                Assert.Equal("USD", source.Currency);
                Assert.Equal("SimulatedSpot", source.Type);
                Assert.Equal(123.0, source.Spot);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void GraphDefinitionLoaderDelegatesToNodeGraphLoader()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "source.json", "A");

#pragma warning disable CS0618
                var nodes = GraphDefinitionLoader.LoadNodes(directory);
#pragma warning restore CS0618

                var source = Assert.Single(nodes);
                Assert.Equal("A", source.Name);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesReadsFilesInStableOrder()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "b.json", "B");
                WriteDefinition(directory, "a.json", "A");

                var sources = NodeGraphLoader.LoadNodes(directory);

                Assert.Equal("A", sources[0].Name);
                Assert.Equal("B", sources[1].Name);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesCreatesBasketFromNamedSourcesAndWeights()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "a.json", "A");
                WriteDefinition(directory, "b.json", "B");
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ """
            {
              "type": "BasketAggregate",
              "name": "EquityBasket",
              "names": ["A", "B"],
              "weights": [0.25, 0.75]
            }
            """);

                var nodes = NodeGraphLoader.LoadNodes(directory);

                var basket = Assert.IsType<BasketAggregate>(nodes[2]);
                Assert.Equal("EquityBasket", basket.Name);
                Assert.Equal("A=0.25, B=0.75", basket.GetWeights());
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
              "type": "BasketAggregate",
              "name": "EquityBasket",
              "names": ["A", "B"],
              "weights": [0.25, 0.75]
            }
            """);

                var graph = NodeGraphLoader.LoadGraph(directory);

                Assert.Equal(["A", "B", "EquityBasket"], graph.Nodes.Select(node => node.Name));
                Assert.Equal(0, graph.GetIndex("a"));
                Assert.Equal(1, graph.NodeIndexByName["B"]);
                Assert.Equal([0, 1], graph.DependenciesByNode[graph.GetIndex("EquityBasket")]);
                Assert.Empty(graph.DependenciesByNode[graph.GetIndex("A")]);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsBasketWithUnknownSource()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"B\",\"names\":[\"Missing\"],\"weights\":[1]}");

                _ = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesReportsMissingSourcePropertyFromSourceNode()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ "{\"type\":\"SimulatedAssetSource\",\"name\":\"A\",\"spot\":100}");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));

                Assert.Contains("volatility", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesReportsMissingBasketPropertyFromBasketNode()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "source.json", "A");
                File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"Basket\",\"names\":[\"A\"]}");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));

                Assert.Contains("weights", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsMissingDirectory()
        {
            _ = Assert.Throws<DirectoryNotFoundException>(() =>
                NodeGraphLoader.LoadNodes(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
        }

        [Fact]
        public void LoadNodesRejectsBlankDirectoryPath()
        {
            _ = Assert.Throws<ArgumentException>(() => NodeGraphLoader.LoadNodes(" "));
        }

        [Fact]
        public void LoadNodesRejectsDuplicateNames()
        {
            var directory = CreateDirectory();
            try
            {
                WriteDefinition(directory, "a.json", "DUPLICATE");
                WriteDefinition(directory, "b.json", "duplicate");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));

                Assert.Contains("Duplicate graph node name", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsDefinitionsWithoutTypes()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ "{\"name\":\"A\"}");

                var exception = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));

                Assert.Contains("type", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsCyclicDependenciesBeforeCreatingNodes()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "a.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"A\",\"names\":[\"B\"],\"weights\":[1]}");
                File.WriteAllText(Path.Combine(directory, "b.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"B\",\"names\":[\"A\"],\"weights\":[1]}");

                var exception = Assert.Throws<InvalidOperationException>(() => NodeGraphLoader.LoadNodes(directory));

                Assert.Contains("Unable to satisfy node dependencies", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsEmptyDirectory()
        {
            var directory = CreateDirectory();
            try
            {
                _ = Assert.Throws<InvalidOperationException>(() =>
                    NodeGraphLoader.LoadNodes(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsInvalidJson()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), "not json");

                _ = Assert.Throws<InvalidDataException>(() =>
                    NodeGraphLoader.LoadNodes(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsJsonArrays()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), "[]");

                var exception = Assert.Throws<InvalidDataException>(() =>
                    NodeGraphLoader.LoadNodes(directory));

                Assert.Contains("JSON object", exception.Message);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void LoadNodesRejectsUnsupportedTypes()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "source.json"), /*lang=json,strict*/ "{\"type\":\"UnknownNode\",\"name\":\"A\"}");

                _ = Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));
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
            File.WriteAllText(Path.Combine(directory, fileName), $"{{\"type\":\"SimulatedAssetSource\",\"name\":\"{name}\",\"spot\":100,\"volatility\":0.2,\"meanTickTimeSeconds\":1}}");
        }
    }
}
