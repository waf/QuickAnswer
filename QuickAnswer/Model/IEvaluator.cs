using System.Threading.Tasks;

namespace QuickAnswer
{
    public interface IEvaluator
    {
        Task<object> AnswerAsync(Question question);
    }
}