using System.Threading.Tasks;

namespace DocuBot.Domain.Interfaces
{
    public interface ISecretScanner
    {
        Task<bool> ScanForSecretsAsync(string diff);
    }
}
