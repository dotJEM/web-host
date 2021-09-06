using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers
{
    public interface INext
    {
        Task<JObject> Invoke();
    }

    public class Next : INext
    {
        protected INode NextNode { get; }
        protected IPipelineContext Context { get; }

        public Next(IPipelineContext context, INode next)
        {
            this.Context = context;
            this.NextNode = next;
        }

        public Task<JObject> Invoke() => NextNode.Invoke(Context);
    }
    

    public interface INext<in T1> : INext
    {
        Task<JObject> Invoke(T1 arg1);
    }

    public class Next<T1> : Next, INext<T1>
    {
        private readonly string arg1Name;

        public Next(IPipelineContext context, INode next, string arg1Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
        }

        public Task<JObject> Invoke(T1 arg1) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1)));
    }

    public interface INext<in T1, in T2> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2);
    }

    public class Next<T1, T2> : Next, INext<T1, T2>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2)));
    }

    public interface INext<in T1, in T2, in T3> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3);
    }

    public class Next<T1, T2, T3> : Next, INext<T1, T2, T3>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3)));
    }

    public interface INext<in T1, in T2, in T3, in T4> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    public class Next<T1, T2, T3, T4> : Next, INext<T1, T2, T3, T4>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }

    public class Next<T1, T2, T3, T4, T5> : Next, INext<T1, T2, T3, T4, T5>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    }

    public class Next<T1, T2, T3, T4, T5, T6> : Next, INext<T1, T2, T3, T4, T5, T6>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7> : Next, INext<T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8, T9> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;
        private readonly string arg9Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
            this.arg9Name = arg9Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;
        private readonly string arg9Name;
        private readonly string arg10Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
            this.arg9Name = arg9Name;
            this.arg10Name = arg10Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;
        private readonly string arg9Name;
        private readonly string arg10Name;
        private readonly string arg11Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
            this.arg9Name = arg9Name;
            this.arg10Name = arg10Name;
            this.arg11Name = arg11Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;
        private readonly string arg9Name;
        private readonly string arg10Name;
        private readonly string arg11Name;
        private readonly string arg12Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
            this.arg9Name = arg9Name;
            this.arg10Name = arg10Name;
            this.arg11Name = arg11Name;
            this.arg12Name = arg12Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;
        private readonly string arg9Name;
        private readonly string arg10Name;
        private readonly string arg11Name;
        private readonly string arg12Name;
        private readonly string arg13Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name, string arg13Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
            this.arg9Name = arg9Name;
            this.arg10Name = arg10Name;
            this.arg11Name = arg11Name;
            this.arg12Name = arg12Name;
            this.arg13Name = arg13Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12), (arg13Name, arg13)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;
        private readonly string arg9Name;
        private readonly string arg10Name;
        private readonly string arg11Name;
        private readonly string arg12Name;
        private readonly string arg13Name;
        private readonly string arg14Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name, string arg13Name, string arg14Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
            this.arg9Name = arg9Name;
            this.arg10Name = arg10Name;
            this.arg11Name = arg11Name;
            this.arg12Name = arg12Name;
            this.arg13Name = arg13Name;
            this.arg14Name = arg14Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12), (arg13Name, arg13), (arg14Name, arg14)));
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    }

    public class Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : Next, INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;
        private readonly string arg9Name;
        private readonly string arg10Name;
        private readonly string arg11Name;
        private readonly string arg12Name;
        private readonly string arg13Name;
        private readonly string arg14Name;
        private readonly string arg15Name;

        public Next(IPipelineContext context, INode next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name, string arg13Name, string arg14Name, string arg15Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
            this.arg7Name = arg7Name;
            this.arg8Name = arg8Name;
            this.arg9Name = arg9Name;
            this.arg10Name = arg10Name;
            this.arg11Name = arg11Name;
            this.arg12Name = arg12Name;
            this.arg13Name = arg13Name;
            this.arg14Name = arg14Name;
            this.arg15Name = arg15Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12), (arg13Name, arg13), (arg14Name, arg14), (arg15Name, arg15)));
    }


}