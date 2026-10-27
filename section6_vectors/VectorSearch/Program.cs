using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using OllamaSharp;

Movie[] MovieData =
[
    new() { Key = 1, Title = "The Last Signal", Description = "An astronaut follows a mysterious radio signal to a distant planet and discovers an abandoned research station." },
    new() { Key = 2, Title = "Kitchen Rivals", Description = "Two rival chefs must work together to save their neighborhood restaurant in a lighthearted comedy." },
    new() { Key = 3, Title = "Mountain Rescue", Description = "A rescue team braves a winter storm to find hikers stranded on a remote mountain." }
];

// The indexing and retrieval pipeline depends on IEmbeddingGenerator,
// not directly on OllamaSharp.
using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    CreateEmbeddingGenerator();

using var vectorStore = new InMemoryVectorStore();
var movies = vectorStore.GetCollection<int, Movie>("movies");
await movies.EnsureCollectionExistsAsync();

foreach (var movie in MovieData)
{
    // Index the description using the same model for every record.
    movie.Vector = await embeddingGenerator.GenerateVectorAsync(movie.Description);
    if (movie.Vector.Length != 384)
        throw new InvalidOperationException($"Expected 384 dimensions, received {movie.Vector.Length} for {movie.Title}.");

    await movies.UpsertAsync(movie);
    Console.WriteLine($"{movie.Title} | dimensions: {movie.Vector.Length} | upsert successful");
}

const float MinimumSimilarityScore = 0.20f;

var query = "A science fiction movie about space travel";
var queryVector = await embeddingGenerator.GenerateVectorAsync(query);

// Top-K controls how many candidates we retrieve.
// Threshold controls whether a candidate is relevant enough to accept.
Console.WriteLine($"\nQuery: {query}");
await foreach (var result in movies.SearchAsync(queryVector, top: 2))
{
    if (result.Score >= MinimumSimilarityScore)
    {
        Console.WriteLine($"Title: {result.Record.Title}");
        Console.WriteLine($"Description: {result.Record.Description}");
        Console.WriteLine($"Similarity/relevance score (not a percentage or probability): {result.Score}");
        Console.WriteLine();
    }
    else
    {
        Console.WriteLine($"Rejected: {result.Record.Title} | similarity score {result.Score} is below threshold {MinimumSimilarityScore}.");
    }
}

static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
{
    return new OllamaApiClient(new Uri("http://localhost:11434/"), "all-minilm");
}
