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
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
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
        private System.Timers.Timer checkGameRangerTimer2;
        private int statusChangeCounter = 0;
        private int openStatusChangeCounter = 0;
        private int closeStatusChangeCounter = 0;
        private bool? lastStatus = null;
        private int userId = 0;
        public System.Timers.Timer timeperActionTimer;
        public int isFactualCounter = 0;
        public bool authenticationFinished = false;
        int timePerAction = 0;
        private string[] motivationalSentenceList = {
            "You're All Set for Fair Play!",
            "Play Fair, Play Hard!",
            "Ready for Cheat-Free Gaming!",
            "Enjoy Your Fair Gaming Journey!",
            "You're in the Arena of Integrity!",
            "Ready for Action? Fair Challenges Await!",
            "Join the League of Honest Heroes!",
            "Your Fair Play Adventure Starts Here!",
            "Step into the World of Fair Gaming!",
            "Honor in Play, Pride in Performance!",
            "Experience the Thrill of Gaming!",
            "Fair Play, Fair Victory!",
            "Let's Make History!",
            "Your Path to the Respect!",
            "To the Crusade for Integrity in Gaming",
            "Stand Tall Amongst Gamers Who Value Honor!",
            "Dive into the Top Gaming Experience",
            "Where Skill Reigns Supreme",
            "Elevate Your Game with Fair Play!",
            "In the Arena of Fairness!",
            "Celebrate the Joy of Gaming with Honor!",
        };
        private string[] welcomeSentenceList = {
            "Welcome Challenger,",
            "Greetings Gamer,",
            "Hello Champ,",
            "Salutations Warrior,",
            "Welcome to the Battle,",
            "Hi Competitor,",
            "Game On,",
            "Hello Adventurer,",
        };

        int welcomeIndex = 0;
        int motivationalIndex = 0;
        string welcomeSentence = "";
        string motivationalSentence = "";
        private Random random = new Random();

        public Form1()
        {
            InitializeComponent();

            user = new User();
            user.UserIdSet += InitializeAfterUserIdSet;


            //var procList = Process.GetProcesses().Where(process => process.ProcessName.Contains("Crusader"));
            //var path = "";
            //foreach (var process in procList)
            //{
            //    path =Path.GetDirectoryName(process.MainModule.FileName);
            //    MessageBox.Show(path);
            //}

            //using (var md5 = MD5.Create())
            //{
            //    using (var stream = File.OpenRead(path + "\\Stronghold Crusader.exe"))
            //    {
            //        MessageBox.Show(BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower());
            //    }
            //}





            welcomeIndex = random.Next(welcomeSentenceList.Length);
            motivationalIndex = random.Next(motivationalSentenceList.Length);

            // Select the sentences
            welcomeSentence = welcomeSentenceList[welcomeIndex];
            motivationalSentence = motivationalSentenceList[motivationalIndex];

            mouseMonitor = new MouseMonitor();

           

            mouseMonitor.HighClickRateDetected += HandleHighClickRate;
            this.FormClosing += Form1_FormClosing;
        }

        private async void InitializeAfterUserIdSet()
        {
            webSocketService = new WebSocketService();
            var userId = this.user.GetUserId();
            FirstAuthentication authenticationFirstPayload = await SendAuthPayload(userId);
            webSocketService.StartConnection(authenticationFirstPayload);
            webSocketService.ConnectionErrorOccurred += WebSocketService_ConnectionErrorOccurred;
            webSocketService.MessageReceived += WebSocketService_MessageReceived;
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
                MessageBox.Show($"MessageReceived: {message}");
                var payload = DeserializePayload(message);
                ProcessPayload(payload);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing message: {ex.Message}");
            }
        }

        private void ProcessPayload(BaseClientPayload payload)
        {
            switch (payload)
            {
                case ClientAuthPayload authPayload:
                    HandleClientAuthRequiredStatus(authPayload);
                    break;
                case ClientGamerangerStatusPayload gamerangerStatusPayload:
                    HandleClientGameRangerStatus(gamerangerStatusPayload);
                    break;
                case ClientAuthCompletePayload clientAuthCompletePayload:
                    HandleClientAuthComplete(clientAuthCompletePayload);
                    break;
                case ClientAuthUpdatePayload clientAuthUpdatePayload:
                    HandleClientAuthUpdate(clientAuthUpdatePayload);
                    break;
                default:
                    MessageBox.Show("Unknown payload type.");
                    break;
            }
        }

        private async void HandleClientGameRangerStatus(ClientGamerangerStatusPayload gamerangerStatusPayload)
        {
            this.Invoke(new Action(() =>
            {
                //MessageBox.Show("isFactual: " + gamerangerStatusPayload.IsFactual.ToString());
                if (gamerangerStatusPayload.IsFactual)
                {
                    timeperActionTimer.Start();
                }
            }));
        }

        private async void HandleClientAuthComplete(ClientAuthCompletePayload clientAuthCompletePayload)
        {
            Action updateUI = () =>
            {
                //MessageBox.Show("Authentication Successful!");
                this.tokenPanel.Visible = false;
                this.openClosePanel.Visible = false;
                this.waitPanel.Visible = false;
                this.textBox1.Text = "";
                this.label3.Visible = false;
                this.button1.Visible = false;
                this.label2.Visible = false;
                this.button2.Visible = false;
                this.label7.Text = welcomeSentence;
                this.label8.Text = motivationalSentence;
                this.label7.Visible = true;
                this.label8.Visible = true;
                this.pictureBox1.Visible = true;
                this.label9.Visible = true;
                this.label9.Text = "Conntected to ID: " + this.userId.ToString();
                this.button2.BackColor = System.Drawing.ColorTranslator.FromHtml("#ff8000");
            };
            if (this.InvokeRequired)
            {
                this.Invoke(updateUI);
            }
            else
            {
                updateUI();
            }
        }

        private async void HandleClientAuthUpdate(ClientAuthUpdatePayload clientAuthUpdatePayload)
        {
            if (authenticationFinished == false)
            {
                this.timeperActionTimer = new System.Timers.Timer(timePerAction * 1000);
                this.timeperActionTimer.Elapsed += (sender, e) => CheckIfAuthFailed();
                this.timeperActionTimer.Start();
                this.checkGameRangerTimer2 = new System.Timers.Timer(500);
                this.checkGameRangerTimer2.Elapsed += (sender, e) => CheckGameRangerStatus2(clientAuthUpdatePayload);
                this.checkGameRangerTimer2.Start();
            }
        }

        private void HandleClientAuthRequiredStatus(ClientAuthPayload authPayload)
        {
            if(authenticationFinished == false)
            {
                this.timePerAction = authPayload.TimeLimitPerAttempt;
                this.timeperActionTimer = new System.Timers.Timer(timePerAction * 1000);
                this.timeperActionTimer.Elapsed += (sender, e) => CheckIfAuthFailed();
                this.timeperActionTimer.Start();
                this.checkGameRangerTimer = new System.Timers.Timer(500);
                this.checkGameRangerTimer.Elapsed += (sender, e) => CheckGameRangerStatus();
                this.checkGameRangerTimer.Start();
            }
        
        }

        private async void CheckGameRangerStatus()
        {
            var gameRangerProcesses = Process.GetProcessesByName("GameRanger");
            bool currentStatus = gameRangerProcesses.Length > 0;

            if (lastStatus == null || currentStatus != lastStatus)
            {
                timeperActionTimer.Stop();
                checkGameRangerTimer.Stop();

                lastStatus = currentStatus;
                statusChangeCounter++;
                if (currentStatus) openStatusChangeCounter++; 
                else closeStatusChangeCounter++;

                var gamerangerStatusCheckPayload = new
                {
                    type = "gameranger_status_check",
                    gamerangerId = user.GetUserId(),
                    gamerangerStatus = currentStatus ? "open" : "closed"
                };

                 //Show "Please Wait..." panel for 3 seconds
                this.Invoke(new Action(() =>
                {
                    tokenPanel.Visible = true;
                    waitPanel.Visible = true; 
                    openClosePanel.Visible = false; 
                }));
                Thread.Sleep(3000);


                this.Invoke(new Action(() =>
                {
                    tokenPanel.Visible = true;
                    waitPanel.Visible = true;
                    openClosePanel.Visible = true;
                    if (currentStatus) label6.Text = "Please Close the GameRanger";
                    else label6.Text = "Please Open the GameRanger";
                }));

                Thread.Sleep(800);
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(gamerangerStatusCheckPayload);
                MessageBox.Show(jsonPayload);
                await webSocketService.SendMessage(jsonPayload);
            }
        }

        private async void CheckGameRangerStatus2(ClientAuthUpdatePayload clientAuthUpdatePayload)
        {
            var gameRangerProcesses = Process.GetProcessesByName("GameRanger");
            bool currentStatus = gameRangerProcesses.Length > 0;

            if (lastStatus == null || currentStatus != lastStatus)
            {
                timeperActionTimer.Stop();
                lastStatus = currentStatus;
                statusChangeCounter++;
                if (currentStatus) openStatusChangeCounter++;
                else closeStatusChangeCounter++;

                var gamerangerStatusCheckPayload = new
                {
                    type = "gameranger_status_check",
                    gamerangerId = user.GetUserId(),
                    gamerangerStatus = currentStatus ? "open" : "closed"
                };

                //Show "Please Wait..." panel for 3 seconds
                this.Invoke(new Action(() =>
                {
                    tokenPanel.Visible = true;
                    waitPanel.Visible = true;
                    openClosePanel.Visible = false;
                }));
                Thread.Sleep(3000);

                //MessageBox.Show(Thread.CurrentThread.ToString());
                // Show "Please Open/Close Gameranger" panel

                this.Invoke(new Action(() =>
                {
                    tokenPanel.Visible = true;
                    waitPanel.Visible = true;
                    openClosePanel.Visible = true;
                    if (currentStatus) label6.Text = "Please Close the GameRanger";
                    else label6.Text = "Please Open the GameRanger";
                }));
                //MessageBox.Show(Thread.CurrentThread.Name.ToString());

                //Thread.Sleep(800);
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(gamerangerStatusCheckPayload);
                MessageBox.Show(jsonPayload);
                await webSocketService.SendMessage(jsonPayload);

                if (clientAuthUpdatePayload.RemainingOpenings <= 0 && clientAuthUpdatePayload.RemainingClosings <= 0)
                {
                    this.checkGameRangerTimer2.Stop();
                    this.checkGameRangerTimer2.Stop();
                    authenticationFinished = true;
                    // I need a payload here that will tell me if the user is authenticated after reaching the 
                    // requiredAttempts in Desktop                   
                }
            }
        }

        private async void CheckIfAuthFailed()
        {
            if(authenticationFinished == false)
            {
                Action updateUI = () =>
                {
                    MessageBox.Show("Authentication Failed. Please try again.");
                    this.tokenPanel.Visible = false;
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
        }


        private BaseClientPayload DeserializePayload(string message)
        {
            var jObject = JObject.Parse(message);
            var type = jObject.Value<string>("type");

            switch (type)
            {
                case "client_auth_required":
                    return jObject.ToObject<ClientAuthPayload>();
                case "client_auth_update":
                    return jObject.ToObject<ClientAuthUpdatePayload>();
                case "client_auth_complete":
                    return jObject.ToObject<ClientAuthCompletePayload>();
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
            //mouseMonitor.CleanUpOldClicks();
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

            MessageBox.Show(jsonPayload);
            await webSocketService.SendMessage(jsonPayload);
        }

        public static long ToUnixTimestamp(DateTime date)
        {
            return (long)(date - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private async Task<FirstAuthentication> SendAuthPayload(int userId)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsPath = Path.Combine(appDataPath, "GameRanger", "GameRanger Prefs", "Settings");

            string email = await GetMostFrequentEmail(settingsPath);

            var macAddressList =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                && nic.GetPhysicalAddress().ToString() != String.Empty
                select nic.GetPhysicalAddress().ToString()
            ).ToArray();

            FirstAuthentication authPayload = new FirstAuthentication
            {
                Type = "auth_payload_type",
                Email = email,
                GameRangerId = userId,
                Token = textBox1.Text,
                KnownMacAddresses = macAddressList
            };

            //string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(authPayload);
            //MessageBox.Show(jsonPayload);
            if (webSocketService != null)
            {
                this.userId = userId;
                return authPayload;
            }
            else
                MessageBox.Show("Please Open GameRanger");

            return null;
            //return authPayload;
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
                gamerangerId = this.userId,
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
            this.tokenPanel.Visible = true;
            this.waitPanel.Visible = false;
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
        private void button2_Click(object sender, EventArgs e)
        {
            var userId = user.ReadGameRangerUserId(button2, textBox1);
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

        private void label7_Click(object sender, EventArgs e) 
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }


        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }
    }
}

