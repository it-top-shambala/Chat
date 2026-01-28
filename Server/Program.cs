using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/send", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string body = await reader.ReadToEndAsync();

    Message? incoming;
    try
    {
        incoming = JsonSerializer.Deserialize<Message>(body);
    }
    catch
    {
        return Results.BadRequest();
    }

    if (incoming == null) return Results.BadRequest();
    if (string.IsNullOrWhiteSpace(incoming.Username)) return Results.BadRequest();
    if (string.IsNullOrWhiteSpace(incoming.Text)) return Results.BadRequest();

    var stored = new Message
    {
        Id = MessageStore.NextId++,
        Username = incoming.Username,
        Text = incoming.Text,
        Timestamp = DateTime.UtcNow
    };

    MessageStore.Enqueue(stored);

    return Results.Json(new[] { stored });
});

app.MapGet("/receive", (HttpContext context) =>
{
    var clientKey = GetClientKey(context);

    var last = MessageStore.LastDeliveredUtc.GetOrAdd(clientKey, DateTime.MinValue);

    var all = MessageStore.Snapshot();

    var fresh = all
        .Where(m => m.Timestamp > last)
        .OrderBy(m => m.Timestamp)
        .Take(200)
        .ToArray();

    if (fresh.Length > 0)
        MessageStore.LastDeliveredUtc[clientKey] = fresh[^1].Timestamp;

    return Results.Json(fresh);
});

app.MapGet("/connect", (string user = "anonymous") =>
{
    MessageStore.ConnectedUsers[user] = DateTime.UtcNow;
    return Results.Json(new[] { user }); // массив
});

app.MapGet("/users", () =>
{
    var users = MessageStore.ConnectedUsers
        .Where(x => x.Value > DateTime.UtcNow.AddMinutes(-5))
        .Select(x => x.Key)
        .Distinct()
        .OrderBy(x => x)
        .ToArray();

    return Results.Json(users); 
});

app.Run("http://localhost:5214");

static string GetClientKey(HttpContext ctx)
{
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var ua = ctx.Request.Headers.UserAgent.ToString();
    return ip + "|" + ua;
}
public class Message
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

static class MessageStore
{
    public static int NextId = 1;

    private static readonly ConcurrentQueue<Message> _messages = new();

    public static ConcurrentDictionary<string, DateTime> LastDeliveredUtc = new();

    public static ConcurrentDictionary<string, DateTime> ConnectedUsers = new();

    public static void Enqueue(Message msg) => _messages.Enqueue(msg);

    public static Message[] Snapshot() => _messages.ToArray();
}