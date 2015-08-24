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
    public class ExactStringLengthJsonConstraintTest
    {
        [TestCase(42, "String length must be '42'.")]
        [TestCase(20, "String length must be '20'.")]
        public void Describe_FormatsDescription(int length, string expected)
        {
            Assert.That(new ExactStringLengthJsonConstraint(length).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class LengthJsonConstraintTest
    {
        [TestCase(20, 42, "String length must be from '20' to '42'.")]
        [TestCase(20, 30, "String length must be from '20' to '30'.")]
        public void Describe_FormatsDescription(int min, int max, string expected)
        {
            Assert.That(new LengthJsonConstraint(min, max).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class MaxLengthJsonConstraintTest
    {
        [TestCase(42, "String length must be less than or equal to '42'.")]
        [TestCase(30, "String length must be less than or equal to '30'.")]
        public void Describe_FormatsDescription(int max, string expected)
        {
            Assert.That(new MaxLengthJsonConstraint(max).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class MinLengthJsonConstraintTest
    {
        [TestCase(42, "String length must be more than or equal to '42'.")]
        [TestCase(30, "String length must be more than or equal to '30'.")]
        public void Describe_FormatsDescription(int min, string expected)
        {
            Assert.That(new MinLengthJsonConstraint(min).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }


}
