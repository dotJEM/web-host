using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers
{


    public interface INext<TResult, in T1> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1);
    }

    public class Next<TResult, T1> : Next<TResult>, INext<TResult, T1>
    {
        private readonly string arg1Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
        }

        public Task<TResult> Invoke(T1 arg1) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1)));
    }

    public interface INext<TResult, in T1, in T2> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2);
    }

    public class Next<TResult, T1, T2> : Next<TResult>, INext<TResult, T1, T2>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
        }

        public Task<TResult> Invoke(T1 arg1, T2 arg2) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2)));
    }

    public interface INext<TResult, in T1, in T2, in T3> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3);
    }

    public class Next<TResult, T1, T2, T3> : Next<TResult>, INext<TResult, T1, T2, T3>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
        }

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    public class Next<TResult, T1, T2, T3, T4> : Next<TResult>, INext<TResult, T1, T2, T3, T4>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
        }

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }

    public class Next<TResult, T1, T2, T3, T4, T5> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
        }

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
            this.arg3Name = arg3Name;
            this.arg4Name = arg4Name;
            this.arg5Name = arg5Name;
            this.arg6Name = arg6Name;
        }

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;
        private readonly string arg3Name;
        private readonly string arg4Name;
        private readonly string arg5Name;
        private readonly string arg6Name;
        private readonly string arg7Name;
        private readonly string arg8Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>
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

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
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

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
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

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
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

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
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

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name, string arg13Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12), (arg13Name, arg13)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
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

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name, string arg13Name, string arg14Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12), (arg13Name, arg13), (arg14Name, arg14)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
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

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name, string arg13Name, string arg14Name, string arg15Name)
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

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12), (arg13Name, arg13), (arg14Name, arg14), (arg15Name, arg15)));
    }

    public interface INext<TResult, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16> : INext<TResult>
    {
        Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
    }

    public class Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : Next<TResult>, INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
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
        private readonly string arg16Name;

        public Next(IPipelineContext context, INode<TResult> next, string arg1Name, string arg2Name, string arg3Name, string arg4Name, string arg5Name, string arg6Name, string arg7Name, string arg8Name, string arg9Name, string arg10Name, string arg11Name, string arg12Name, string arg13Name, string arg14Name, string arg15Name, string arg16Name)
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
            this.arg16Name = arg16Name;
        }

        public Task<TResult> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2), (arg3Name, arg3), (arg4Name, arg4), (arg5Name, arg5), (arg6Name, arg6), (arg7Name, arg7), (arg8Name, arg8), (arg9Name, arg9), (arg10Name, arg10), (arg11Name, arg11), (arg12Name, arg12), (arg13Name, arg13), (arg14Name, arg14), (arg15Name, arg15), (arg16Name, arg16)));
    }

    public static class NextFactory
    {

        public static INext<TResult, T1> Create<TResult, T1>(IPipelineContext context, INode<TResult> next, string param1Name)
            => new Next<TResult, T1>(context, next, param1Name);

        public static INext<TResult, T1, T2> Create<TResult, T1, T2>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name)
            => new Next<TResult, T1, T2>(context, next, param1Name, param2Name);

        public static INext<TResult, T1, T2, T3> Create<TResult, T1, T2, T3>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name)
            => new Next<TResult, T1, T2, T3>(context, next, param1Name, param2Name, param3Name);

        public static INext<TResult, T1, T2, T3, T4> Create<TResult, T1, T2, T3, T4>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name)
            => new Next<TResult, T1, T2, T3, T4>(context, next, param1Name, param2Name, param3Name, param4Name);

        public static INext<TResult, T1, T2, T3, T4, T5> Create<TResult, T1, T2, T3, T4, T5>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name)
            => new Next<TResult, T1, T2, T3, T4, T5>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6> Create<TResult, T1, T2, T3, T4, T5, T6>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7> Create<TResult, T1, T2, T3, T4, T5, T6, T7>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name, string param13Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name, param13Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name, string param13Name, string param14Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name, param13Name, param14Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name, string param13Name, string param14Name, string param15Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name, param13Name, param14Name, param15Name);

        public static INext<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Create<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(IPipelineContext context, INode<TResult> next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name, string param13Name, string param14Name, string param15Name, string param16Name)
            => new Next<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name, param13Name, param14Name, param15Name, param16Name);

    }




}