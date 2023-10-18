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

    // Define an event for when a message is received, allowing other classes to subscribe
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

    private async Task ConnectWebSocket()
    {
        try
        {
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri("ws://echo.websocket.org"), CancellationToken.None);
        }
        catch (Exception ex)
        {
            RaiseConnectionError(ex);
        }
    }

    public async Task StartListeningForMessages()
    {
        var buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                MessageReceived?.Invoke(message); // Notify any subscribers about the received message
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
