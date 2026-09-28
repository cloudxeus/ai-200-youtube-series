using Azure.AI.OpenAI;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using OpenAI.Embeddings;
using StackExchange.Redis;
using System.ClientModel;

using static NRedisStack.Search.Schema;


// --------------------------------------------------
// Configuration
// --------------------------------------------------

string redisHost =
    "";

string redisKey =
    "";

string openAIEndpoint =
    "";

string openAIKey =
    "";

string embeddingDeployment =
    "text-embedding-3-small";


// --------------------------------------------------
// Connect to Redis
// --------------------------------------------------

var redis = await ConnectionMultiplexer.ConnectAsync(
    $"{redisHost}:10000,password={redisKey},ssl=True");

var db = redis.GetDatabase();


// --------------------------------------------------
// Azure OpenAI embedding client
// --------------------------------------------------

var openAIClient =
    new AzureOpenAIClient(
        new Uri(openAIEndpoint),
        new ApiKeyCredential(openAIKey));

EmbeddingClient embeddingClient =
    openAIClient.GetEmbeddingClient(
        embeddingDeployment);


// --------------------------------------------------
// Sample data
// --------------------------------------------------

var articles = new[]
{
    new
    {
        Id = "1",
        Title = "Refund policy",
        Content =
            "Customers can request a refund within 30 days. " +
            "Refunds are returned to the original payment method."
    },

    new
    {
        Id = "2",
        Title = "Reset password",
        Content =
            "Users can reset their password from the sign-in page. " +
            "A password reset link is sent by email."
    },

    new
    {
        Id = "3",
        Title = "Cancel subscription",
        Content =
            "A subscription can be cancelled from the account page. " +
            "It remains active until the end of the billing period."
    },

    new
    {
        Id = "4",
        Title = "Download invoice",
        Content =
            "Invoices can be downloaded from the billing section " +
            "of the customer account."
    }
};


// --------------------------------------------------
// Create vector index
// --------------------------------------------------

var schema =
    new Schema()
        .AddTextField("title")
        .AddTextField("content")
        .AddVectorField(
            "embedding",
            VectorField.VectorAlgo.HNSW,
            new Dictionary<string, object>
            {
                ["TYPE"] = "FLOAT32",
                ["DIM"] = "1536",
                ["DISTANCE_METRIC"] = "COSINE"
            });


db.FT().Create(
    "article-index",
    new FTCreateParams()
        .On(IndexDataType.HASH)
        .Prefix("article:"),
    schema);


// --------------------------------------------------
// Generate embeddings and store articles
// --------------------------------------------------

foreach (var article in articles)
{
    OpenAIEmbedding embedding =
        embeddingClient.GenerateEmbedding(
            article.Content);

    db.HashSet(
        $"article:{article.Id}",
        new HashEntry[]
        {
            new("title", article.Title),
            new("content", article.Content),
            new(
                "embedding",
                ToBytes(
                    embedding.ToFloats()))
        });
}


// --------------------------------------------------
// Search
// --------------------------------------------------


string searchText ="How can I get my money back?";

OpenAIEmbedding queryEmbedding =
    embeddingClient.GenerateEmbedding(
        searchText);


var query =
    new Query(
        "*=>[KNN 3 @embedding $vector AS score]")
        .AddParam(
            "vector",
            ToBytes(
                queryEmbedding.ToFloats()))
        .ReturnFields(
            "title",
            "content",
            "score")
        .SetSortBy(
            "score",
            true)
        .Dialect(2);


var results =
    db.FT().Search(
        "article-index",
        query);


// --------------------------------------------------
// Display results
// --------------------------------------------------

foreach (var document in results.Documents)
{
    Console.WriteLine();

    Console.WriteLine(
        document["title"]);

    Console.WriteLine(
        document["content"]);

    Console.WriteLine(
        $"Distance: {document["score"]}");
}


// --------------------------------------------------
// Convert FLOAT32 vector to bytes
// --------------------------------------------------

static byte[] ToBytes(
    ReadOnlyMemory<float> vector)
{
    float[] values =
        vector.ToArray();

    byte[] bytes =
        new byte[
            values.Length * sizeof(float)];

    Buffer.BlockCopy(
        values,
        0,
        bytes,
        0,
        bytes.Length);

    return bytes;
}