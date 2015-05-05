using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DotJEM.Web.Host.Util
{
    public interface IJsonConverter
    {
        JToken FromObject(object obj);
        JObject ToJObject(object obj);
        JArray ToJArray(object obj);
    }

    public class DotjemJsonConverter : IJsonConverter
    {
        private readonly JsonSerializer serializer = new JsonSerializer();

        public DotjemJsonConverter()
        {
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.Converters.Add(new StringEnumConverter { CamelCaseText = true });
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            JObject.FromObject(new {}, serializer);
            JArray.FromObject(new int[0], serializer);
        }

        public JToken FromObject(object obj)
        {
            using (JTokenWriter writer = new JTokenWriter())
            {
                serializer.Serialize(writer, obj);
                return writer.Token;
            }
        }
        public JObject ToJObject(object obj)
        {
            JToken jtoken = FromObject(obj);
            if (jtoken != null && jtoken.Type != JTokenType.Object)
                throw new ArgumentException(string.Format("Object serialized to {0}. JObject instance expected.", jtoken.Type));
            return (JObject)jtoken;
        }

        public JArray ToJArray(object obj)
        {
            JToken jtoken = FromObject(obj);
            if (jtoken.Type != JTokenType.Array)
                throw new ArgumentException(string.Format("Object serialized to {0}. JArray instance expected.", jtoken.Type));
            return (JArray)jtoken;
        }
    }
}