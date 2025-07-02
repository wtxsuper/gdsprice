using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using QuantityCalculator;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Logger logger = LogManager.GetCurrentClassLogger();

try
{
    CancellationTokenSource cts = new(TimeSpan.FromSeconds(Settings.CONNECTION_TIMEOUT_SECONDS));

    var factory = new ConnectionFactory { HostName = Settings.RABBIT_HOSTNAME };
    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    await channel.QueueDeclareAsync(queue: Settings.RECEIVE_QUEUE, durable: false, exclusive: false, autoDelete: false,
        arguments: null);

    logger.Debug("Waiting for messages.");

    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += OnReceivedAsync;

    await channel.BasicConsumeAsync(Settings.RECEIVE_QUEUE, autoAck: true, consumer: consumer);

    Console.BackgroundColor = ConsoleColor.Green;
    Console.WriteLine("> Press any key to exit.");
    Console.ResetColor();
    Console.ReadKey();
}
catch (Exception ex) { logger.Error(ex); }

async Task OnReceivedAsync(object sender, BasicDeliverEventArgs e)
{
    try
    {
        var body = e.Body.ToArray();
        logger.Debug("Received message. Hash: " + body.GetHashCode());
        var message = Encoding.UTF8.GetString(body);
        await ProcessJsonAsync(message);
    }
    catch (Exception ex) { logger.Error(ex); }
}

async Task ProcessJsonAsync(string message)
{
    JArray json = JArray.Parse(message);

    List<CountedProduct> countedList = new List<CountedProduct>();
    foreach (JObject j in json)
    {
        Product? product = j.ToObject<Product>();

        if (product == null)
        {
            throw new ArgumentNullException();
        }

        CountedProduct counted = await Task.Run(() => CountProductAsync(product));
        countedList.Add(counted);
    }
    string outJson = JsonConvert.SerializeObject(countedList);
    await SendMessageAsync(outJson);
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

async Task<CountedProduct> CountProductAsync(Product product)
{
    CountedProduct cp = new CountedProduct(product);

    if (cp.Type == "product")
    {
        cp.WarehouseQuantity = await Task.Run(() => CountAllWarehouse(cp.Warehouses));
        cp.SupplierQuantity = await Task.Run(() => CountAllSupplier(cp.Suppliers));
    }
    else
    {
        int minSubWh = int.MaxValue; // minimum quantity in warehouses from subproducts for sets or variants
        foreach (Product sub in cp.SubProducts)
        {
            int subWh = await Task.Run(() => CountAllWarehouse(sub.Warehouses));
            if (subWh < minSubWh) { minSubWh = subWh; }
        }
        cp.WarehouseQuantity = minSubWh;
        cp.SupplierQuantity = 0;
    }
    cp.Quantity = cp.WarehouseQuantity + cp.SupplierQuantity;
    return cp;
}

int CountAllWarehouse(List<Warehouse> warehouses)
{
    int sum = 0;
    foreach (Warehouse w in warehouses)
    {
        sum += w.Quantity;
    }
    return sum;
}

int CountAllSupplier(List<Supplier> suppliers)
{
    int sum = 0;
    foreach (Supplier s in suppliers)
    {
        sum += s.Quantity;
    }
    return sum;
}




