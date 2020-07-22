using Microsoft.Extensions.Caching.Memory;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.NumberWithUnit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickAnswer.Evaluators
{
    class CurrencyEvaluator : IEvaluator
    {
        private readonly HttpClient http;
        private readonly IMemoryCache cache;
        private readonly IDictionary<string, string> symbolsByCode;

        public CurrencyEvaluator(IMemoryCache cache = null)
        {
            this.http = new HttpClient();
            this.cache = cache ?? new MemoryCache(new MemoryCacheOptions());

            symbolsByCode = CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Select(culture => new RegionInfo(culture.LCID))
                .Select(region => (region.ISOCurrencySymbol, region.CurrencySymbol))
                .ToLookup(r => r.ISOCurrencySymbol, r => r.CurrencySymbol)
                .ToDictionary(r => r.Key, r => r.First());
        }

        public async Task<object> AnswerAsync(Question question)
        {
            var results = NumberWithUnitRecognizer
                .RecognizeCurrency(question.ToString(), Culture.English)
                .Where(r => r.TypeName == "currency" && r.AttributesStrings().ContainsKey("isoCurrency"))
                .ToList();

            if(!results.Any())
            {
                throw new ArgumentException("Unknown currency: " + question);
            }

            // currency conversion
            if (results.Count == 2
                && results[0].TypeName == "currency"
                && results[1].TypeName == "currency")
            {
                var from = results[0].AttributesStrings();
                var to = results[1].AttributesStrings()["isoCurrency"];
                var rate = await GetCurrencyRateAsync(from["isoCurrency"], to);

                return from.ContainsKey("value") && from["value"] is string value // present and not a null value
                    ? new Currency(decimal.Parse(value) * rate, to, symbolsByCode[to])
                    : rate as object;
            }

            var parsed = string.Join(
                Environment.NewLine,
                results
                    .Select(d => d.AttributesStrings())
                    .Select(d => symbolsByCode[d["isoCurrency"]] + " " + d["unit"] + " (" + d["isoCurrency"] + ")")
            );

            return parsed;
        }

        private async Task<decimal> GetCurrencyRateAsync(string from, string to)
        {
            var cacheKey = CacheKey(from, to);
            if (cache.TryGetValue(cacheKey, out decimal cachedRate))
            {
                return cachedRate;
            }

            var result = await http.GetAsync($"https://api.exchangeratesapi.io/latest?base={from}&symbols={to}");
            var json = await result.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(json);

            var rate = doc
                .RootElement
                .GetProperty("rates")
                .GetProperty(to)
                .GetDecimal();

            cache.Set(cacheKey, rate);
            return rate;
        }

        internal static string CacheKey(string from, string to) =>
            nameof(CurrencyEvaluator) + from + to;
    }

    public class Currency : IEvaluated
    {
        public Currency(decimal value, string code, string symbol)
        {
            Value = Math.Round(value, 2);
            Code = code;
            Symbol = symbol;
        }

        public decimal Value { get; }
        public string Code { get; }
        public string Symbol { get; }

        public object ToEvaluatedValue() =>
            (double)Value;

        public string ToDisplayString() =>
            $"{Symbol} {Value:n} {Code}";

        public override string ToString() =>
            $"{Value:n} {Code}";
    }
}
