using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Initialization
{
    public interface IInitializationTracker
    {
        string Message { get; }
        double Percent { get; }
        bool Completed { get; }

        void SetProgress(double percent);
        void SetProgress(string message, params object[] args);
        void SetProgress(double percent, string message, params object[] args);

        void Complete();
    }

    public class InitializationTracker : IInitializationTracker
    {
        public string Message { get; private set; }
        public double Percent { get; private set; }
        public bool Completed { get; private set; }

        public InitializationTracker()
        {
            Message = "";
            Percent = 0;
        }

        public void SetProgress(double percent)
        {
            Percent = percent;
        }

        public void SetProgress(string message, params object[] args)
        {
            SetProgress(Percent, message);
        }

        public void SetProgress(double percent, string message, params object[] args)
        {
            Percent = percent;
            Message = args.Any() ? string.Format(message, args) : message;
        }

        public void Complete()
        {
            Percent = 100;
            Completed = true;
        }
    }
}
