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
    public class JTokenMergeVisitorTest_SimpleArrayHandling : AbstractJTokenMergeVisitorTest
    {
        [Test]
        public void Merge_ScrambledIntArray_IsConflicted()
        {
            JToken update = Json("{ arr: [1,2,3] }");
            JToken conflict = Json("{ arr: [2,3,1] }");
            JToken origin = Json("{ arr: [3,1,2] }");
            JToken diff = Json("{ arr: { origin: [3,1,2], conflict: [2,3,1], update: [1,2,3] } }");

            IJTokenMergeVisitor visitor = new JTokenMergeVisitor();

            IMergeResult result = visitor.Merge(update, conflict, origin);

            Assert.That(result, HAS.Property<MergeResult>(x => x.HasConflicts).True
                                & HAS.Property<MergeResult>(x => x.Conflicts).EqualTo(diff));
        }
        [Test]
        public void Merge_TypeMismatchArray_IsConflicted()
        {
            JToken update = Json("{ arr: [1,2,3] }");
            JToken conflict = Json("{ arr: [2,3,1] }");
            JToken origin = Json("{ arr: 'foo' }");
            JToken diff = Json("{ arr: { origin: 'foo', conflict: [2,3,1], update: [1,2,3] } }");

            IJTokenMergeVisitor visitor = new JTokenMergeVisitor();

            IMergeResult result = visitor.Merge(update, conflict, origin);

            string a = result.Conflicts.ToString(Formatting.Indented);
            string b = diff.ToString(Formatting.Indented);

            Assert.That(result, HAS.Property<MergeResult>(x => x.HasConflicts).True
                                & HAS.Property<MergeResult>(x => x.Conflicts).EqualTo(diff));
        }

        [TestCaseSource(nameof(NonConflictedMerges))]
        public void Merge_WithoutConflicts_Merges(JToken update, JToken conflict, JToken parent, JToken expected)
        {
            IJTokenMergeVisitor service = new JTokenMergeVisitor();

            IMergeResult result = service.Merge(update, conflict, parent);

            Assert.That(result, HAS.Property<MergeResult>(x => x.HasConflicts).EqualTo(false)
                                & HAS.Property<MergeResult>(x => x.Merged).EqualTo(expected));
        }

        public IEnumerable NonConflictedMerges
        {
            get
            {
                //Note: All equals
                yield return Case(
                    "{ arr: [1,2,3] }",
                    "{ arr: [1,2,3] }",
                    "{ arr: [1,2,3] }",
                    "{ arr: [1,2,3] }"
                    );

                //Note: Only update changed
                yield return Case(
                    "{ arr: [2,3] }",
                    "{ arr: [1,2,3] }",
                    "{ arr: [1,2,3] }",
                    "{ arr: [2,3] }"
                    );

                //Note: Only conflict changed
                yield return Case(
                    "{ arr: [1,2,3] }",
                    "{ arr: [2,3] }",
                    "{ arr: [1,2,3] }",
                    "{ arr: [2,3] }"
                    );

                //Note: Both changed, added item
                yield return Case(
                    "{ arr: [1,2,3] }",
                    "{ arr: [1,2,3] }",
                    "{ arr: [2,3] }",
                    "{ arr: [1,2,3] }"
                    );

                //Note: Both changed, removed item
                yield return Case(
                    "{ arr: [2,3] }",
                    "{ arr: [2,3] }",
                    "{ arr: [1,2,3] }",
                    "{ arr: [2,3] }"
                    );
            }
        }
    }
}