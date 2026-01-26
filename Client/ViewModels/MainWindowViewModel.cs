using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using Client.Models;
using Client.Services;

namespace Client.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Message> Messages { get; set; } = [];
    private string _serverUrl;

    private string? _username;
    public string? Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    private string? _sendText;
    public string? SendText 
    {
        get => _sendText;
        set => SetField(ref _sendText, value);
    }

    public LambdaCommand SendButton {  get; }
    public MainWindowViewModel()
    {
        GetServerUrl();
        Task.Run(StartRecieve);
        SendButton = new LambdaCommand(
            async (_) => SendMessageAsync(_sendText),
            _ => !string.IsNullOrEmpty(_sendText) 
            && !string.IsNullOrEmpty(_username)
            );
    }
    private async Task StartRecieve()
    {
        while (true)
        {
            await Task.Delay(1000);

            var recieve = await SendAndRecieveService.RecieveMessageAsync(_serverUrl + "/recieve");
            if (recieve == null) continue;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in recieve)
                {
                    Messages.Add(item);
                }
            });
        }
    }
    private async void SendMessageAsync(string sendText)
    {
        var sendMessage = new Message()
        {
            Text = sendText,
            Username = _username,
        };
        SendAndRecieveService.SendMessageAsync(_serverUrl + "/send", sendMessage);
    }
    private void GetServerUrl()
    {
        _serverUrl = "http://localhost:5000";
        var configPath = "ServerUrl.json";
        if (System.IO.File.Exists(configPath))
        {
            try
            {
                var json = System.IO.File.ReadAllText(configPath);
                var tempConfig = System.Text.Json.JsonSerializer.Deserialize<string>(json);
                if (tempConfig != null)
                {
                    _serverUrl = tempConfig;
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Ошибка чтения конфига: {ex.Message}");
            }
        }
        else
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_serverUrl);
            System.IO.File.WriteAllText(configPath, json);
        }
    }


    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    #endregion
}
