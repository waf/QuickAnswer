using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace QuickAnswer.Evaluators
{
    class ExpressionEvaluator : IEvaluator
    {
        private readonly ScriptOptions options;
        private readonly Globals globals;
        private ScriptState<object> persistentState;

        public ExpressionEvaluator()
        {
            options = ScriptOptions.Default
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(ExpandoObject).Assembly.Location), // required for dynamic
                    MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location), // required for dynamic
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
                )
                .WithImports("System", "System.Math", "System.Linq");

            globals = new Globals();
        }

        public async Task<object> AnswerAsync(Question question)
        {
            // rewrite question variable names to be references to our global object.
            var questionText = question.TransformVariableNames(i =>
                $"{nameof(Globals.QuickAnswerVariables)}.{i}"
            );
            SetGlobalVariables(globals, question.Variables);

            var transientState = persistentState is null
                ? await CSharpScript.RunAsync(questionText, options, globals)
                : await persistentState.ContinueWithAsync(questionText, options);

            if (transientState.Exception != null)
            {
                throw transientState.Exception;
            }

            persistentState = transientState;
            return persistentState.ReturnValue;
        }

        private static void SetGlobalVariables(Globals globals, IDictionary<string, object> variables)
        {
            var globalVariables = (IDictionary<string, object>)globals.QuickAnswerVariables;
            foreach (var (variableName, variableValue) in variables)
            {
                globalVariables[variableName] = variableValue.ToEvaluatedValue();
            }
        }
    }

    /// <summary>
    /// Globals that run in our C# roslyn script, giving access to historical answers.
    /// </summary>
    public class Globals
    {
        public dynamic QuickAnswerVariables { get; set; } = new ExpandoObject();
    }
}
