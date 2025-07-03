using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using QuantityCalculator;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Logger logger = LogManager.GetCurrentClassLogger();
var sem = new SemaphoreSlim(Settings.MAX_CONCURRENT);
logger.Info("Program started.");


CancellationTokenSource cts = new(TimeSpan.FromSeconds(Settings.CONNECTION_TIMEOUT_SECONDS));
try
{
    var factory = new ConnectionFactory { HostName = Settings.RABBIT_HOSTNAME };
    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    await channel.QueueDeclareAsync(queue: Settings.RECEIVE_QUEUE, durable: true, exclusive: false, autoDelete: false,
        arguments: null);

    logger.Debug("Ready to receive messages.");

    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += OnReceivedAsync;

    await channel.BasicConsumeAsync(Settings.RECEIVE_QUEUE, autoAck: true, consumer: consumer);

    Console.WriteLine("Started listening for messages...");
    Console.BackgroundColor = ConsoleColor.Green;
    Console.WriteLine("> Press any key to exit.");
    Console.ResetColor();
    Console.ReadKey();
    logger.Info("Program terminated by user input.");
}
catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) { logger.Error("RabbitMQ connection timeout", ex); }
catch (Exception ex) { logger.Error(ex); }

async Task OnReceivedAsync(object sender, BasicDeliverEventArgs e)
{
    try
    {
        var body = e.Body.ToArray();
        var messageJson = Encoding.UTF8.GetString(body);
        var received = JsonConvert.DeserializeObject<Message>(messageJson);
        if (received != null)
        {
            await ProcessMessageAsync(received);
        }
        else
        {
            throw new ArgumentNullException(nameof(received), "Message deserialization failed.");
        }
    }
    catch (Exception ex) { logger.Error(ex); }
}

async Task ProcessMessageAsync(Message message)
{
    await sem.WaitAsync(); // wait for semaphore to allow concurrent processing
    try
    {
        JArray json = JArray.Parse(message.Content); // products array

        List<CountedProduct> countedList = new List<CountedProduct>();
        foreach (JObject j in json)
        {
            Product? product = j.ToObject<Product>();
            if (product != null)
            {
                CountedProduct counted = new CountedProduct(product);
                countedList.Add(counted);
            }
            else
            {
                throw new ArgumentNullException(nameof(product), "Product deserialization failed.");
            }
        }
        string outJson = JsonConvert.SerializeObject(countedList);
        await SendMessageAsync(outJson, message.FileName);
    }
    finally { sem.Release(); }
}

async Task SendMessageAsync(string json, string filename = "")
{
    // Connection time limit
    CancellationTokenSource cts = new(TimeSpan.FromSeconds(Settings.CONNECTION_TIMEOUT_SECONDS));
    try
    {
        var factory = new ConnectionFactory { HostName = Settings.RABBIT_HOSTNAME };
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
    catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) { logger.Error("RabbitMQ connection timeout", ex); }
}




