using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var endpoint = configuration["AI:Endpoint"];
var apiKey = configuration["AI:ApiKey"];
var chatModel = configuration["AI:ChatModel"];

if (string.IsNullOrWhiteSpace(endpoint))
    throw new InvalidOperationException("Missing required setting: AI:Endpoint");

if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException("Missing required setting: AI:ApiKey");

if (string.IsNullOrWhiteSpace(chatModel))
    throw new InvalidOperationException("Missing required setting: AI:ChatModel");

Console.WriteLine($"Endpoint: {endpoint}");
using var httpClient = new HttpClient();
var modelsUri = new Uri(new Uri(endpoint), "/api/tags");
using var modelsJson = JsonDocument.Parse(await httpClient.GetStringAsync(modelsUri));
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
    Console.WriteLine("Invalid model number. Run the app again and choose a listed number.");
    return;
}

chatModel = models[selection - 1];
Console.WriteLine($"ChatModel: {chatModel}");

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

using IChatClient chatClient = openAIClient
    .GetChatClient(chatModel)
    .AsIChatClient();

Console.WriteLine("IChatClient created successfully.");


#region Basic Completion

// var prompt = "What is AI? Explain in maximum 20 words.";
// Console.WriteLine($"User: {prompt}");

// var response = await chatClient.GetResponseAsync(prompt);
// Console.WriteLine($"Model: {response.Text}");

// Console.WriteLine($"Input tokens: {response.Usage?.InputTokenCount?.ToString() ?? "Not available from this provider/client."}");
// Console.WriteLine($"Output tokens: {response.Usage?.OutputTokenCount?.ToString() ?? "Not available from this provider/client."}");
// Console.WriteLine($"Total tokens: {response.Usage?.TotalTokenCount?.ToString() ?? "Not available from this provider/client."}");

#endregion

#region Streaming

// var prompt = "What is AI? Explain in maximum 20 words.";
// Console.WriteLine($"User: {prompt}");
// Console.Write("Model: ");

// await foreach (var update in chatClient.GetStreamingResponseAsync(prompt))
// {
//     Console.Write(update.Text);
// }

// Console.WriteLine();

#endregion

#region Classification

// var classificationPrompt = """
// Please classify the following sentences into categories:
// - 'complaint'
// - 'suggestion'
// - 'praise'
// - 'other'.

// 1) "I love the new layout!"
// 2) "You should add a night mode."
// 3) "When I try to log in, it keeps failing."
// 4) "This app is decent."
// """;

// Console.WriteLine($"User: {classificationPrompt}");
// var classificationResponse = await chatClient.GetResponseAsync(classificationPrompt);
// Console.WriteLine($"Model: {classificationResponse.Text}");

#endregion

#region Summarization

//var summaryPrompt = """
//Summarize the following blog in 1 concise sentences:

//"Microservices architecture is increasingly popular for building complex applications, but it comes with additional overhead. It's crucial to ensure each service is as small and focused as possible, and that the team invests in robust CI/CD pipelines to manage deployments and updates. Proper monitoring is also essential to maintain reliability as the system grows."
//""";

//Console.WriteLine($"user >>> {summaryPrompt}");

//ChatResponse summaryResponse = await client.GetResponseAsync(summaryPrompt);

//Console.WriteLine($"assistant >>> \n{summaryResponse}");

#endregion

#region Sentiment Analysis

//var analysisPrompt = """
//        You will analyze the sentiment of the following product reviews. 
//        Each line is its own review. Output the sentiment of each review in a bulleted list and then provide a generate sentiment of all reviews.

//        I bought this product and it's amazing. I love it!
//        This product is terrible. I hate it.
//        I'm not sure about this product. It's okay.
//        I found this product based on the other reviews. It worked for a bit, and then it didn't.
//        """;

//Console.WriteLine($"user >>> {analysisPrompt}");

//ChatResponse responseAnalysis = await client.GetResponseAsync(analysisPrompt);

//Console.WriteLine($"assistant >>> \n{responseAnalysis}");

#endregion

#region Structured Output

// var carListings = new[]
// {
//     "Check out this stylish 2019 Toyota Camry. It has a clean title, only 40,000 miles on the odometer, and a well-maintained interior. The car offers great fuel efficiency, a spacious trunk, and modern safety features like lane departure alert. Minimum offer price: $18,000. Contact Metro Auto at (555) 111-2222 to schedule a test drive.",
//     "Lease this sporty 2021 Honda Civic! With only 10,000 miles, it includes a sunroof, premium sound system, and backup camera. Perfect for city driving with its compact size and great fuel mileage. Located in Uptown Motors, monthly lease starts at $250 (excl. taxes). Call (555) 333-4444 for more info.",
//     "A classic 1968 Ford Mustang, perfect for enthusiasts. The vehicle needs some interior restoration, but the engine runs smoothly. V8 engine, manual transmission, around 80,000 miles. This vintage gem is priced at $25,000. Contact Retro Wheels at (555) 777-8888 if you’re interested.",
//     "Brand new 2023 Tesla Model 3 for lease. Zero miles, fully electric, autopilot capabilities, and a sleek design. Monthly lease starts at $450. Clean lines, minimalist interior, top-notch performance. For more details, call EVolution Cars at (555) 999-0000.",
//     "Selling a 2015 Subaru Outback in good condition. 60,000 miles on it, includes all-wheel drive, heated seats, and ample cargo space for family getaways. Minimum offer price: $14,000. Contact Forrest Autos at (555) 222-1212 if you want a reliable adventure companion.",
// };

// foreach (var listingText in carListings)
// {
//     var response = await chatClient.GetResponseAsync<CarDetails>(
//         $"""
//         Convert the following car listing into a JSON object matching this C# schema:
//         Condition: "New" or "Used"
//         Make: (car manufacturer)
//         Model: (car model)
//         Year: (four-digit year)
//         ListingType: "Sale" or "Lease"
//         Price: integer only
//         Features: array of short strings
//         TenWordSummary: exactly ten words to summarize this listing

//         Here is the listing:
//         {listingText}
//         """);

//     if (response.TryGetResult(out var info))
//     {
//         // Convert the CarDetails object to JSON for display
//         Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
//             info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
//     }
//     else
//     {
//         Console.WriteLine("Response was not in the expected format.");
//     }
// }

// class CarDetails
// {
//     public required string Condition { get; set; }  // e.g. "New" or "Used"
//     public required string Make { get; set; }
//     public required string Model { get; set; }
//     public int Year { get; set; }
//     public CarListingType ListingType { get; set; }
//     public int Price { get; set; }
//     public required string[] Features { get; set; }
//     public required string TenWordSummary { get; set; }
// }

// [JsonConverter(typeof(JsonStringEnumConverter))]
// enum CarListingType { Sale, Lease }

#endregion

#region ChatApp

List<ChatMessage> chatHistory = new()
{
    new ChatMessage(ChatRole.System, """
        You are a friendly hiking enthusiast who helps people discover fun hikes in their area.
        You introduce yourself when first saying hello.
        When helping people out, you always ask them for this information
        to inform the hiking recommendation you provide:

        1. The location where they would like to hike
        2. What hiking intensity they are looking for

        You will then provide three suggestions for nearby hikes that vary in length
        after you get that information. You will also share an interesting fact about
        the local nature on the hikes when making a recommendation. At the end of your
        response, ask if there is anything else you can help with.
    """)
};

while (true)
{
    Console.Write("User: ");
    var userMessage = Console.ReadLine();
    if (userMessage is null)
        break;

    chatHistory.Add(new ChatMessage(ChatRole.User, userMessage));

    Console.Write("Assistant: ");
    var assistantResponse = "";
    await foreach (var item in chatClient.GetStreamingResponseAsync(chatHistory))
    {
        Console.Write(item.Text);
        assistantResponse += item.Text;
    }

    Console.WriteLine();
    chatHistory.Add(new ChatMessage(ChatRole.Assistant, assistantResponse));
}

#endregion
