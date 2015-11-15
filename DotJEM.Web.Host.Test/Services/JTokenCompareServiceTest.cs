using System.Collections;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Web.Host.Providers.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Services
{
    [TestFixture]
    public class JTokenCompareServiceTest
    {
        [TestCaseSource(nameof(NonConflictedMerges))]
        public void Merge_WithoutConflicts_AllowsUpdate(JToken update, JToken conflict, JToken parent, JToken expected)
        {
            IJTokenMergeVisitor service = new JTokenMergeVisitor();

            MergeResult result = service.Merge(update, conflict, parent);

            Assert.That(result.IsConflict, Is.False);
            Assert.That(result.Update.ToString(Formatting.Indented), Is.EqualTo(expected.ToString(Formatting.Indented)));

            //Assert.That(result, HAS.Property<MergeResult>(x => x.IsConflict).EqualTo(false)
            //    & HAS.Property<MergeResult>(x => x.Update).EqualTo(expected));
        }

        [TestCaseSource(nameof(NonConflictedMerges))]
        public void Merge_WithConflicts_DisallowsUpdate(JToken update, JToken conflict, JToken parent, JToken expected)
        {
            IJTokenMergeVisitor service = new JTokenMergeVisitor();

            MergeResult result = service.Merge(update, conflict, parent);

            Assert.That(result, HAS.Property<MergeResult>(x => x.IsConflict).EqualTo(false)
                & HAS.Property<MergeResult>(x => x.Update).EqualTo(expected));
        }


        public static IEnumerable NonConflictedMerges
        {
            get
            {
                yield return BuildCase(
                    "{ prop: 'what' }",
                    "{ prop: 'what' }", 
                    "{ prop: 'what' }",
                    "{ prop: 'what' }"
                    );

                yield return BuildCase(
                    "{ prop: 'what' }",
                    "{ prop: 'x' }",
                    "{ prop: 'what' }",
                    "{ prop: 'x' }"
                    );
                
                yield return BuildCase(
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }"
                    );

                yield return BuildCase(
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }"
                    );

                yield return BuildCase(
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }",
                    "{ prop: { a: 42, b: 'foo' } }",
                    "{ prop: { a: 42 } }"
                    );
            }
        }

        private static object[] BuildCase(string update = "{}", string conflict = "{}", string parent = "{}", string expected = "{}")
        {
            return new object[] { JToken.Parse(update), JToken.Parse(conflict), JToken.Parse(parent), JToken.Parse(expected) };
        }
    }
}


//        [Test, TestCaseSource("DivideCases")]
//        public void DivideTest(int n, int d, int q)
//        {
//            Assert.AreEqual(q, n / d);
//        }

//        static object[] DivideCases =
//        {
//    new object[] { 12, 3, 4 },
//    new object[] { 12, 2, 6 },
//    new object[] { 12, 4, 3 }
//};