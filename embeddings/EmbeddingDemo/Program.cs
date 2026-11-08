using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>Demonstrates text embeddings and an in-memory semantic search.</summary>
internal static class Program
{
    /// <summary>Generates document and query embeddings, then ranks the documents.</summary>
    /// <param name="args">An optional search query supplied as one quoted argument.</param>
    private static async Task Main(string[] args)
    {
        try
        {
            // Step 1: Load configuration and define the helpdesk example.
            string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(
                await File.ReadAllTextAsync(settingsPath))
                ?? throw new InvalidOperationException("Configuration is missing.");

            if (string.IsNullOrWhiteSpace(settings.EmbeddingModel))
                throw new InvalidOperationException("Set EmbeddingModel in appsettings.json.");

            string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Set the OPENAI_API_KEY environment variable.");

            string query = args.Length > 0 ? args[0] : "My laptop won't turn on.";
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("The search query cannot be empty.");

            string[] documents =
            [
                "A computer fails to start when the power button is pressed.",
                "The best laptop sleeves for international travel.",
                "How to reset your account password.",
                "A guide to configuring wireless printers."
            ];

            // Step 2: Show the deliberately simple exact-keyword baseline.
            Console.WriteLine($"Query: {query}");
            Console.WriteLine("\nExact keyword filter: laptop");
            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i].Contains("laptop", StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine($"{(char)('A' + i)} | {documents[i]}");
            }

            using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Step 3: Embed all documents in one request and the query in another.
            Console.WriteLine("\nGenerating document embeddings...");
            float[][] documentVectors = await GenerateEmbeddingsAsync(
                http, settings.EmbeddingModel, documents);
            Console.WriteLine("Generating query embedding...");
            float[] queryVector = (await GenerateEmbeddingsAsync(
                http, settings.EmbeddingModel, [query]))[0];

            Console.WriteLine($"\nVector dimensions: {queryVector.Length}");
            Console.WriteLine("Query vector preview: [" + string.Join(", ",
                queryVector.Take(5).Select(value => value.ToString("F6", CultureInfo.InvariantCulture)))
                + ", ...]");

            // Step 4: Compare complete vectors and sort by similarity.
            var ranked = documents.Select((text, index) => new
            {
                Id = (char)('A' + index),
                Text = text,
                Score = CosineSimilarity(queryVector, documentVectors[index])
            }).OrderByDescending(result => result.Score);

            Console.WriteLine("\nSemantic search results (higher = more similar):");
            foreach (var result in ranked)
                Console.WriteLine(FormattableString.Invariant(
                    $"{result.Score:F3} | {result.Id} | {result.Text}"));

            Console.WriteLine("\nSimilarity is a ranking signal, not a probability of correctness.\n");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
            or IOException or JsonException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"Demo could not finish: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>Calls the Embeddings API and restores vectors to input order using each index.</summary>
    /// <param name="http">The reusable HTTP client with bearer authentication.</param>
    /// <param name="model">The embedding model selected in configuration.</param>
    /// <param name="inputs">The nonempty texts to embed in one request.</param>
    /// <returns>One floating-point vector for each input text.</returns>
    private static async Task<float[][]> GenerateEmbeddingsAsync(
        HttpClient http, string model, string[] inputs)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings",
            new { model, input = inputs, encoding_format = "float" });

        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement data = json.RootElement.GetProperty("data");
        if (data.GetArrayLength() != inputs.Length)
            throw new InvalidOperationException("Unexpected number of returned embeddings.");

        float[][] vectors = new float[inputs.Length][];
        foreach (JsonElement item in data.EnumerateArray())
        {
            int index = item.GetProperty("index").GetInt32();
            if (index < 0 || index >= inputs.Length || vectors[index] is not null)
                throw new InvalidOperationException("Invalid or duplicate embedding index.");

            vectors[index] = item.GetProperty("embedding").EnumerateArray()
                .Select(value => value.GetSingle()).ToArray();
        }

        if (vectors.Any(vector => vector is null || vector.Length == 0))
            throw new InvalidOperationException("An embedding is missing or empty.");

        return vectors;
    }

    /// <summary>Calculates cosine similarity using every component of both vectors.</summary>
    /// <param name="a">The query vector.</param>
    /// <param name="b">A document vector from the same model and configuration.</param>
    /// <returns>A score in the mathematical range -1 to 1, subject to rounding.</returns>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || a.Length != b.Length)
            throw new ArgumentException("Vectors must have matching, nonzero dimensions.");

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            throw new ArgumentException("Cosine similarity requires nonzero vectors.");

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

/// <summary>Contains the model setting without storing an API key.</summary>
internal sealed class AppSettings
{
    /// <summary>Gets or sets the embedding model used for both documents and queries.</summary>
    public string EmbeddingModel { get; set; } = "";
}
