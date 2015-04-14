using DotJEM.Web.Host.Validation.Constraints;

namespace DotJEM.Web.Host.Validation
{
    public static class StringFieldValidatorBuilderExtensions
    {
        public static IFieldValidatorBuilder HaveExactLength(this IFieldValidatorBuilder self, int exact)
        {
            return self.Append(new ExactLengthFieldConstraint(exact));
        }

        public static IFieldValidatorBuilder HaveMinLength(this IFieldValidatorBuilder self, int minLength)
        {
            return self.Append(new MinLengthFieldConstraint(minLength));
        }
        public static IFieldValidatorBuilder HaveMaxLength(this IFieldValidatorBuilder self, int maxLength)
        {
            return self.Append(new MaxLengthFieldConstraint(maxLength));
        }
        public static IFieldValidatorBuilder HaveLength(this IFieldValidatorBuilder self, int minLength, int maxLength)
        {
            return self.Append(new LengthFieldConstraint(minLength, maxLength));
        }

        public static IFieldValidatorBuilder Match(this IFieldValidatorBuilder self, string regex)
        {
            return self.Append(new MatchFieldConstraint(regex));
        }

        public static IFieldValidatorBuilder Required(this IFieldValidatorBuilder self)
        {
            return self.Append(new RequiredFieldConstraint());
        }
    }
}