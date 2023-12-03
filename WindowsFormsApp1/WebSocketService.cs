using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;

public class WebSocketService
{
    private ClientWebSocket webSocket;
    private const int ReconnectDelayMilliseconds = 5000;

    public event Action<string> MessageReceived;
    public event EventHandler<Exception> ConnectionErrorOccurred;
    public delegate void ConnectionErrorHandler(object sender, Exception ex);

    public WebSocketService()
    {
        webSocket = new ClientWebSocket();
    }

    private void RaiseConnectionError(Exception ex)
    {
        ConnectionErrorOccurred?.Invoke(this, ex);
    }

    public void StartConnection()
    {
        _ = ConnectWebSocket();
    }

    public async Task StopConnection()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
            webSocket.Dispose();
        }
    }

    private async Task ConnectWebSocket()
    {
        try
        {
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri("ws://shc-ac-backend.onrender.com"), CancellationToken.None);
            await StartListeningForMessages();
        }
        catch (Exception ex)
        {
            RaiseConnectionError(ex);
            await Task.Delay(ReconnectDelayMilliseconds); // Wait before attempting to reconnect
            _ = ConnectWebSocket(); // Attempt to reconnect
        }
    }

    public async Task StartListeningForMessages()
    {
        var buffer = new byte[1024];
        while (true)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(message);
                }
            }
            catch (WebSocketException)
            {
                await Task.Delay(ReconnectDelayMilliseconds);
                _ = ConnectWebSocket();
            }
        }
    }

    public async Task SendMessage(string payload)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(payload);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
