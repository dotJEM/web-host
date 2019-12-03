using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Initialization
{
    public interface IInitializationTracker
    {
        event EventHandler<EventArgs> Progress;

        JObject Json { get; }
        string Message { get; }
        double Percent { get; }
        bool Completed { get; }
        DateTime StarTime { get; }
        TimeSpan Duration { get; }

        void SetProgress(double percent);
        void SetProgress(string message, params object[] args);
        void SetProgress(double percent, string message, params object[] args);

        void SetProgress(JObject json, double percent);
        void SetProgress(JObject json, string message, params object[] args);
        void SetProgress(JObject json, double percent, string message, params object[] args);
        void Complete();
    }

    public class InitializationTracker : IInitializationTracker
    {
        public event EventHandler<EventArgs> Progress;

        private JObject jsonData = new JObject();

        public JObject Json => CreateJObject();
        public string Message { get; private set; } = "";
        public double Percent { get; private set; } = 0;
        public bool Completed { get; private set; } = false;
        public DateTime StarTime { get; } = DateTime.Now;
        public TimeSpan Duration => DateTime.Now - StarTime;

        private JObject CreateJObject()
        {
            JObject json = JObject.FromObject(new
            {
                completed = Completed,
                percent = Percent,
                starTime = StarTime,
                duration = Duration,
                message = Message,
                metaData = jsonData
            });
            return json;
        }

        public void SetProgress(double percent)
            => SetProgress(percent, Message);

        public void SetProgress(string message, params object[] args)
            => SetProgress(Percent, message, args);

        public void SetProgress(double percent, string message, params object[] args)
            => SetProgress(jsonData, Percent, message, args);

        public void SetProgress(JObject json, double percent)
            => SetProgress(json, percent, Message);

        public void SetProgress(JObject json, string message, params object[] args)
            => SetProgress(json, Percent, message, args);

        public void SetProgress(JObject json, double percent, string message, params object[] args)
        {
            Percent = percent;
            Message = args.Any() ? string.Format(message, args) : message;
            jsonData = json;
            OnProgress();
        }

        public void Complete()
        {
            Percent = 100;
            Completed = true;
            OnProgress();
        }

        protected virtual void OnProgress()
        {
            Progress?.Invoke(this, EventArgs.Empty);
        }
    }
}
