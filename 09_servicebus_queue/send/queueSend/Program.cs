using Azure.Messaging.ServiceBus;
using System.Text.Json;

string connString="";

string queueName = "orders";

await using ServiceBusClient client =
    new(connString);

ServiceBusSender sender =
    client.CreateSender(queueName);

List<ServiceBusMessage> messages = [];

for (int i = 1; i <= 5; i++)
{
    var order = new
    {
        OrderId = $"ORD-{i:000}",
        CustomerId = $"CUST-{i:000}",
        Product = "Laptop",
        Quantity = i
    };

    string json =
        JsonSerializer.Serialize(order);

    ServiceBusMessage message =
        new(json)
        {
            MessageId = order.OrderId,
            ContentType = "application/json"
        };

    messages.Add(message);
}

await sender.SendMessagesAsync(messages);

Console.WriteLine(
    $"{messages.Count} messages sent.");
