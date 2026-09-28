using Azure;
using Azure.Messaging.EventGrid;

string topicEndpoint =
    "";

string topicKey =
    "";

var client =
    new EventGridPublisherClient(
        new Uri(topicEndpoint),
        new AzureKeyCredential(topicKey));


var orders = new[]
{
    new
    {
        OrderId = "ORD-1001",
        CustomerId = "CUST-001",
        Total = 1500
    },
    new
    {
        OrderId = "ORD-1002",
        CustomerId = "CUST-002",
        Total = 800
    },
    new
    {
        OrderId = "ORD-1003",
        CustomerId = "CUST-003",
        Total = 2200
    }
};


List<EventGridEvent> events = [];

foreach (var order in orders)
{
    var eventGridEvent =
        new EventGridEvent(
            subject: $"/orders/{order.OrderId}",
            eventType: "Order.Created",
            dataVersion: "1.0",
            data: order);

    events.Add(eventGridEvent);
}


await client.SendEventsAsync(events);

Console.WriteLine(
    $"{events.Count} events sent.");