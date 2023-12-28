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
        private static readonly string ApiURL = "https://api.github.com/repos/johnoclockdk/CssCompiler/releases/latest";
        private static readonly HttpClient httpClient = new HttpClient();

        static GithubUpdater()
        {
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        }

        public static async Task UpdateFromGithubAsync(Config config)
        {
            try
            {
                var latestRelease = await GetLatestReleaseAsync();
                var latestVersion = new Version(latestRelease["tag_name"]!.ToString());

                if (IsUpdateRequired(config, latestVersion))
                {
                    var tempFilePath = Path.GetTempFileName();
                    await DownloadLatestVersionAsync(latestRelease, tempFilePath);

                    config.Version = latestVersion.ToString();
                    ConfigurationManager.SaveConfig(config);

                    ApplyUpdate(tempFilePath);
                }
                else
                {
                    Console.WriteLine("No update required. Running the latest version.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating: " + ex.Message);
            }
        }

        private static bool IsUpdateRequired(Config config, Version latestVersion)
        {
            return latestVersion > new Version(config.Version);
        }

        private static async Task<JObject> GetLatestReleaseAsync()
        {
            var response = await httpClient.GetStringAsync(ApiURL);
            return JObject.Parse(response);
        }

        private static async Task DownloadLatestVersionAsync(JObject latestRelease, string tempFilePath)
        {
            string latestVersion = latestRelease["tag_name"]!.ToString();
            string downloadUrl = latestRelease["assets"]![0]!["browser_download_url"]!.ToString();

            Console.WriteLine("Downloading Latest Version: " + latestVersion);
            using var downloadResponse = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            using var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            await CopyContentToStream(downloadResponse.Content, fs);
        }

        private static async Task CopyContentToStream(HttpContent content, FileStream fileStream)
        {
            var totalBytes = content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            var totalRead = 0L;
            var watch = Stopwatch.StartNew();

            using var stream = await content.ReadAsStreamAsync();

            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;

                var elapsedSeconds = watch.ElapsedMilliseconds / 1000.0;
                if (elapsedSeconds > 0)
                {
                    var downloadSpeed = totalRead / elapsedSeconds; // bytes per second
                    Console.Write($"\rDownload progress: {totalRead * 100 / totalBytes}% ({downloadSpeed / 1024:F2} KB/s)");
                }
            }

            watch.Stop();
            Console.WriteLine("\nDownload Complete.");
        }


        private static void ApplyUpdate(string tempFilePath)
        {
            string currentExecutablePath = Process.GetCurrentProcess().MainModule!.FileName;
            string batchScriptPath = Path.GetTempFileName() + ".bat";

            CreateBatchUpdateScript(batchScriptPath, tempFilePath, currentExecutablePath);
            Process.Start(batchScriptPath);
            Environment.Exit(0);
        }

        private static void CreateBatchUpdateScript(string batchScriptPath, string tempFilePath, string currentExecutablePath)
        {
            using var sw = new StreamWriter(batchScriptPath);
            sw.WriteLine("@echo off");
            sw.WriteLine($"COPY /Y \"{tempFilePath}\" \"{currentExecutablePath}\"");
            sw.WriteLine($"DEL \"{tempFilePath}\"");
            sw.WriteLine($"START \"\" \"{currentExecutablePath}\"");
            sw.WriteLine($"DEL \"%~f0\"");
        }
    }
}
