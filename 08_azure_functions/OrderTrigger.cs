using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public class SubmitOrder
{
    private readonly ILogger<SubmitOrder> _logger;

    public SubmitOrder(
        ILogger<SubmitOrder> logger)
    {
        _logger = logger;
    }

    [Function("SubmitOrder")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "orders")]
        HttpRequestData request)
    {
        OrderRequest? order =
            await request.ReadFromJsonAsync<OrderRequest>();

        if (order == null ||
            order.Items == null ||
            order.Items.Count == 0)
        {
            var badRequest =
                request.CreateResponse(
                    HttpStatusCode.BadRequest);

            await badRequest.WriteStringAsync(
                "Order must contain at least one item.");

            return badRequest;
        }

        decimal subtotal =
            order.Items.Sum(
                item => item.Price * item.Quantity);

        decimal shipping =
            subtotal >= 100 ? 0 : 10;

        decimal total =
            subtotal + shipping;

        string orderId =
            Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Order {OrderId} received for customer {CustomerId}",
            orderId,
            order.CustomerId);

        var result = new
        {
            OrderId = orderId,
            CustomerId = order.CustomerId,
            ItemCount = order.Items.Sum(x => x.Quantity),
            Subtotal = subtotal,
            Shipping = shipping,
            Total = total,
            Status = "Accepted"
        };

        var response =
            request.CreateResponse(
                HttpStatusCode.Created);

        await response.WriteAsJsonAsync(result);

        return response;
    }
}

public class OrderRequest
{
    public string CustomerId { get; set; } = "";
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public string ProductId { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}