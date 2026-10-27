using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var endpoint = configuration["AI:Endpoint"];
var chatModel = configuration["AI:ChatModel"];

if (string.IsNullOrWhiteSpace(endpoint))
    throw new InvalidOperationException("Missing required setting: AI:Endpoint");

if (string.IsNullOrWhiteSpace(chatModel))
    throw new InvalidOperationException("Missing required setting: AI:ChatModel");

Console.WriteLine($"Endpoint: {endpoint}");
using var httpClient = new HttpClient();
using var modelsJson = JsonDocument.Parse(
    await httpClient.GetStringAsync(new Uri(new Uri(endpoint), "/api/tags")));
var models = modelsJson.RootElement.GetProperty("models")
    .EnumerateArray()
    .Select(model => model.GetProperty("name").GetString()!)
    .ToArray();

if (models.Length == 0)
{
    Console.WriteLine("No models are installed in Ollama.");
    return;
}

Console.WriteLine("Available Ollama models:");
for (var i = 0; i < models.Length; i++)
    Console.WriteLine($"{i + 1}. {models[i]}");

Console.Write("Choose a model number: ");
if (!int.TryParse(Console.ReadLine(), out var selection)
    || selection < 1 || selection > models.Length)
{
    Console.WriteLine("Invalid model number.");
    return;
}

chatModel = models[selection - 1];
Console.WriteLine($"ChatModel: {chatModel}");

using var detailsResponse = await httpClient.PostAsJsonAsync(
    new Uri(new Uri(endpoint), "/api/show"), new { model = chatModel });
detailsResponse.EnsureSuccessStatusCode();
using var details = JsonDocument.Parse(await detailsResponse.Content.ReadAsStringAsync());
if (!details.RootElement.TryGetProperty("capabilities", out var capabilities)
    || !capabilities.EnumerateArray().Any(value => value.GetString() == "tools"))
{
    Console.WriteLine("This model does not report tool support. Choose a tool-capable model such as llama3.2.");
    return;
}

IChatClient baseClient = new OllamaApiClient(new Uri(endpoint), chatModel);

using IChatClient chatClient = new ChatClientBuilder(baseClient)
    .UseFunctionInvocation()
    .Build();

var weatherFunction = AIFunctionFactory.Create(
    ([Description("City and country, for example Riyadh, Saudi Arabia.")] string location,
     [Description("Temperature unit: use exactly Celsius or Fahrenheit. Default to Celsius.")] string unit) =>
    {
        Console.WriteLine($"[Tool invoked] get_current_weather(location={location}, unit={unit})");
        if (!string.Equals(unit, "Celsius", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(unit, "Fahrenheit", StringComparison.OrdinalIgnoreCase))
        {
            return "Invalid unit. No weather result was generated. Use Celsius or Fahrenheit.";
        }

        var temperature = Random.Shared.Next(5, 20);
        if (string.Equals(unit, "Fahrenheit", StringComparison.OrdinalIgnoreCase))
            temperature = (int)Math.Round(temperature * 9.0 / 5 + 32);
        var conditions = Random.Shared.Next(0, 2) == 0 ? "sunny" : "rainy";
        return $"The weather in {location} is {temperature} degrees {unit} and {conditions}.";
    },
    name: "get_current_weather",
    description: "Get the current weather in a given location");

var chatOptions = new ChatOptions
{
    Tools = [weatherFunction]
};

List<ChatMessage> chatHistory =
[
    new(ChatRole.System, """
        You are a helpful outdoor activity assistant. Use the weather tool when asked
        about current weather. Pass unit exactly as "Celsius" unless the user requests "Fahrenheit". Never pass "object".
        Weather tool results are simulated for this demo; clearly mention this.
        Only use weather values returned by the tool. If the tool reports an error, explain it without inventing weather. Keep your final answer brief.
        """),
    new(ChatRole.User, """
        I live in Riyadh and I'm looking for an outdoor activity.
        What's the current weather like?
        """)
];

Console.WriteLine($"User: {chatHistory[1].Text}");
var response = await chatClient.GetResponseAsync(chatHistory, chatOptions);
Console.WriteLine($"Assistant: {response.Text}");





