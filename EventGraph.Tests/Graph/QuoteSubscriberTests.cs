using System.Reflection;

namespace EventGraph.Tests
{
    [Collection(ConsoleTestGroup.Name)]
    public class QuoteSubscriberTests
    {
        private static IReadOnlyList<ISpotNode> CreateSpotNodes(IEnumerable<EquitySource> sources)
        {
            return [.. sources.Select(source => new SpotNode(source))];
        }

        private static string[] GetOutputLines(StringWriter output)
        {
            return output.ToString()
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetIdentifier(string line)
        {
            var timestampEnd = line.IndexOf(']');
            Assert.True(timestampEnd >= 0, $"Expected timestamp prefix in line: '{line}'");

            var contentStart = timestampEnd + 2;
            var contentEnd = line.IndexOf(" updated to", contentStart, StringComparison.Ordinal);
            Assert.True(contentEnd > contentStart, $"Expected update marker in line: '{line}'");

            return line[contentStart..contentEnd];
        }

        [Fact]
        public async Task QuoteSubscriberSubscribesAndFormatsIdentifiers()
        {
            var output = new StringWriter();
            var originalOut = Console.Out;

            try
            {
                Console.SetOut(output);
                var subscriber = new QuoteSubscriber();
                var source = new EquitySource("XYZ", 100.0, 0.0, 0.0);

                subscriber.Subscribe(source);
                await source.Start(1);

                var rendered = output.ToString();
                Assert.Contains("Subscribed to XYZ", rendered);
                Assert.Contains("EquitySource::XYZ", rendered);

                var sourceLine = GetOutputLines(output)
                    .Single(line => line.Contains("EquitySource::XYZ") && line.Contains("updated to"));
                var sourceIdentifier = GetIdentifier(sourceLine);

                Assert.Equal(40, sourceIdentifier.Length);
                Assert.StartsWith("EquitySource::XYZ", sourceIdentifier);

                var discountFactor = new RateCurveNode(new CurrencyRateSource("USD", 0.05));
                var forward = new ForwardCurveNode(new SpotNode(source), discountFactor);
                var volatility = new VolatilityNode(source);
                var option = new EquityOptionNode("XYZ_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0);
                subscriber.Subscribe(option);
                await source.Start(1);

                Assert.Contains("Subscribed to XYZ_CALL", output.ToString());
                var optionLine = GetOutputLines(output)
                    .Single(line => line.Contains("EquityOptionNode::XYZ_CALL") && line.Contains("updated to"));
                Assert.Equal(40, GetIdentifier(optionLine).Length);

                var sources = new[]
                {
                    new EquitySource("TSLA", 100.0, 0.0, 0.0),
                    new EquitySource("GOOG", 200.0, 0.0, 0.0),
                    new EquitySource("AMZN", 300.0, 0.0, 0.0),
                    new EquitySource("MSFT", 400.0, 0.0, 0.0)
                };

                var basket = new BasketSpotNode(CreateSpotNodes(sources));
                subscriber.Subscribe(basket);
                await basket.RunOnceAsync();

                var basketLine = GetOutputLines(output)
                    .Single(line => line.Contains("BasketSpotNode::") && line.Contains("updated to"));
                var basketIdentifier = GetIdentifier(basketLine);

                Assert.Equal(40, basketIdentifier.Length);
                Assert.Equal("BasketSpotNode::B TSLA,GOOG,AMZN,MSFT", basketIdentifier.TrimEnd());
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public async Task QuoteSubscriberKeepsConcurrentUpdatesOnSeparateLines()
        {
            var output = new StringWriter();
            var originalOut = Console.Out;

            try
            {
                Console.SetOut(output);
                var subscriber = new QuoteSubscriber();
                var sources = new[]
                {
                    new EquitySource("A", 100.0, 0.0, 0.0),
                    new EquitySource("B", 200.0, 0.0, 0.0),
                    new EquitySource("C", 300.0, 0.0, 0.0)
                };

                foreach (var source in sources)
                {
                    subscriber.Subscribe(source);
                }

                await Task.WhenAll(sources.Select(source => source.Start(1)));

                var updateLines = GetOutputLines(output)
                    .Where(line => line.Contains("EquitySource::A") || line.Contains("EquitySource::B") || line.Contains("EquitySource::C"))
                    .ToArray();

                Assert.Equal(3, updateLines.Length);
                Assert.All(updateLines, line => Assert.StartsWith("[", line));
                Assert.All(updateLines, line => Assert.Contains("updated to", line));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public async Task QuoteSubscriberLogsConstituentBeforeBasketUpdate()
        {
            var output = new StringWriter();
            var originalOut = Console.Out;

            try
            {
                Console.SetOut(output);
                var subscriber = new QuoteSubscriber();
                var sources = new[]
                {
                    new EquitySource("A", 100.0, 0.0, 0.0),
                    new EquitySource("B", 200.0, 0.0, 0.0)
                };

                subscriber.Subscribe(sources[0]);
                subscriber.Subscribe(sources[1]);
                var basket = new BasketSpotNode(CreateSpotNodes(sources));
                subscriber.Subscribe(basket);
                basket.Connect();

                await sources[0].Start(1);
                await sources[1].Start(1);

                var lines = GetOutputLines(output);
                var sourceIndex = Array.FindIndex(lines, line => line.Contains("EquitySource::B") && line.Contains("updated to"));
                var basketIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::B A,B") && line.Contains("updated to"));

                Assert.True(sourceIndex >= 0);
                Assert.True(basketIndex > sourceIndex);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void QuoteSubscriberCyclesDefaultColorsAcrossSourcesAndBaskets()
        {
            var subscriber = new QuoteSubscriber();
            var expectedPalette = new[]
            {
                ConsoleColor.DarkBlue,
                ConsoleColor.DarkGreen,
                ConsoleColor.DarkCyan,
                ConsoleColor.DarkRed,
                ConsoleColor.DarkMagenta,
                ConsoleColor.DarkYellow,
                ConsoleColor.Blue,
                ConsoleColor.Green,
                ConsoleColor.Cyan,
                ConsoleColor.Red,
                ConsoleColor.Magenta,
                ConsoleColor.Yellow,
                ConsoleColor.Gray,
                ConsoleColor.DarkGray
            };
            var sources = Enumerable.Range(0, expectedPalette.Length + 1)
                .Select(index => new EquitySource($"SOURCE_{index}", 100.0, 0.0, 0.0))
                .ToArray();
            var baskets = sources
                .Select((source, index) => new BasketSpotNode($"BASKET_{index}", [new SpotNode(source)]))
                .ToArray();

            foreach (var source in sources)
            {
                subscriber.Subscribe(source);
            }

            foreach (var basket in baskets)
            {
                subscriber.Subscribe(basket);
            }

            var colorsField = typeof(QuoteSubscriber).GetField("nodeColors", BindingFlags.Instance | BindingFlags.NonPublic);
            var nodeColors = (IDictionary<IGraphNode, ConsoleColor>)colorsField!.GetValue(subscriber)!;

            Assert.Equal(expectedPalette, sources.Take(expectedPalette.Length).Select(source => nodeColors[source]));
            Assert.Equal(ConsoleColor.Blue, nodeColors[sources[6]]);
            Assert.Equal(expectedPalette[0], nodeColors[sources.Last()]);
            Assert.Equal(expectedPalette, baskets.Take(expectedPalette.Length).Select(basket => nodeColors[basket]));
            Assert.Equal(expectedPalette[0], nodeColors[baskets.Last()]);
        }

        [Fact]
        public async Task QuoteSubscriberHandlesQuietAndCustomColorModes()
        {
            var output = new StringWriter();
            var originalOut = Console.Out;
            var originalColor = Console.ForegroundColor;

            try
            {
                Console.SetOut(output);
                var quietSubscriber = new QuoteSubscriber(quiet: true);
                var source = new EquitySource("XYZ", 100.0, 0.0, 0.0);

                quietSubscriber.Subscribe(source);
                await source.Start(1);
                Assert.Empty(output.ToString());

                _ = output.GetStringBuilder().Clear();

                var coloredSubscriber = new QuoteSubscriber(quiet: false, basketColor: ConsoleColor.Red);
                var sources = new[]
                {
                    new EquitySource("A", 100.0, 0.0, 0.0),
                    new EquitySource("B", 200.0, 0.0, 0.0)
                };
                var basket = new BasketSpotNode(CreateSpotNodes(sources));

                coloredSubscriber.Subscribe(basket);
                await basket.RunOnceAsync();

                Assert.Contains("Subscribed to B A,B", output.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.ForegroundColor = originalColor;
            }
        }

        [Fact]
        public void NodeGraphLoaderResolvesBasketDependenciesRecursively()
        {
            var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _ = Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(Path.Combine(directory, "a.json"), /*lang=json,strict*/ "{\"type\":\"EquitySource\",\"name\":\"A\",\"spot\":10,\"volatility\":0,\"meanTickTimeSeconds\":1}");
                File.WriteAllText(Path.Combine(directory, "b.json"), /*lang=json,strict*/ "{\"type\":\"BasketSpotNode\",\"name\":\"B\",\"constituents\":[\"A\"],\"weights\":[1.0]}");
                File.WriteAllText(Path.Combine(directory, "c.json"), /*lang=json,strict*/ "{\"type\":\"BasketSpotNode\",\"name\":\"C\",\"constituents\":[\"B\"],\"weights\":[1.0]}");

                var nodes = NodeGraphLoader.LoadGraph(directory).Nodes;

                var basketByName = nodes.OfType<BasketSpotNode>().ToDictionary(node => node.Name, StringComparer.OrdinalIgnoreCase);
                Assert.True(basketByName.ContainsKey("B"));
                Assert.True(basketByName.ContainsKey("C"));
                Assert.All(basketByName["B"].Dependencies, dependency => Assert.IsAssignableFrom<ISpotNode>(dependency));
                Assert.All(basketByName["C"].Dependencies, dependency => Assert.IsAssignableFrom<ISpotNode>(dependency));
                Assert.Contains(basketByName["B"].Dependencies, dependency => dependency.Name == "A");
                Assert.Contains(basketByName["C"].Dependencies, dependency => dependency.Name == "B");

                var order = nodes.Select(node => node.Name).ToList();
                Assert.True(order.IndexOf("A") < order.IndexOf("B"));
                Assert.True(order.IndexOf("B") < order.IndexOf("C"));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void NodeGraphLoaderResolvesAllNodesBeforeAnyDependentNodeIsBuilt()
        {
            var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _ = Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(Path.Combine(directory, "alpha.json"), /*lang=json,strict*/ "{\"type\":\"EquitySource\",\"name\":\"ALPHA\",\"spot\":10,\"volatility\":0,\"meanTickTimeSeconds\":1}");
                File.WriteAllText(Path.Combine(directory, "beta.json"), /*lang=json,strict*/ "{\"type\":\"EquitySource\",\"name\":\"BETA\",\"spot\":20,\"volatility\":0,\"meanTickTimeSeconds\":1}");
                File.WriteAllText(Path.Combine(directory, "mix.json"), /*lang=json,strict*/ "{\"type\":\"BasketSpotNode\",\"name\":\"MIX\",\"constituents\":[\"ALPHA\",\"BETA\"],\"weights\":[0.5,0.5]}");
                File.WriteAllText(Path.Combine(directory, "combo.json"), /*lang=json,strict*/ "{\"type\":\"BasketSpotNode\",\"name\":\"COMBO\",\"constituents\":[\"MIX\"],\"weights\":[1.0]}");

                var nodes = NodeGraphLoader.LoadNodes(directory);
                var order = nodes.Select(node => node.Name).ToList();

                Assert.True(order.IndexOf("ALPHA") < order.IndexOf("MIX"));
                Assert.True(order.IndexOf("BETA") < order.IndexOf("MIX"));
                Assert.True(order.IndexOf("MIX") < order.IndexOf("COMBO"));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public async Task QuoteSubscriberLogsParentBasketBeforeDependentBasket()
        {
            var output = new StringWriter();
            var originalOut = Console.Out;

            try
            {
                Console.SetOut(output);

                var baseSource = new EquitySource("BASE", 100.0, 0.0, 0.0);
                var childSource = new EquitySource("CHILD", 200.0, 0.0, 0.0);
                var parent = new BasketSpotNode("PARENT", [new SpotNode(baseSource)]);
                var child = new BasketSpotNode("CHILD_BASKET", [parent, new SpotNode(childSource)]);
                var subscriber = new QuoteSubscriber();

                subscriber.Subscribe(parent);
                subscriber.Subscribe(child);
                parent.Connect();
                child.Connect();

                await Task.WhenAll(baseSource.Start(1), childSource.Start(1));

                var lines = GetOutputLines(output)
                    .Where(line => line.Contains("BasketSpotNode::PARENT") || line.Contains("BasketSpotNode::CHILD_BASKET"))
                    .ToArray();

                Assert.NotEmpty(lines);

                var parentIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::PARENT"));
                var childIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::CHILD_BASKET"));

                Assert.True(parentIndex >= 0, "The upstream basket should emit before the dependent basket.");
                Assert.True(childIndex > parentIndex, "A dependent basket must not log before its parent basket has updated.");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public async Task QuoteSubscriberLogsNestedBasketHierarchyInDependencyOrder()
        {
            var output = new StringWriter();
            var originalOut = Console.Out;

            try
            {
                Console.SetOut(output);

                var sourceA = new EquitySource("A", 100.0, 0.0, 0.0);
                var sourceB = new EquitySource("B", 200.0, 0.0, 0.0);
                var sourceC = new EquitySource("C", 300.0, 0.0, 0.0);
                var sourceD = new EquitySource("D", 400.0, 0.0, 0.0);

                var parent = new BasketSpotNode("PARENT", [new SpotNode(sourceA), new SpotNode(sourceB)]);
                var child = new BasketSpotNode("CHILD", [parent, new SpotNode(sourceC)]);
                var root = new BasketSpotNode("ROOT", [child, new SpotNode(sourceD)]);
                var subscriber = new QuoteSubscriber();

                foreach (var source in new IGraphNode[] { sourceA, sourceB, sourceC, sourceD, parent, child, root })
                {
                    if (source is BasketSpotNode basket)
                    {
                        subscriber.Subscribe(basket);
                    }
                    else
                    {
                        subscriber.Subscribe((EquitySource)source);
                    }
                }

                parent.Connect();
                child.Connect();
                root.Connect();

                await Task.WhenAll(
                    sourceA.Start(1),
                    sourceB.Start(1),
                    sourceC.Start(1),
                    sourceD.Start(1));

                var lines = GetOutputLines(output)
                    .Where(line => line.Contains("BasketSpotNode::PARENT") || line.Contains("BasketSpotNode::CHILD") || line.Contains("BasketSpotNode::ROOT"))
                    .ToArray();

                var parentIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::PARENT"));
                var childIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::CHILD"));
                var rootIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::ROOT"));

                Assert.True(parentIndex >= 0, "The root basket should emit when its dependencies are available.");
                Assert.True(childIndex > parentIndex, "A basket depending on a parent basket must wait for that parent update.");
                Assert.True(rootIndex > childIndex, "A nested basket must log after the basket it depends on.");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public async Task QuoteSubscriberLogsAllParentBasketsBeforeDependentBasket()
        {
            var output = new StringWriter();
            var originalOut = Console.Out;

            try
            {
                Console.SetOut(output);

                var sourceA = new EquitySource("A", 100.0, 0.0, 0.0);
                var sourceB = new EquitySource("B", 200.0, 0.0, 0.0);
                var sourceC = new EquitySource("C", 300.0, 0.0, 0.0);
                var sourceD = new EquitySource("D", 400.0, 0.0, 0.0);

                var leftParent = new BasketSpotNode("LEFT_PARENT", [new SpotNode(sourceA), new SpotNode(sourceB)]);
                var rightParent = new BasketSpotNode("RIGHT_PARENT", [new SpotNode(sourceC), new SpotNode(sourceD)]);
                var child = new BasketSpotNode("CHILD", [leftParent, rightParent]);
                var subscriber = new QuoteSubscriber();

                subscriber.Subscribe(leftParent);
                subscriber.Subscribe(rightParent);
                subscriber.Subscribe(child);

                leftParent.Connect();
                rightParent.Connect();
                child.Connect();

                await Task.WhenAll(
                    sourceA.Start(1),
                    sourceB.Start(1),
                    sourceC.Start(1),
                    sourceD.Start(1));

                var lines = GetOutputLines(output)
                    .Where(line => line.Contains("BasketSpotNode::LEFT_PARENT") || line.Contains("BasketSpotNode::RIGHT_PARENT") || line.Contains("BasketSpotNode::CHILD"))
                    .ToArray();

                var leftIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::LEFT_PARENT"));
                var rightIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::RIGHT_PARENT"));
                var childIndex = Array.FindIndex(lines, line => line.Contains("BasketSpotNode::CHILD"));

                Assert.True(leftIndex >= 0, "The first parent basket should update.");
                Assert.True(rightIndex >= 0, "The second parent basket should update.");
                Assert.True(childIndex > leftIndex, "The child basket must log after its left parent.");
                Assert.True(childIndex > rightIndex, "The child basket must log after each of its parents.");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
