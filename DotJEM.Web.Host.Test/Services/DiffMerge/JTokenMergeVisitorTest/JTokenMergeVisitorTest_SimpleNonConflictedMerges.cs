using System.Collections;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services.DiffMerge.JTokenMergeVisitorTest
{
    [TestFixture]
    // ReSharper disable once InconsistentNaming
    public class JTokenMergeVisitorTest_SimpleNonConflictedMerges : AbstractJTokenMergeVisitorTest
    {
        [TestCaseSource(nameof(NonConflictedMerges))]
        public void Merge_WithoutConflicts_AllowsUpdate(JToken update, JToken conflict, JToken parent, JToken expected)
        {
            IJsonMergeVisitor service = new JsonMergeVisitor();

            IMergeResult result = service.Merge(update, conflict, parent);


            Assert.That(result,
                HAS.Property<MergeResult>(x => x.HasConflicts).False
                & HAS.Property<MergeResult>(x => x.Merged).EqualTo(expected));
        }

        public IEnumerable NonConflictedMerges
        {
            get
            {
                yield return Case(
                    "{ prop: 'what' }",
                    "{ prop: 'what' }",
                    "{ prop: 'what' }",
                    "{ prop: 'what' }"
                    );

                yield return Case(
                    "{ prop: 'what' }",
                    "{ prop: 'x' }",
                    "{ prop: 'what' }",
                    "{ prop: 'x' }"
                    );

                yield return Case(
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }"
                    );

                yield return Case(
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }"
                    );

                yield return Case(
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }"
                    );
            }
        }
    }

    public class JTokenMergeVisitor_SimpleNonConflictedMerges_TestData : AbstractJTokenMergeVisitorTest
    {
        public IEnumerable Cases
        {
            get
            {
                yield return Case(
                    "{ prop: 'what' }",
                    "{ prop: 'what' }",
                    "{ prop: 'what' }",
                    "{ prop: 'what' }"
                    );

                yield return Case(
                    "{ prop: 'what' }",
                    "{ prop: 'x' }",
                    "{ prop: 'what' }",
                    "{ prop: 'x' }"
                    );

                yield return Case(
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }"
                    );

                yield return Case(
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }"
                    );

                yield return Case(
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }"
                    );
            }
        }
    }
}