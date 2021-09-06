using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;
using DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Factories
{
    public interface IPipelineExecutorDelegateFactory
    {
        MethodNode CreateNode(object target, MethodInfo method, PipelineFilterAttribute[] filters);
        PipelineExecutorDelegate CreateInvocator(object target, MethodInfo method);
        Expression<PipelineExecutorDelegate> BuildLambda(object target, MethodInfo method);
        NextFactoryDelegate CreateNextFactoryDelegate(MethodInfo method);
        Expression<NextFactoryDelegate> CreateNextStuff(MethodInfo method);
    }

    public class PipelineExecutorDelegateFactory : IPipelineExecutorDelegateFactory
    {
        private static readonly MethodInfo contextParameterGetter = typeof(IPipelineContext).GetMethod("GetParameter");

        public MethodNode CreateNode(object target, MethodInfo method, PipelineFilterAttribute[] filters)
        {
            PipelineExecutorDelegate @delegate = CreateInvocator(target, method);
            NextFactoryDelegate nextFactory = CreateNextFactoryDelegate(method);

            string parameters = string.Join(", ", method.GetParameters().Select(param => $"{param.ParameterType.Name} {param.Name}"));
            string signature = $"{ target.GetType().Name}.{method.Name}({parameters})";
            return new MethodNode(filters, @delegate, nextFactory, signature);
        }

        public PipelineExecutorDelegate CreateInvocator(object target, MethodInfo method)
        {
            Expression<PipelineExecutorDelegate> lambda = BuildLambda(target, method);
            return lambda.Compile();
        }

        public Expression<PipelineExecutorDelegate> BuildLambda(object target, MethodInfo method)
        {
            ConstantExpression targetParameter = Expression.Constant(target);
            ParameterExpression contextParameter = Expression.Parameter(typeof(IPipelineContext), "context");
            ParameterExpression nextParameter = Expression.Parameter(typeof(INext), "next");

            // context.GetParameter("first"), ..., context, (INextHandler<...>) next);
            List<Expression> parameters = BuildParameterList(method, contextParameter, nextParameter);
            UnaryExpression convertTarget = Expression.Convert(targetParameter, target.GetType());
            MethodCallExpression methodCall = Expression.Call(convertTarget, method, parameters);
            UnaryExpression castMethodCall = Expression.Convert(methodCall, typeof(Task<JObject>));
            return Expression.Lambda<PipelineExecutorDelegate>(castMethodCall, contextParameter, nextParameter);
        }

        private List<Expression> BuildParameterList(MethodInfo method, Expression contextParameter, Expression nextParameter)
        {
            // Validate that method's signature ends with Context and Next.
            ParameterInfo[] list = method.GetParameters();
            ParameterInfo contextParameterInfo = list[list.Length - 2];

            if (contextParameterInfo.ParameterType != typeof(IPipelineContext))
                contextParameter = Expression.Convert(contextParameter, contextParameterInfo.ParameterType);

            return list
                .Take(list.Length - 2)
                .Select(info =>
                {
                    // context.GetParameter("name");
                    MethodCallExpression call = Expression.Call(contextParameter, contextParameterGetter, Expression.Constant(info.Name));

                    // (parameterType) context.GetParameter("name"); 
                    return (Expression)Expression.Convert(call, info.ParameterType);
                })
                .Append(contextParameter)
                .Append(Expression.Convert(nextParameter, list.Last().ParameterType))
                .ToList();
        }



        public NextFactoryDelegate CreateNextFactoryDelegate(MethodInfo method)
        {
            Expression<NextFactoryDelegate> lambda = CreateNextStuff(method);
            return lambda.Compile();
        }

        public Expression<NextFactoryDelegate> CreateNextStuff(MethodInfo method)
        {
            ParameterInfo[] list = method.GetParameters();
            ParameterInfo nextParameterInfo = list[list.Length - 1];
            Type[] generics = nextParameterInfo.ParameterType.GetGenericArguments();

            ParameterExpression contextParameter = Expression.Parameter(typeof(IPipelineContext), "context");
            ParameterExpression nodeParameter = Expression.Parameter(typeof(INode), "node");

            Expression[] arguments = list
                .Take(list.Length - 2)
                .Select(p => (Expression)Expression.Constant(p.Name))
                .Prepend(nodeParameter)
                .Prepend(contextParameter)
                .ToArray();
            MethodCallExpression methodCall = Expression.Call(typeof(NextFactory), nameof(NextFactory.Create), generics, arguments);

            return Expression.Lambda<NextFactoryDelegate>(methodCall, contextParameter, nodeParameter);
        }

        //public static class NextFactory
        //{
        //    public static INext<T> Create<T>(IPipelineContext context, INode next, string paramName)
        //        => new Next<T>(context, next, paramName);
        //    public static INext<T1, T2> Create<T1, T2>(IPipelineContext context, INode next, string paramName1, string paramName2)
        //        => new Next<T1, T2>(context, next, paramName1, paramName2);
        //}
    }
}