using Azure.Messaging.ServiceBus;
using System.Text.Json;

string connString="";


string queueName = "orders";

await using ServiceBusClient client =
    new(connString);


ServiceBusReceiver receiver =
    client.CreateReceiver(
        queueName,
        new ServiceBusReceiverOptions
        {
            ReceiveMode =
                ServiceBusReceiveMode.PeekLock
        });

IReadOnlyList<ServiceBusReceivedMessage>
    receivedMessages =
        await receiver.ReceiveMessagesAsync(
            maxMessages: 5,
            maxWaitTime: TimeSpan.FromSeconds(10));

Console.WriteLine(
    $"\nReceived {receivedMessages.Count} messages.\n");

foreach (ServiceBusReceivedMessage message
         in receivedMessages)
{
    Console.WriteLine(
        $"Message ID: {message.MessageId}");

    Console.WriteLine(
        $"Body: {message.Body}");

    Console.WriteLine(
        $"Delivery Count: {message.DeliveryCount}");

    Console.WriteLine(
        "Processing message...");

    await Task.Delay(1000);

    await receiver.CompleteMessageAsync(
        message);

    Console.WriteLine(
        "Message completed.\n");
}

