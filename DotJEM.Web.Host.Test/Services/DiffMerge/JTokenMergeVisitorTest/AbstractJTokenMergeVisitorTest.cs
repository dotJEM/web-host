using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services.DiffMerge.JTokenMergeVisitorTest
{
    public abstract class AbstractJTokenMergeVisitorTest
    {
        public JToken Json(object json)
        {
            string jsonStr = json as string;
            if(jsonStr != null)
                return JToken.Parse(jsonStr);

            JToken jsonToken = json as JToken;
            if (jsonToken != null)
                return jsonToken;

            return JToken.FromObject(json);
        }

        //public object[] Case(params string[] args)
        //{
        //    return args.Select(Json).Cast<object>().ToArray();
        //}

        public TestCaseData Case(params string[] args)
        {
            return new TestCaseData(args.Select(Json).Cast<object>().ToArray());
        }
    }
}