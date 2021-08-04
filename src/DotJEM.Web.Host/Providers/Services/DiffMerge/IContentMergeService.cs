using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IContentMergeService
    {
        JObject EnsureMerge(Guid id, JObject entity, JObject prev);
    }
}