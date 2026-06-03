using System.Text;

namespace DocuBot.Agent.Services
{
    public sealed record ProjectSummary(string ProjectName, string TechStack, string ProjectDirectory, string Summary);

    public static class ProjectSummaryHelper
    {
        private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".vs",
            "bin",
            "obj",
            "node_modules",
            "dist",
            "build",
            ".venv",
            "venv",
            "__pycache__",
            "vendor"
        };

        public static IReadOnlyList<ProjectSummary> GetProjectSummaries(string solutionRoot)
        {
            var projects = new List<ProjectSummary>();

            // .NET projects
            foreach (var csproj in FindFiles(solutionRoot, "*.csproj"))
            {
                var projName = Path.GetFileNameWithoutExtension(csproj);
                var projDir = Path.GetDirectoryName(csproj)!;
                var mainFiles = Directory.GetFiles(projDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "Program.cs" || f!.EndsWith("Service.cs") || f!.EndsWith("Startup.cs"))
                    .ToList();
                var folders = Directory.GetDirectories(projDir).Select(Path.GetFileName).ToList();
                var summary = $"- Project: {projName} (.NET)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}";
                projects.Add(new ProjectSummary(projName, ".NET", projDir, summary));
            }

            // Node.js projects
            foreach (var pkg in FindFiles(solutionRoot, "package.json"))
            {
                var projDir = Path.GetDirectoryName(pkg)!;
                var projName = new DirectoryInfo(projDir).Name;
                var mainFiles = Directory.GetFiles(projDir, "*.js", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "app.js" || f == "index.js").ToList();
                var folders = Directory.GetDirectories(projDir).Select(Path.GetFileName).ToList();
                var summary = $"- Project: {projName} (Node.js)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}";
                projects.Add(new ProjectSummary(projName, "Node.js", projDir, summary));
            }

            // Python projects
            foreach (var req in FindFiles(solutionRoot, "requirements.txt"))
            {
                var projDir = Path.GetDirectoryName(req)!;
                var projName = new DirectoryInfo(projDir).Name;
                var mainFiles = Directory.GetFiles(projDir, "*.py", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "main.py" || f == "app.py").ToList();
                var folders = Directory.GetDirectories(projDir).Select(Path.GetFileName).ToList();
                var summary = $"- Project: {projName} (Python)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}";
                projects.Add(new ProjectSummary(projName, "Python", projDir, summary));
            }

            // PHP projects
            foreach (var composer in FindFiles(solutionRoot, "composer.json"))
            {
                var projDir = Path.GetDirectoryName(composer)!;
                var projName = new DirectoryInfo(projDir).Name;
                var mainFiles = Directory.GetFiles(projDir, "*.php", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f == "index.php").ToList();
                var publicDir = Path.Combine(projDir, "public");
                var folders = Directory.Exists(publicDir) ? new[] { "public" } : Array.Empty<string>();
                var summary = $"- Project: {projName} (PHP)\n  Main files: {string.Join(", ", mainFiles)}{(folders.Any() ? ", folders: " + string.Join(", ", folders) : "")}";
                projects.Add(new ProjectSummary(projName, "PHP", projDir, summary));
            }

            return projects;
        }

        private static IEnumerable<string> FindFiles(string root, string pattern)
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var current = pending.Pop();

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(current, pattern, SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                IEnumerable<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (var directory in directories)
                {
                    var name = Path.GetFileName(directory);
                    if (IgnoredDirectoryNames.Contains(name))
                    {
                        continue;
                    }

                    pending.Push(directory);
                }
            }
        }

        public static string BuildProjectSummaries(string solutionRoot)
        {
            var sb = new StringBuilder();

            foreach (var project in GetProjectSummaries(solutionRoot))
            {
                sb.AppendLine(project.Summary);
            }

            return sb.ToString();
        }
    }
}