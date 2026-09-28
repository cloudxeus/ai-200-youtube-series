using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using System.ClientModel;

var builder =
    WebApplication.CreateBuilder(args);


// --------------------------------------------------
// Configuration
// --------------------------------------------------

var cosmosConnectionString =
    builder.Configuration[
        "CosmosDb:ConnectionString"
    ]!;

var databaseName =
    builder.Configuration[
        "CosmosDb:DatabaseName"
    ]!;

var containerName =
    builder.Configuration[
        "CosmosDb:ContainerName"
    ]!;


var openAIEndpoint =
    builder.Configuration[
        "AzureOpenAI:Endpoint"
    ]!;

var openAIKey =
    builder.Configuration[
        "AzureOpenAI:ApiKey"
    ]!;

var embeddingDeployment =
    builder.Configuration[
        "AzureOpenAI:EmbeddingDeployment"
    ]!;


// --------------------------------------------------
// Cosmos DB
// --------------------------------------------------

var cosmosClient =
    new CosmosClient(
        cosmosConnectionString
    );

var container =
    cosmosClient.GetContainer(
        databaseName,
        containerName
    );


// --------------------------------------------------
// Azure OpenAI
// --------------------------------------------------

var openAIClient =
    new AzureOpenAIClient(
        new Uri(openAIEndpoint),
        new ApiKeyCredential(openAIKey)
    );

var embeddingClient =
    openAIClient.GetEmbeddingClient(
        embeddingDeployment
    );


var app = builder.Build();


// --------------------------------------------------
// Add support articles with embeddings
// --------------------------------------------------

app.MapPost(
    "/api/seed",
    async () =>
{
    var articles =
        new List<Article>
        {
            new Article
            {
                id = "1001",
                title = "Request a refund",
                category = "Billing",
                content =
                    "Customers can request a refund for eligible purchases by opening their order history and selecting the refund option."
            },

            new Article
            {
                id = "1002",
                title = "Update payment details",
                category = "Billing",
                content =
                    "Customers can update their credit card or other payment information from the billing settings page."
            },

            new Article
            {
                id = "1003",
                title = "Reset your password",
                category = "Account",
                content =
                    "Customers who forget their password can request a password reset link from the sign-in page."
            },

            new Article
            {
                id = "1004",
                title = "Cancel a subscription",
                category = "Subscription",
                content =
                    "Customers can cancel an active subscription from the subscription management page."
            },

            new Article
            {
                id = "1005",
                title = "Troubleshoot login problems",
                category = "Account",
                content =
                    "If a customer cannot sign in, verify the email address, password, and account status."
            }
        };


    foreach (var article in articles)
    {
        var textToEmbed =
            $"{article.title}. {article.content}";


        var embedding =
            await embeddingClient
                .GenerateEmbeddingAsync(
                    textToEmbed
                );


        article.embedding =
            embedding.Value
                .ToFloats()
                .ToArray();


        await container.UpsertItemAsync(
            article,
            new PartitionKey(
                article.id
            )
        );


        Console.WriteLine(
            $"Added: {article.title}"
        );
    }


    return Results.Ok(
        "Articles and embeddings added."
    );
});


// --------------------------------------------------
// Vector search
// --------------------------------------------------

app.MapGet(
    "/api/search",
    async (string query) =>
{
    // Convert the user's question into a vector
    var queryEmbedding =
        await embeddingClient
            .GenerateEmbeddingAsync(
                query
            );


    var queryVector =
        queryEmbedding.Value
            .ToFloats()
            .ToArray();


    // Search Cosmos DB
    var cosmosQuery =
        new QueryDefinition(
            """
            SELECT TOP 3
                c.id,
                c.title,
                c.category,
                c.content,
                VectorDistance(
                    c.embedding,
                    @embedding
                ) AS score
            FROM c
            ORDER BY VectorDistance(
                c.embedding,
                @embedding
            )
            """
        )
        .WithParameter(
            "@embedding",
            queryVector
        );


    var results =
        new List<SearchResult>();


    using var iterator =
        container
            .GetItemQueryIterator<SearchResult>(
                cosmosQuery
            );


    while (iterator.HasMoreResults)
    {
        var response =
            await iterator.ReadNextAsync();

        results.AddRange(
            response
        );
    }


    return Results.Ok(
        results
    );
});


app.Run();