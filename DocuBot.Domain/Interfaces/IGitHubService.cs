using System.Threading.Tasks;

namespace DocuBot.Domain.Interfaces
{
    public interface IGitHubService
    {
        Task<string> GetPullRequestDetailsAsync(int prNumber);
    }
}
