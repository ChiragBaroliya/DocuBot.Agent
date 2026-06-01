namespace DocuBot.Infrastructure.Utils
{
    public static class AiResponseCleaner
    {
        public static string RemoveCodeFences(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var trimmed = input.Trim();

            // Remove ```html ... ``` or ``` ... ```
            var regex = new System.Text.RegularExpressions.Regex(@"^```(?:html)?\s*([\s\S]*?)\s*```$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var match = regex.Match(trimmed);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // Also handle if only at the start/end
            if (trimmed.StartsWith("```html"))
                trimmed = trimmed.Substring(7).TrimStart();
            if (trimmed.StartsWith("```"))
                trimmed = trimmed.Substring(3).TrimStart();
            if (trimmed.EndsWith("```"))
                trimmed = trimmed.Substring(0, trimmed.Length - 3).TrimEnd();

            return trimmed;
        }
    }
}