using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Providers.Index.Schemas
{
    public interface ISchemaCollection : IEnumerable<JSchema>
    {
        IEnumerable<string> ContentTypes { get; }
        JSchema this[string contentType] { get; set; }

        JsonSchemaExtendedType ExtendedType(string field);
        IEnumerable<string> AllFields();
        IEnumerable<string> Fields(string contentType);

        JSchema Add(string contentType, JSchema schema);
    }

    public class SchemaCollection : ISchemaCollection
    {
        private readonly IDictionary<string, JSchema> schemas = new ConcurrentDictionary<string, JSchema>();

        public IEnumerable<string> ContentTypes => schemas.Keys;

        public JSchema this[string contentType]
        {
            get => schemas.TryGetValue(contentType, out JSchema schema) ? schema : null;
            set
            {
                if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentNullException(nameof(contentType));
                schemas[contentType] = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public JSchema Add(string contentType, JSchema schema)
        {
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            schema.ContentType = contentType;
            if (schemas.ContainsKey(contentType))
            {
                return this[contentType] = this[contentType].Merge(schema);
            }
            schemas.Add(contentType, schema);
            return schema;
        }

        public IEnumerable<string> AllFields()
        {
            return schemas.Values
                .SelectMany(s => s.Traverse())
                .Select(s => s.Field)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct();
        }

        public JsonSchemaExtendedType ExtendedType(string field)
        {
            return schemas.Aggregate(JsonSchemaExtendedType.None,
                (types, next) => next.Value.LookupExtentedType(field) | types);
        }

        public IEnumerable<string> Fields(string contentType)
        {
            JSchema schema = this[contentType];

            return schema == null
                ? Enumerable.Empty<string>()
                : schema.Traverse().Select(s => s.Field).Where(f => !string.IsNullOrEmpty(f));
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IEnumerator<JSchema> GetEnumerator()
            => schemas.Values.GetEnumerator();
    }
}