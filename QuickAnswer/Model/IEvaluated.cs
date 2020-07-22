namespace QuickAnswer.Evaluators
{
    public interface IEvaluated
    {
        object ToEvaluatedValue();
        string ToDisplayString();
        string ToString();
    }
}