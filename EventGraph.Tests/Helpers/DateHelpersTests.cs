namespace EventGraph.Tests
{
    public class DateHelpersTests
    {
        [Theory]
        [InlineData("1D", 1.0 / 365.0)]
        [InlineData("1W", 7.0 / 365.0)]
        [InlineData("1M", 1.0 / 12.0)]
        [InlineData("6M", 0.5)]
        [InlineData("1Y", 1.0)]
        [InlineData("5Y", 5.0)]
        public void ToYearFractionConvertsRecognizedTenors(string tenor, double expected)
        {
            Assert.Equal(expected, DateHelpers.ToYearFraction(tenor), precision: 12);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("Y")]
        [InlineData("1X")]
        [InlineData(null)]
        public void ToYearFractionRejectsInvalidTenors(string tenor)
        {
            _ = Assert.Throws<FormatException>(() => DateHelpers.ToYearFraction(tenor));
        }

        [Fact]
        public void TryAddTenorAddsCalendarAccurateUnits()
        {
            var today = DateTime.Today;

            Assert.True(DateHelpers.TryAddTenor(today, "1Y", out var oneYear));
            Assert.Equal(today.AddYears(1), oneYear);

            Assert.True(DateHelpers.TryAddTenor(today, "6M", out var sixMonths));
            Assert.Equal(today.AddMonths(6), sixMonths);

            Assert.True(DateHelpers.TryAddTenor(today, "2W", out var twoWeeks));
            Assert.Equal(today.AddDays(14), twoWeeks);

            Assert.True(DateHelpers.TryAddTenor(today, "10D", out var tenDays));
            Assert.Equal(today.AddDays(10), tenDays);
        }

        [Fact]
        public void TryAddTenorReturnsFalseForInvalidTenor()
        {
            Assert.False(DateHelpers.TryAddTenor(DateTime.Today, "not-a-tenor", out var result));
            Assert.Equal(default, result);
        }

        [Fact]
        public void YearFractionComputesActOver365()
        {
            var start = new DateTime(2024, 1, 1);
            var end = new DateTime(2025, 1, 1);

            Assert.Equal(366.0 / 365.0, DateHelpers.YearFraction(start, end), precision: 12);
        }
    }
}
