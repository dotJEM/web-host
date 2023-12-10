using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index;
using DotJEM.Json.Index2;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services;

public interface ITermService
{
    JObject Get(string contentType, string field);
}

public class TermService : ITermService
{
    private readonly IJsonIndex index;

    public TermService(IJsonIndex index)
    {
        this.index = index;
    }

    public JObject Get(string contentType, string field)
    {
        if (field == null) 
            throw new ArgumentNullException("field");

        if (contentType == null)
            throw new ArgumentNullException("contentType");

        if(string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("field was empty or only had whitespaces.","field");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("contentType was empty or only had whitespaces.", "field");


        DirectoryReader reader = index.WriterManager.Writer.GetReader(true);
        Fields fields = MultiFields.GetFields(reader);
        if (fields != null)
        {
            Terms terms = fields.GetTerms(field);
            return new JObject()
            {
                ["terms"] = JArray.FromObject(terms.AsEnumerable()
                    .Select(bytes => bytes.Utf8ToString())
                    .ToArray())
            };
        }

        return new JObject();
    }
}

public static class TermsEnumEnumerable
{
    public static IEnumerator<BytesRef> GetEnumerator(this TermsEnum self)
        => new BytesRefEnumerator(self);

    public static IEnumerable<BytesRef> AsEnumerable(this Terms self)
    {
        foreach (BytesRef bytesRef in self.GetEnumerator())
            yield return bytesRef;
    }
}

public class BytesRefEnumerator : IEnumerator<BytesRef>
{
    private readonly IBytesRefEnumerator iterator;

    public BytesRefEnumerator(IBytesRefEnumerator iterator)
    {
        this.iterator = iterator;
    }

    public void Dispose()
    {

    }

    public bool MoveNext() => iterator.MoveNext();

    public void Reset()
    {
        throw new InvalidOperationException();
    }

    public BytesRef Current => iterator.Current;

    object IEnumerator.Current => Current;
}