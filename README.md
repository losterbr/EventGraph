# EventGraph

EventGraph is a small .NET sample that demonstrates a simple event-driven quote pipeline.

## What it does

- Simulates a few stock-like price updates using `SimulatedSpot`.
- Subscribes a `Listener` to individual spots and to a basket aggregation.
- Emits basket updates whenever all constituent prices have been observed.

## Basket update behavior

The basket does not emit a new value on every single constituent tick. Instead, it stores each constituent's latest value and only publishes a new basket value once all constituents have provided an update for the current cycle. This makes the basket behave like a synchronized aggregate over the constituent prices.

## How the spot simulation works

Each simulated spot is modeled as a simple geometric Brownian motion (GBM)-style process. At each step, the code:

1. Samples a random time interval from a Poisson distribution.
2. Converts that interval into a volatility scaling using the annualized volatility and the number of milliseconds per year.
3. Applies a lognormal update using a standard normal shock.
4. Includes the Itô drift correction term $-\frac{1}{2}\sigma^2$ so the process remains consistent with a lognormal spot model.

The update is effectively:

$$
S_{t+\Delta t} = S_t \exp\left(\sigma Z \sqrt{\Delta t} - \frac{1}{2}\sigma^2 \Delta t\right)
$$

This is the standard approach for a basic spot simulation. A separate convexity adjustment is not added inside the underlying spot generator; that type of adjustment is typically applied when pricing a specific payoff or forward contract, not when simulating the spot itself.

## Run the sample

```bash
dotnet run --project EventGraph/EventGraph.csproj
```

You can also customize the run with the available CLI options:

```bash
dotnet run --project EventGraph/EventGraph.csproj -- --ticks 3 --quiet --symbols A,B,C
```

## Run the tests

```bash
dotnet test EventGraph.Tests/EventGraph.Tests.csproj
```

## Quality checks

The repository includes a quality script and a pre-commit hook:

```bash
./scripts/quality.sh
```

The quality gate now runs tests with code coverage collection and requires line coverage of at least 90% for the production code paths.

## Notes

- The sample uses Math.NET for random sampling.
- The current implementation runs a fixed number of ticks so the process completes predictably.
