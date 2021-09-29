using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AgileObjects.ReadableExpressions;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;
using DotJEM.Web.Host.Providers.AsyncPipeline.Factories;
using DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers;
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
            PipelineExecutorDelegate<JObject> action = factory.CreateInvocator<JObject>(target, target.GetType().GetMethod(nameof(FakeFirstTarget.Run)));
            FakeContext context = new FakeContext(new Dictionary<string, object>()
            {
                {"id", 42},
                {"name", "Foo"}
            });
            action(context, new FakeNextHandler<JObject, int, string>());
        }

        [Test]
        public void CreateInvocator_ReturnsDelegat2e()
        {

            PipelineExecutorDelegateFactory factory = new PipelineExecutorDelegateFactory();


            FakeFirstTarget target = new FakeFirstTarget();
            Expression<NextFactoryDelegate<JObject>> exp = factory.CreateNextStuff<JObject>(target.GetType().GetMethod(nameof(FakeFirstTarget.Run)));

            Console.WriteLine(exp.ToReadableString());

            exp.Compile();

        }

        [Test]
        public void Manager_ReturnsDelegat2e()
        {

            PipelineManager manager = new PipelineManager(new FakeLogger(), new PipelineHandlerCollection(new IPipelineHandler[]
            {
                new FakeFirstTarget(),
                new FakeSecondTarget(),
                new FakeThirdTarget()
            }));
            FakeContext context = new FakeContext(new Dictionary<string, object>()
            {
                {"id", 42},
                {"name", "Foo"},
                { "test", "x" }
            });
            manager.For(context, async fakeContext => new JObject()).Invoke();
            manager.For(context, async fakeContext => new JObject()).Invoke();
            manager.For(context, async fakeContext => new JObject()).Invoke();
            manager.For(context, async fakeContext => new JObject()).Invoke();

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

    public class FakeFirstTarget : IPipelineHandler
    {
        [PropertyFilter("test", ".*")]
        public Task<JObject> Run(int id, string name, IPipelineContext context, INext<JObject, int, string> next)
        {
            Console.WriteLine($"FakeFirstTarget.Run({id}, {name})");
            return next.Invoke();
        }
    }

        [PropertyFilter("name", ".*")]
    public class FakeSecondTarget : IPipelineHandler
    {
        [PropertyFilter("test", ".*")]
        public Task<JObject> Run(int id, string name, IPipelineContext context, INext<JObject, int, string> next)
        {
            Console.WriteLine($"FakeSecondTarget.Run({id}, {name})");
            return next.Invoke(50, "OPPS");
        }

    }

    public class FakeThirdTarget : IPipelineHandler
    {
        [PropertyFilter("test", ".*")]
        public Task<JObject> Run(int id, string name, FakeContext context, INext<JObject, int, string> next)
        {
            Console.WriteLine($"FakeSecondTarget.Run({id}, {name})");
            return next.Invoke();
        }

    }

    public class FakeNextHandler<TResult, T1, T2> : INext<TResult, T1, T2>
    {
        public Task<TResult> Invoke()
        {
            Console.WriteLine($"FakeNextHandler.Invoke()");
            return Task.FromResult(default(TResult));
        }

        public Task<TResult> Invoke(T1 arg1, T2 arg2)
        {
            Console.WriteLine($"FakeNextHandler.Invoke({arg1}, {arg2})");
            return Task.FromResult(default(TResult));
        }
    }

    public class FakeContext : IPipelineContext
    {
        private readonly Dictionary<string, object> values;

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

        public object this[string key]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out object value) => values.TryGetValue(key, out value);

        public object GetParameter(string key)
        {
            return values[key];
        }

        public IPipelineContext Replace(params (string key, object value)[] values)
        {
            foreach ((string key, object value)  in values)
                this.values[key] = value;
            return this;
        }

        public IPipelineContext Add(string key, object value)
        {
            throw new NotImplementedException();
        }

        public IPipelineContext Set(string key, object value)
        {
            throw new NotImplementedException();
        }
    }
}
