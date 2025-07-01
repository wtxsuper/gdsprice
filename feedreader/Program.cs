using feedreader;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NLog;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;

Logger logger = LogManager.GetCurrentClassLogger();
ConcurrentQueue<string> fileQueue = new();
bool processingFiles = false; // is queue processing?

try
{
    using FileSystemWatcher watcher = new(Settings.DIRECTORY_PATH);
    watcher.Filter = "*.json";
    watcher.Created += OnFileCreated;

    watcher.EnableRaisingEvents = true;
    logger.Debug($"Watch files in \"{Settings.DIRECTORY_PATH}\"");

    Console.BackgroundColor = ConsoleColor.Green;
    Console.WriteLine("> Press any key to exit.");
    Console.ResetColor();
    _ = Console.ReadKey();
}
catch (Exception ex)
{
    logger.Error(ex);
}

void OnFileCreated(object sender, FileSystemEventArgs e)
{
    try
    {
        // Add file to queue
        fileQueue.Enqueue(e.FullPath);
        logger.Debug($"File queued for processing: \"{e.Name}\".");

        // Run processing if not running
        if (!processingFiles)
        {
            processingFiles = true;
            _ = Task.Run(ProcessFilesQueue);
        }
    }
    catch (Exception ex)
    {
        logger.Error(ex);
    }
}

async void ProcessFilesQueue()
{
    while (fileQueue.TryDequeue(out string? filePath))
    {
        int attempt = 1;
        bool isProcessed = false; // is file successfully opened and processed?

        string filename = Path.GetFileName(filePath);

        while (!isProcessed && attempt <= Settings.MAX_ATTEMPTS)
        {
            try
            {
                // max time to read file
                CancellationTokenSource cts = new(TimeSpan.FromSeconds(Settings.READ_FILE_TIMEOUT_SECONDS));

                string schemaJson = await File.ReadAllTextAsync(Settings.SCHEMA_PATH, cts.Token);
                string goodsJson = await File.ReadAllTextAsync(filePath, cts.Token);


                JSchema schema = JSchema.Parse(schemaJson);
                JArray goods = JArray.Parse(goodsJson);

                bool isValid = goods.IsValid(schema, out IList<string> errors);

                if (isValid)
                {
                    isProcessed = true;
                    logger.Debug($"Is valid: \"{filename}\".");
                    SendData(goodsJson, filename);
                }
                else
                {
                    isProcessed = true;
                    logger.Warn($"Not valid: \"{filename}\".\nSchema validation errors: \n" + string.Join('\n', errors));
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException && attempt <= Settings.MAX_ATTEMPTS && !isProcessed)
                {
                    logger.Warn($"Can't open file \"{filename}\". Try again... ({attempt})");
                    attempt++;

                    // Wait or file process can be blocked
                    await Task.Delay(500);
                }
                else if (ex is TaskCanceledException)
                {
                    logger.Error(ex, "File read timeout");
                    break;
                }
                else
                {
                    logger.Error(ex);
                    break; // to next file
                }

            }
        }
    }
    // when queue is empty make flag false
    processingFiles = false;
}

async void SendData(string json, string messageName = "")
{
    try
    {
        string queueName = "toCalc";

        // Connection time limit
        CancellationTokenSource cts = new(TimeSpan.FromSeconds(Settings.CONNECTION_TIMEOUT_SECONDS));
        
        ConnectionFactory factory = new() { HostName = Settings.RABBIT_HOSTNAME };
        using IConnection connection = await factory.CreateConnectionAsync(cts.Token);
        using IChannel channel = await connection.CreateChannelAsync();

        _ = await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false,
            arguments: null);

        byte[] body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: body);

        if (string.IsNullOrEmpty(messageName))
        {
            logger.Debug("Message sent to broker");
        }
        else
        {
            logger.Debug($"\"{messageName}\" sent to broker");
        }
    }
    catch (TaskCanceledException ex)
    {
        logger.Error(ex, "RabbitMQ connection timeout");
    }
    catch (Exception ex)
    {
        logger.Error(ex);
    }
}
