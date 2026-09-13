using System;
using System.Collections.Generic;
using System.Linq;

namespace EventGraph
{
    /// <summary>
    /// Coordinates the lifecycle of the active QuoteGraph against a market valuation clock.
    /// Automatically recompiles and reconnects a new graph when the valuation date changes.
    /// </summary>
    public sealed class GraphSession : IDisposable
    {
        private readonly string definitionDirectory;
        private readonly QuoteSubscriber subscriber;
        private readonly bool preserveStateAcrossDates;

        public IMarketClock Clock { get; }

        public QuoteGraph CurrentGraph { get; private set; }

        public GraphSession(
            string definitionDirectory,
            IMarketClock clock = null,
            QuoteSubscriber subscriber = null,
            bool preserveStateAcrossDates = true)
        {
            if (string.IsNullOrWhiteSpace(definitionDirectory))
            {
                throw new ArgumentException("A graph definition directory is required.", nameof(definitionDirectory));
            }

            this.definitionDirectory = definitionDirectory;
            Clock = clock ?? new MarketClock();
            this.subscriber = subscriber;
            this.preserveStateAcrossDates = preserveStateAcrossDates;

            RebuildGraph(Clock.Today);
            Clock.DateChanged += OnDateChanged;
        }

        public event EventHandler<QuoteGraph> GraphRebuilt;

        private void OnDateChanged(object sender, DateTime newDate)
        {
            RebuildGraph(newDate);
        }

        public void RebuildGraph(DateTime valuationDate)
        {
            IReadOnlyDictionary<string, double> spotState = null;
            if (preserveStateAcrossDates && CurrentGraph != null)
            {
                spotState = CurrentGraph.Nodes
                    .OfType<ISpotSourceNode>()
                    .ToDictionary(node => node.Name, node => node.Spot, StringComparer.OrdinalIgnoreCase);
            }

            TearDownCurrentGraph();

            CurrentGraph = NodeGraphLoader.LoadGraph(definitionDirectory, valuationDate, spotState);

            SetUpGraph(CurrentGraph);
            GraphRebuilt?.Invoke(this, CurrentGraph);
        }

        private void SetUpGraph(QuoteGraph graph)
        {
            if (subscriber != null)
            {
                foreach (var quote in graph.Nodes.OfType<EquitySource>())
                {
                    subscriber.Subscribe(quote);
                }

                foreach (var option in graph.Nodes.OfType<IEquityOptionNode>())
                {
                    subscriber.Subscribe(option);
                }

                foreach (var basket in graph.Nodes.OfType<BasketSpotNode>())
                {
                    subscriber.Subscribe(basket);
                }
            }

            GraphValidator.EnsureAcyclic(graph.Nodes);

            foreach (var basket in graph.Nodes.OfType<BasketSpotNode>())
            {
                basket.Connect();
            }
        }

        private void TearDownCurrentGraph()
        {
            if (CurrentGraph == null)
            {
                return;
            }

            foreach (var basket in CurrentGraph.Nodes.OfType<BasketSpotNode>())
            {
                basket.Disconnect();
            }

            if (subscriber != null)
            {
                foreach (var ticking in CurrentGraph.Nodes.OfType<ITickingNode>())
                {
                    subscriber.Unsubscribe(ticking);
                }
            }
        }

        public void Dispose()
        {
            Clock.DateChanged -= OnDateChanged;
            TearDownCurrentGraph();
        }
    }
}
