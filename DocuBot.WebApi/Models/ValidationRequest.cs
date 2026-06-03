namespace DocuBot.WebApi.Models
{
    public class ValidationRequest
    {
        public string CommitMessage { get; set; } = string.Empty;
        public string Diff { get; set; } = string.Empty;
    }
}
