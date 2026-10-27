using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Numerics.Tensors;

// Converts text into numerical vectors using a local embedding model.
using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    new OllamaApiClient(new Uri("http://localhost:11434/"), "all-minilm");

#region Single Embedding

// var embedding = await embeddingGenerator.GenerateVectorAsync("Hello, world!");
// Console.WriteLine($"Embedding dimensions: {embedding.Length}");
// Console.WriteLine(string.Join(", ", embedding.ToArray().Take(10)));

#endregion

#region Cosine Similarity

// All vectors must come from the same embedding model for comparison.
// var catVector = await embeddingGenerator.GenerateVectorAsync("cat");
// var dogVector = await embeddingGenerator.GenerateVectorAsync("dog");
// var kittenVector = await embeddingGenerator.GenerateVectorAsync("kitten");

// Console.WriteLine($"Embedding dimensions: {catVector.Length}");
// Console.WriteLine($"cat-dog similarity: {TensorPrimitives.CosineSimilarity(catVector.Span, dogVector.Span):F2}");
// Console.WriteLine($"cat-kitten similarity: {TensorPrimitives.CosineSimilarity(catVector.Span, kittenVector.Span):F2}");
// Console.WriteLine($"dog-kitten similarity: {TensorPrimitives.CosineSimilarity(dogVector.Span, kittenVector.Span):F2}");

#endregion

#region Semantic Search

string[] documents =
[
    "Reset your password to regain access to your account.",
    "Track your order using the shipment tracking number.",
    "Update your payment method in the billing settings.",
    "Enable dark mode from the appearance settings.",
    "Contact support to report a damaged delivery."
];

var query = "How can I recover my password?";
var queryVector = await embeddingGenerator.GenerateVectorAsync(query);
var results = new List<(string Text, float Score)>();

foreach (var document in documents)
{
    // Use the same model for the query and every document.
    var documentVector = await embeddingGenerator.GenerateVectorAsync(document);
    var score = TensorPrimitives.CosineSimilarity(queryVector.Span, documentVector.Span);
    results.Add((document, score));
}

Console.WriteLine($"Query: {query}");
Console.WriteLine("Results ranked by cosine similarity (not percentages):");
foreach (var result in results.OrderByDescending(result => result.Score))
{
    Console.WriteLine($"{result.Score:F2} | {result.Text}");
}

#endregion
