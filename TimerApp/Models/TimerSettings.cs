using System;

namespace IntervalTimerApp.Models
{
    /// <summary>
    /// Timer settings
    /// </summary>
    [Serializable]
    public class TimerSettings
    {
        // Timer Interval
        public TimeSpan TimeSpan { get; set; }

        // Notification Message Text
        public string Message { get; set; }

        public void InitializeDefault()
        {
            TimeSpan = new TimeSpan(0, 30, 0);
            Message = "Default Message";
        }
    }
}