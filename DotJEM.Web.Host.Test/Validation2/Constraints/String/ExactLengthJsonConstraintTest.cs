using System;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Test.Validation.V2;
using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Constraints.String;
using DotJEM.Web.Host.Validation2.Constraints.String.Length;
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

        [TestCase(42, "This string should be 42 characters long..", true)]
        [TestCase(42, "This string is certainly not 42 characters long..", false)]
        public void Describe_FormatsDescription(int length, string str, bool expected)
        {
            Assert.That(new ExactStringLengthJsonConstraint(length).Matches(null, JToken.FromObject(str)), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class StringLengthJsonConstraintTest
    {
        [TestCase(20, 42, "String length must be from '20' to '42'.")]
        [TestCase(20, 30, "String length must be from '20' to '30'.")]
        public void Describe_FormatsDescription(int min, int max, string expected)
        {
            Assert.That(new StringLengthJsonConstraint(min, max).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class MaxStringLengthJsonConstraintTest
    {
        [TestCase(42, "String length must be less than or equal to '42'.")]
        [TestCase(30, "String length must be less than or equal to '30'.")]
        public void Describe_FormatsDescription(int max, string expected)
        {
            Assert.That(new MaxStringLengthJsonConstraint(max).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class MinStringLengthJsonConstraintTest
    {
        [TestCase(42, "String length must be more than or equal to '42'.")]
        [TestCase(30, "String length must be more than or equal to '30'.")]
        public void Describe_FormatsDescription(int min, string expected)
        {
            Assert.That(new MinStringLengthJsonConstraint(min).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class MatchStringJsonConstraintString
    {
        public void Describe_FormatsDescription(string regex, string expected)
        {
            Assert.That(new MatchStringJsonConstraint(new Regex(regex, RegexOptions.Compiled)).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class StringEqualsJsonConstraintTest
    {
        [TestCase("helloWorld", StringComparison.InvariantCulture, "String must be equal to 'helloWorld' (InvariantCulture).")]
        [TestCase("helloWorld", StringComparison.Ordinal, "String must be equal to 'helloWorld' (Ordinal).")]
        [TestCase("helloWorld", StringComparison.CurrentCultureIgnoreCase, "String must be equal to 'helloWorld' (CurrentCultureIgnoreCase).")]
        public void Describe_FormatsDescription(string str, StringComparison comparison, string expected)
        {
            Assert.That(new StringEqualsJsonConstraint(str, comparison).Describe(null, null).ToString(), Is.EqualTo(expected));
        }
    }


}
