# EventGraph

EventGraph is a small .NET sample that demonstrates an event-driven market quote pipeline using simulated instruments and basket aggregation.

## Overview

- Simulates stock-like quotes with `SimulatedQuoteSource`.
- Subscribes the same `QuoteSubscriber` to both individual quotes and an aggregated basket.
- Emits basket updates only after all constituent values are available for the current cycle.
- Validates the quote-node dependency graph at startup and rejects cycles before processing begins.
- Keeps the sample self-contained, deterministic enough for tests, and suitable for public release as a demo project.

### Graph definitions

Each JSON file in `EventGraph/graph definition` defines one `SimulatedQuoteSource`. The required fields are:

```json
{
	"type": "SimulatedQuoteSource",
	"name": "TSLA",
	"spot": 800.0,
	"volatility": 0.2,
	"meanTickTimeSeconds": 3.0
}
```

The `type` field selects the node implementation. Each node constructor owns the interpretation and validation of its complete JSON property dictionary, so adding or removing a property only requires changing that node's code. A `BasketAggregate` definition uses `name`, `names`, and `weights` to reference source nodes and assign their weights. The application loads all JSON definitions from this folder at startup, in filename order. Terminal colors are assigned by `QuoteSubscriber`, not stored as node properties.

## How it works

### Quote simulation

Each simulated spot follows a GBM-style lognormal process. The model does this at each step:

1. Samples a random time interval from a Poisson distribution.
2. Converts the interval into a volatility scaling over a year.
3. Applies a normal shock and the Itô drift correction.
4. Updates the current value and raises the tick event.

The underlying update is:

$$
S_{t+\Delta t} = S_t \exp\left(\sigma Z \sqrt{\Delta t} - \frac{1}{2}\sigma^2 \Delta t\right)
$$

This is the standard base-process approach for a spot simulation. It does not add a pricing-specific convexity adjustment, because that is a payoff-level consideration rather than a spot-generation concern.

### Basket aggregation

The basket does not publish on every constituent tick. Instead, it stores the latest values and emits a new basket value only after all constituents have provided an observation for the current cycle.

## Prerequisites

- .NET 8 SDK
- A terminal with access to the repository root

## Run the sample

```bash
dotnet run --project EventGraph/EventGraph.csproj
```

Customise the run with CLI options:

```bash
dotnet run --project EventGraph/EventGraph.csproj -- --ticks 3 --quiet
```

Available options:

- `--ticks <n>`: Number of ticks to emit; defaults to continuous mode until interrupted
- `--quiet`: Suppresses subscription and quote output
- `--basket-color <color>`: Console color used for basket updates
- `--help`: Displays usage information

## Run tests and coverage

```bash
dotnet test EventGraph.Tests/EventGraph.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Quality gate:

```bash
./scripts/quality.sh
```

The repo enforces at least 95% line coverage for the production code path.

## Public-repo readiness checklist

- Source is organized around a single library and a focused test project.
- Test naming follows production types (`AppOptions`, `BasketAggregate`, `QuoteSubscriber`, `SimulatedQuoteSource`, `QuoteTick`).
- Coverage, quality checks, and clean test execution are included in the repo workflow.
- The project includes a permissive open-source license in the root of the repository.
- No secrets, credentials, or local-only environment artifacts are required to build or run the sample.

## Notes

- The sample uses Math.NET for random sampling.
- The default execution model is continuous mode unless `--ticks` is supplied.
