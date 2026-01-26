using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Client.Models;

namespace Client.Services;

public class SendAndRecieveService
{
    private static HttpClient _httpClient = new HttpClient();

    public static async void SendMessageAsync(string serverUrl, Message message)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        await _httpClient.PostAsync(serverUrl, content);
    }
    public static async Task<List<Message>> RecieveMessageAsync(string serverUrl)
    {
        using var result = await _httpClient.GetAsync(serverUrl);
        return await result.Content.ReadFromJsonAsync<List<Message>>()??[];
    }
}
