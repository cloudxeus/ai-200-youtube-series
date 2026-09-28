using Npgsql;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var openAIEndpoint =
    builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException(
        "Azure OpenAI endpoint was not configured.");

var openAIKey =
    builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException(
        "Azure OpenAI API key was not configured.");

var embeddingDeployment =
    builder.Configuration["AzureOpenAI:EmbeddingDeployment"]
    ?? throw new InvalidOperationException(
        "Embedding deployment was not configured.");

var connectionString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "PostgreSQL connection string has not been configured.");

var openAIOptions = new OpenAIClientOptions
{
    Endpoint = new Uri(
        $"{openAIEndpoint.TrimEnd('/')}/openai/v1/")
};

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(openAIKey),
    openAIOptions);

var embeddingClient =
    openAIClient.GetEmbeddingClient(
        embeddingDeployment);

builder.Services.AddSingleton(
    NpgsqlDataSource.Create(connectionString));

builder.Services.AddSingleton(embeddingClient);

var app = builder.Build();


app.MapGet("/api/articles", async (
    NpgsqlDataSource dataSource) =>
{
    const string sql = """
        SELECT
            article_id,
            title,
            category,
            content,
            created_at
        FROM support_articles
        ORDER BY article_id;
        """;

    await using var connection =
        await dataSource.OpenConnectionAsync();

    await using var command =
        new NpgsqlCommand(sql, connection);

    await using var reader =
        await command.ExecuteReaderAsync();

    var articles = new List<SupportArticle>();

    while (await reader.ReadAsync())
    {
        articles.Add(new SupportArticle(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetDateTime(4)
        ));
    }

    return Results.Ok(articles);
});


app.MapGet("/api/articles/{id:int}", async (
    int id,
    NpgsqlDataSource dataSource) =>
{
    const string sql = """
        SELECT
            article_id,
            title,
            category,
            content,
            created_at
        FROM support_articles
        WHERE article_id = @id;
        """;

    await using var connection =
        await dataSource.OpenConnectionAsync();

    await using var command =
        new NpgsqlCommand(sql, connection);

    command.Parameters.AddWithValue("id", id);

    await using var reader =
        await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return Results.NotFound();
    }

    var article = new SupportArticle(
        reader.GetInt32(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetDateTime(4)
    );

    return Results.Ok(article);
});


app.MapPost("/api/articles", async (
    CreateArticleRequest request,
    NpgsqlDataSource dataSource) =>
{
    const string sql = """
        INSERT INTO support_articles
            (title, category, content)
        VALUES
            (@title, @category, @content)
        RETURNING article_id;
        """;

    await using var connection =
        await dataSource.OpenConnectionAsync();

    await using var command =
        new NpgsqlCommand(sql, connection);

    command.Parameters.AddWithValue(
        "title",
        request.Title);

    command.Parameters.AddWithValue(
        "category",
        request.Category);

    command.Parameters.AddWithValue(
        "content",
        request.Content);

    var articleId =
        (int)(await command.ExecuteScalarAsync())!;

    return Results.Created(
        $"/api/articles/{articleId}",
        new { ArticleId = articleId });
});


app.MapPost("/api/articles/generate-embeddings", async (
    NpgsqlDataSource dataSource,
    EmbeddingClient embeddingClient) =>
{
    var articles =
        new List<(int Id, string Title, string Content)>();


    const string selectSql = """
        SELECT
            article_id,
            title,
            content
        FROM support_articles
        WHERE embedding IS NULL;
        """;


    await using (var connection =
        await dataSource.OpenConnectionAsync())
    {
        await using var command =
            new NpgsqlCommand(selectSql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            articles.Add(
                (
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2)
                )
            );
        }
    }


    foreach (var article in articles)
    {
        var textToEmbed =
            $"{article.Title}\n{article.Content}";


        OpenAIEmbedding embedding =
            embeddingClient.GenerateEmbedding(
                textToEmbed);


        var vector =
            ToPgVector(
                embedding.ToFloats());


        const string updateSql = """
            UPDATE support_articles
            SET embedding =
                CAST(@embedding AS vector)
            WHERE article_id = @articleId;
            """;


        await using var connection =
            await dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(
                updateSql,
                connection);


        command.Parameters.AddWithValue(
            "embedding",
            vector);

        command.Parameters.AddWithValue(
            "articleId",
            article.Id);


        await command.ExecuteNonQueryAsync();
    }


    return Results.Ok(
        new
        {
            ArticlesUpdated = articles.Count
        });
});

static string ToPgVector(
    ReadOnlyMemory<float> embedding)
{
    return "[" +
        string.Join(
            ",",
            embedding.ToArray()
                .Select(value =>
                    value.ToString(
                        "R",
                        CultureInfo.InvariantCulture)))
        + "]";
}

app.Run();


record SupportArticle(
    int ArticleId,
    string Title,
    string Category,
    string Content,
    DateTime CreatedAt);


record CreateArticleRequest(
    string Title,
    string Category,
    string Content);