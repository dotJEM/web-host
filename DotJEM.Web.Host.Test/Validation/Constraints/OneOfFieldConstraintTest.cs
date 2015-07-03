using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
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
        public void Validate_InvalidData_ShouldReturnErrors(string json)
        {
            Validator validator = new OneOfValidator(true);

            var entity = JObject.Parse(json);
            ValidationResult result = validator.Validate(entity, new ValidationContext(entity, null, HttpVerbs.Post));

            Assert.That(result.HasErrors, Is.True);
        }
        [TestCase("{ foo: 'No'}")]
        [TestCase("{ foo: 'Not Applicable'}")]
        [TestCase("{ foo: 'Yes'}")]
        public void Validate_ValidData_ShouldReturnSuccess(string json)
        {
            Validator validator = new OneOfValidator(true);

            var entity = JObject.Parse(json);
            ValidationResult result = validator.Validate(entity, new ValidationContext(entity, null, HttpVerbs.Post));

            Assert.That(result.HasErrors, Is.False);
        }
        [TestCase("{ foo: 'no'}")]
        [TestCase("{ foo: 'not Applicable'}")]
        [TestCase("{ foo: 'yes'}")]
        public void Validate_ValidDataIgnoreCase_ShouldReturnSuccess(string json)
        {
            Validator validator = new OneOfValidator(false);

            var entity = JObject.Parse(json);
            ValidationResult result = validator.Validate(entity, new ValidationContext(entity, null, HttpVerbs.Post));

            Assert.That(result.HasErrors, Is.False);
        }
    }

    public class OneOfValidator : Validator
    {
        public OneOfValidator(bool caseSensitive)
        {
            Field("foo",
                caseSensitive
                    ? Must.BeOneOf(new List<string> { "Yes", "No", "Not Applicable" }, StringComparer.InvariantCulture)
                    : Must.BeOneOf(new List<string> { "Yes", "No", "Not Applicable" },
                        StringComparer.InvariantCultureIgnoreCase));
        }
    }
}
