using System;
using System.IO;
using System.Diagnostics;

namespace Compiler
{
    public class DirectoryProcessor
    {

        private const string CsProjFilePattern = "*.csproj";


        public static void ProcessDirectory(string folderPath, Config config)
        {
            if (DirectoryProcessor.IsValidDirectory(folderPath))
            {
                DotNetCommandExecutor.RunDotNetPublish(folderPath, config); // Pass the config to the method.
            }
            else
            {
                Console.WriteLine("The specified path is not a valid directory or project is not valid");
            }
        }

        public static void ProcessCurrentDirectory(Config config)
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
                    DotNetCommandExecutor.RunDotNetPublish(folder, config); // Pass the config to the method.
                }
            });
        }

        public static bool IsValidDirectory(string folderPath)
        {
            return Directory.Exists(folderPath) && Directory.EnumerateFiles(folderPath, CsProjFilePattern, SearchOption.TopDirectoryOnly).Any();
        }
    }
}
