using System;

namespace EventGraph
{
    /// <summary>
    /// Stores the runtime configuration for the simulated market graph.
    /// </summary>
    public sealed class AppOptions
    {
        public int TickCount { get; set; } = 0;
        public bool Quiet { get; set; }
        public bool ShowHelp { get; set; }
        public bool BasketColorSpecified { get; set; }
        public ConsoleColor BasketColor { get; set; } = ConsoleColor.Cyan;
    }

    public static class AppOptionsParser
    {
        public static AppOptions Parse(string[] args)
        {
            var options = new AppOptions();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--ticks":
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("--ticks requires a value.");
                        }

                        if (!int.TryParse(args[i + 1], out var tickCount) || tickCount <= 0)
                        {
                            throw new ArgumentException("--ticks must be a positive integer.");
                        }

                        options.TickCount = tickCount;
                        i++;
                        break;

                    case "--quiet":
                        options.Quiet = true;
                        break;

                    case "--help":
                        options.ShowHelp = true;
                        break;

                    case "--basket-color":
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("--basket-color requires a value.");
                        }

                        if (!Enum.TryParse(args[i + 1], true, out ConsoleColor basketColor))
                        {
                            throw new ArgumentException("--basket-color must be a valid console color.");
                        }

                        options.BasketColorSpecified = true;
                        options.BasketColor = basketColor;
                        i++;
                        break;

                    default:
                        throw new ArgumentException($"Unknown argument: {args[i]}");
                }
            }

            return options;
        }
    }
}
