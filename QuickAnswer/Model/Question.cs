using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuickAnswer
{
    public class Question
    {
        public string Text { get; }
        public IDictionary<string, object> Variables { get; }

        public Question(string text, IDictionary<string, object> variables)
        {
            Text = text;
            Variables = variables;
        }

        public override string ToString() =>
            TransformVariableNames(varNumber => Variables[varNumber].ToString());

        public string TransformVariableNames(Func<string, string> transformer) =>
            Regex.Replace(Text, @"\bvar\d+", match =>
                transformer(match.Value)
            );
    }
}
