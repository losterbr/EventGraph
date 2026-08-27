using System.Text.Json;

namespace EventGraph.Tests
{
    public class RateCurveSourceTests
    {
        [Fact]
        public void RateCurveEvaluatesFromTodayUsingContinuousCompounding()
        {
            var source = new RateCurveSource("USD", 0.02);

            Assert.Equal(Math.Exp(0.02), source.RateCurve(DateTime.Today.AddDays(365)), precision: 12);
            Assert.Equal(1.0, source.RateCurve(DateTime.Today), precision: 12);
        }

        [Fact]
        public void RateCurveSourceLoadsInterestRateFromJson()
        {
            using var document = JsonDocument.Parse("{\"name\":\"USD\",\"currency\":\"USD\",\"interestRate\":0.02}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var source = new RateCurveSource(definition);

            Assert.Equal("USD", source.Name);
            Assert.Equal("USD", source.Currency);
            Assert.Equal(0.02, source.InterestRate);
            Assert.Equal(nameof(RateCurveSource), source.Type);
        }
    }
}