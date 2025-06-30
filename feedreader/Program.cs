using NLog;

namespace feedreader
{
    internal class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            try
            {
                const string directoryPATH = @"D:\Производственная практика 2025\inFiles";

                Logger.Info("Start feed reader module");

                using (FileSystemWatcher watcher = new FileSystemWatcher(directoryPATH))
                {
                    watcher.IncludeSubdirectories = true;
                    watcher.Filter = "*.json";
                    watcher.Created += OnFileCreated;

                    watcher.EnableRaisingEvents = true;
                    Logger.Debug($"Watch files in \"{directoryPATH}\"");

                    Console.WriteLine("Press enter to exit.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
