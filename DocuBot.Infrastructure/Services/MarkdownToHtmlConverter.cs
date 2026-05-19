using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DocuBot.Infrastructure.Services
{
    public static class MarkdownToHtmlConverter
    {
        public static string Convert(string markdown)
        {
            var html = new StringBuilder();
            
            // Header template
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("    <title>DocuBot Security & Code Review Report</title>");
            html.AppendLine("    <link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">");
            html.AppendLine("    <link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>");
            html.AppendLine("    <link href=\"https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap\" rel=\"stylesheet\">");
            html.AppendLine("    <style>");
            html.AppendLine("        :root {");
            html.AppendLine("            --bg-gradient: linear-gradient(135deg, #0f172a 0%, #1e1b4b 100%);");
            html.AppendLine("            --card-bg: rgba(30, 41, 59, 0.7);");
            html.AppendLine("            --border-color: rgba(255, 255, 255, 0.08);");
            html.AppendLine("            --text-primary: #f8fafc;");
            html.AppendLine("            --text-secondary: #94a3b8;");
            html.AppendLine("            --accent-success: #10b981;");
            html.AppendLine("            --accent-fail: #ef4444;");
            html.AppendLine("            --accent-info: #3b82f6;");
            html.AppendLine("        }");
            html.AppendLine("        body {");
            html.AppendLine("            margin: 0;");
            html.AppendLine("            padding: 40px 20px;");
            html.AppendLine("            font-family: 'Outfit', sans-serif;");
            html.AppendLine("            background: var(--bg-gradient);");
            html.AppendLine("            background-attachment: fixed;");
            html.AppendLine("            color: var(--text-primary);");
            html.AppendLine("            line-height: 1.6;");
            html.AppendLine("        }");
            html.AppendLine("        .container {");
            html.AppendLine("            max-width: 900px;");
            html.AppendLine("            margin: 0 auto;");
            html.AppendLine("        }");
            html.AppendLine("        .header {");
            html.AppendLine("            text-align: center;");
            html.AppendLine("            margin-bottom: 40px;");
            html.AppendLine("            animation: fadeIn 0.8s ease;");
            html.AppendLine("        }");
            html.AppendLine("        .header h1 {");
            html.AppendLine("            font-size: 2.5rem;");
            html.AppendLine("            margin: 0;");
            html.AppendLine("            font-weight: 700;");
            html.AppendLine("            background: linear-gradient(to right, #60a5fa, #a78bfa);");
            html.AppendLine("            -webkit-background-clip: text;");
            html.AppendLine("            -webkit-text-fill-color: transparent;");
            html.AppendLine("        }");
            html.AppendLine("        .header p {");
            html.AppendLine("            color: var(--text-secondary);");
            html.AppendLine("            margin-top: 10px;");
            html.AppendLine("            font-size: 1.1rem;");
            html.AppendLine("        }");
            html.AppendLine("        .card {");
            html.AppendLine("            background: var(--card-bg);");
            html.AppendLine("            border: 1px solid var(--border-color);");
            html.AppendLine("            border-radius: 20px;");
            html.AppendLine("            padding: 40px;");
            html.AppendLine("            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);");
            html.AppendLine("            backdrop-filter: blur(12px);");
            html.AppendLine("            animation: slideUp 0.8s cubic-bezier(0.16, 1, 0.3, 1);");
            html.AppendLine("        }");
            html.AppendLine("        h2 {");
            html.AppendLine("            font-size: 1.8rem;");
            html.AppendLine("            border-bottom: 1px solid var(--border-color);");
            html.AppendLine("            padding-bottom: 10px;");
            html.AppendLine("            margin-top: 30px;");
            html.AppendLine("            color: #60a5fa;");
            html.AppendLine("        }");
            html.AppendLine("        h3 {");
            html.AppendLine("            font-size: 1.4rem;");
            html.AppendLine("            margin-top: 25px;");
            html.AppendLine("            color: #c084fc;");
            html.AppendLine("        }");
            html.AppendLine("        p {");
            html.AppendLine("            margin: 15px 0;");
            html.AppendLine("            color: #cbd5e1;");
            html.AppendLine("        }");
            html.AppendLine("        .badge {");
            html.AppendLine("            display: inline-block;");
            html.AppendLine("            padding: 6px 16px;");
            html.AppendLine("            border-radius: 9999px;");
            html.AppendLine("            font-weight: 600;");
            html.AppendLine("            font-size: 0.9rem;");
            html.AppendLine("            text-transform: uppercase;");
            html.AppendLine("            margin-bottom: 20px;");
            html.AppendLine("        }");
            html.AppendLine("        .badge-success {");
            html.AppendLine("            background: rgba(16, 185, 129, 0.15);");
            html.AppendLine("            color: var(--accent-success);");
            html.AppendLine("            border: 1px solid rgba(16, 185, 129, 0.3);");
            html.AppendLine("        }");
            html.AppendLine("        .badge-fail {");
            html.AppendLine("            background: rgba(239, 68, 68, 0.15);");
            html.AppendLine("            color: var(--accent-fail);");
            html.AppendLine("            border: 1px solid rgba(239, 68, 68, 0.3);");
            html.AppendLine("        }");
            html.AppendLine("        ul, ol {");
            html.AppendLine("            margin: 15px 0;");
            html.AppendLine("            padding-left: 25px;");
            html.AppendLine("        }");
            html.AppendLine("        li {");
            html.AppendLine("            margin: 8px 0;");
            html.AppendLine("            color: #cbd5e1;");
            html.AppendLine("        }");
            html.AppendLine("        code {");
            html.AppendLine("            font-family: 'JetBrains Mono', monospace;");
            html.AppendLine("            background: rgba(0, 0, 0, 0.3);");
            html.AppendLine("            padding: 2px 6px;");
            html.AppendLine("            border-radius: 4px;");
            html.AppendLine("            font-size: 0.9em;");
            html.AppendLine("            color: #f43f5e;");
            html.AppendLine("        }");
            html.AppendLine("        pre {");
            html.AppendLine("            background: rgba(15, 23, 42, 0.8);");
            html.AppendLine("            border: 1px solid var(--border-color);");
            html.AppendLine("            border-radius: 12px;");
            html.AppendLine("            padding: 20px;");
            html.AppendLine("            overflow-x: auto;");
            html.AppendLine("            margin: 20px 0;");
            html.AppendLine("        }");
            html.AppendLine("        pre code {");
            html.AppendLine("            background: none;");
            html.AppendLine("            padding: 0;");
            html.AppendLine("            color: #e2e8f0;");
            html.AppendLine("            font-size: 0.95em;");
            html.AppendLine("            display: block;");
            html.AppendLine("        }");
            html.AppendLine("        blockquote {");
            html.AppendLine("            border-left: 4px solid #60a5fa;");
            html.AppendLine("            margin: 20px 0;");
            html.AppendLine("            padding: 10px 20px;");
            html.AppendLine("            background: rgba(96, 165, 250, 0.05);");
            html.AppendLine("            border-radius: 0 8px 8px 0;");
            html.AppendLine("        }");
            html.AppendLine("        blockquote p {");
            html.AppendLine("            font-style: italic;");
            html.AppendLine("            color: #94a3b8;");
            html.AppendLine("            margin: 0;");
            html.AppendLine("        }");
            html.AppendLine("        @keyframes fadeIn {");
            html.AppendLine("            from { opacity: 0; }");
            html.AppendLine("            to { opacity: 1; }");
            html.AppendLine("        }");
            html.AppendLine("        @keyframes slideUp {");
            html.AppendLine("            from { opacity: 0; transform: translateY(30px); }");
            html.AppendLine("            to { opacity: 1; transform: translateY(0); }");
            html.AppendLine("        }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class=\"container\">");
            html.AppendLine("        <div class=\"header\">");
            html.AppendLine("            <h1>DocuBot Security Review</h1>");
            html.AppendLine($"            <p>Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine("        </div>");
            html.AppendLine("        <div class=\"card\">");
            
            // Check status in report
            if (markdown.Contains("Status: PASS", StringComparison.OrdinalIgnoreCase))
            {
                html.AppendLine("            <span class=\"badge badge-success\">Status: PASS</span>");
            }
            else
            {
                html.AppendLine("            <span class=\"badge badge-fail\">Status: REVIEW REQUIRED</span>");
            }

            // Convert body lines
            var lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inCodeBlock = false;
            bool inList = false;
            
            foreach (var rawLine in lines)
            {
                var line = rawLine;
                
                // Code block toggle
                if (line.TrimStart().StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        html.AppendLine("</code></pre>");
                        inCodeBlock = false;
                    }
                    else
                    {
                        html.AppendLine("<pre><code>");
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    html.AppendLine(System.Net.WebUtility.HtmlEncode(line));
                    continue;
                }

                // Parse standard markdown
                line = System.Net.WebUtility.HtmlEncode(line);

                // Inline code formatting
                line = Regex.Replace(line, @"`([^`]+)`", "<code>$1</code>");

                // Bold formatting
                line = Regex.Replace(line, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");

                // Headers
                if (line.StartsWith("### "))
                {
                    if (inList) { html.AppendLine("</ul>"); inList = false; }
                    html.AppendLine($"<h3>{line.Substring(4)}</h3>");
                }
                else if (line.StartsWith("## "))
                {
                    if (inList) { html.AppendLine("</ul>"); inList = false; }
                    html.AppendLine($"<h2>{line.Substring(3)}</h2>");
                }
                else if (line.StartsWith("# "))
                {
                    if (inList) { html.AppendLine("</ul>"); inList = false; }
                    html.AppendLine($"<h2>{line.Substring(2)}</h2>");
                }
                // Bullet list items
                else if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
                {
                    if (!inList)
                    {
                        html.AppendLine("<ul>");
                        inList = true;
                    }
                    var content = line.TrimStart().Substring(2);
                    html.AppendLine($"<li>{content}</li>");
                }
                // Blockquotes
                else if (line.StartsWith("&gt; ") || line.StartsWith("> "))
                {
                    if (inList) { html.AppendLine("</ul>"); inList = false; }
                    var content = line.StartsWith("&gt; ") ? line.Substring(5) : line.Substring(2);
                    html.AppendLine($"<blockquote><p>{content}</p></blockquote>");
                }
                // Paragraph or blank
                else
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (inList)
                        {
                            html.AppendLine("</ul>");
                            inList = false;
                        }
                    }
                    else
                    {
                        if (inList) { html.AppendLine("</ul>"); inList = false; }
                        html.AppendLine($"<p>{line}</p>");
                    }
                }
            }

            if (inList)
            {
                html.AppendLine("</ul>");
            }
            if (inCodeBlock)
            {
                html.AppendLine("</code></pre>");
            }

            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}
