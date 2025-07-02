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
    string queueName = "toCalc";

    var factory = new ConnectionFactory { HostName = Settings.RABBIT_HOSTNAME };
    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false,
        arguments: null);

    logger.Debug("Waiting for messages.");

    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += OnReceivedAsync;

    await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

    Console.BackgroundColor = ConsoleColor.Green;
    Console.WriteLine("> Press any key to exit.");
    Console.ResetColor();
    Console.ReadKey();
}
catch (Exception ex)
{
    logger.Error(ex);
}

async Task OnReceivedAsync(object sender, BasicDeliverEventArgs e)
{
    var body = e.Body.ToArray();
    // logger.Debug("Received message. Hash: " + body.GetHashCode());
    var message = Encoding.UTF8.GetString(body);
    logger.Warn("Received message: " + message);
    JArray json = JArray.Parse(message);
    await ProcessJsonAsync(json);
}

async Task ProcessJsonAsync (JArray json)
{
    foreach (JObject j in json)
    {
        Product? product = j.ToObject<Product>();

        if (product == null)
        {
            throw new ArgumentNullException();
        }

        if (product.Type == "product")
        {
            product.WarehouseQuantity = CountInWarehouses(product.Warehouses);
            product.SupplierQuantity = CountInSuppliers(product.Suppliers);
            product.Quantity = product.WarehouseQuantity + product.SupplierQuantity;
        }
        else
        {
            int minSubProductWarehouseCount = int.MaxValue;
            foreach (Product sub in product.SubProducts)
            {
                int i = CountInWarehouses(sub.Warehouses);
                if (i < minSubProductWarehouseCount) { minSubProductWarehouseCount = i; }
            }

            product.WarehouseQuantity = minSubProductWarehouseCount;
            product.SupplierQuantity = 0;
            product.Quantity = product.WarehouseQuantity + product.SupplierQuantity;
        }
        
    }
}

int CountInWarehouses (List<Warehouse> warehouses)
{
    int sum = 0;
    foreach (Warehouse w in warehouses)
    {
        sum += w.Quantity;
    }
    return sum;
}

int CountInSuppliers (List<Supplier> suppliers)
{
    int sum = 0;
    foreach (Supplier s in suppliers)
    {
        sum += s.Quantity;
    }
    return sum;
}




