using System.Text;

namespace DocuBot.Agent.Services
{
    public static class ProjectSummaryHelper
    {
        public static string BuildProjectSummaries(string solutionRoot)
        {
            var sb = new StringBuilder();

            // .NET projects
            foreach (var csproj in Directory.GetFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories))
            {
                var projName = Path.GetFileNameWithoutExtension(csproj);
                var projDir = Path.GetDirectoryName(csproj)!;
                var mainFiles = Directory.GetFiles(projDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "Program.cs" || f!.EndsWith("Service.cs") || f!.EndsWith("Startup.cs"))
                    .ToList();
                var folders = Directory.GetDirectories(projDir).Select(Path.GetFileName).ToList();
                sb.AppendLine($"- Project: {projName} (.NET)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}");
            }

            // Node.js projects
            foreach (var pkg in Directory.GetFiles(solutionRoot, "package.json", SearchOption.AllDirectories))
            {
                var projDir = Path.GetDirectoryName(pkg)!;
                var projName = new DirectoryInfo(projDir).Name;
                var mainFiles = Directory.GetFiles(projDir, "*.js", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "app.js" || f == "index.js").ToList();
                var folders = Directory.GetDirectories(projDir).Select(Path.GetFileName).ToList();
                sb.AppendLine($"- Project: {projName} (Node.js)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}");
            }

            // Python projects
            foreach (var req in Directory.GetFiles(solutionRoot, "requirements.txt", SearchOption.AllDirectories))
            {
                var projDir = Path.GetDirectoryName(req)!;
                var projName = new DirectoryInfo(projDir).Name;
                var mainFiles = Directory.GetFiles(projDir, "*.py", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "main.py" || f == "app.py").ToList();
                var folders = Directory.GetDirectories(projDir).Select(Path.GetFileName).ToList();
                sb.AppendLine($"- Project: {projName} (Python)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}");
            }

            // PHP projects
            foreach (var composer in Directory.GetFiles(solutionRoot, "composer.json", SearchOption.AllDirectories))
            {
                var projDir = Path.GetDirectoryName(composer)!;
                var projName = new DirectoryInfo(projDir).Name;
                var mainFiles = Directory.GetFiles(projDir, "*.php", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "index.php").ToList();
                var publicDir = Path.Combine(projDir, "public");
                var folders = Directory.Exists(publicDir) ? new[] { "public" } : Array.Empty<string>();
                sb.AppendLine($"- Project: {projName} (PHP)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}");
            }

            return sb.ToString();
        }
    }
}