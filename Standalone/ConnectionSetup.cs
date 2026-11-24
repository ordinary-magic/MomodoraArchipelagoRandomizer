using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;


namespace Momo4Standalone
{

    /// <summary>
    /// Standlone momodora 4 archipelago launcher.
    /// </summary>
    public class ConnectionSetup : Form
    {
        private const string CACHE_FILE_PATH = "momo4ap.cache";
        private TextBox server;
        private TextBox port;
        private TextBox slot;
        private TextBox password;
        private readonly Action<string, int, string, string> Callback;
        private readonly Action<string, Color> SendMessage;

        public ConnectionSetup(Action<string, int, string, string> callback, Action<string, Color> sendMessage)
        {
            Text = "Archipealgo Configuration"; // the title
            ClientSize = new Size(350, 144);
            StartPosition = FormStartPosition.CenterParent;
            Callback = callback;
            SendMessage = sendMessage;
            //FormClosed += (sender, e) => SaveConnectionInfo(server.Text, port.Text, slot.Text, password.Text);

            var grid = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                //CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble,
                RowCount = 5,
                ColumnCount = 2,
                Padding = new Padding(8),
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

            grid.Controls.Add(MakeLabel("Address:"));
            server = MakeTextbox();
            grid.Controls.Add(server);

            grid.Controls.Add(MakeLabel("Port:"));
            port = MakeTextbox();
            grid.Controls.Add(port);

            grid.Controls.Add(MakeLabel("Slot:"));
            slot = MakeTextbox();
            grid.Controls.Add(slot);

            grid.Controls.Add(MakeLabel("Password:"));
            password = MakeTextbox(censored: true);
            grid.Controls.Add(password);

            grid.Controls.Add(MakeConnectButton(), 1, 4);

            Controls.Add(grid);

            TryToLoadCacheValues();
        }

        private Label MakeLabel(string text)
        {
            return new Label()
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left,
            };
        }

        private TextBox MakeTextbox(string text = "", bool censored = false)
        {
            return new TextBox()
            {
                Text = text,
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = censored,
            };
        }

        private Button MakeConnectButton()
        {
            var button = new Button()
            {
                Text = "Connect",
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
            };
            button.Click += (sender, e) => DoConnectButtonClick();
            return button;
        }

        /// <summary>
        /// Try to load cached connection info from the cache file.
        /// </summary>
        private static Dictionary<string, string> TryToReadCacheFile()
        {
            if (File.Exists(CACHE_FILE_PATH))
                try
                {
                    var lines = File.ReadAllLines(CACHE_FILE_PATH);
                    var dict = lines
                        .Where(line => line.Contains('='))
                        .Select(line => line.Split(new[] { '=' }, 2))
                        .Where(parts => parts.Length == 2)
                        .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    return dict;
                }
                // So many things can throw here, missing fields, parse errors, but we kinda dont care except that we failed to get any info
                catch (Exception) { }
            return new Dictionary<string, string>();
        }

        private static void SaveConnectionInfo(string addr, string port, string slot, string password)
        {
            //try
            //{
                // Create directory if needed
                var dir = Path.GetDirectoryName(CACHE_FILE_PATH);
                if (dir != null && dir != "" && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Write connection info
                File.WriteAllLines(CACHE_FILE_PATH, new List<string> {
                    $"server={addr}",
                    $"port={port}",
                    $"slot={slot}",
                    $"password={password}"
                });
            //}
            //catch (Exception) { }
        }

        private void TryToLoadCacheValues()
        {
            // Try to pre-load cached connection info as a convenience
            var cache = TryToReadCacheFile();

            // Must explicitly set "" on cache miss, as setting text to null will throw an exception
            cache.TryGetValue("server", out string val1);
            server.Text = val1 ?? "127.0.0.1";
            cache.TryGetValue("port", out string val2);
            port.Text = val2 ?? "38281";
            cache.TryGetValue("slot", out string val3);
            slot.Text = val3 ?? "Kaho";
            cache.TryGetValue("password", out string val4);
            password.Text = val4;
        }

        private void DoConnectButtonClick()
        {
            if (!ushort.TryParse(port.Text, out ushort portNum))
            {
                SendMessage($"Error: '{port.Text}' is not a valid port number", Color.DarkRed);
                return;
            }
            Callback(server.Text, portNum, slot.Text, password.Text);
            SaveConnectionInfo(server.Text, port.Text, slot.Text, password.Text);
            Close();
        }
    }
}