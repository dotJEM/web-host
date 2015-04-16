using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation.Constraints
{
    [TestFixture]
    public class OneOfFieldConstraintTest
    {
        [TestCase("{ foo: 'NoYes'}")]
        [TestCase("{ foo: 'No Yes'}")]
        [TestCase("{ foo: 'Not applicable'}")]
        [TestCase("{ foo: 'no'}")]
        [TestCase("{ foo: 'yes'}")]
        public void OneOf_InvalidData_ShouldReturnErrors(string json)
        {
            Validator validator = new OneOfValidator(true);

            ValidationResult result = validator.Validate(JObject.Parse(json));

            Assert.That(result.HasErrors, Is.True);
            Debug.WriteLine(result.ToString());
        }
        [TestCase("{ foo: 'No'}")]
        [TestCase("{ foo: 'Not Applicable'}")]
        [TestCase("{ foo: 'Yes'}")]
        public void OneOf_ValidData_ShouldReturnSuccess(string json)
        {
            Validator validator = new OneOfValidator(true);

            ValidationResult result = validator.Validate(JObject.Parse(json));

            Assert.That(result.HasErrors, Is.False);
            Debug.WriteLine(result.ToString());
        }
        [TestCase("{ foo: 'no'}")]
        [TestCase("{ foo: 'not Applicable'}")]
        [TestCase("{ foo: 'yes'}")]
        public void OneOf_ValidDataIgnoreCase_ShouldReturnSuccess(string json)
        {
            Validator validator = new OneOfValidator(false);

            ValidationResult result = validator.Validate(JObject.Parse(json));

            Assert.That(result.HasErrors, Is.False);
            Debug.WriteLine(result.ToString());
        }
    }
    public class OneOfValidator : Validator
    {
        public OneOfValidator(bool caseSensitive)
        {
            Field("foo",
                caseSensitive
                    ? Must.BeOneOf(new List<string> { "Yes", "No", "Not Applicable" })
                    : Must.BeOneOf(new List<string> { "Yes", "No", "Not Applicable" },
                        StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
