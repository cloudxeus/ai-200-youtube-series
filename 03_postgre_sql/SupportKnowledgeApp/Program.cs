using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "PostgreSQL connection string has not been configured.");

builder.Services.AddSingleton(
    NpgsqlDataSource.Create(connectionString));

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