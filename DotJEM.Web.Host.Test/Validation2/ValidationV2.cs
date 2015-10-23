using System;
using DotJEM.Web.Host.Validation2;
using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Constraints.Common;
using DotJEM.Web.Host.Validation2.Constraints.String;
using DotJEM.Web.Host.Validation2.Constraints.String.Length;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation2
{
    [TestFixture]
    public class ValidationV2ConstraintBuilder
    {
        [Test]
        public void Validate_InvalidData_ShouldReturnErrors()
        {
            var constraint = (N & N) | (N & N & !N & N);

            string str = constraint.ToString();

            Assert.That(constraint, Is.TypeOf<OrJsonConstraint>());

            constraint = constraint.Optimize();

            string str2 = constraint.ToString();
            Assert.That(constraint, Is.TypeOf<OrJsonConstraint>());
        }

        public int counter = 1;

        public FakeJsonConstraint N
        {
            get { return new FakeJsonConstraint("" + counter++); }
        }

        [Test]
        public void SpecificValidator_InvalidData_ShouldReturnErrors()
        {
            TestValidator validator = new TestValidator();


            var result = validator.Validate(new JsonValidationContext(null, null), JObject.FromObject(new
            {
                test= "01234567890123456789", other="0", A = "asd"
            }));

            string description = validator.Describe().ToString();
            Console.WriteLine(description);
            Assert.That(result.IsValid, Is.False);
        }
    }

    public class TestValidator : JsonValidator
    {
        public TestValidator()
        {
            When("test", Has.MaxLength(5)).Then("test", Must.Have.MaxLength(200));
            When("other", Has.MinLength(0)).Then("test", Must.Have.MaxLength(25));

            When(Field("test", Has.MinLength(5))).Then(Field("other", Should.Be.Equal("0")));

            When(Field("A", Is.Defined()) | Field("B", Is.Defined()))
                .Then(
                      Field("A", Must.Be.Equal("") | Must.Be.Equal(""))
                    & Field("B", Must.Be.Equal("")));
        }
    }


    public class FakeJsonConstraint : JsonConstraint
    {
        public string Name { get; private set; }

        public FakeJsonConstraint(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Matches(IJsonValidationContext context, JToken token)
        {
            return true;
        }
    }
}
