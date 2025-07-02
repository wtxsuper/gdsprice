using FeedReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NLog;
using RabbitMQ.Client;
using System.Text;

Logger logger = LogManager.GetCurrentClassLogger();

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
    Console.ReadKey();
}
catch (Exception ex)
{
    logger.Error(ex);
}

async void OnFileCreated(object sender, FileSystemEventArgs e)
{
    try
    {
        logger.Debug($"Try processing file: \"{e.Name}\".");
        await ProcessFileAsync(e.FullPath);
    }
    catch (Exception ex)
    {
        logger.Error(ex);
    }
}

async Task ProcessFileAsync(string path)
{
    string filename = Path.GetFileName(path);

    bool isProcessed = false; // is file successfully processed;
    int attempt = 0;

    while (!isProcessed && attempt < Settings.MAX_ATTEMPTS)
    {
        try
        {
            // max time to read file
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(Settings.READ_FILE_TIMEOUT_SECONDS));

            string schemaJson = await File.ReadAllTextAsync(Settings.SCHEMA_PATH, cts.Token);
            string productsJson = await File.ReadAllTextAsync(path, cts.Token);


            JSchema schema = JSchema.Parse(schemaJson);
            JArray products = JArray.Parse(productsJson);

            bool isValid = products.IsValid(schema, out IList<string> errors);

            if (isValid)
            {
                isProcessed = true;
                logger.Debug($"Is valid: \"{filename}\".");
                await SendMessageAsync(productsJson, filename);
            }
            else
            {
                isProcessed = true;
                logger.Debug($"Not valid: \"{filename}\".\nSchema validation errors: \n" + string.Join('\n', errors));
            }
        }
        catch (IOException)
        {
            if (!isProcessed && attempt < Settings.MAX_ATTEMPTS)
            {
                attempt++;
                logger.Warn($"Can't open file \"{filename}\". Try again... ({attempt})");

                // Wait or file process can be blocked
                await Task.Delay(500);
            }
            else
            {
                throw;
            }
        }
    }
}

async Task SendMessageAsync(string json, string filename = "")
{
    // Connection time limit
    CancellationTokenSource cts = new(TimeSpan.FromSeconds(Settings.CONNECTION_TIMEOUT_SECONDS));

    ConnectionFactory factory = new() { HostName = Settings.RABBIT_HOSTNAME };
    using var connection = await factory.CreateConnectionAsync(cts.Token);
    using var channel = await connection.CreateChannelAsync();

    await channel.QueueDeclareAsync(queue: Settings.SEND_QUEUE, durable: true, exclusive: false, autoDelete: false,
        arguments: null);

    var message = new
    {
        FileName = filename,
        Content = json,
        Timestamp = DateTime.UtcNow
    };

    string messageJson = JsonConvert.SerializeObject(message);
    byte[] body = Encoding.UTF8.GetBytes(messageJson);

    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: Settings.SEND_QUEUE, body: body);

    if (string.IsNullOrEmpty(filename))
    {
        logger.Debug("Message sent to broker");
    }
    else
    {
        logger.Debug($"\"{filename}\" sent to broker");
    }
}
