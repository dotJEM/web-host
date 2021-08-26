using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgileObjects.ReadableExpressions;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.AsyncPipeline.Handlers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services.AsyncPipeline
{
    [TestFixture]
    public class PipelineExecutorDelegateFactoryTest
    {

        [Test]
        public void CreateInvocator_ReturnsDelegate()
        {
            PipelineExecutorDelegateFactory factory = new PipelineExecutorDelegateFactory();

            FakeFirstTarget target = new FakeFirstTarget();
            PipelineExecutorDelegate action = factory.CreateInvocator(target, target.GetType().GetMethod(nameof(FakeFirstTarget.Run)));
            FakeContext context = new FakeContext(new Dictionary<string, object>()
            {
                {"id", 42},
                {"name", "Foo"}
            });
            action(context, new FakeNextHandler<int, string>());
        }

        [Test]
        public void CreateInvocator_ReturnsDelegat2e()
        {
            PipelineExecutorDelegateFactory factory = new PipelineExecutorDelegateFactory();

            FakeFirstTarget target = new FakeFirstTarget();
            var lambda = factory.BuildLambda(target, target.GetType().GetMethod(nameof(FakeFirstTarget.Run)));

            Console.WriteLine(lambda.ToReadableString());
        }

        [Test]
        public void Manager_ReturnsDelegat2e()
        {

            JsonPipelineManager manager = new JsonPipelineManager(new FakeLogger(), new IJsonPipelineHandler[]
            {
                new FakeFirstTarget(),
                new FakeSecondTarget(),
                new FakeThirdTarget()
            });
            FakeContext context = new FakeContext(new Dictionary<string, object>()
            {
                {"id", 42},
                {"name", "Foo"},
                { "test", "x" }
            });
            manager.For(context, async fakeContext => new JObject()).Invoke(context);

        }


    }

    public class FakeLogger : ILogger
    {
        public Task LogAsync(string type, object customData = null)
        {
            Console.WriteLine(type);
            return Task.CompletedTask;
        }
    }

    public class FakeFirstTarget : IJsonPipelineHandler
    {
        [PropertyFilter("test", ".*")]
        public Task<JObject> Run(int id, string name, IJsonPipelineContext context, INext<int, string> next)
        {
            Console.WriteLine($"FakeFirstTarget.Run({id}, {name})");
            return next.Invoke();
        }
    }

    public class FakeSecondTarget : IJsonPipelineHandler
    {
        [PropertyFilter("test", ".*")]
        public Task<JObject> Run(int id, string name, IJsonPipelineContext context, INext<int, string> next)
        {
            Console.WriteLine($"FakeSecondTarget.Run({id}, {name})");
            return next.Invoke();
        }

    }

    public class FakeThirdTarget : IJsonPipelineHandler
    {
        [PropertyFilter("test", ".*")]
        public Task<JObject> Run(int id, string name, FakeContext context, INext<int, string> next)
        {
            Console.WriteLine($"FakeSecondTarget.Run({id}, {name})");
            return next.Invoke();
        }

    }

    public class FakeNextHandler<T1, T2> : INext<T1, T2>
    {
        public Task<JObject> Invoke()
        {
            Console.WriteLine($"FakeNextHandler.Invoke()");
            return Task.FromResult(new JObject());
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2)
        {
            Console.WriteLine($"FakeNextHandler.Invoke({arg1}, {arg2})");
            return Task.FromResult(new JObject());
        }
    }

    public class FakeContext : IJsonPipelineContext
    {
        private Dictionary<string, object> values;

        public FakeContext(Dictionary<string, object> values)
        {
            this.values = values;
        }

        public bool TryGetValue(string key, out string value)
        {
            if (values.TryGetValue(key, out object val) && val is string str)
            {
                value = str;
                return true;
            }

            value = null;
            return false;
        }

        public object GetParameter(string key)
        {
            return values[key];
        }

        public IJsonPipelineContext Replace(params (string key, object value)[] values)
        {
            throw new NotImplementedException();
        }
    }
}
