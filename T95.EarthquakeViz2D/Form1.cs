using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using T95.EarthquakeViz2D.Properties;
using Timer = System.Windows.Forms.Timer;

namespace T95.EarthquakeViz2D
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        //variable declaration
        private const int Clat = 0, Clon = 0, Zoom = 1, Ww = 1024, Hh = 512;
        private string _periodChooser, _lastHourEarthquakesId;
        private bool _firstTimeOpen, _go, _canRefresh;
        private Timer _t1;
        private MenuItem _startUp;
        private Thread _latestEarthquakes,_earthQ;
        private readonly List<CSVCells> earthQ = new List<CSVCells>();
        private readonly CSVParser _csvParser = new CSVParser();
        
       
        private void Form1_Load(object sender, EventArgs e)
        {
            this.DisableCloseButton();
            _firstTimeOpen = true; //make sure you're not showing all the earthquakes from the last hour
            pictureBox1.LoadAsync("https://api.mapbox.com/styles/v1/mapbox/dark-v9/static/" +
            Clat + "," + Clon + "," + Zoom + "/" + Ww + "x" + Hh +
            "?access_token=pk.eyJ1IjoiY29kaW5ndHJhaW4iLCJhIjoiY2l6MGl4bXhsMDRpNzJxcDh0a2NhNDExbCJ9.awIfnl6ngyHoB3Xztkzarw");
            comboBox1.SelectedIndex = 0; //load default "last day" earthquakes to visualize

            InitializeNotificationArea();
          
            _startUp.Click += StartUp_Click;
            notifyIcon1.DoubleClick += ShowForm;
            _t1 = new Timer
            {
                Interval = 300_000
            };
            _t1.Tick += CheckForNewEarthQuakes;
            _t1.Start();
            pictureBox1.Paint += PictureBox1_Paint;


            button1.PerformClick();
            WindowState = FormWindowState.Minimized;
        }

      
     
        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (_go)
            {
                _earthQ = new Thread(FindEarthQuakes);
                _earthQ.Start();
                _go = false;
            }
        }
       
      

        private float MercX(float lon)
        {
            lon = (float)ExtensionClass.DegreeToRadian(lon);
            float a = (256 / (float)Math.PI) * (float)Math.Pow(2, Zoom);
            float b = lon + (float)Math.PI;
            return a * b;
        }

        private float MercY(float lat)
        {
            lat = (float)ExtensionClass.DegreeToRadian(lat);
            var a = (256 / (float)Math.PI) * (float)Math.Pow(2, Zoom);
            var b = (float)Math.Tan(Math.PI / 4 + lat / 2);
            var c = (float)Math.PI - (float)Math.Log(b);
            return a * c;
        }

       
        private void Button1_Click_1(object sender, EventArgs e)
        {
            this.EnableCloseButton();
            label1.Location = new Point(700, 441);
            label1.Visible = true;
            label1.Text = $@"Trying to get earthquakes from {_periodChooser.ToLower()}";

            _go = true;
            formActivated = false; //for refocus
            _canRefresh = true; //minimizing problems

            button1.Visible = false;
            button2.Visible = true;
        }

       
        private void FindEarthQuakes()
        {
            float cx = MercX(Clon);
            float cy = MercY(Clat);
            var earthquakes = new List<CSVCells>();

            switch (_periodChooser)
            {
                case "Important":
                    earthquakes = _csvParser.SplitCSVFile("https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/significant_month.csv");
                    break;
                case "Last day":
                    earthquakes = _csvParser.SplitCSVFile("http://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_day.csv");
                    break;
                case "Last week":
                    earthquakes = _csvParser.SplitCSVFile("http://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_week.csv");
                    break;
                case "Last month":
                    earthquakes = _csvParser.SplitCSVFile("http://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_month.csv");
                    break;
                default:
                    earthquakes = _csvParser.SplitCSVFile("http://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_day.csv");
                    break;
            }
            earthQ.Clear();
            foreach (var eq in earthquakes)
            {
                using (var g = pictureBox1.CreateGraphics())
                {
                    try
                    {
                        float x = MercX(eq.Longitude) - cx;
                        float y = MercY(eq.Latitude) - cy;
                        earthQ.Add(new CSVCells(eq.Place, new Point((int)x, (int)y), eq.Updated, eq.Magnitude, eq.Type));
                        float mag = (float)Math.Pow(10, eq.Magnitude);
                        mag = (float)Math.Sqrt(mag);
                        float magmax = (float)Math.Sqrt(Math.Pow(10, 10));
                        float d = ExtensionClass.Map(mag, 0, magmax, 1, 500);
                        g.TranslateTransform(pictureBox1.ClientSize.Width / 2 - (d / 2),
                            pictureBox1.ClientSize.Height / 2 - (d / 2));
                        g.FillEllipse(eq.Magnitude < 5 ? Brushes.Magenta : Brushes.Red, x, y, d, d);
                        g.Dispose();
                    }
                    catch (Exception)
                    {

                    }
                  
                }
            }

            //Changing the label's text in a cross thread
            label1.Invoke(new Action(() =>
            {
                if (_periodChooser.Contains("Important"))
                {
                    label1.Location = new Point(680, 441);
                    label1.Text = $@"Done. In the last month there were {earthquakes.Count}  important earthquakes.";
                }
                else
                {
                    label1.Text = $@"Done. There were {earthquakes.Count} earthquakes in the{_periodChooser.ToLower()}";
                    label1.Location = new Point(710, 441);
                }
            }));
            _earthQ.Abort();
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            var mea = (MouseEventArgs)e;
            var biggest = 0f;

            try
            {
                foreach (var eq in earthQ)
                {
                    int d = (int)GetDistance(mea.X, mea.Y - (pictureBox1.ClientSize.Height / 2), eq.Coordinates.X + (pictureBox1.ClientSize.Width / 2), eq.Coordinates.Y);
                    if (d <= 9)
                    {
                        float current = eq.Magnitude;
                        if (current > biggest)
                            biggest = current;
                        label1.Location = new Point(620, 420);
                        label1.Text = $@"Place: {eq.Place} " +
                            $"\nDate: { Convert.ToDateTime(eq.Updated).ToShortDateString()}" +
                            $" Time: {Convert.ToDateTime(eq.Updated).ToShortTimeString()}" +
                            $"\nMagnitude: {biggest} Richter scale";
                        if (!eq.Type.Equals("earthquake"))
                        {
                            label1.Location = new Point(620, 400);
                            label1.Text += $"\nType: {eq.Type}";
                        }
                    }
                }
            }
            catch (Exception) { Application.Restart(); }
        }
        private double GetDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(Math.Pow((double)x1 - x2, 2) + Math.Pow((double)y1 - y2, 2));
        }

      
        private void LatestEarthquakes()
        {
            var earthquakes = _csvParser.SplitCSVFile("https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/4.5_hour.csv");
            //foreach (var eq in earthquakes)
            foreach (var t in earthquakes)
            {
                var currentEarthquakeId = t.Id;
                if (_firstTimeOpen)
                {
                    _lastHourEarthquakesId = currentEarthquakeId;
                    _firstTimeOpen = false;
                    break;
                }
                if (currentEarthquakeId == _lastHourEarthquakesId)
                {
                    _lastHourEarthquakesId = earthquakes[0].Id;
                    _latestEarthquakes.Abort();
                    break;
                }
                if (currentEarthquakeId != _lastHourEarthquakesId)
                {
                    notifyIcon1.BalloonTipTitle = $"A significant {t.Type.ToLower()} just took place.";
                    notifyIcon1.BalloonTipText = $"Place: {t.Place} " +
                                                 $"\nDate: { Convert.ToDateTime(t.Updated).ToShortDateString()}" +
                                                 $" Time: {Convert.ToDateTime(t.Updated).ToShortTimeString()}" +
                                                 $"\nMagnitude: {t.Magnitude} Richter scale";
                    notifyIcon1.ShowBalloonTip(2000);
                }
            }
        }
        private void CheckForNewEarthQuakes(object sender, EventArgs e)
        {
            _periodChooser = "Last hour";
            _latestEarthquakes = new Thread(LatestEarthquakes);
            _latestEarthquakes.Start();
        }


        #region interface stuff
        private void InitializeNotificationArea()
        {
            //notification icon
            var cm = new ContextMenu();
            var quit = new MenuItem("Quit");
            var breakItem = new MenuItem("-");
            var breakItem2 = new MenuItem("-");
            _startUp = new MenuItem("StartWithWindows")
            {
                Checked = Settings.Default.isChecked
            };
            var show = new MenuItem("Show");
            cm.MenuItems.Add(show);
            cm.MenuItems.Add(breakItem2);
            cm.MenuItems.Add(_startUp);
            cm.MenuItems.Add(breakItem);
            cm.MenuItems.Add(quit);
            notifyIcon1.ContextMenu = cm;

            show.Click += Show_Click;
            quit.Click += QuitApp;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _periodChooser = comboBox1.SelectedItem.ToString();
            pictureBox1.Invalidate();
            _go = true;
            button1.PerformClick();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
        private void Show_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            if (_canRefresh)
            {
                pictureBox1.Invalidate();
                _go = true;
            }
        }

        private void StartUp_Click(object sender, EventArgs e)
        {
            if (Settings.Default.isChecked)
            {
                Settings.Default.isChecked = false;
                _startUp.Checked = false;
                Settings.Default.Save();
            }
            else
            {
                Settings.Default.isChecked = true;
                _startUp.Checked = true;
                Settings.Default.Save();
            }
            RegisterAppToStartUp();
        }

        private void ShowForm(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            if (_canRefresh)
            {
                pictureBox1.Invalidate();
                _go = true;
            }
        }

        private void QuitApp(object sender, EventArgs e)
        {
            notifyIcon1.Dispose();
            Application.Exit();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            //notifyIcon1.BalloonTipTitle = $"Equakes";
            //notifyIcon1.BalloonTipText = "Earthquakes notifier will run in the background";
            //notifyIcon1.ShowBalloonTip(100);
            WindowState = FormWindowState.Minimized;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                ShowInTaskbar = false;
        }

        private void RegisterAppToStartUp()
        {
            using (var Key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run\", true))
            {
                if (Key != null)
                {
                    var val = Key.GetValue("EQuakes");
                    if (val == null && Settings.Default.isChecked)
                        Key.SetValue("EQuakes", Application.ExecutablePath);
                    else if (val != null && Settings.Default.isChecked == false)
                        Key.DeleteValue("EQuakes");
                }
            }
        }

        bool formActivated = true;
        private void Form1_Activated(object sender, EventArgs e)
        {
            if (!formActivated)
            {
                pictureBox1.Invalidate();
                _go = true;
            }

        }
        #endregion

    }
}
