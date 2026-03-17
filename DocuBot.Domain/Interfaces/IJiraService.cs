using System.Threading.Tasks;

namespace DocuBot.Domain.Interfaces
{
    public interface IJiraService
    {
        Task<string> GetIssueDetailsAsync(string issueKey);
    }
}
