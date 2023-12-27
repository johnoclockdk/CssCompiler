using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Compiler
{
    class Program
    {
        private const string CsProjFilePattern = "*.csproj";
        private const string TargetFramework = "net7.0";
        private static readonly string[] FilesToDelete = new string[] { "Serilog.*.dll", "Serilog.dll", "Microsoft*.dll", "CounterStrikeSharp.API.dll", "McMaster.NETCore.Plugins.dll", "Scrutor.dll", "System.Diagnostics.EventLog.dll" };

        static async Task Main(string[] args)
        {
            // Load the configuration from the JSON file.
            var configManager = new ConfigurationManager();
            Config config = configManager.LoadConfig();

            if (config.Update)
            {
                await UpdateFromGithub();
            }

            if (args.Length > 0)
            {
                UpdateApi(args[0], config);
                ProcessDirectory(args[0], config); // Pass the config to the method.
            }
            else
            {
                UpdateApiAll(config);

                ProcessCurrentDirectory(config); // Pass the config to the method.
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
        private static async Task UpdateFromGithub()
        {
            string apiURL = $"https://api.github.com/repos/johnoclock/CssCompiler/releases/latest";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");  // GitHub API requires a user-agent

                try
                {
                    var response = await httpClient.GetStringAsync(apiURL);
                    var latestRelease = JObject.Parse(response);

                    // Extract data as needed, for example, the tag name (version)
                    string latestVersion = latestRelease["tag_name"].ToString();
                    Console.WriteLine("Latest Version: " + latestVersion);


                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error fetching latest release: " + ex.Message);
                }
            }
        }


        private static void ProcessDirectory(string folderPath, Config config)
        {
            if (IsValidDirectory(folderPath))
            {
                RunDotNetPublish(folderPath, config); // Pass the config to the method.
            }
            else
            {
                Console.WriteLine("The specified path is not a valid directory or project is not valid");
            }
        }

        private static void UpdateApiAll(Config config)
        {
            if (config.Updateapis)
            {
                string[] folders = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory);

                if (!folders.Any())
                {
                    Console.WriteLine("No folders found in the current directory.");
                    return;
                }

                Parallel.ForEach(folders, folder =>
                {
                    if (IsValidDirectory(folder))
                    {
                        DotnetUpdateApi(folder, config); // Pass the config to the method.
                    }
                });
            }
        }

        private static void UpdateApi(string folderPath, Config config)
        {
            if (config.Updateapis)
            {
                if (IsValidDirectory(folderPath))
                {
                    DotnetUpdateApi(folderPath, config); // Pass the config to the method.
                }
            }
        }

        private static void DotnetUpdateApi(string folderPath, Config config)
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
                        Console.WriteLine($"Update {file} package: {package}", ConsoleColor.Magenta);
                    }
                    RunDotnetAddPackage(file, package, config);
                }
            }
        }

        private static void RunDotnetAddPackage(string filePath, string packageName, Config config)
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

        private static void ProcessCurrentDirectory(Config config)
        {
            Console.WriteLine("Listing all folders in the current directory:");
            string[] folders = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory);

            if (!folders.Any())
            {
                Console.WriteLine("No folders found in the current directory.");
                return;
            }

            Parallel.ForEach(folders, folder =>
            {
                if (IsValidDirectory(folder))
                {
                    RunDotNetPublish(folder, config); // Pass the config to the method.
                }
            });
        }

        private static bool IsValidDirectory(string folderPath)
        {
            return Directory.Exists(folderPath) && Directory.EnumerateFiles(folderPath, CsProjFilePattern, SearchOption.TopDirectoryOnly).Any();
        }

        private static void RunDotNetPublish(string folderPath, Config config)
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

                CleanupArtifacts(outputPath);
                stopwatch.Stop();

                Console.WriteLine($"{folderName} completed in {stopwatch.Elapsed.TotalSeconds} seconds.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running dotnet publish: {ex.Message}");
            }
        }

        private static void CleanupArtifacts(string folderPath)
        {
            try
            {
                foreach (string filePattern in FilesToDelete)
                {
                    foreach (string file in Directory.GetFiles(folderPath, filePattern))
                    {
                        File.Delete(file);
                    }
                }

                string runtimesDir = Path.Combine(folderPath, "runtimes");
                if (Directory.Exists(runtimesDir))
                {
                    Directory.Delete(runtimesDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up artifacts: {ex.Message}");
            }
        }

    }
}