using Microsoft.Extensions.AI;
using OllamaSharp;
using System.ComponentModel;

IChatClient baseClient = new OllamaApiClient(
    new Uri("http://localhost:11434/"), "llama3.2:latest");

using IChatClient chatClient = new ChatClientBuilder(baseClient)
    .UseFunctionInvocation()
    .Build();

var stockFunction = AIFunctionFactory.Create(
    ([Description("The exact product code, for example LAPTOP-001 or MOUSE-001.")] string productCode) =>
    {
        Console.WriteLine($"[Tool invoked] get_product_stock(productCode={productCode})");

        // Simulated inventory for this lesson; no database is queried.
        var stock = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["LAPTOP-001"] = 7,
            ["MOUSE-001"] = 0
        };

        if (stock.TryGetValue(productCode, out var quantity))
            return $"Simulated stock for product {productCode}: {quantity} units available.";

        return $"Product {productCode} was not found in the simulated inventory. Its stock is unknown.";
    },
    name: "get_product_stock",
    description: "Get the available stock quantity for a given product code");

var chatOptions = new ChatOptions
{
    Tools = [stockFunction]
};

List<ChatMessage> chatHistory =
[
    new(ChatRole.System, """
        You are a helpful product inventory assistant.
        Always use get_product_stock to answer questions about product availability.
        Pass the exact product code from the user's message.
        Never invent stock quantities. If the product is not found, say its stock is unknown.
        Clearly mention that the inventory data is simulated. Keep your answer brief.
        """),
    new(ChatRole.User, "Is product LAPTOP-001 available? How many units are in stock?")
];

Console.WriteLine($"User: {chatHistory[1].Text}");
var response = await chatClient.GetResponseAsync(chatHistory, chatOptions);
Console.WriteLine($"Assistant: {response.Text}");
