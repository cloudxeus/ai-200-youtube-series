using System.Net;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);


// Get configuration values
var connectionString =
    builder.Configuration["CosmosDb:ConnectionString"]!;

var databaseName =
    builder.Configuration["CosmosDb:DatabaseName"]!;

var containerName =
    builder.Configuration["CosmosDb:ContainerName"]!;


// Create one CosmosClient for the application
builder.Services.AddSingleton(
    new CosmosClient(connectionString)
);


// Register the container
builder.Services.AddSingleton<Container>(serviceProvider =>
{
    var cosmosClient =
        serviceProvider.GetRequiredService<CosmosClient>();

    return cosmosClient.GetContainer(
        databaseName,
        containerName
    );
});



var app = builder.Build();

app.MapGet(
    "/api/articles/{id}",
    async (string id, Container container) =>
{
    try
    {
        var response =
            await container.ReadItemAsync<Article>(
                id,
                new PartitionKey(id)
            );

        return Results.Ok(response.Resource);
    }
    catch (CosmosException ex)
        when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
});

app.MapGet(
    "/api/articles",
    async (string category, Container container) =>
{
    var query =
        new QueryDefinition(
            "SELECT * FROM c WHERE c.category = @category"
        )
        .WithParameter(
            "@category",
            category
        );

    var results =
        new List<Article>();

    using var iterator =
        container.GetItemQueryIterator<Article>(
            query
        );

    while (iterator.HasMoreResults)
    {
        var response =
            await iterator.ReadNextAsync();

        results.AddRange(response);
    }

    return Results.Ok(results);
});

app.Run();

public record Article(
    string id,
    string title,
    string category,
    string content
);