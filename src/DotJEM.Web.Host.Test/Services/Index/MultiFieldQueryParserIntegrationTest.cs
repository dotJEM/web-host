using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Documents;
using DotJEM.Json.Index2.Documents.Builder;
using DotJEM.Json.Index2.Documents.Fields;
using DotJEM.Json.Index2.Documents.Strategies;
using DotJEM.Json.Index2.IO;
using DotJEM.Json.Index2.Storage;
using DotJEM.Web.Host.Providers.Data.Index;
using DotJEM.Web.Host.Providers.Data.Index.Builder;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services.Index;

public class MultiFieldQueryParserIntegrationTest
{
    [TestCase("test:foo AND arr.@count: [1 TO *]", "+test:foo +(+arr.@count:[1 TO *])")]
    [TestCase("test:353167C6-FEEB-4CF1-9E48-0A7B54979A68", "test:353167c6-feeb-4cf1-9e48-0a7b54979a68")]
    public void Parse_WithInput_SucceedsWithOutput(string input, string expected)
    {
        MultiFieldQueryParserIntegration parserIntegration = new (
            new JsonIndexConfiguration(LuceneVersion.LUCENE_48, new List<ServiceDescriptor>()), 
            new QueryParserConfiguration(), new SchemaCollection());
        Query q = parserIntegration.Parse(input);
        Assert.That(q.ToString(), Is.EqualTo(expected));
    }


}

public class IndexIntegrationTest
{
    [Test]
    public void Search_SlashField_ReturnsResults()
    {
        IJsonIndex index = CreateIndexWith(
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000001"                      , users = new[] { "domain\\joe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000001/ORG-000002"           , users = new[] { "domain\\joe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000001/ORG-000002/ORG-000003", users = new[] { "domain\\joe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000001/ORG-000002/ORG-000004", users = new[] { "domain\\joe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000001/ORG-000005"           , users = new[] { "domain\\joe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000001/ORG-000005/ORG-000006", users = new[] { "domain\\joe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000001/ORG-000005/ORG-000007", users = new[] { "domain\\joe", "domain\\test" } },

            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000011",                       users = new[] { "domain\\poe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000011/ORG-000012",            users = new[] { "domain\\poe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000011/ORG-000012/ORG-000013", users = new[] { "domain\\poe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000011/ORG-000012/ORG-000014", users = new[] { "domain\\poe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000011/ORG-000015",            users = new[] { "domain\\poe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000011/ORG-000015/ORG-000016", users = new[] { "domain\\poe", "domain\\test" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000011/ORG-000015/ORG-000017", users = new[] { "domain\\poe", "domain\\test" } },

            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000021"                      , users = new[] { "domain\\alice", "domain\\tour" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000021/ORG-000022"           , users = new[] { "domain\\alice", "domain\\tour" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000021/ORG-000022/ORG-000023", users = new[] { "domain\\alice", "domain\\tour" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000021/ORG-000022/ORG-000024", users = new[] { "domain\\alice", "domain\\tour" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000021/ORG-000025"           , users = new[] { "domain\\alice", "domain\\tour" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000021/ORG-000025/ORG-026"   , users = new[] { "domain\\alice", "domain\\tour" } },
            new { id = Guid.NewGuid(), contentType = "test", identifier = "ORG-000021/ORG-000025/ORG-000027", users = new[] { "domain\\alice", "domain\\tour" } }
        );

        Console.WriteLine(index.Search("contentType:test").Take(30).Execute().TotalHits);

        Console.WriteLine(index.Search("identifier:ORG-000021\\/ORG-000025\\/ORG-000027").Take(30).Execute().TotalHits);
        Console.WriteLine(index.Search("identifier:ORG-000021\\/ORG-000025\\/ORG-000027*").Take(30).Execute().TotalHits);
        Console.WriteLine(index.Search("identifier:ORG-000021\\/ORG-000025*").Take(30).Execute().TotalHits);
        Console.WriteLine(index.Search("identifier:ORG-000021*").Take(30).Execute().TotalHits);
        Console.WriteLine(index.Search("identifier:ORG-000001\\/ORG-000002").Take(30).Execute().TotalHits);

        Console.WriteLine(index.Search("users:domain\\\\alice").Take(30).Execute().TotalHits);


    }

    private IJsonIndex CreateIndexWith(params object[] objects)
        => CreateIndexWith(objects.Select(JObject.FromObject));

    private IJsonIndex CreateIndexWith(IEnumerable<JObject> objects)
    {
        IQueryParserConfiguration parserConfig = new QueryParserConfiguration();
        parserConfig.Field("users", new TermFieldStrategy());
        ISchemaCollection schemas = new SchemaCollection();


        JsonIndexBuilder builder = new JsonIndexBuilder("Test");
        builder.UsingMemmoryStorage();
        builder.WithAnalyzer(c => new JsonAnalyzer(c.Version));
        builder.WithClassicLuceneQueryParser(schemas, parserConfig);
        IFactory<ILuceneDocumentBuilder> factory = new FuncFactory<ILuceneDocumentBuilder>(() => new TestDocumentBuilder());
        builder.WithDocumentFactory(c => new WebHostLuceneDocumentFactory(new LuceneDocumentFactory(c.FieldInformationManager,factory), schemas));
        builder.WithFieldResolver(new FieldResolver("id", "contentType"));
        IJsonIndex index = builder.Build();
        using IJsonIndexWriter writer = index.CreateWriter();
        foreach (JObject obj in objects)
            writer.Create(obj);
        writer.Commit();
        return index;
    }
}
public class TestDocumentBuilder : LuceneDocumentBuilder
{
    protected override void VisitGuid(JValue json, IPathContext context)
    {
        this.Add(new IdentityFieldStrategy().CreateFields(json, context));
    }

    protected override void VisitBoolean(JValue json, IPathContext context)
    {
        Add(new TextFieldStrategy().CreateFields(json, context));
    }

    protected override void VisitString(JValue json, IPathContext context)
    {
        string value = (string)json;
        if (value?.Length == 36 && Guid.TryParse(value, out _))
            base.VisitGuid(json, context);
        else
            base.VisitString(json, context);
    }

    protected override void Visit(JValue json, IPathContext context)
    {
        switch (context.Path)
        {
            case "users":
            case "identifier":
                this.Add(new IdentityFieldStrategy()
                    .CreateFields(json.ToObject<string>().ToLowerInvariant(), context));
                break;

            default:
                base.Visit(json, context);
                break;
        }




    }
}
public class JsonAnalyzer : Analyzer
{
    public LuceneVersion Version { get; }

    public int MaxTokenLength { get; set; } = 4096;

    public JsonAnalyzer(LuceneVersion version) => this.Version = version;

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        ClassicTokenizer classicTokenizer = new ClassicTokenizer(this.Version, reader);
        classicTokenizer.MaxTokenLength = this.MaxTokenLength;
        TokenStream tok = new LowerCaseFilter(this.Version, new ClassicFilter(classicTokenizer));
        return new TokenStreamComponentsAnonymousClass(this, classicTokenizer, tok);
    }

    private sealed class TokenStreamComponentsAnonymousClass : TokenStreamComponents
    {
        private readonly JsonAnalyzer analyzer;
        private readonly ClassicTokenizer src;

        public TokenStreamComponentsAnonymousClass(
            JsonAnalyzer analyzer,
            ClassicTokenizer src,
            TokenStream tok)
            : base((Tokenizer)src, tok)
        {
            this.analyzer = analyzer;
            this.src = src;
        }

        protected override void SetReader(TextReader reader)
        {
            this.src.MaxTokenLength = this.analyzer.MaxTokenLength;
            base.SetReader(reader);
        }
    }
}