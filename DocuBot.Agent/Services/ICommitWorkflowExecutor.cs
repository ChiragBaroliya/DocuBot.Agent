using System.Threading.Tasks;

namespace DocuBot.Agent.Services
{
    public interface ICommitWorkflowExecutor
    {
        Task ExecuteAsync(string[] args);
    }
}
