using System;
using System.IO;

namespace Compiler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load the configuration from the JSON file.
            var configManager = new ConfigurationManager();
            Config config = configManager.LoadConfig();

            Console.WriteLine($"[Version {config.Version}] by johnoclock");

            if (config.Update)
            {
                await GithubUpdater.UpdateFromGithub(config);
            }

            if (args.Length > 0)
            {
                ApiUpdater.UpdateApi(args[0], config);
                DirectoryProcessor.ProcessDirectory(args[0], config); // Pass the config to the method.
            }
            else
            {
                ApiUpdater.UpdateApiAll(config);

                DirectoryProcessor.ProcessCurrentDirectory(config); // Pass the config to the method.
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}