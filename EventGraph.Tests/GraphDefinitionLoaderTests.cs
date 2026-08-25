using System;
using System.IO;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class GraphDefinitionLoaderTests
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
              "meanTickTimeSeconds": 2.0
            }
            """);

            var sources = GraphDefinitionLoader.LoadNodes(directory);

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
    public void LoadNodesReadsFilesInStableOrder()
    {
        var directory = CreateDirectory();
        try
        {
            WriteDefinition(directory, "b.json", "B");
            WriteDefinition(directory, "a.json", "A");

            var sources = GraphDefinitionLoader.LoadNodes(directory);

            Assert.Equal("A", sources[0].Name);
            Assert.Equal("B", sources[1].Name);
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
            GraphDefinitionLoader.LoadNodes(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }

    [Fact]
    public void LoadNodesRejectsEmptyDirectory()
    {
        var directory = CreateDirectory();
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                GraphDefinitionLoader.LoadNodes(directory));
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
                GraphDefinitionLoader.LoadNodes(directory));
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
            File.WriteAllText(Path.Combine(directory, "source.json"), "{\"type\":\"UnknownNode\"}");

            Assert.Throws<InvalidDataException>(() => GraphDefinitionLoader.LoadNodes(directory));
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
