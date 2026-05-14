using System.Threading.Tasks;

namespace DocuBot.Application.Interfaces
{
    public interface ISecretsManagerService
    {
        Task<string> GetSecretAsync(string secretName);
    }
}
