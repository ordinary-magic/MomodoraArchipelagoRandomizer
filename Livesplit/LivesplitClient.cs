using LiveSplit.Model;
using Momo4Randomizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    /// <summary>
    /// Client which is used to run the livesplit verison of the randomizer.
    /// </summary>
    public class LivesplitClient : IComponent
    {
        // Controls and Connections //
        private readonly SimpleLabel RandomizerLabel;
        private readonly RandomizerSettings settings;
        private GameMonitor gameProc;
        private Randomizer randomizer;

        // Logging //
        private readonly List<string> Events = new List<string>();

        public LivesplitClient(LiveSplitState state)
        {
            RandomizerLabel = new SimpleLabel();
            settings = new RandomizerSettings();

            // Register the live split methods.
            state.OnStart += OnStart;
            state.OnReset += OnReset;
        }

        private void OnReset(object sender, TimerPhase value)
        {
            WriteLogfile();

            randomizer?.Teardown();
            gameProc = null;
            randomizer = null;
        }

        private void OnStart(object sender, EventArgs e)
        {
            gameProc = GameMonitor.TryConnectToGame() ??
                throw new InvalidOperationException("Could not connect to game process");

            if (gameProc.VerifyProcessRunning())
            {
                Events.Clear();

                // Archipelago setup contains a lot of server connection and 
                // data retrieval waiting that is better done asynchronously.
                Task.Run(() => InitializeRandomizer(gameProc));
            }
        }

        // Shim to fetch the return value from Randomizer.InitializeRandomizer asyncronously 
        private async void InitializeRandomizer(GameMonitor game)
        {
            randomizer = await Randomizer.InitializeRandomizer(game,
                settings.GetServerAddress(), settings.GetServerPort(), settings.GetSlot(), settings.GetServerPassword(),
                ShowMessage, LogEvent);
        }

        public string ComponentName => "Momodora Archipelago Randomizer";

        public float HorizontalWidth { get; set; }

        public float MinimumHeight => 10;

        public float VerticalHeight { get; set; }

        public float MinimumWidth => 200;

        public float PaddingTop => 1;

        public float PaddingBottom => 1;

        public float PaddingLeft => 1;

        public float PaddingRight => 1;

        public IDictionary<string, Action> ContextMenuControls => null;

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            throw new NotImplementedException();
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            var textHeight = g.MeasureString("A", state.LayoutSettings.TextFont).Height;
            VerticalHeight = textHeight * 1.5f;

            PrepareDraw(state);
            RandomizerLabel.SetActualWidth(g);
            RandomizerLabel.Width = RandomizerLabel.ActualWidth;
            RandomizerLabel.Height = VerticalHeight;
            RandomizerLabel.X = width - PaddingRight - RandomizerLabel.Width;
            RandomizerLabel.Y = 3f;

            DrawBackground(g, width, VerticalHeight);

            RandomizerLabel.Draw(g);
        }

        public void PrepareDraw(LiveSplitState state)
        {
            RandomizerLabel.Font = settings.OverrideTextFont ? settings.TextFont : state.LayoutSettings.TextFont;
            RandomizerLabel.ForeColor = settings.OverrideTextColor ? settings.TextColor : state.LayoutSettings.TextColor;
            RandomizerLabel.OutlineColor = settings.OverrideTextColor ? settings.OutlineColor : state.LayoutSettings.TextOutlineColor;
            RandomizerLabel.ShadowColor = settings.OverrideTextColor ? settings.ShadowColor : state.LayoutSettings.ShadowsColor;

            RandomizerLabel.VerticalAlignment = StringAlignment.Center;
            RandomizerLabel.HorizontalAlignment = StringAlignment.Center;
        }

        private void DrawBackground(Graphics g, float width, float height)
        {
            if (settings.BackgroundColor.A > 0
                || settings.BackgroundGradient != GradientType.Plain
                && settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            settings.BackgroundColor,
                            settings.BackgroundGradient == GradientType.Plain
                            ? settings.BackgroundColor
                            : settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            return settings.GetSettings(document);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            return settings;
        }

        public void SetSettings(XmlNode settings)
        {
            this.settings.SetSettings(settings);
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (gameProc != null && gameProc.VerifyProcessRunning())
            {
                randomizer?.Update();
                invalidator?.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Show a message to the player.
        /// </summary>
        private void ShowMessage(string message)
        {
            LogEvent(message);
            RandomizerLabel.Text = message;
        }


        /// <summary>
        /// Log a game event.
        /// </summary>
        /// <param name="text"></param>
        private void LogEvent(string text)
        {
            if (settings.logEnabled == true)
            {
                if (text.StartsWith("\n"))
                {
                    Events.Add("\n" + DateTime.Now.ToString("T") + ":" + text.Replace("\n", "") + "\n");
                }
                else Events.Add(DateTime.Now.ToString("T") + ":" + text);
            }
            Debug.WriteLine(text.Replace("\n", ""));
        }

        /// <summary>
        /// Create the logging file.
        /// TODO: decouple this from the reset button?
        /// </summary>
        private void WriteLogfile()
        {
            if (settings.logEnabled == true)
            {
                Debug.WriteLine("Creating event log file");
                var path = "Components/MomodoraArchipelagoRandomizer.log";
                try
                {
                    File.AppendAllText(path, "Running on version " + gameProc?.Version ?? 0 + "\n");
                    Debug.WriteLine("Writing run events");
                    foreach (var Event in Events)
                    {
                        File.AppendAllText(path, Event);
                    }
                }
                catch (FileNotFoundException)
                {
                    Debug.WriteLine("Failed to create event log file");
                }
            }
        }
    }

}