using System;

namespace DotJEM.Web.Host.Providers.Scheduler.Tasks;

public interface ITrigger
{
    bool TryGetNext(bool firstExecution, out TimeSpan timeSpan);
}