using Newtonsoft.Json;
using SmartFormat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
//using static System.Net.Mime.MediaTypeNames;

namespace gadgets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new();
        private readonly string LogLocation = @".\latest.log";
        private readonly string ConfigLocation = @".\config.json";
        private readonly int Delay = 10;
        private readonly double Tolerance = 5;
        //private NotifyIcon TrayButton;
        private int Decay = 1;
        Config config = new()
        {
            Comment = "bruh u messed up config",
            Id = 0,
            Text = "omg u just messed up config :O\n\nNever Gonna Give You up!",
            Color = "#FFFFFFFF",
            Font = "Microsoft Jhenghei",
            Size = 16,
            DragAcceleration = 0.9,
            Bounciness = 0.5, 
            DefaultPosX = 500,
            DefaultPosY = 500
        };

        List<double> NewWindowPos = new();
        List<double> OldWindowPos = new();
        List<double> DeltaWindowMovement = new();
        List<double> ComputedWindowPos = new();
        public class Config
        {
            public string? Comment { get; set; }
            public int Id { get; set; }
            public string? Text { get; set; }
            public string? Color { get; set; }
            public string? Font { get; set; }
            public double Size { get; set; }
            public double DragAcceleration { get; set; }
            public double Bounciness { get; set; }
            public int DefaultPosX { get; set; }
            public int DefaultPosY { get; set; }
        }
        private void Log(string message)
        {
            if (!File.Exists(LogLocation)) { File.Create(LogLocation); }
            File.AppendAllText(LogLocation, String.Format("[{0}] {1}\n", DateTime.Now, message));
        }
        public MainWindow()
        {
            InitializeComponent();
            /*TrayButton = new NotifyIcon()
            {
                Icon = new System.Drawing.Icon("Main.ico"),
                ContextMenuStrip = new ContextMenuStrip()
                {
                    new MenuItem[] { new MenuItem("Close", null, ExitButtonEvent)}
                }
            };*/
            worker.DoWork += Main;
            worker.RunWorkerAsync();
        }
        void Main(object sender, DoWorkEventArgs e)
        {
            LoadConfig();
            ApplyConfig();
            while (true)
            {
                var currentTime = DateTime.Now;
                Dispatcher.Invoke(() =>
                {
                    OldWindowPos = NewWindowPos;
                    NewWindowPos = GetWindowPos(GadgetWindow);
                    DeltaWindowMovement = new() { NewWindowPos[0] - OldWindowPos[0], NewWindowPos[1] - OldWindowPos[1] };
                    ComputedWindowPos = new() { NewWindowPos[0] + DeltaWindowMovement[0] * config.DragAcceleration * Decay,
                        NewWindowPos[1] + DeltaWindowMovement[1] * config.DragAcceleration * Decay };
                    if ((DeltaWindowMovement[0] >= -Tolerance) && (DeltaWindowMovement[0] <= Tolerance))
                    {
                        ComputedWindowPos[0] = NewWindowPos[0] + CustomFloor(DeltaWindowMovement[0] * config.DragAcceleration * Decay);
                    }
                    if ((DeltaWindowMovement[1] >= -Tolerance) && (DeltaWindowMovement[1] <= Tolerance))
                    {
                        ComputedWindowPos[1] = NewWindowPos[1] + CustomFloor(DeltaWindowMovement[1] * config.DragAcceleration * Decay);
                    }
                    if ((ComputedWindowPos[0] <= 0) || (ComputedWindowPos[0] >= SystemParameters.VirtualScreenWidth - GadgetWindow.Width)) {
                        DeltaWindowMovement[0] *= -1;
                        ComputedWindowPos[0] += 2 * DeltaWindowMovement[0] * config.Bounciness;
                    }
                    if ((ComputedWindowPos[1] <= 0) || (ComputedWindowPos[1] >= SystemParameters.VirtualScreenHeight - GadgetWindow.Height))
                    {
                        DeltaWindowMovement[1] *= -1;
                        ComputedWindowPos[1] += 2 * DeltaWindowMovement[1] * config.Bounciness;
                    }
                    SetWindowPos(GadgetWindow, ComputedWindowPos);
                    if (ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(GadgetWindow) > 0) {
                        //DeltaWindowMovement[0] *= 2;
                        //DeltaWindowMovement[1] *= 3;
                        /*DeltaWindowMovement = new() { 0, 0 };
                        NewWindowPos = GetWindowPos(GadgetWindow);
                        OldWindowPos = GetWindowPos(GadgetWindow);
                        ComputedWindowPos = GetWindowPos(GadgetWindow);*/

                    }
                    var CombinedData = new
                    {
                        year = currentTime.Year.ToString(),
                        taiwanYear = (currentTime.Year - 1911).ToString(),
                        month = currentTime.Month.ToString(),
                        week = currentTime.DayOfWeek.ToString(),
                        day = currentTime.Day.ToString(),
                        hour = currentTime.Hour.ToString(),
                        minute = currentTime.Minute.ToString(),
                        second = currentTime.Second.ToString(),
                        Debug00 = OldWindowPos[0],
                        Debug10 = NewWindowPos[0],
                        Debug20 = DeltaWindowMovement[0],
                        Debug30 = ComputedWindowPos[0],
                        Debug01 = OldWindowPos[1],
                        Debug11 = NewWindowPos[1],
                        Debug21 = DeltaWindowMovement[1],
                        Debug31 = ComputedWindowPos[1]
                    };
                    GadgetText.Text = Smart.Format(config.Text, CombinedData);
                    Double StringSize = GetStringSize(GadgetText.Text)[1];
                    System.Windows.Application.Current.MainWindow.Width = StringSize;

                    //double speed = Math.Sqrt(Math.Pow(DeltaWindowMovement[0], 2.0) + Math.Pow(DeltaWindowMovement[1], 2));
                });
                Thread.Sleep(Delay);
            }
        }
        private List<double> GetStringSize(String str)
        {
            var formattedText = new FormattedText(
                str,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(GadgetText.FontFamily, GadgetText.FontStyle, GadgetText.FontWeight, GadgetText.FontStretch),
                GadgetText.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);
            List<double> result = new()
            {
                formattedText.Height,
                formattedText.Width
            };
            return result;
        }
        private double CustomFloor(double num)
        {
            if (num < 0)
            {
                return Math.Ceiling(num);
            } else if (num > 0) {
                return Math.Floor(num);
            } else
            {
                return 0;
            }
        }
        private int LoadConfig()
        {
            try
            {
                /*List<ConfigTemplate> Config = new() {
                    new ConfigTemplate(){}
                };*/
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigLocation));
                //Dictionary<string, object> dick = serializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch (Exception error)
            {
                Log(String.Format("Error: {0}, Did you enter config correctly or does it even exist? Default config is being load!", error));
                return 1;
            }
            return 0;
        }
        public void ApplyConfig()
        {
            try
            {
                DeltaWindowMovement = new() { 0, 0 };
                NewWindowPos = new() { config.DefaultPosX, config.DefaultPosY };
                OldWindowPos = new() { config.DefaultPosX, config.DefaultPosY };
                ComputedWindowPos = new() { config.DefaultPosX, config.DefaultPosY };
                Dispatcher.Invoke(() =>
                {
                    GadgetText.Text = config.Text;
                    GadgetText.Foreground = ConvertToBrush(config.Color);
                    GadgetText.FontFamily = new FontFamily(config.Font);
                    GadgetText.FontSize = config.Size;
                });
            }
            catch (Exception error)
            {
                Log(String.Format("Error: {0}", error));
            }
        }
        public Brush? ConvertToBrush(String? hex) { return new BrushConverter().ConvertFromString(hex) as Brush; }
        private List<double>? GetWindowPos(Window Win)
        {
            try { return new List<double> { Win.Left, Win.Top }; }
            catch (Exception error) { Log("Error: " + error); return null; }
        }
        private void SetWindowPos(Window win, List<double> pos)
        {
            try
            {
                win.Left = pos[0];
                win.Top = pos[1];
            }
            catch (Exception error) { Log("Error: " + error); }
        }


        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Decay = 0;
                this.DragMove();
            }
            else
            {
                Decay = 1;
            }
        }

        /*private void ExitButtonEvent(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ReloadButtonEvent(object sender, EventArgs e)
        {
            LoadConfig();
            ApplyConfig();
        }*/
    }
}
