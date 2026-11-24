using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Momo4Randomizer;
using Serilog;

namespace Momo4Standalone
{

    /// <summary>
    /// Standlone momodora 4 archipelago launcher.
    /// </summary>
    public class StandaloneClient : Form
    {
        private Label randomizerStatus;
        private Button connectButton;
        private RichTextBox messageLog;
        private Randomizer randomizer = null;
        private Timer updateTimer;

        // Lazy load the log file so it crashes after the cosntructor finishes if there's an issue
        private ILogger<StandaloneClient> _logFile;
        private ILogger<StandaloneClient> LogFile
        {
            get
            {
                if (_logFile == null)
                    _logFile = MakeLogger("logs/momo4_rando.log");
                return _logFile;
            }
        }

        public StandaloneClient()
        {
            // Initialize the form itself
            Text = "Momodora RuTM Client"; // the title
            // Icon = ; TODO
            ClientSize = new Size(350, 700);
            StartPosition = FormStartPosition.CenterScreen;

            Resize += (sender, e) => ResizeMessageLog();
            FormClosing += (sender, e) => Disconnect();

            // Add the status panel first, since its fixed size, then add the log to fill the remaining space
            Controls.Add(MakeStatusPanel());
            Controls.Add(MakeMessageLog());
            ResizeMessageLog();

            /*
            // DEBUG:
            SendMessage("Hello!");
            SendMessage("I AM MOMO!!");
            SendMessage("meow mewo :3", Color.Purple);
            */
        }

        public void ResizeMessageLog()
        {
            messageLog.Size = ClientSize - new Size(0, 48);
        }

        public static void Main()
        {
            Application.Run(new StandaloneClient());
        }

        /// <summary>
        /// Make the auto-scrolling status log, which shows all the messages sent by the randomizer
        /// </summary>
        private RichTextBox MakeMessageLog()
        {
            // Initialize the textbox
            messageLog = new RichTextBox()
            {
                Multiline = true,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Dock = DockStyle.Top,
                ReadOnly = true,
                BackColor = Color.LightPink,
                Font = new Font(FontFamily.GenericSansSerif, 14),
            };

            return messageLog;
        }

        private Label MakeStatusLabel()
        {
            randomizerStatus = new Label()
            {
                Text = "Disconnected",
                ForeColor = Color.DarkRed,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(FontFamily.GenericSansSerif, 14),
            };
            return randomizerStatus;
        }

        private Button MakeConnectButton()
        {
            connectButton = new Button()
            {
                Text = "Connect",
                //Image = ,
                ForeColor = Color.Blue,
                Anchor = AnchorStyles.Right,
            };
            connectButton.Click += ConnectClick;
            return connectButton;
        }

        /// <summary>
        /// Create a panel which contains the Conneciton status label and Connect button
        /// </summary>
        private Panel MakeStatusPanel()
        {
            var statusPanel = new TableLayoutPanel()
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                BackColor = Color.SlateGray,
                //CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble,
                RowCount = 1,
                ColumnCount = 2,
            };

            // Must manually set the size of each column as they are added or it geusses badly
            statusPanel.Controls.Add(MakeStatusLabel());
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            statusPanel.Controls.Add(MakeConnectButton());
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            return statusPanel;
        }

        /// <summary>
        /// Create the logging interface.
        /// </summary>
        public static ILogger<StandaloneClient> MakeLogger(string logfile)
        {
            var loggerFactory = new LoggerFactory();
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.File(logfile, rollingInterval: RollingInterval.Day)
                .CreateLogger();
            loggerFactory.AddSerilog(loggerConfig);
            return loggerFactory.CreateLogger<StandaloneClient>();
        }

        /// <summary>
        /// Send a text message in the log.
        /// </summary>
        public void SendMessage(string text)
        {
            LogFile.LogInformation(text);
            messageLog.AppendText(text + "\n");
            messageLog.ScrollToCaret();
        }

        /// <summary>
        /// Send a colored text message in the log.
        /// TODO: integrate with rest of code.
        /// </summary>
        public void SendMessage(string text, Color color)
        {
            LogFile.LogInformation(text);

            // Create a region of text for this message so that we can set its color
            messageLog.SelectionStart = messageLog.TextLength;
            messageLog.SelectionLength = 0;
            messageLog.SelectionColor = color;
            messageLog.AppendText(text + "\n");
            messageLog.SelectionColor = messageLog.ForeColor;
            messageLog.ScrollToCaret();
        }

        /// <summary>
        /// Method executed when the "Connect" button is pressed, to initialize the randomizer
        /// </summary>
        private void ConnectClick(object sender, EventArgs e)
        {
            connectButton.Enabled = false; // dont let this fire more than once
            new ConnectionSetup(OnConnectMenuExit, SendMessage).Show();
        }

        /// <summary>
        /// Method called by the connection setup menu when it is done.
        /// </summary>
        private void OnConnectMenuExit(string addr, int port, string slot, string password)
        {
            SendMessage("Setting Up Randomizer...");
            Task.Run(() => InitialzeRandomizer(addr, port, slot, password));
            updateTimer = StartUpdates();
        }

        /// <summary>
        /// Async method to create the randomizer object
        /// </summary>
        private async void InitialzeRandomizer(string addr, int port, string slot, string password)
        {
            var game = GameMonitor.TryConnectToGame();
            if (game != null)
                randomizer = await Randomizer.InitializeRandomizer(
                    game, addr, port, slot, password, SendMessage, text => LogFile.LogDebug(text));
            else
                SendMessage("Could not connect to game process!");

            // Re-enable the connect button if the randomizer is not valid
            if (randomizer == null)
                connectButton.Enabled = true;
            else
            {
                randomizerStatus.Text = "Ready";
                randomizerStatus.ForeColor = Color.ForestGreen;
            }
        }

        public Timer StartUpdates()
        {
            Timer update = new Timer()
            {
                Interval = 50
            };
            update.Tick += (sender, e) => randomizer?.Update();
            update.Start();
            return update;
        }

        private void Disconnect()
        {
            SendMessage("Shutting down randomizer...");
            randomizer?.Teardown();
            updateTimer?.Stop();
            randomizer = null;
            SendMessage("Shutdown Complete!");
        }
    }
}