using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Web.Host.Providers.Data.Index;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services.Index
{
    public class MultiFieldQueryParserTest
    {
        [Test]
        public void Parse_OpenRange_Success()
        {
            MultiFieldQueryParser parser = new (new JsonIndexConfiguration(LuceneVersion.LUCENE_48, Array.Empty<ServiceDescriptor>()), new QueryParserConfiguration(), new SchemaCollection());

            Query q = parser.Parse("test:foo AND arr.@count: [1 TO *]");

            Assert.That(q.ToString(), Is.EqualTo("+test:foo +(+arr.@count:[1 TO *])"));
        }
    }
}
