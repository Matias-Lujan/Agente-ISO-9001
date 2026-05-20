using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

var key = "x";
var baseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/";
var model = "gemini-2.5-flash";

var opts = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
var oai = new OpenAIClient(new ApiKeyCredential(key), opts);
var chat = oai.GetChatClient(model).AsIChatClient();

var resp = await chat.GetResponseAsync(
    new List<ChatMessage>
    {
        new(ChatRole.System, "You reply only with JSON."),
        new(ChatRole.User, "{\"ping\":1}")
    },
    new ChatOptions { Temperature = 0.1f },
    CancellationToken.None);

Console.WriteLine(resp.Messages.Count);
