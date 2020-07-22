using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("QuickAnswer.Tests")]

namespace QuickAnswer
{
    public class MainLoop
    {
        private readonly IEvaluator[] evaluators;

        public MainLoop(params IEvaluator[] evaluators)
        {
            this.evaluators = evaluators;
        }

        public async Task RunAsync()
        {
            var variables = ImmutableDictionary.Create<string, object>();
            for(var i = 0; i < int.MaxValue; i++)
            {
                Write(Environment.NewLine + i + "> ");
                var questionText = ReadLine();
                var question = new Question(questionText, variables);
                if(question.Text == "exit")
                {
                    break;
                }
                var answer = await AnswerAsync(question);
                variables = variables.Add("var" + i, answer);
                WriteLine(answer.ToDisplayString());
            }
        }

        private async Task<object> AnswerAsync(Question question)
        {
            var failures = new List<(string evaluatorType, Exception exception)>();
            foreach (var evaluator in evaluators)
            {
                try
                {
                    var answer = await evaluator.AnswerAsync(question);
                    return answer;
                }
                catch (Exception ex)
                {
                    failures.Add((evaluator.GetType().Name, ex));
                }
            }

            return "I don't know how to answer that!"
                + Environment.NewLine
                + string.Join(Environment.NewLine,
                    failures.Select(failure => $"{failure.evaluatorType}: {failure.exception.Message}")
                  );
        }

        // quick and dirty testable io
        internal Action<string> WriteLine = Console.WriteLine;
        internal Action<string> Write = Console.Write;
        internal Func<string> ReadLine = Console.ReadLine;
    }
}
