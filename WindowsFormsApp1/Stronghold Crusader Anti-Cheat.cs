using System;
using System.ComponentModel;
using System.Drawing;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private MouseMonitor mouseMonitor;
        private WebSocketService webSocketService;

        private System.Windows.Forms.Timer timer10s = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer timer60s = new System.Windows.Forms.Timer();

        private User user;

        public Form1()
        {
            InitializeComponent();

            // Initialize User
            user = new User();

            // Initialize and start MouseMonitor
            mouseMonitor = new MouseMonitor();
            mouseMonitor.HighClickRateDetected += HandleHighClickRate;

            // Initialize WebSocketService
            webSocketService = new WebSocketService();
            webSocketService.ConnectionErrorOccurred += WebSocketService_ConnectionErrorOccurred;
            webSocketService.MessageReceived += WebSocketService_MessageReceived;
            webSocketService.StartConnection();

            // 10-second timer setup
            timer10s.Interval = 10000; // 10 seconds
            timer10s.Tick += Timer10s_Tick;
            timer10s.Start();

            // 60-second timer setup
            timer60s.Interval = 60000; // 60 seconds
            timer60s.Tick += Timer60s_Tick;
            timer60s.Start();
        }

        private void WebSocketService_ConnectionErrorOccurred(object sender, Exception e)
        {
            MessageBox.Show($"Error connecting: {e.Message}");
        }

        private void HandleHighClickRate()
        {
            IntPtr activeWindowHandle = GetForegroundWindow();
            if (activeWindowHandle != IntPtr.Zero)
            {
                ShowWindow(activeWindowHandle, SW_MINIMIZE);
            }
            MessageBox.Show("High click rate detected!");
        }

        private void WebSocketService_ConnectionErrorOccurred(Exception ex)
        {
            MessageBox.Show($"Error connecting: {ex.Message}");
        }

        private void WebSocketService_MessageReceived(string message)
        {
            MessageBox.Show($"Echoed: {message}");
        }

        private void Timer10s_Tick(object sender, EventArgs e)
        {
            SendCpsUpdate();
        }

        private void Timer60s_Tick(object sender, EventArgs e)
        {
            SendGamerangerUpdate();
        }

        private async void SendCpsUpdate()
        {
            string cpsUpdatePayload = "{ \"type\": \"cps_update\", \"data\": {\"exampleKey\": \"exampleValue\"} }";
            await webSocketService.SendMessage(cpsUpdatePayload);
        }

        private async void SendGamerangerUpdate()
        {
            string gamerangerUpdatePayload = "{ \"type\": \"gameranger_update\", \"data\": {\"exampleKey\": \"exampleValue\"} }";
            await webSocketService.SendMessage(gamerangerUpdatePayload);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            user.ReadGameRangerUserId(button1, label3);
        }

        // ... (Other code in your Form1 class)

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_MINIMIZE = 6;

    }
}

