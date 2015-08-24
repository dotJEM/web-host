using DotJEM.Web.Host.Test.Validation.V2;
using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Constraints.String;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation2.Constraints.String
{
    [TestFixture]
    public class ExactLengthJsonConstraintTest
    {
        [TestCase(42, "String length must be '42'.")]
        [TestCase(20, "String length must be '20'.")]
        public void Describe_FormatsDescription(int length, string expected)
        {
            Assert.That(new ExactLengthJsonConstraint(length).Describe(null, null).ToString(), Is.EqualTo(expected));
        }

    }


}
