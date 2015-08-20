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
        [TestCase("This is {number}!", "This is 42!")]
        [TestCase("This is {number:0000}!", "This is 0042!")]
        [TestCase("This is {number, 10}!", "This is         42!")]
        [TestCase("This is {number, -10}!", "This is 42        !")]
        [TestCase("This is {number, 10:x}!", "This is         2a!")]
        [TestCase("This is {number} and {text}!", "This is 42 and Hello Field!")]
        //[TestCase("This is {NumberProperty} and {TextProperty}!", "This is 26 and Hello Property!")]
        public void ToString_Fake_ReturnsFormattedString(string format, string expected)
        {
            Assert.That(new JsonConstraintDescription(new Fake(), format).ToString(), Is.EqualTo(expected));
        }

        public class Fake : JsonConstraint
        {
            // ReSharper disable UnusedMember.Local
            // NOTE: For testing formatter access
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
            // ReSharper restore UnusedMember.Local
        }
    }
}