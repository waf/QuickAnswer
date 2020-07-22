using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuickAnswer.Evaluators
{
    class DateEvaluator : IEvaluator
    {
        private static readonly string[] SupportedTypes =
        {
            "datetimeV2.date",
            "datetimeV2.time",
            "datetimeV2.datetime",
            "datetimeV2.timezone"
        };

        private readonly DateTime? referenceTime;

        public DateEvaluator(DateTime? referenceTime = null)
        {
            this.referenceTime = referenceTime;
        }

        public Task<object> AnswerAsync(Question question)
        {
            var dates = DateTimeRecognizer
                .RecognizeDateTime(
                    question.ToString(),
                    Culture.English,
                    DateTimeOptions.EnablePreview | DateTimeOptions.ExperimentalMode,
                    referenceTime
                )
                .Where(r => SupportedTypes.Contains(r.TypeName))
                .ToList();

            if(!dates.Any())
            {
                throw new ArgumentException("Unknown date: " + question);
            }

            // user is asking for a timezone lookup
            if(dates.Count == 1
                && dates[0].TypeName == "datetimeV2.timezone")
            {
                var attrs = dates[0].AttributesValues();
                return Task.FromResult<object>(
                    NowOffset.ToOffset(
                        ParseTimeZoneOffset(attrs["value"], dates[0].Text)
                    )
                );
            }

            // user is asking for a timezone conversion
            if(dates.Count == 2
                && (dates[0].TypeName == "datetimeV2.time" || dates[0].TypeName == "datetimeV2.datetime")
                && dates[1].TypeName == "datetimeV2.timezone")
            {
                return Task.FromResult<object>(
                    ConvertTimeToTimeZone(
                        dates[0].AttributesValues(),
                        dates[1].AttributesValues(),
                        dates[1].Text
                    )
                );
            }

            // general, just show whatever datetimes we were able to parse
            var parsed = dates
                .SelectMany(d => d.AttributesValues())
                .Where(kvp => kvp.Key == "value")
                .Select(kvp => DateTime.Parse(kvp.Value))
                .ToList();

            return Task.FromResult(parsed.Count == 1 ? parsed[0] as object : parsed);
        }

        private DateTimeOffset ConvertTimeToTimeZone(Dictionary<string, string> time, Dictionary<string, string> timezone, string timezoneText)
        {
            var from = time["timex"] == "PRESENT_REF"
                ? NowOffset
                : new DateTimeOffset(
                    TimeSpan.TryParse(time["value"], out TimeSpan parsedTime)
                        ? DateTime.SpecifyKind(Now.Date, DateTimeKind.Unspecified).Add(parsedTime)
                        : DateTime.Parse(time["value"]),
                    ParseTimeZoneOffset(time["timezone"], time["timezoneText"])
                  );

            var to = ParseTimeZoneOffset(timezone["value"], timezoneText);

            return from.ToOffset(to);
        }

        private TimeSpan ParseTimeZoneOffset(string utcString, string city) =>
            UTCStringToTimeSpan(utcString) ?? LookupTimeZone(city).BaseUtcOffset;

        private TimeZoneInfo LookupTimeZone(string location)
        {
            var city = Regex.Replace(location, " time$", "").ToLower();
            return TimeZoneInfo
               .GetSystemTimeZones()
               .Where(k => k.DisplayName.Substring(k.DisplayName.IndexOf(')') + 2).ToLower().IndexOf(city) >= 0)
               .FirstOrDefault();
        }

        private TimeSpan? UTCStringToTimeSpan(string utcString) =>
            TimeSpan.TryParse(utcString.Replace("UTC+", "").Replace("UTC", ""), out var ts)
            ? ts as TimeSpan?
            : null;


        private DateTimeOffset NowOffset => referenceTime is null
            ? DateTimeOffset.Now
            : new DateTimeOffset(referenceTime.Value);

        private DateTime Now => referenceTime is null
            ? DateTime.Now
            : referenceTime.Value;
    }
}
