public class Article
{
    public string id { get; set; } = "";

    public string title { get; set; } = "";

    public string category { get; set; } = "";

    public string content { get; set; } = "";

    public float[] embedding { get; set; } = [];
}

public class SearchResult
{
    public string id { get; set; } = "";

    public string title { get; set; } = "";

    public string category { get; set; } = "";

    public string content { get; set; } = "";

    public double score { get; set; }
}