using System;
using System.Reflection;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class ProgramTests
{
    [Fact]
    public async Task ProgramMainRunsWithTickOptions()
    {
        var programType = typeof(AppOptions).Assembly.GetType("EventGraph.Program");
        var method = programType!.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);

        var task = (Task)method!.Invoke(null, new object[] { new[] { "--ticks", "1", "--quiet", "--basket-color", "Yellow" } })!;
        await task;
    }

    [Fact]
    public async Task ProgramMainPrintsHelpWhenRequested()
    {
        var programType = typeof(AppOptions).Assembly.GetType("EventGraph.Program");
        var method = programType!.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);

        var task = (Task)method!.Invoke(null, new object[] { new[] { "--help" } })!;
        await task;
    }
}
