using DotJEM.Web.Host.Validation2;
using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation.V2
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

        public NamedJsonConstraint N
        {
            get { return new NamedJsonConstraint("" + counter++); }
        }

        [Test]
        public void SpecificValidator_InvalidData_ShouldReturnErrors()
        {
            SpecificValidator validator = new SpecificValidator();


            var result = validator.Validate(new JsonValidationContext(null, null), JObject.FromObject(new
            {
                test="01234567890123456789", other="0"
            }));

            Assert.That(result.IsValid, Is.True);
        }
    }

    public class SpecificValidator : JsonValidator
    {
        public SpecificValidator()
        {
            When("test", Is.LongerThan(5)).Then("test", Must.BeShorterThan(200));
            When("other", Is.LongerThan(0)).Then("test", Must.BeShorterThan(10));

            //When(Field("A", Is.Defined()) | Field("B", Is.Defined()))
            //    .Then(
            //          Field("A", Must.BeEqual("") | Must.BeEqual(""))
            //        & Field("B", Must.Not().BeEqual("")));
        }

    }


    public class NamedJsonConstraint : JsonConstraint
    {
        public string Name { get; private set; }

        public NamedJsonConstraint(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            return true;
        }
    }

    public class ShorterJsonConstraint : JsonConstraint
    {
        private readonly int maxLength;

        public ShorterJsonConstraint(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            string value = (string)token;
            if (value.Length >= maxLength)
            {
                //TODO: Provide Constraint Desciption instead.
                return false;
            }
            return true;
        }
        }

    public class LongerJsonConstraint : JsonConstraint
    {
        private readonly int minLength;

        public LongerJsonConstraint(int minLength)
        {
            this.minLength = minLength;
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            string value = (string)token;
            if (value.Length <= minLength)
            {
                //TODO: Provide Constraint Desciption instead.
                return false;
            }
            return true;
        }
    }
}
