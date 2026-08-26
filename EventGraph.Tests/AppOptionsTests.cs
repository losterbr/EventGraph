namespace EventGraph.Tests
{
    public class AppOptionsTests
    {
        [Fact]
        public void ParseArgumentsSupportsCustomTickCount()
        {
            var options = AppOptionsParser.Parse(["--ticks", "12"]);

            Assert.Equal(12, options.TickCount);
        }

        [Fact]
        public void ParseArgumentsSupportsHelpFlag()
        {
            var options = AppOptionsParser.Parse(["--help"]);

            Assert.True(options.ShowHelp);
        }

        [Fact]
        public void ParseArgumentsSupportsQuiet()
        {
            var options = AppOptionsParser.Parse(["--quiet"]);

            Assert.True(options.Quiet);
        }

        [Fact]
        public void ParseArgumentsSupportsCustomBasketColor()
        {
            var options = AppOptionsParser.Parse(["--basket-color", "Yellow"]);

            Assert.Equal(ConsoleColor.Yellow, options.BasketColor);
        }

        [Fact]
        public void ParseArgumentsReturnsDefaultValuesWhenNoArgumentsAreProvided()
        {
            var options = AppOptionsParser.Parse([]);

            Assert.Equal(0, options.TickCount);
            Assert.False(options.Quiet);
            Assert.False(options.ShowHelp);
            Assert.Equal(ConsoleColor.Cyan, options.BasketColor);
        }

        [Fact]
        public void ParseArgumentsThrowsForMissingTickValue()
        {
            _ = Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(["--ticks"]));
        }

        [Fact]
        public void ParseArgumentsThrowsForInvalidTickValue()
        {
            _ = Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(["--ticks", "0"]));
        }

        [Fact]
        public void ParseArgumentsThrowsForMissingBasketColorValue()
        {
            _ = Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(["--basket-color"]));
        }

        [Fact]
        public void ParseArgumentsThrowsForInvalidBasketColor()
        {
            _ = Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(["--basket-color", "not-a-color"]));
        }

        [Fact]
        public void ParseArgumentsThrowsForUnknownArgument()
        {
            _ = Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(["--unexpected"]));
        }
    }
}
