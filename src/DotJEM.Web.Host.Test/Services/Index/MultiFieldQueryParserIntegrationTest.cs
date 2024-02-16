using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Web.Host.Providers.Data.Index;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services.Index;

public class MultiFieldQueryParserIntegrationTest
{
    [TestCase("test:foo AND arr.@count: [1 TO *]", "+test:foo +(+arr.@count:[1 TO *])")]
    [TestCase("test:353167C6-FEEB-4CF1-9E48-0A7B54979A68", "test:353167c6-feeb-4cf1-9e48-0a7b54979a68")]
    public void Parse_WithInput_SucceedsWithOutput(string input, string expected)
    {
        ServiceDescriptor descriptor = new ServiceDescriptor
        {
            Type = typeof(Analyzer),
            Factory = c => new ClassicAnalyzer(c.Version, CharArraySet.EMPTY_SET)
        };
        MultiFieldQueryParserIntegration parserIntegration = new (
            new JsonIndexConfiguration(LuceneVersion.LUCENE_48, new []{ descriptor }), 
            new QueryParserConfiguration(), new SchemaCollection());
        Query q = parserIntegration.Parse(input);
        Assert.That(q.ToString(), Is.EqualTo(expected));
    }
}