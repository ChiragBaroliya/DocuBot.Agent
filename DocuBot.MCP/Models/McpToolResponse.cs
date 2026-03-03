namespace DocuBot.MCP.Models
{
    public class McpToolResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
