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

                    await ApplyUpdateAsync(tempFilePath);
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
            string downloadUrl = latestRelease["assets"]![0]!["browser_download_url"]!.ToString();

            Console.WriteLine("Downloading Latest Version...");
            using var downloadResponse = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            using var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            await CopyContentToStream(downloadResponse.Content, fs);
        }

        private static async Task CopyContentToStream(HttpContent content, FileStream fileStream)
        {
            var totalBytes = content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            var totalRead = 0L;
            using var stream = await content.ReadAsStreamAsync();

            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;
                Console.Write($"\rDownload progress: {totalRead * 100 / totalBytes}%");
            }
            Console.WriteLine("\nDownload Complete.");
        }

        private static async Task ApplyUpdateAsync(string tempFilePath)
        {
            string currentExecutablePath = Process.GetCurrentProcess().MainModule!.FileName;
            string backupExecutablePath = currentExecutablePath + ".bak";

            // Asynchronously wait for the file move to complete
            await Task.Run(() =>
            {
                // Check if backup executable already exists and delete it
                if (File.Exists(backupExecutablePath))
                {
                    File.Delete(backupExecutablePath);
                }

                // Rename current executable as a backup
                File.Move(currentExecutablePath, backupExecutablePath, true);

                // Move new executable to application path, overwrite if existing
                File.Move(tempFilePath, currentExecutablePath, true);
            });

            // Restart application
            ProcessStartInfo startInfo = new ProcessStartInfo(currentExecutablePath)
            {
                Arguments = "restart",
                UseShellExecute = false
            };

            Process.Start(startInfo);

            // Exit current application
            Environment.Exit(0);
        }
    }
}