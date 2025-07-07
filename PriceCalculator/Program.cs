using ClassLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using PriceCalculator;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

Meter meter = new("PriceCalculator.Metrics", "1.0");
using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("PriceCalculator.Metrics")
            .AddConsoleExporter()
            .Build();

Counter<long> calculationStarted = meter.CreateCounter<long>("price_calculation_started_total", "count", "Number of price calculations started");
Histogram<double> calculationDuration = meter.CreateHistogram<double>("price_calculation_duration_seconds", "seconds", "Duration of price calculation");

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
            logger.Debug($"Received message: {received.FileName}");
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

    var stopwatch = Stopwatch.StartNew();
    calculationStarted.Add(1);

    try
    {
        JArray json = JArray.Parse(message.Content); // products array

        List<PricedProduct> pricedList = new List<PricedProduct>();
        foreach (JObject j in json)
        {
            CountedProduct? counted = j.ToObject<CountedProduct>();
            if (counted != null)
            {
                PricedProduct priced = new PricedProduct(counted);
                pricedList.Add(priced);
            }
            else
            {
                throw new ArgumentNullException(nameof(counted), "Product deserialization failed.");
            }
        }
        string resultJson = JsonConvert.SerializeObject(pricedList);
        string resultFilename = Path.Combine(Settings.DIRECTORY_PATH, $"c_{message.FileName}");
        await File.WriteAllTextAsync(resultFilename, resultJson);
        logger.Debug($"Wrote result to file \"{resultFilename}\"");
    }
    finally { sem.Release(); }

    stopwatch.Stop();
    var duration = stopwatch.Elapsed.TotalSeconds;
    calculationDuration.Record(duration);
}