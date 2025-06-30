using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NLog;
using System.Collections.Concurrent;

namespace feedreader
{
    internal class Program
    {
        private const string DIRECTORY_PATH = @"D:\Производственная практика 2025\inFiles";
        private const string SCHEMA_PATH = @"schema.json";
        private const int MAX_ATTEMPTS = 5;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();
        private static bool processingFiles = false; // is queue processing?

        static void Main(string[] args)
        {
            try
            {
                using (FileSystemWatcher watcher = new FileSystemWatcher(DIRECTORY_PATH))
                {
                    watcher.IncludeSubdirectories = true;
                    watcher.Filter = "*.json";
                    watcher.Created += OnFileCreated;

                    watcher.EnableRaisingEvents = true;
                    Logger.Debug($"Watch files in \"{DIRECTORY_PATH}\"");

                    Console.WriteLine("Press enter to exit.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Initilization");
            }
        }
        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Add file to queue
                fileQueue.Enqueue(e.FullPath);
                Logger.Info($"File queued for processing: {e.FullPath}");

                // Run processing if not running
                if (!processingFiles)
                {
                    processingFiles = true;
                    Task.Run(() => ProcessFilesQueue());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static async void ProcessFilesQueue()
        {
            while (fileQueue.TryDequeue(out string filePath))
            {
                int attempt = 1;
                bool success = false; // is file successfully opened and process?

                while (!success && attempt <= MAX_ATTEMPTS)
                {
                    try
                    {
                        string schemaJson = await File.ReadAllTextAsync(SCHEMA_PATH);
                        string goodsJson = await File.ReadAllTextAsync(filePath);


                        JSchema schema = JSchema.Parse(schemaJson);
                        JArray goods = JArray.Parse(goodsJson);

                        IList<string> errors;
                        bool isValid = goods.IsValid(schema, out errors);

                        if (isValid)
                        {
                            success = true;
                            Logger.Info($"{filePath} is valid.");
                            throw new NotImplementedException();
                        }
                        else
                        {
                            Logger.Error("Schema validation errors: \n" + string.Join('\n', errors));
                            throw new Exception("Validation error with file " + filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is IOException && attempt <= MAX_ATTEMPTS && !success)
                        {
                            Logger.Warn($"Can't open file. Try again... ({attempt})");
                            attempt++;

                            // Wait or file process can be blocked
                            await Task.Delay(500);
                        }
                        else
                        {
                            Logger.Error(ex);
                            break;
                        }

                    }
                }
            }
            processingFiles = false;
        }
    }
}
