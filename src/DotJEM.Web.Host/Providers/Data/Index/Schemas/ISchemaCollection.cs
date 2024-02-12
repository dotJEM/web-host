using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Providers.Data.Index.Schemas;

public interface ISchemaCollection : IEnumerable<JSchema>
{
    IEnumerable<string> ContentTypes { get; }
    JSchema this[string contentType] { get; set; }

    JsonSchemaExtendedType ExtendedType(string field);
    IEnumerable<Field> AllFields();
    IEnumerable<Field> Fields(string contentType);

    JSchema AddOrUpdate(string contentType, JSchema schema);
}

public class SchemaCollection : ISchemaCollection
{
    private readonly ConcurrentDictionary<string, JSchema> schemas = new ConcurrentDictionary<string, JSchema>();

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

    public JSchema AddOrUpdate(string contentType, JSchema schema)
    {
        if (contentType == null) throw new ArgumentNullException(nameof(contentType));
        if (schema == null) throw new ArgumentNullException(nameof(schema));

        schema.ContentType = contentType;
        schemas.AddOrUpdate(contentType,
            _ => schema,
            (_, jSchema) => jSchema.Merge(schema));

        return schema;
    }

    public IEnumerable<Field> AllFields()
    {
        return schemas.Values
            .SelectMany(s => s.Traverse())
            .Select(s => new Field(s.Field, s.ExtendedType))
            .Where(f => !string.IsNullOrEmpty(f.FullName))
            .Distinct();
    }

    public JsonSchemaExtendedType ExtendedType(string field)
    {
        return schemas.Aggregate(JsonSchemaExtendedType.None,
            (types, next) => next.Value.LookupExtentedType(field) | types);
    }

    public IEnumerable<Field> Fields(string contentType)
    {
        JSchema schema = this[contentType];

        return schema == null
            ? Enumerable.Empty<Field>()
            : schema.Traverse().Select(s => new Field(s.Field, s.ExtendedType))
                .Where(f => !string.IsNullOrEmpty(f.FullName));
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IEnumerator<JSchema> GetEnumerator()
        => schemas.Values.GetEnumerator();
}

public readonly struct Field
{
    public string FullName { get; }
    public JsonSchemaExtendedType Type { get; }

    public Field(string fullName, JsonSchemaExtendedType type)
    {
        FullName = fullName;
        Type = type;
    }
}