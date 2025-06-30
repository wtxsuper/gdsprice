using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NLog;

namespace feedreader
{
    internal class Program
    {
        private const string directoryPATH = @"D:\Производственная практика 2025\inFiles";
        private const string schemaPATH = @"schema.json";
        
        JSchema schema;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
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
                string schemaJson = File.ReadAllText(schemaPATH);
                string goodsJson = File.ReadAllText(e.FullPath);

                JSchema schema = JSchema.Parse(schemaJson);
                JArray goods = JArray.Parse(goodsJson);

                IList<string> errors;
                bool isValid = goods.IsValid(schema, out errors);

                if (isValid)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    Logger.Error("Schema validation errors: \n" + string.Join('\n', errors));
                    throw new Exception("Validation error with file " + e.FullPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
