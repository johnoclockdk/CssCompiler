using System;
using System.IO;

namespace Compiler
{
    public class ArtifactCleaner
    {
        private static readonly string[] FilesToDelete = new string[] { "Serilog.*.dll", "Serilog.dll", "Microsoft*.dll", "CounterStrikeSharp.API.dll", "McMaster.NETCore.Plugins.dll", "Scrutor.dll", "System.Diagnostics.EventLog.dll" };

        public static void CleanupArtifacts(string folderPath)
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
