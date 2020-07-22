using Microsoft.Recognizers.Text;
using QuickAnswer.Evaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickAnswer
{
    public static class Extensions
    {
        public static Dictionary<string, string> AttributesValues(this ModelResult model)
        {
            return (model.Resolution["values"] as List<Dictionary<string, string>>).First();
        }

        public static Dictionary<string, string> AttributesStrings(this ModelResult model)
        {
            return model.Resolution.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
        }

        public static object ToEvaluatedValue(this object obj) =>
            obj is IEvaluated evaluated
                ? evaluated.ToEvaluatedValue()
                : obj;

        public static string ToDisplayString(this object obj) =>
            obj is IEvaluated evaluated
                ? evaluated?.ToDisplayString()
                : obj?.ToString();
    }
}
