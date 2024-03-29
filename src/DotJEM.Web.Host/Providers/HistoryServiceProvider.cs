﻿using DotJEM.Diagnostic;
using DotJEM.Json.Storage;
using DotJEM.Pipelines;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Providers
{
    public class HistoryServiceProvider : ServiceProvider<IHistoryService>
    {
        private readonly IStorageContext context;

        public HistoryServiceProvider(IStorageContext context, IStorageIndexManager manager, IPipelines pipelines, ILogger logger,
            IPerformanceLogAspectSignatureCache cache = null,
            IPipelineContextFactory contextFactory = null)
            : base(name => new HistoryService(context.Area(name), manager, pipelines, contextFactory), logger, cache)
        {
            this.context = context;
        }

        public override bool Release(string areaName)
        {
            return base.Release(areaName) && context.Release(areaName);
        }
    }
}