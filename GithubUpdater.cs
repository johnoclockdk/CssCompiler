using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Compiler
{
    public class GithubUpdater
    {
        public static async Task UpdateFromGithub(Config config)
        {
            string apiURL = $"https://api.github.com/repos/johnoclockdk/CssCompiler/releases/latest";
            string tempFilePath = Path.Combine(Path.GetTempPath(), "newExecutable.exe");
            string currentExecutablePath = Process.GetCurrentProcess().MainModule!.FileName;
            string batchScriptPath = Path.Combine(Path.GetTempPath(), "updateScript.bat");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");

                try
                {
                    var response = await httpClient.GetStringAsync(apiURL);
                    var latestRelease = JObject.Parse(response);

                    string latestVersion = latestRelease["tag_name"]!.ToString();

                    Version latestVer = new Version(latestVersion);
                    Version currentVer = new Version(config.Version);

                    if (latestVer > currentVer)
                    {
                        Console.WriteLine("Downloading Latest Version: " + latestVersion);

                        string downloadUrl = latestRelease["assets"]![0]!["browser_download_url"]!.ToString();

                        var downloadResponse = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                        using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await downloadResponse.Content.CopyToAsync(fs);
                        }

                        config.Version = latestVersion;
                        ConfigurationManager.SaveConfig(config);

                        // Create a batch script for updating
                        using (StreamWriter sw = new StreamWriter(batchScriptPath))
                        {
                            sw.WriteLine("@echo off");
                            //sw.WriteLine("TIMEOUT /T 2 /NOBREAK");
                            sw.WriteLine($"COPY /Y \"{tempFilePath}\" \"{currentExecutablePath}\"");
                            sw.WriteLine($"DEL \"{tempFilePath}\"");
                            sw.WriteLine($"START \"\" \"{currentExecutablePath}\"");
                            sw.WriteLine($"DEL \"%~f0\"");
                        }

                        // Start the batch script and exit the application
                        Process.Start(batchScriptPath);
                        Environment.Exit(0);
                    }
                    else
                    {
                        Console.WriteLine("No update required. Running the latest version.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
    }
}
