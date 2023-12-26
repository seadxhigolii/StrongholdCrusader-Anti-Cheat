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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
        private System.Windows.Forms.Timer timerGameRunning = new System.Windows.Forms.Timer();
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
        private int lastActiveScreen = -1; 
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


            //var procList = Process.GetProcesses().Where(process => process.ProcessName.Contains("Stronghold Crusader"));
            //var path = "";
            //foreach (var process in procList)
            //{
            //    path = Path.GetDirectoryName(process.MainModule.FileName);
            //    MessageBox.Show(path);
            //}

            //using (var md5 = MD5.Create())
            //{
            //    using (var stream = File.OpenRead(path + "\\Stronghold Crusader.exe"))
            //    {
            //        MessageBox.Show(BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower());
            //        if (BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower() == "20070495755f5adcb4e9245b25a4c13c")
            //        {
            //            MessageBox.Show("GameID Matches");
            //        }
            //    }
            //}

            //gameID : 20070495755f5adcb4e9245b25a4c13c



            welcomeIndex = random.Next(welcomeSentenceList.Length);
            motivationalIndex = random.Next(motivationalSentenceList.Length);

            // Select the sentences
            welcomeSentence = welcomeSentenceList[welcomeIndex];
            motivationalSentence = motivationalSentenceList[motivationalIndex];

            mouseMonitor = new MouseMonitor();



            //mouseMonitor.HighClickRateDetected += HandleHighClickRate;
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
            //MessageBox.Show("High click rate detected!");
        }

        private void WebSocketService_MessageReceived(string message)
        {
            try
            {
                //MessageBox.Show($"MessageReceived: {message}");
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
                this.authenticationFinished = true;
                if(this.checkGameRangerTimer != null)
                {
                    this.checkGameRangerTimer.Stop();
                    this.checkGameRangerTimer.Dispose();
                }
                if (this.checkGameRangerTimer2 != null)
                {
                    this.checkGameRangerTimer2.Stop();
                    this.checkGameRangerTimer2.Dispose();
                }
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
                //MessageBox.Show(jsonPayload);
                await webSocketService.SendMessage(jsonPayload);
            }
        }

        private async void CheckGameRangerStatus2(ClientAuthUpdatePayload clientAuthUpdatePayload)
        {
            if(authenticationFinished != true)
            {
                var gameRangerProcesses = Process.GetProcessesByName("GameRanger");
                bool currentStatus = gameRangerProcesses.Length > 0;

                if (lastStatus == null || currentStatus != lastStatus)
                {
                    //MessageBox.Show("Authentication Finished: " + authenticationFinished.ToString());
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
                    //MessageBox.Show(jsonPayload);
                    await webSocketService.SendMessage(jsonPayload);

                    if (clientAuthUpdatePayload.RemainingOpenings <= 0 && clientAuthUpdatePayload.RemainingClosings <= 0)
                    {
                        this.checkGameRangerTimer2.Stop();
                        this.checkGameRangerTimer.Stop();
                        authenticationFinished = true;
                    }
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
                    button2.Enabled = true;
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
                //await this.webSocketService.StopConnection();
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
        private void TimerGameRunning_Tick(object sender, EventArgs e)
        {

        }

        private void Timer1s_Tick(object sender, EventArgs e)
        {
            CheckMemoryValue();
        }

        private async void Timer10s_Tick(object sender, EventArgs e)
        {
            var highestCPSInLast10Seconds = mouseMonitor.HighestCPSLast10Seconds;
            SendGameUpdateStatus();
            SendGameSettingsStatus();
            //Task<DayOfWeek> taskA = Task.Run(() => DateTime.Today.DayOfWeek);
            //taskA.ContinueWith(antecedent => Console.WriteLine($"Today is {antecedent.Result}."));
            SendCpsUpdate(highestCPSInLast10Seconds);//.ContinueWith(_ => mouseMonitor.Clear());

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
                interval = 10
            };

            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(cpsUpdatePayload);
            //MessageBox.Show(jsonPayload);
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
            Process gameProcess = MemoryReader.GetProcessByName("Stronghold Crusader");

            if (gameProcess == null)
            {
                return;
            }
            else
            {
                //Cheese
                int cheeseValue1 = MemoryReader.ReadMemoryInt(gameProcess, "9611F8");
                int cheeseValue2 = MemoryReader.ReadMemoryInt(gameProcess, "962D8C");

                //Bread
                int breadValue1 = MemoryReader.ReadMemoryInt(gameProcess, "9611F4");
                int breadValue2 = MemoryReader.ReadMemoryInt(gameProcess, "962D88");
                int breadValue3 = MemoryReader.ReadMemoryInt(gameProcess, "164A250");

                //Current Rations

                int rationsValue1 = MemoryReader.ReadMemoryInt(gameProcess, "960D14");
                int rationsValue2 = MemoryReader.ReadMemoryInt(gameProcess, "962E88");

                int cheeseValue = cheeseValue1 != 0 ? cheeseValue1 : cheeseValue2;
                int breadValue = breadValue1 != 0 ? breadValue1 : (breadValue2 != 0 ? breadValue2 : breadValue3);
                int rationsValue = rationsValue1 != 0 ? rationsValue1 : rationsValue2;

                var payload = new OngoingGameUpdate
                {
                    Type = "ongoing_game_update",
                    granary = new GranaryInfo
                    {
                        inventory = new InventoryInfo
                        {
                            apples = MemoryReader.ReadMemoryInt(gameProcess, "961200"),
                            meat = MemoryReader.ReadMemoryInt(gameProcess, "9611FC"),
                            cheese = cheeseValue,
                            bread = breadValue
                        },
                        currentRations = rationsValue
                    },
                    fearFactor = MemoryReader.ReadMemoryInt(gameProcess, "962E30"),
                    activeTaxes = MemoryReader.ReadMemoryInt(gameProcess, "962E84"),
                    currentDate = new CurrentDateInfo
                    {
                        month = MemoryReader.ReadMemoryInt(gameProcess, "97DFC4"),
                        year = MemoryReader.ReadMemoryInt(gameProcess, "97DFC8")
                    },
                    population = new PopulationInfo
                    {
                        count = MemoryReader.ReadMemoryInt(gameProcess, "962E7C"),
                        max = MemoryReader.ReadMemoryInt(gameProcess, "960D70"),
                        popularity = MemoryReader.ReadMemoryInt(gameProcess, "960D5C")
                    },
                    leaderboard = new LeaderboardInfo
                    {
                        red = new PlayerInfo
                        {
                            nickname = MemoryReader.ReadMemoryString(gameProcess, "1364FD6", 100),
                            gold = MemoryReader.ReadMemoryInt(gameProcess, "961208"),
                            troopsCount = MemoryReader.ReadMemoryInt(gameProcess, "961238"),
                            lordHp = MemoryReader.ReadMemoryInt(gameProcess, "137D3B8")
                        },
                        orange = new PlayerInfo
                        {
                            nickname = MemoryReader.ReadMemoryString(gameProcess, "1365030", 100),
                            gold = MemoryReader.ReadMemoryInt(gameProcess, "964BFC"),
                            troopsCount = MemoryReader.ReadMemoryInt(gameProcess, "964C2C"),
                            lordHp = MemoryReader.ReadMemoryInt(gameProcess, "137DCD8")
                        }
                    }
                };

                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                //MessageBox.Show(jsonPayload);
                await webSocketService.SendMessage(jsonPayload);
            }
        }


        private async void SendGameSettingsStatus()
        {
            Process gameProcess = MemoryReader.GetProcessByName("Stronghold Crusader");
            if (gameProcess == null)
            {
                return;
            }
            else
            {
                var payload = new GameSettings
                {
                    Type = "game_settings_update",
                    Gold = MemoryReader.ReadMemoryInt(gameProcess, "1362CA4"),
                    Pt = MemoryReader.ReadMemoryInt(gameProcess, "1364C84"),
                    GameSpeed = MemoryReader.ReadMemoryInt(gameProcess, "1362B90"),
                    GameType = MemoryReader.ReadMemoryInt(gameProcess, "1362B94"),
                    MapName = MemoryReader.ReadMemoryString(gameProcess, "1361924",100)
                };
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                //MessageBox.Show(jsonPayload);
                await webSocketService.SendMessage(jsonPayload);
            }
        }

        private async void CheckMemoryValue()
        {
            Process gameProcess = MemoryReader.GetProcessByName("Stronghold Crusader");
            if (gameProcess == null)
            {
                return;
            }

            int currentValue = MemoryReader.ReadMemoryInt(gameProcess, "1362CA4");
            if (currentValue != lastActiveScreen)
            {
                if (currentValue == 30)
                {
                    var endGamepPayload = new EndGameStats
                    {
                        Type = "end_game_stats",
                        Red = new Player
                        {
                            GoldAcquired = MemoryReader.ReadMemoryInt(gameProcess, "13652B4"),
                            FoodProduced = MemoryReader.ReadMemoryInt(gameProcess, "1365480"),
                            WoodProduced = MemoryReader.ReadMemoryInt(gameProcess, "13654EC"),
                            StoneProduced = MemoryReader.ReadMemoryInt(gameProcess, "13654C8"),
                            IronProduced = MemoryReader.ReadMemoryInt(gameProcess, "13654A4"),
                            WeaponsProduced = MemoryReader.ReadMemoryInt(gameProcess, "1365548"),
                            TroopsProduced = MemoryReader.ReadMemoryInt(gameProcess, "13655D8"),
                            HighestPopulation = MemoryReader.ReadMemoryInt(gameProcess, "13652D6"),
                            EnemyBuildingsRazed = MemoryReader.ReadMemoryInt(gameProcess, "136545C"),
                            BuildingsLost = MemoryReader.ReadMemoryInt(gameProcess, "136556C")
                        },
                        Orange = new Player
                        {
                            GoldAcquired = MemoryReader.ReadMemoryInt(gameProcess, "13652B8"),
                            TroopsLost = MemoryReader.ReadMemoryInt(gameProcess, "1365340"),
                            TroopsKilled = MemoryReader.ReadMemoryInt(gameProcess, "1365360"),
                            FoodProduced = MemoryReader.ReadMemoryInt(gameProcess, "1365484"),
                            WoodProduced = MemoryReader.ReadMemoryInt(gameProcess, "13654F0"),
                            StoneProduced = MemoryReader.ReadMemoryInt(gameProcess, "13654CC"),
                            IronProduced = MemoryReader.ReadMemoryInt(gameProcess, "13654A8"),
                            WeaponsProduced = MemoryReader.ReadMemoryInt(gameProcess, "136554C"),
                            TroopsProduced = MemoryReader.ReadMemoryInt(gameProcess, "13655DC"),
                            HighestPopulation = MemoryReader.ReadMemoryInt(gameProcess, "13652D8"),
                            EnemyBuildingsRazed = MemoryReader.ReadMemoryInt(gameProcess, "1365460"),
                            BuildingsLost = MemoryReader.ReadMemoryInt(gameProcess, "1365570")
                        }
                    };

                    string endGameJsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(endGamepPayload);
                    //MessageBox.Show(jsonPayload);
                    await webSocketService.SendMessage(endGameJsonPayload);
                }
                lastActiveScreen = currentValue;
                SendActiveScreenUpdateStatus();
            }
        }


        private async void SendActiveScreenUpdateStatus()
        {
            Process gameProcess = MemoryReader.GetProcessByName("Stronghold Crusader");
            if (gameProcess == null)
            {
                return;
            }
            else
            {
                var payload = new ActiveScreenUpdate
                { 
                    Type = "active_screen_update",
                    Value = MemoryReader.ReadMemoryInt(gameProcess, "916A28")
                };
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                //MessageBox.Show(jsonPayload);
                await webSocketService.SendMessage(jsonPayload);
            }
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

