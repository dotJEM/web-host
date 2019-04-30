using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Initialization
{
    public interface IInitializationTracker
    {
        event EventHandler<EventArgs> Progress; 

        string Message { get; }
        double Percent { get; }
        bool Completed { get; }
        DateTime StarTime { get; }
        TimeSpan Duration { get; }

        void SetProgress(double percent);
        void SetProgress(string message, params object[] args);
        void SetProgress(double percent, string message, params object[] args);

        void Complete();
    }

    public class InitializationTracker : IInitializationTracker
    {
        public event EventHandler<EventArgs> Progress;

        public string Message { get; private set; }
        public double Percent { get; private set; }
        public bool Completed { get; private set; }
        public DateTime StarTime { get; } = DateTime.Now;
        public TimeSpan Duration => DateTime.Now - StarTime;

        public InitializationTracker()
        {
            Message = "";
            Percent = 0;
        }

        public void SetProgress(double percent)
        {
            Percent = percent;
            OnProgress();
        }

        public void SetProgress(string message, params object[] args)
        {
            SetProgress(Percent, message, args);
        }

        public void SetProgress(double percent, string message, params object[] args)
        {
            Percent = percent;
            Message = args.Any() ? string.Format(message, args) : message;
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
