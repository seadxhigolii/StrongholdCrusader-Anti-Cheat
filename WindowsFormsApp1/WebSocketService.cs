using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp1.Models;

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

    public void StartConnection(FirstAuthentication authenticationFirstPayload)
    {
        _ = ConnectWebSocket(authenticationFirstPayload);
    }

    public async Task StopConnection()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
            webSocket.Dispose();
        }
    }

    private async Task ConnectWebSocket(FirstAuthentication authenticationFirstPayload)
    {
        try
        {
            webSocket = new ClientWebSocket();
            string stringKnownAddresses = String.Join(",", authenticationFirstPayload.KnownMacAddresses.Select(a => $"{a}"));
            //ws://shc-ac-backend.onrender.com
            //var uri = new Uri($"ws://shc-ac-backend.onrender.com?email={authenticationFirstPayload.Email}&gamerangerId={authenticationFirstPayload.GameRangerId}&token={authenticationFirstPayload.Token}&knownMacAddresses={stringKnownAddresses}");
            //https://ad1d-2a02-1810-3e2e-8900-cd9d-481e-95f5-70e9.ngrok-free.app
            await webSocket.ConnectAsync(new Uri($"ws://ad1d-2a02-1810-3e2e-8900-cd9d-481e-95f5-70e9.ngrok-free.app?email={authenticationFirstPayload.Email}&gamerangerId={authenticationFirstPayload.GameRangerId}&token={authenticationFirstPayload.Token}&knownMacAddresses={stringKnownAddresses}"), CancellationToken.None);
            await StartListeningForMessages(authenticationFirstPayload);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            RaiseConnectionError(ex);
            await Task.Delay(ReconnectDelayMilliseconds);
            _ = ConnectWebSocket(authenticationFirstPayload);
        }
    }

    public async Task StartListeningForMessages(FirstAuthentication authenticationFirstPayload)
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
            catch (WebSocketException ex)
            {
                MessageBox.Show(ex.Message);
                await Task.Delay(ReconnectDelayMilliseconds);
                _ = ConnectWebSocket(authenticationFirstPayload);
            }
        }
    }

    public async Task SendMessage(string payload)
    {
        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(payload);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("SHC AC failed to send the message: " + e.Message);
            throw e;
        }
       
    }
}
