using System;
using System.IO;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class NodeGraphLoaderTests
{
    [Fact]
    public void LoadNodesCreatesNodesFromJson()
    {
        var directory = CreateDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "source.json"), """
            {
                            "type": "SimulatedQuoteSource",
              "name": "JSON",
              "spot": 123.0,
              "volatility": 0.15,
              "meanTickTimeSeconds": 2.0,
              "futureProperty": "owned by the source node"
            }
            """);

            var sources = NodeGraphLoader.LoadNodes(directory);

            var source = Assert.Single(sources);
            Assert.Equal("JSON", source.Name);
            Assert.Equal("SimulatedSpot", source.Type);
            Assert.Equal(123.0, source.CurrentValue);
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
            File.WriteAllText(Path.Combine(directory, "basket.json"), """
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
    public void LoadNodesRejectsBasketWithUnknownSource()
    {
        var directory = CreateDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "basket.json"), "{\"type\":\"BasketAggregate\",\"name\":\"B\",\"names\":[\"Missing\"],\"weights\":[1]}");

            Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));
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
            File.WriteAllText(Path.Combine(directory, "source.json"), "{\"type\":\"SimulatedQuoteSource\",\"name\":\"A\",\"spot\":100}");

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
            File.WriteAllText(Path.Combine(directory, "basket.json"), "{\"type\":\"BasketAggregate\",\"name\":\"Basket\",\"names\":[\"A\"]}");

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
        Assert.Throws<DirectoryNotFoundException>(() =>
            NodeGraphLoader.LoadNodes(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }

    [Fact]
    public void LoadNodesRejectsBlankDirectoryPath()
    {
        Assert.Throws<ArgumentException>(() => NodeGraphLoader.LoadNodes(" "));
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
            File.WriteAllText(Path.Combine(directory, "source.json"), "{\"name\":\"A\"}");

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
            File.WriteAllText(Path.Combine(directory, "a.json"), "{\"type\":\"BasketAggregate\",\"name\":\"A\",\"names\":[\"B\"],\"weights\":[1]}");
            File.WriteAllText(Path.Combine(directory, "b.json"), "{\"type\":\"BasketAggregate\",\"name\":\"B\",\"names\":[\"A\"],\"weights\":[1]}");

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
            Assert.Throws<InvalidOperationException>(() =>
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

            Assert.Throws<InvalidDataException>(() =>
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
            File.WriteAllText(Path.Combine(directory, "source.json"), "{\"type\":\"UnknownNode\",\"name\":\"A\"}");

            Assert.Throws<InvalidDataException>(() => NodeGraphLoader.LoadNodes(directory));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteDefinition(string directory, string fileName, string name)
    {
        File.WriteAllText(Path.Combine(directory, fileName), $"{{\"type\":\"SimulatedQuoteSource\",\"name\":\"{name}\",\"spot\":100,\"volatility\":0.2,\"meanTickTimeSeconds\":1}}");
    }
}
