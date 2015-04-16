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

namespace DotJEM.Web.Host.Test.Validation
{
    [TestFixture]
    public class ValidatorIntegrationTest
    {
        [TestCase("{ }")]
        [TestCase("{ foo: { barMax: 'this is way to long' } }")]
        [TestCase("{ foo: { barMin: 'to short' } }")]
        [TestCase("{ foo: { barMin: 'to short', barMax: 'this is way to long' } }")]
        [TestCase("{ combi: 'what' }")]
        public void Validate_InvalidData_ShouldReturnErrors(string json)
        {
            TestValidator validator = new TestValidator();

            ValidationResult result = validator.Validate(JObject.Parse(json));

            Assert.That(result.HasErrors, Is.True);
        }
    }

    public class TestValidator : Validator
    {
        public TestValidator()
        {
            Field("foo", Is.Required());
            Field("foo.barMax", Must.HaveMaxLength(10));
            Field("foo.barMin", Must.HaveMinLength(10));
            Field("combi", Must.HaveLength(10, 20).Match("^[0-9]*$"));
        }
    }
}
