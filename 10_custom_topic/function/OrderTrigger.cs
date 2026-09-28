using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class OrderTrigger
{
    private readonly ILogger<OrderTrigger> _logger;

    public OrderTrigger(
        ILogger<OrderTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(OrderTrigger))]
    public void Run(
        [EventGridTrigger]
        EventGridEvent eventGridEvent)
    {
        _logger.LogInformation(
            "Event type: {EventType}",
            eventGridEvent.EventType);

        _logger.LogInformation(
            "Subject: {Subject}",
            eventGridEvent.Subject);

        _logger.LogInformation(
            "Event ID: {Id}",
            eventGridEvent.Id);

        _logger.LogInformation(
            "Data: {Data}",
            eventGridEvent.Data.ToString());
    }
}