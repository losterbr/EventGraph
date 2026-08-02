using System;

namespace EventGraph
{
    public sealed class AppOptions
    {
        public int TickCount { get; set; } = 0;
        public bool Quiet { get; set; }
        public bool ShowHelp { get; set; }
        public string[] Symbols { get; set; } = { "TSLA", "GOOG", "AMZN" };
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

                        options.BasketColor = basketColor;
                        i++;
                        break;

                    case "--symbols":
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("--symbols requires a value.");
                        }

                        options.Symbols = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (options.Symbols.Length == 0)
                        {
                            throw new ArgumentException("--symbols must contain at least one symbol.");
                        }

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
