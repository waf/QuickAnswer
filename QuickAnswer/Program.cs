using QuickAnswer.Evaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickAnswer
{
    public class Program
    {
        private static IEvaluator[] Evaluators => new IEvaluator[]
        {
            new ExpressionEvaluator(),
            new CurrencyEvaluator(),
            new DateEvaluator(),
            new UnitEvaluator(),
        };

        static async Task Main(string[] _)
        {
            PrintIntroduction();
            await new MainLoop(Evaluators).RunAsync();
        }

        private static void PrintIntroduction() =>
            Console.WriteLine(
                @$"Welcome to {nameof(QuickAnswer)}! Try out the following questions:" + Environment.NewLine +
                @$"Date and Time:" + Environment.NewLine +
                @$" - Two days from now" + Environment.NewLine +
                @$" - 5 minutes ago" + Environment.NewLine +
                @$" - Indiana time" + Environment.NewLine +
                @$" - 10pm Bangkok time in Indiana time" + Environment.NewLine +
                @$" - Christmas 2020" + Environment.NewLine +
                @$"Expressions: (C#)" + Environment.NewLine +
                @$" - 25 / 5" + Environment.NewLine +
                @$" - 2 * PI" + Environment.NewLine +
                @$"Currency:" + Environment.NewLine +
                @$" - 100 USD to GBP" + Environment.NewLine +
                @$" - GBP to USD" + Environment.NewLine +
                @$"Unit Conversion:" + Environment.NewLine +
                @$" - 10 KG to lbs" + Environment.NewLine +
                @$" - 25 C to F" + Environment.NewLine +
                Environment.NewLine +
                $@"You can reference historical answers using `var0`, `var1` placeholders," + Environment.NewLine +
                $@"where the number is the line number."
            );
    }
}
