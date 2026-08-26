using System.Reflection;

namespace EventGraph.Tests
{
    [Collection(ConsoleTestGroup.Name)]
    public class QuoteSubscriberTests
    {
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
                var source = new SimulatedAssetSource("XYZ", 100.0, 0.0, 0.0);

                subscriber.Subscribe(source);
                await source.Start(1);

                var rendered = output.ToString();
                Assert.Contains("Subscribed to XYZ", rendered);
                Assert.Contains("SimulatedSpot::XYZ", rendered);

                var sourceLine = GetOutputLines(output)
                    .Single(line => line.Contains("SimulatedSpot::XYZ") && line.Contains("updated to"));
                var sourceIdentifier = GetIdentifier(sourceLine);

                Assert.Equal(40, sourceIdentifier.Length);
                Assert.StartsWith("SimulatedSpot::XYZ", sourceIdentifier);

                var sources = new[]
                {
                    new SimulatedAssetSource("TSLA", 100.0, 0.0, 0.0),
                    new SimulatedAssetSource("GOOG", 200.0, 0.0, 0.0),
                    new SimulatedAssetSource("AMZN", 300.0, 0.0, 0.0),
                    new SimulatedAssetSource("MSFT", 400.0, 0.0, 0.0)
                };

                var basket = new BasketAggregate(sources);
                subscriber.Subscribe(basket);
                await basket.RunOnceAsync();

                var basketLine = GetOutputLines(output)
                    .Single(line => line.Contains("CalculatedBasket::B TSLA,GOOG,AMZN,MSFT") && line.Contains("updated to"));
                var basketIdentifier = GetIdentifier(basketLine);

                Assert.Equal(40, basketIdentifier.Length);
                Assert.StartsWith("CalculatedBasket::B TSLA,GOOG,AMZN,MSFT", basketIdentifier);
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
                    new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                    new SimulatedAssetSource("B", 200.0, 0.0, 0.0),
                    new SimulatedAssetSource("C", 300.0, 0.0, 0.0)
                };

                foreach (var source in sources)
                {
                    subscriber.Subscribe(source);
                }

                await Task.WhenAll(sources.Select(source => source.Start(1)));

                var updateLines = GetOutputLines(output)
                    .Where(line => line.Contains("SimulatedSpot::A") || line.Contains("SimulatedSpot::B") || line.Contains("SimulatedSpot::C"))
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
                    new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                    new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
                };

                subscriber.Subscribe(sources[0]);
                subscriber.Subscribe(sources[1]);
                var basket = new BasketAggregate(sources);
                subscriber.Subscribe(basket);
                basket.Connect();

                await sources[0].Start(1);
                await sources[1].Start(1);

                var lines = GetOutputLines(output);
                var sourceIndex = Array.FindIndex(lines, line => line.Contains("SimulatedSpot::B") && line.Contains("updated to"));
                var basketIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::B A,B") && line.Contains("updated to"));

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
                .Select(index => new SimulatedAssetSource($"SOURCE_{index}", 100.0, 0.0, 0.0))
                .ToArray();
            var baskets = sources
                .Select((source, index) => new BasketAggregate($"BASKET_{index}", [source]))
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
                var source = new SimulatedAssetSource("XYZ", 100.0, 0.0, 0.0);

                quietSubscriber.Subscribe(source);
                await source.Start(1);
                Assert.Empty(output.ToString());

                _ = output.GetStringBuilder().Clear();

                var coloredSubscriber = new QuoteSubscriber(quiet: false, basketColor: ConsoleColor.Red);
                var sources = new[]
                {
                    new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                    new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
                };
                var basket = new BasketAggregate(sources);

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
                File.WriteAllText(Path.Combine(directory, "a.json"), /*lang=json,strict*/ "{\"type\":\"SimulatedAssetSource\",\"name\":\"A\",\"spot\":10,\"volatility\":0,\"meanTickTimeSeconds\":1}");
                File.WriteAllText(Path.Combine(directory, "b.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"B\",\"names\":[\"A\"],\"weights\":[1.0]}");
                File.WriteAllText(Path.Combine(directory, "c.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"C\",\"names\":[\"B\"],\"weights\":[1.0]}");

                var nodes = NodeGraphLoader.LoadNodes(directory);

                var nodeByName = nodes.ToDictionary(node => node.Name, StringComparer.OrdinalIgnoreCase);
                Assert.True(nodeByName.ContainsKey("A"));
                Assert.True(nodeByName.ContainsKey("B"));
                Assert.True(nodeByName.ContainsKey("C"));
                Assert.Contains(nodeByName["B"].Dependencies, dependency => dependency.Name == "A");
                Assert.Contains(nodeByName["C"].Dependencies, dependency => dependency.Name == "B");

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
                File.WriteAllText(Path.Combine(directory, "alpha.json"), /*lang=json,strict*/ "{\"type\":\"SimulatedAssetSource\",\"name\":\"ALPHA\",\"spot\":10,\"volatility\":0,\"meanTickTimeSeconds\":1}");
                File.WriteAllText(Path.Combine(directory, "beta.json"), /*lang=json,strict*/ "{\"type\":\"SimulatedAssetSource\",\"name\":\"BETA\",\"spot\":20,\"volatility\":0,\"meanTickTimeSeconds\":1}");
                File.WriteAllText(Path.Combine(directory, "mix.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"MIX\",\"names\":[\"ALPHA\",\"BETA\"],\"weights\":[0.5,0.5]}");
                File.WriteAllText(Path.Combine(directory, "combo.json"), /*lang=json,strict*/ "{\"type\":\"BasketAggregate\",\"name\":\"COMBO\",\"names\":[\"MIX\"],\"weights\":[1.0]}");

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

                var baseSource = new SimulatedAssetSource("BASE", 100.0, 0.0, 0.0);
                var childSource = new SimulatedAssetSource("CHILD", 200.0, 0.0, 0.0);
                var parent = new BasketAggregate("PARENT", [baseSource]);
                var child = new BasketAggregate("CHILD_BASKET", [parent, childSource]);
                var subscriber = new QuoteSubscriber();

                subscriber.Subscribe(parent);
                subscriber.Subscribe(child);
                parent.Connect();
                child.Connect();

                await Task.WhenAll(baseSource.Start(1), childSource.Start(1));

                var lines = GetOutputLines(output)
                    .Where(line => line.Contains("CalculatedBasket::PARENT") || line.Contains("CalculatedBasket::CHILD_BASKET"))
                    .ToArray();

                Assert.NotEmpty(lines);

                var parentIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::PARENT"));
                var childIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::CHILD_BASKET"));

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

                var sourceA = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
                var sourceB = new SimulatedAssetSource("B", 200.0, 0.0, 0.0);
                var sourceC = new SimulatedAssetSource("C", 300.0, 0.0, 0.0);
                var sourceD = new SimulatedAssetSource("D", 400.0, 0.0, 0.0);

                var parent = new BasketAggregate("PARENT", [sourceA, sourceB]);
                var child = new BasketAggregate("CHILD", [parent, sourceC]);
                var root = new BasketAggregate("ROOT", [child, sourceD]);
                var subscriber = new QuoteSubscriber();

                foreach (var source in new IGraphNode[] { sourceA, sourceB, sourceC, sourceD, parent, child, root })
                {
                    if (source is BasketAggregate basket)
                    {
                        subscriber.Subscribe(basket);
                    }
                    else
                    {
                        subscriber.Subscribe((SimulatedAssetSource)source);
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
                    .Where(line => line.Contains("CalculatedBasket::PARENT") || line.Contains("CalculatedBasket::CHILD") || line.Contains("CalculatedBasket::ROOT"))
                    .ToArray();

                var parentIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::PARENT"));
                var childIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::CHILD"));
                var rootIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::ROOT"));

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

                var sourceA = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
                var sourceB = new SimulatedAssetSource("B", 200.0, 0.0, 0.0);
                var sourceC = new SimulatedAssetSource("C", 300.0, 0.0, 0.0);
                var sourceD = new SimulatedAssetSource("D", 400.0, 0.0, 0.0);

                var leftParent = new BasketAggregate("LEFT_PARENT", [sourceA, sourceB]);
                var rightParent = new BasketAggregate("RIGHT_PARENT", [sourceC, sourceD]);
                var child = new BasketAggregate("CHILD", [leftParent, rightParent]);
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
                    .Where(line => line.Contains("CalculatedBasket::LEFT_PARENT") || line.Contains("CalculatedBasket::RIGHT_PARENT") || line.Contains("CalculatedBasket::CHILD"))
                    .ToArray();

                var leftIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::LEFT_PARENT"));
                var rightIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::RIGHT_PARENT"));
                var childIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::CHILD"));

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
