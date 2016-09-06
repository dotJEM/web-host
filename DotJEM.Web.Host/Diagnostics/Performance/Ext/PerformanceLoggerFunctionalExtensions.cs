using System;

namespace DotJEM.Web.Host.Diagnostics.Performance.Ext
{
    public static class PerformanceLoggerFunctionalExtensions
    {
        public static T TrackFunction<T>(this IPerformanceLogger self, string name, Func<T> func, params object[] args)
        {
            T output = default(T);
            self.TrackAction(name, () => output = func(), args);
            return output;
        }
        public static T TrackFunction<T>(this IPerformanceLogger self, Func<T> func, params object[] args) => self.TrackFunction(func.Method.Name, func, args);

        public static void TrackAction(this IPerformanceLogger self, Action action, params object[] args) => self.TrackAction(action.Method.Name, action);

        public static void TrackAction(this IPerformanceLogger self, string name, Action action, params object[] args)
        {
            using (self.Track(name, args))
                action();
        }
    }
}