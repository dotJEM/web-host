using System.Collections;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services.DiffMerge.JTokenMergeVisitorTest
{
    [TestFixture]
    // ReSharper disable once InconsistentNaming
    public class JTokenMergeVisitorTest_SimpleConflictedMerges : AbstractJTokenMergeVisitorTest
    {
        [TestCaseSource(nameof(ConflictedMerges))]
        public void Merge_WithConflicts_DisallowsUpdate(JToken update, JToken conflict, JToken parent, JToken expected)
        {
            IJTokenMergeVisitor service = new JTokenMergeVisitor();

            MergeResult result = service.Merge(update, conflict, parent);

            Assert.That(result, HAS.Property<MergeResult>(x => x.IsConflict).True
                                & HAS.Property<MergeResult>(x => x.Diff).EqualTo(expected));
        }

        public IEnumerable ConflictedMerges
        {
            get
            {
                yield return Case(
                    "{ prop: 'hey' }",
                    "{ prop: 'ho' }",
                    "{ prop: 'what' }",
                    "{ prop: { origin: 'what', update: 'hey', conflict: 'ho' } }"
                    );

                yield return Case(
                    "{ prop: { a: 42, b: 'hey' } }",
                    "{ prop: { a: 42, b: 'ho' } }",
                    "{ prop: { a: 42, b: 'what' } }",
                    "{ 'prop.b': { origin: 'what', update: 'hey', conflict: 'ho' } }"
                    );
            }
        }
    }
}