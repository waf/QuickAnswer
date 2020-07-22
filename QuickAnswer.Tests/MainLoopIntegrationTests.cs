using Microsoft.Extensions.Caching.Memory;
using QuickAnswer.Evaluators;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace QuickAnswer.Tests
{
    public class MainLoopIntegrationTests
    {
        // system under test
        private readonly MainLoop loop;

        // test data
        private static readonly DateTime referenceDateTime = new DateTime(2020, 8, 29);
        private static readonly decimal ExchangeRateUSDGBP = 0.79m;
        private static readonly (string question, string answer)[] expectedTranscript =
        {
            ("4 + 5", "9"),
            ("var0 + 3", "12"),
            ("now", "29/08/2020 12:00:00 AM"),
            ("bangkok time", "29/08/2020 12:00:00 AM +07:00"),
            ("12pm bangkok time to tokyo time", "29/08/2020 2:00:00 PM +09:00"),
            ("var4.AddDays(1)", "30/08/2020 2:00:00 PM +09:00"),
            ("-PI", "-3.141592653589793"),
            ("10 miles to kilometers", "16.0934"),
            ("77 F to C", "25"),
            ("GBP", "£ British pound (GBP)"),
            ("10 USD to GBP", "£ 7.90 GBP"),
            ("var10 * 2", "15.8"),
            ("exit", null)
        };

        public MainLoopIntegrationTests()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set(CurrencyEvaluator.CacheKey("USD", "GBP"), ExchangeRateUSDGBP);
            cache.Set(CurrencyEvaluator.CacheKey("GBP", "USD"), 1 / ExchangeRateUSDGBP);

            loop = new MainLoop(new IEvaluator[]
            {
                new ExpressionEvaluator(),
                new CurrencyEvaluator(cache),
                new DateEvaluator(referenceDateTime),
                new UnitEvaluator(),
            });
        }

        [Fact]
        public async Task Transcript()
        {
            // fake IO to feed transcript and capture output
            int transcriptIndex = -1;
            string[] actualOutput = new string[expectedTranscript.Length];
            this.loop.ReadLine = () => expectedTranscript[++transcriptIndex].question;
            this.loop.WriteLine = line => actualOutput[transcriptIndex] = line.Trim();
            this.loop.Write = _ => { }; // discard prompt (">") output

            // system under test
            await this.loop.RunAsync();

            var actualTranscript = actualOutput
                .Select((answer, i) => (expectedTranscript[i].question, answer))
                .ToArray();

            // verify actual vs expected transcript in sets of 5 so any assertion failures are obvious.
            var sets = expectedTranscript
                .Zip(actualTranscript, (expected, actual) => (expected, actual))
                .InSetsOf(5);
            foreach (var set in sets)
            {
                var expected = set.Select(s => s.expected).ToArray();
                var actual = set.Select(s => s.actual).ToArray();
                Assert.Equal(expected, actual);
            }
        }
    }
}
