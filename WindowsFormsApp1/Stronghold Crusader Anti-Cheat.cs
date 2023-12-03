using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.Models;
using WindowsFormsApp1.Models.Client;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private MouseMonitor mouseMonitor;
        private WebSocketService webSocketService;
        private User user;
        private System.Windows.Forms.Timer timer10s = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer timer60s = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer timer1s = new System.Windows.Forms.Timer();
        public string type = "ongoing_game_update";
        public Granary granary;
        public int fearFactor;
        public int activeTaxes;
        public CurrentDate currentDate;
        public Population population;
        public Leaderboard leaderboard;
        private System.Timers.Timer checkGameRangerTimer;
        private int statusChangeCounter = 0;
        private int openStatusChangeCounter = 0;
        private int closeStatusChangeCounter = 0;
        private bool? lastStatus = null;
        private int userId = 0;
        public System.Timers.Timer timeperActionTimer;

        public Form1()
        {
            InitializeComponent();

            user = new User();
            user.UserIdSet += InitializeAfterUserIdSet;

            mouseMonitor = new MouseMonitor();
            mouseMonitor.HighClickRateDetected += HandleHighClickRate;
            this.FormClosing += Form1_FormClosing;
        }

        private void InitializeAfterUserIdSet()
        {
            webSocketService = new WebSocketService();
            webSocketService.ConnectionErrorOccurred += WebSocketService_ConnectionErrorOccurred;
            webSocketService.MessageReceived += WebSocketService_MessageReceived;
            webSocketService.StartConnection();

            timer1s.Interval = 1000;
            timer1s.Tick += Timer1s_Tick;
            timer1s.Start();


            timer10s.Interval = 10000;
            timer10s.Tick += Timer10s_Tick;
            timer10s.Start();
            
            timer60s.Interval = 60000; 
            timer60s.Tick += Timer60s_Tick;
            timer60s.Start();
        }

        private void WebSocketService_ConnectionErrorOccurred(object sender, Exception e)
        {
            //MessageBox.Show($"Error connecting: {e.Message}");
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

        private void WebSocketService_MessageReceived(string message)
        {
            try
            {
                var payload = DeserializePayload(message);
                ProcessPayload(payload);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing message: {ex.Message}");
            }
            MessageBox.Show($"Echoed: {message}");
        }

        private void ProcessPayload(BaseClientPayload payload)
        {
            switch (payload)
            {
                case ClientAuthPayload authPayload:
                    HandleClientAuthStatus(authPayload);
                    break;
                case ClientGamerangerStatusPayload gamerangerStatusPayload:
                    HandleClientGameRangerStatus(gamerangerStatusPayload);
                    break;
                default:
                    MessageBox.Show("Unknown payload type.");
                    break;
            }
        }

        

        private void MysticalTest()
        {

        }

        private async void HandleClientGameRangerStatus(ClientGamerangerStatusPayload gamerangerStatusPayload)
        {
            this.Invoke(new Action(() =>
            {
                if (gamerangerStatusPayload.IsFactual)
                {
                    panel1.Visible = true;
                    panel2.Visible = false;
                    panel3.Visible = false;
                    timeperActionTimer.Start();
                }
                else
                {
                    panel1.Visible = false;
                    panel2.Visible = true;
                    panel3.Visible = false;
                }
            }));
        }


        private void HandleClientAuthStatus(ClientAuthPayload authPayload)
        {
            this.timeperActionTimer = new System.Timers.Timer(authPayload.TimeLimitPerAttempt * 1000);
            this.timeperActionTimer.Elapsed += (sender, e) => CheckIfAuthFailed();
            this.timeperActionTimer.Start();
            this.checkGameRangerTimer = new System.Timers.Timer(500);
            this.checkGameRangerTimer.Elapsed += (sender, e) => CheckGameRangerStatus(authPayload.RequiredAttempts);
            this.checkGameRangerTimer.Start();
        }

        private async void CheckGameRangerStatus(int requiredAttempts)
        {
            var gameRangerProcesses = Process.GetProcessesByName("GameRanger");
            bool currentStatus = gameRangerProcesses.Length > 0;

            if (lastStatus == null || currentStatus != lastStatus)
            {
                timeperActionTimer.Stop();
                lastStatus = currentStatus;
                statusChangeCounter++;
                if (currentStatus) openStatusChangeCounter++; else closeStatusChangeCounter++;

                var gamerangerStatusCheckPayload = new
                {
                    type = "gameranger_status_check",
                    gamerangerId = this.userId,
                    gamerangerStatus = currentStatus ? "open" : "closed"
                };
                await Thread.Sleep(500);
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(gamerangerStatusCheckPayload);
                await webSocketService.SendMessage(jsonPayload);

                // Show "Please Wait..." panel for 3 seconds
                this.Invoke(new Action(() => { panel2.Visible = true; panel3.Visible = false; }));
                await Task.Delay(3000);

                // Show "Please Open/Close Gameranger" panel
                this.Invoke(new Action(() => { panel2.Visible = false; panel3.Visible = true; }));

                if (openStatusChangeCounter >= requiredAttempts && closeStatusChangeCounter >= requiredAttempts)
                {
                    this.checkGameRangerTimer.Stop();
                    MessageBox.Show("AUTH DONE");
                }
            }
        }


        private async void CheckIfAuthFailed()
        {
            Action updateUI = () =>
            {
                this.panel1.Visible = false;
                this.textBox1.Text = "";
                button2.Text = "VALIDATE";
                button2.BackColor = System.Drawing.ColorTranslator.FromHtml("#ff8000");
            };

            if (this.InvokeRequired)
            {
                this.Invoke(updateUI);
            }
            else
            {
                updateUI();
            }

            await this.webSocketService.StopConnection();
        }


        private BaseClientPayload DeserializePayload(string message)
        {
            var jObject = JObject.Parse(message);
            var type = jObject.Value<string>("type");

            switch (type)
            {
                case "client_auth_status":
                    return jObject.ToObject<ClientAuthPayload>();
                case "gameranger_status_update":
                    return jObject.ToObject<ClientGamerangerStatusPayload>();
                default:
                    throw new InvalidOperationException("Unknown payload type.");
            }
        }

        private void Timer1s_Tick(object sender, EventArgs e)
        {
            //SendAuthPayload();
        }        

        private void Timer10s_Tick(object sender, EventArgs e)
        {
            var highestCPSInLast10Seconds = mouseMonitor.HighestCPSLast10Seconds;
            //SendCpsUpdate(highestCPSInLast10Seconds);
            //SendGameUpdateStatus();
            //SendGameSettingsStatus();
            //mouseMonitor.Reset10SecondCPSData();
        }

        private void Timer60s_Tick(object sender, EventArgs e)
        {
            SendGamerangerUpdate();
        }

        private async void SendCpsUpdate(double highestCps)
        {
            var cpsUpdatePayload = new
            {
                type = "cps_update",
                userId = user.GetUserId(),  
                maxCps = highestCps,
                averageCps = mouseMonitor.AverageCPSLast10Seconds,
                interval = 10,  
                timestamp = ToUnixTimestamp(DateTime.Now)
            };

            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(cpsUpdatePayload);

            //MessageBox.Show(jsonPayload);
            await webSocketService.SendMessage(jsonPayload);
        }

        public static long ToUnixTimestamp(DateTime date)
        {
            return (long)(date - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private async void SendAuthPayload(int userId)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsPath = Path.Combine(appDataPath, "GameRanger", "GameRanger Prefs", "Settings");

            string email = await GetMostFrequentEmail(settingsPath);
            var authPayload = new
            {
                type = "auth_payload_type",
                email = email,
                gamerangerId = userId,
                token = textBox1.Text
            };
            this.userId = userId;
            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(authPayload);
            MessageBox.Show(jsonPayload);
            await webSocketService.SendMessage(jsonPayload);
        }

        private async Task<string> GetMostFrequentEmail(string filePath)
        {
            string tempFilePath = Path.GetTempFileName();

            try
            {
                string fileContent;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    fileContent = await reader.ReadToEndAsync();
                }

                var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
                var matches = Regex.Matches(fileContent, emailPattern);

                var emailCounts = new Dictionary<string, int>();

                foreach (Match match in matches)
                {
                    if (emailCounts.ContainsKey(match.Value))
                    {
                        emailCounts[match.Value]++;
                    }
                    else
                    {
                        emailCounts[match.Value] = 1;
                    }
                }

                if (emailCounts.Count == 0)
                {
                    return null;
                }

                return emailCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file: " + ex.Message);
                return null;
            }
        }
        private async void SendGamerangerUpdate()
        {
            bool isGameRangerRunning = IsApplicationRunning("GameRanger");

            var gamerangerUpdatePayload = new
            {
                type = "gameranger_status_check",
                gamerangerId = user.GetUserId(),
                gamerangerStatus = isGameRangerRunning ? "open" : "closed"
            };

            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(gamerangerUpdatePayload);

            //MessageBox.Show(jsonPayload);
            await webSocketService.SendMessage(jsonPayload);
        }

        private async void SendGameUpdateStatus()
        {
            //string macAddresses = string.Empty;

            //foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            //{
            //    if (nic.OperationalStatus == OperationalStatus.Up)
            //    {
            //        macAddresses += nic.GetPhysicalAddress().ToString() + "\n" ;
            //    }
            //}

            //MessageBox.Show(macAddresses);
            Process gameProcess = MemoryReader.GetProcessByName("Stronghold Crusader");

            var payload = new
            {
                granary = new Granary
                {
                    inventory = new Granary.Inventory
                    {
                        apples = MemoryReader.ReadMemoryInt(gameProcess, 0x01A4A25C),
                        meat = MemoryReader.ReadMemoryInt(gameProcess, 0x0164A258),
                        cheese = MemoryReader.ReadMemoryInt(gameProcess, 0x0164A254),
                        bread = MemoryReader.ReadMemoryInt(gameProcess, 0x01A4A250)
                    },
                    currentRations = MemoryReader.ReadMemoryInt(gameProcess, 0x962DAC)
                },
                fearFactor = MemoryReader.ReadMemoryInt(gameProcess, 0x962E30),
                activeTaxes = MemoryReader.ReadMemoryInt(gameProcess, 0x962E84),
                currentDate = new CurrentDate
                {
                    month = MemoryReader.ReadMemoryInt(gameProcess, 0x97DFC4),
                    year = MemoryReader.ReadMemoryInt(gameProcess, 0x97DFC8)
                },
                population = new Population
                {
                    count = MemoryReader.ReadMemoryInt(gameProcess, 0x962E7C),
                    hovelsCount = MemoryReader.ReadMemoryInt(gameProcess, 0x1365898),
                    popularity = MemoryReader.ReadMemoryInt(gameProcess, 0x960D5C)
                },
                leaderboard = new Leaderboard
                {
                    red = new Leaderboard.Player
                    {
                        nickname = MemoryReader.ReadMemoryString(gameProcess, 0x1364FD6),
                        gold = MemoryReader.ReadMemoryInt(gameProcess, 0x961208),
                        troopsCount = MemoryReader.ReadMemoryInt(gameProcess, 0x961238)
                    },
                    orange = new Leaderboard.Player
                    {
                        nickname = MemoryReader.ReadMemoryString(gameProcess, 0x1365030),
                        gold = MemoryReader.ReadMemoryInt(gameProcess, 0x964BFC),
                        troopsCount = MemoryReader.ReadMemoryInt(gameProcess, 0x964C2C)
                    }
                }
            };
            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            MessageBox.Show(jsonPayload);
            await webSocketService.SendMessage(jsonPayload);
        }

        private async void SendGameSettingsStatus()
        {
            Process gameProcess = MemoryReader.GetProcessByName("Stronghold Crusader");

            var payload = new
            {
                gold = MemoryReader.ReadMemoryInt(gameProcess, 0x1362CA4),
                pt = MemoryReader.ReadMemoryInt(gameProcess, 0x1364C84),
                gameSpeed = MemoryReader.ReadMemoryInt(gameProcess, 0x1362B90),
                gameType = MemoryReader.ReadMemoryInt(gameProcess, 0x1362B94)
            };
            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            MessageBox.Show(jsonPayload);
            await webSocketService.SendMessage(jsonPayload);
        }

        private bool IsApplicationRunning(string processName)
        {
            foreach (var process in System.Diagnostics.Process.GetProcesses())
            {
                if (process.ProcessName.Contains(processName))
                {
                    return true;
                }
            }
            return false;
        }

        private void button1_Click(object sender, EventArgs e)
        { 
            this.panel1.Visible = true;
            this.panel2.Visible = false;
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to close?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                if(webSocketService != null)
                {
                   await webSocketService.StopConnection();
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_MINIMIZE = 6;

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var userId = user.ReadGameRangerUserId(button2, textBox1);
            SendAuthPayload(userId);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}

