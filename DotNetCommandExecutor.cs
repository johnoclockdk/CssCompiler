using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Compiler
{
    public class DotNetCommandExecutor
    {
        private const string CsProjFilePattern = "*.csproj";
        private const string TargetFramework = "net7.0";

        public static void DotnetUpdateApi(string folderPath, Config config)
        {
            var regex = new Regex(@"PackageReference Include=""([^""]*)"" Version=""([^""]*)""");

            foreach (var file in Directory.EnumerateFiles(folderPath, CsProjFilePattern, SearchOption.AllDirectories))
            {
                string fileContent = File.ReadAllText(file);
                var matches = regex.Matches(fileContent);
                var packages = new HashSet<string>();

                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        packages.Add(match.Groups[1].Value);
                    }
                }

                foreach (var package in packages)
                {
                    // Check if the Silencelog setting is false before logging
                    if (config.Silencelog != true)
                    {
                        Console.WriteLine($"Update {file} package: {package}");
                    }
                    RunDotnetAddPackage(file, package, config);
                }
            }
        }
        public static void RunDotnetAddPackage(string filePath, string packageName, Config config)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"add \"{filePath}\" package {packageName}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process!.WaitForExit();

                // Check if the Silencelog setting is false before logging
                if (config.Silencelog != true)
                {
                    string result = process.StandardOutput.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
        }

        public static void RunDotNetPublish(string folderPath, Config config)
        {
            try
            {
                string folderName = new DirectoryInfo(folderPath).Name;
                string outputPath = Path.Combine(folderPath, "../compiled", folderName);
                var stopwatch = Stopwatch.StartNew();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish -f {TargetFramework} -c Release -o \"{outputPath}\" \"{folderPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi)!)
                {
                    process!.WaitForExit();

                    // Check if the Silencelog setting is false before logging
                    if (config.Silencelog != true)
                    {
                        string log = process.StandardOutput.ReadToEnd();
                        Console.WriteLine(log);
                    }
                }

                ArtifactCleaner.CleanupArtifacts(outputPath);
                stopwatch.Stop();

                Console.WriteLine($"{folderName} completed in {stopwatch.Elapsed.TotalSeconds} seconds.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running dotnet publish: {ex.Message}");
            }
        }
    }
}
