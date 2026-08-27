using System.Text.Json;

namespace EventGraph.Tests
{
    public class CurrencyRateSourceTests
    {
        [Fact]
        public void CurrencyRateSourceExposesInterestRate()
        {
            var source = new CurrencyRateSource("USD", 0.02);

            Assert.Equal("USD", source.Name);
            Assert.Equal(0.02, source.InterestRate);
            Assert.Equal(nameof(CurrencyRateSource), source.Type);
            Assert.Empty(source.Dependencies);
        }

        [Fact]
        public void CurrencyRateSourceRejectsBlankName()
        {
            _ = Assert.Throws<ArgumentException>(() => new CurrencyRateSource(" ", 0.02));
        }

        [Fact]
        public void CurrencyRateSourceRejectsNonFiniteRate()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new CurrencyRateSource("USD", double.NaN));
        }

        [Fact]
        public void CurrencyRateSourceLoadsFromJson()
        {
            using var document = JsonDocument.Parse("{\"name\":\"USD\",\"interestRate\":0.02}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var source = new CurrencyRateSource(definition);

            Assert.Equal("USD", source.Name);
            Assert.Equal(0.02, source.InterestRate);
        }
    }
}
