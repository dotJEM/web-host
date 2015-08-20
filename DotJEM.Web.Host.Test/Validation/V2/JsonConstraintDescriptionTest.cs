using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    [TestFixture]
    public class JsonConstraintDescriptionTest
    {
        [Test]
        public void ToString_FakeObject_ShouldReturnErrors()
        {
            var description = new JsonConstraintDescription(new Fake(), "This is {number}!");
            Assert.That(description.ToString(), Is.EqualTo("This is 42!"));
        }

        public class Fake : JsonConstraint
        {
            private const string ConstantStr = "Hello Constant";
            private static string staticStr = "Hello Static";


            private int number = 42;
            private string text = "Hello Field";

            public int NumberProperty
            {
                get { return 26; }
            }

            public string TextProperty
            {
                get { return "Hello Property"; }
            }

            public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
            {
                return null;
            }
        }
    }
}