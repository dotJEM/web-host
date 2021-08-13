using System;
using System.Linq;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public abstract class PipelineHandler : IPipelineHandler
    {

        protected PipelineHandler()
        {
           
        }

        public virtual bool Accept(string contentType)
        {
            return true;
        }

        public virtual JObject BeforeGet(dynamic entity, string contentType, PipelineContext context)
        {
            return BeforeGet(entity, contentType);
        }

        public virtual JObject BeforeGet(dynamic entity, string contentType)
        {
            return entity;
        }

        public virtual JObject AfterGet(dynamic entity, string contentType, PipelineContext context)
        {
            return AfterGet(entity, contentType);
        }

        public virtual JObject AfterGet(dynamic entity, string contentType)
        {
            return entity;
        }

        public virtual JObject BeforePost(dynamic entity, string contentType, PipelineContext context)
        {
            return BeforePost(entity, contentType);
        }

        public virtual JObject BeforePost(dynamic entity, string contentType)
        {
            return entity;
        }

        public virtual JObject AfterPost(dynamic entity, string contentType, PipelineContext context)
        {
            return AfterPost(entity, contentType);
        }

        public virtual JObject AfterPost(dynamic entity, string contentType)
        {
            return entity;
        }
        
        public virtual JObject BeforeDelete(dynamic entity, string contentType, PipelineContext context)
        {
            return BeforeDelete(entity, contentType);
        }

        public virtual JObject BeforeDelete(dynamic entity, string contentType)
        {
            return entity;
        }

        public virtual JObject AfterDelete(dynamic entity, string contentType, PipelineContext context)
        {
            return AfterDelete(entity, contentType);
        }

        public virtual JObject AfterDelete(dynamic entity, string contentType)
        {
            return entity;
        }

        public virtual JObject BeforePut(dynamic entity, dynamic previous, string contentType, PipelineContext context)
        {
            return BeforePut(entity, previous, contentType);
        }

        public virtual JObject BeforePut(dynamic entity, dynamic previous, string contentType)
        {
            return entity;
        }

        public virtual JObject AfterPut(dynamic entity, dynamic previous, string contentType, PipelineContext context)
        {
            return AfterPut(entity, previous, contentType);
        }

        public virtual JObject AfterPut(dynamic entity, dynamic previous, string contentType)
        {
            return entity;
        }

        public virtual JObject BeforeRevert(dynamic entity, dynamic current, string contentType, PipelineContext context)
        {
            return BeforeRevert(entity, current, contentType);
        }

        public virtual JObject BeforeRevert(dynamic entity, dynamic current, string contentType)
        {
            return entity;
        }

        public virtual JObject AfterRevert(dynamic entity, dynamic current, string contentType, PipelineContext context)
        {
            return AfterRevert(entity, current, contentType);
        }

        public virtual JObject AfterRevert(dynamic entity, dynamic current, string contentType)
        {
            return entity;
        }
    }
}