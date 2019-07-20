using IntervalTimerApp.Models;
using MaterialDesignThemes.Wpf;
using System;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace IntervalTimerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables Block

        private DispatcherTimer _dispatcherTimer;

        private WindowState _windowState;

        // Tray Menu
        private ContextMenu _trayMenu;

        // Timer Settings
        private TimerSettings _settings;

        // Tray Icon
        private System.Windows.Forms.NotifyIcon _trayIcon;

        // Can close the window or not?
        private bool _canClose;

        // Is the timer running now?
        private bool _isTimerStart;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Initialize variables
            _trayIcon = null;
            _trayMenu = null;
            _canClose = _isTimerStart = false;
            _windowState = WindowState.Normal;

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        #region UI block

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Creating a tray icon
            CreateTrayIcon();

            // Load timer settings from file
            LoadSettings();

            StartStop();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();

                if (_trayMenu.Items[0] is MenuItem)
                    (_trayMenu.Items[0] as MenuItem).Header = "Show";
            }
            else
            {
                _windowState = WindowState.Normal;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_canClose)
            {
                e.Cancel = true;
                _windowState = WindowState;

                if (_trayMenu.Items[0] is MenuItem)
                    (_trayMenu.Items[0] as MenuItem).Header = "Show";

                Hide();
            }
            else
            {
                _trayIcon.Visible = false;
                SaveSettings();
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            StartStop();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            StartTimer();
        }

        private void SaveTimeButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.TimeSpan = GetTimeSpan();
        }

        private void SaveMessageButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Message = MessageTextBox.Text;
        }

        /// <summary>
        /// Processing a click on the tray
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrayIcon_Click(object sender, EventArgs e)
        {
            // If you clicked the left mouse button 
            // then show/hide the window
            if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowHideWindow(sender, null);
            }
            else
            {
                // otherwise display the context menu
                _trayMenu.IsOpen = true;
                Activate();
            }
        }

        private void ShowHideMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowHide(sender);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _canClose = true;
            Close();
        }

        #endregion

        /// <summary>
        /// Icon tray creation
        /// </summary>
        /// <returns></returns>
        private void CreateTrayIcon()
        {
            if (_trayIcon is null)
            {
                _trayIcon = new System.Windows.Forms.NotifyIcon();
                _trayIcon.Icon = TimerApp.Properties.Resources.StopWatchIcon;

                _trayIcon.Text = "Interval Timer";
                _trayMenu = Resources["TrayMenu"] as ContextMenu;

                _trayIcon.Click += TrayIcon_Click;
            }

            _trayIcon.Visible = true;
        }

        private void ShowHideWindow(object sender, RoutedEventArgs e)
        {
            _trayMenu.IsOpen = false;
            ShowHide(_trayMenu.Items[0]);
        }

        private void ShowHide(object sender)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem != null)
            {
                if (IsVisible)
                {
                    Hide();
                    menuItem.Header = "Show";
                }
                else
                {
                    Show();
                    menuItem.Header = "Hide";

                    WindowState = _windowState;
                    Activate();
                }
            }
        }

        /// <summary>
        /// Display notification
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            _trayIcon.BalloonTipTitle = "Timer Notification";
            _trayIcon.BalloonTipText = _settings.Message;
            _trayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
            _trayIcon.ShowBalloonTip(30000);
        }

        private void StartStop()
        {
            // If the timer is not running
            if (!_isTimerStart)
            {
                // then starting
                StartTimer();
                _isTimerStart = true;

                SetSettingsStartStopButton("Stop", PackIconKind.StopCircleOutline);
            }
            else
            {
                // otherwise stoping
                _dispatcherTimer.Stop();
                _isTimerStart = false;

                SetSettingsStartStopButton("Start", PackIconKind.ClockStart);
            }
        }

        /// <summary>
        /// Change start/stop button icon and updating menu items in tray
        /// </summary>
        /// <param name="tooltip"></param>
        /// <param name="icon"></param>
        private void SetSettingsStartStopButton(string tooltip, PackIconKind icon)
        {
            StartButton.ToolTip = tooltip;
            StartStopButtonIcon.Kind = icon;

            if (_trayMenu.Items[1] is MenuItem)
                (_trayMenu.Items[1] as MenuItem).Header = _isTimerStart ? "Stop" : "Start";
        }

        /// <summary>
        /// Set message to the status bar
        /// </summary>
        /// <param name="message"></param>
        private void SetMessageToStatusBar(string message)
        {
            StatusBarText.Text = message;
        }

        private void StartTimer()
        {
            _dispatcherTimer.Interval = _settings.TimeSpan;
            _dispatcherTimer.Start();
        }

        private TimeSpan GetTimeSpan()
        {
            int.TryParse(hoursTextBox.Text, out int hours);
            int.TryParse(minutesTextBox.Text, out int minutes);
            int.TryParse(secondsTextBox.Text, out int seconds);

            return new TimeSpan(hours, minutes, seconds);
        }


        #region Settings block

        /// <summary>
        /// Loading settings from file
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = config.AppSettings.Settings;

                string path = Path.Combine(Directory.GetCurrentDirectory(), settings["settingsFileName"].Value);

                if (File.Exists(path))
                {
                    try
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            _settings = formatter.Deserialize(fs) as TimerSettings;
                        }

                        SetMessageToStatusBar("Settings loaded successfully");
                    }
                    catch
                    {
                        SetMessageToStatusBar("Error reading settings!!!");
                        _settings = new TimerSettings();
                        _settings.InitializeDefault();
                    }
                }
                else
                {
                    SetMessageToStatusBar("The file with the settings was not found!!!");
                    _settings = new TimerSettings();
                    _settings.InitializeDefault();
                }
            }
            catch (Exception ex)
            {
                SetMessageToStatusBar($"An error has occurred!!! Details: {ex.Message}");
            }

            if (_settings != null)
            {
                hoursTextBox.Text = _settings.TimeSpan.Hours.ToString();
                minutesTextBox.Text = _settings.TimeSpan.Minutes.ToString();
                secondsTextBox.Text = _settings.TimeSpan.Seconds.ToString();
                MessageTextBox.Text = _settings.Message;
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = config.AppSettings.Settings;

                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream fs = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), settings["settingsFileName"].Value), FileMode.OpenOrCreate))
                {
                    formatter.Serialize(fs, _settings);
                }
            }
            catch (Exception ex)
            {
                SetMessageToStatusBar($"An error has occurred!!! Details: {ex.Message}");
            }
        }

        #endregion
    }
}