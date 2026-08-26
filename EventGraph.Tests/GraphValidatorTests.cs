using System;
using System.Collections.Generic;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class GraphValidatorTests
{
    [Fact]
    public void EnsureAcyclicAcceptsAnAcyclicGraph()
    {
        var source = new TestNode("Source");
        var left = new TestNode("Left", source);
        var right = new TestNode("Right", source);

        GraphValidator.EnsureAcyclic(new[] { left, right });
    }

    [Fact]
    public void EnsureAcyclicRejectsASelfCycle()
    {
        var node = new TestNode("Self");
        node.SetDependencies(node);

        var exception = Assert.Throws<InvalidOperationException>(() => GraphValidator.EnsureAcyclic(new[] { node }));

        Assert.Contains("Self", exception.Message);
    }

    [Fact]
    public void EnsureAcyclicRejectsAnIndirectCycle()
    {
        var first = new TestNode("First");
        var second = new TestNode("Second", first);
        first.SetDependencies(second);

        Assert.Throws<InvalidOperationException>(() => GraphValidator.EnsureAcyclic(new[] { first }));
    }

    [Fact]
    public void EnsureAcyclicRejectsNullRoots()
    {
        Assert.Throws<ArgumentNullException>(() => GraphValidator.EnsureAcyclic(null!));
    }

    [Fact]
    public void EnsureAcyclicRejectsNullNodes()
    {
        Assert.Throws<ArgumentException>(() => GraphValidator.EnsureAcyclic(new IQuoteNode?[] { null! }));
    }

    private sealed class TestNode : IQuoteNode
    {
        private IReadOnlyList<IQuoteNode> dependencies;

        public TestNode(string name, params IQuoteNode[] dependencies)
        {
            Name = name;
            this.dependencies = dependencies;
        }

        public event EventHandler<QuoteTick> Tick
        {
            add { }
            remove { }
        }

        public string Name { get; }

        public string Type => "TestNode";

        public double CurrentValue => 0.0;

        public IReadOnlyList<IQuoteNode> Dependencies => dependencies;

        public void SetDependencies(params IQuoteNode[] dependencies)
        {
            this.dependencies = dependencies;
        }
    }
}