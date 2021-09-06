namespace DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers
{
    public static class NextFactory
    {

        public static INext<T1> Create<T1>(IPipelineContext context, INode next, string param1Name)
            => new Next<T1>(context, next, param1Name);

        public static INext<T1, T2> Create<T1, T2>(IPipelineContext context, INode next, string param1Name, string param2Name)
            => new Next<T1, T2>(context, next, param1Name, param2Name);

        public static INext<T1, T2, T3> Create<T1, T2, T3>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name)
            => new Next<T1, T2, T3>(context, next, param1Name, param2Name, param3Name);

        public static INext<T1, T2, T3, T4> Create<T1, T2, T3, T4>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name)
            => new Next<T1, T2, T3, T4>(context, next, param1Name, param2Name, param3Name, param4Name);

        public static INext<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name)
            => new Next<T1, T2, T3, T4, T5>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name);

        public static INext<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name)
            => new Next<T1, T2, T3, T4, T5, T6>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1, T2, T3, T4, T5, T6, T7, T8>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8, T9>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name, string param13Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name, param13Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name, string param13Name, string param14Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name, param13Name, param14Name);

        public static INext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(IPipelineContext context, INode next, string param1Name, string param2Name, string param3Name, string param4Name, string param5Name, string param6Name, string param7Name, string param8Name, string param9Name, string param10Name, string param11Name, string param12Name, string param13Name, string param14Name, string param15Name)
            => new Next<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(context, next, param1Name, param2Name, param3Name, param4Name, param5Name, param6Name, param7Name, param8Name, param9Name, param10Name, param11Name, param12Name, param13Name, param14Name, param15Name);

    }
}