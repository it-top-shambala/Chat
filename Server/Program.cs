using System.Collections.Concurrent;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());




app.MapGet("/connect", (string user = "anonymous") =>
{
    MessageStore.ConnectedUsers[user] = DateTime.Now;
    return Results.Json(new { status = "connected", user });
});


app.MapPost("/send", async Task<IResult> (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string body = await reader.ReadToEndAsync();

    Message? message = null;
    try
    {
        message = JsonSerializer.Deserialize<Message>(body);
    }
    catch { }

    if (message?.Text != null)
    {
        MessageStore.Messages.Add(message with { Timestamp = DateTime.Now });
        return Results.Json(new { status = "sent", message = message with { Timestamp = DateTime.Now } });
    }

    return Results.Json(new { error = "Invalid message" }, statusCode: 400);
});


app.MapGet("/receive", (string since = "") =>
{
    var lastTime = DateTime.TryParse(since, out var parsed) ? parsed : DateTime.MinValue;
    var newMessages = MessageStore.Messages
        .Where(m => m.Timestamp > lastTime)
        .OrderBy(m => m.Timestamp)
        .Take(100)
        .ToArray();
    return Results.Json(new { messages = newMessages });
});


app.MapGet("/users", () =>
{
    var activeUsers = MessageStore.ConnectedUsers
        .Where(kvp => kvp.Value > DateTime.Now.AddMinutes(-5))
        .Select(kvp => kvp.Key)
        .ToArray();
    return Results.Json(new { users = activeUsers });
});

app.Run("http://localhost:5000");

Console.WriteLine("🚀 Сервер запущен на http://localhost:5000");



record Message(string User, string Text, DateTime Timestamp);

static class MessageStore
{
    public static ConcurrentBag<Message> Messages = new();
    public static ConcurrentDictionary<string, DateTime> ConnectedUsers = new();
}
