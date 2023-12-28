using System;
using System.IO;

namespace Compiler
{
    public class ApiUpdater
    {
        public static void UpdateApi(string folderPath, Config config)
        {
            if (config.Updateapis)
            {
                if (DirectoryProcessor.IsValidDirectory(folderPath))
                {
                    DotNetCommandExecutor.DotnetUpdateApi(folderPath, config); // Pass the config to the method.
                }
            }
        }
        public static void UpdateApiAll(Config config)
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
                    if (DirectoryProcessor.IsValidDirectory(folder))
                    {
                        DotNetCommandExecutor.DotnetUpdateApi(folder, config); // Pass the config to the method.
                    }
                });
            }
        }
    }
}
