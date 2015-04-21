using System;
using System.Collections.Generic;
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
        
        public static IFieldValidatorBuilder EqualTo(this IFieldValidatorBuilder self, string value, StringComparison comparison = StringComparison.InvariantCulture)
        {
            return self.Append(new StringEqualToFieldConstraint(value, comparison));
        }

        public static IFieldValidatorBuilder Required(this IFieldValidatorBuilder self)
        {
            return self.Append(new RequiredFieldConstraint());
        }

        public static IFieldValidatorBuilder Defined(this IFieldValidatorBuilder self)
        {
            //NOTE: HACK!... we need to separate the Validators from the guards, but this will do for now.
            return self.Append(new RequiredFieldConstraint());
        }

        public static IFieldValidatorBuilder BeOneOf(this IFieldValidatorBuilder self, IEnumerable<string> strings)
        {
            return self.Append(new OneOfFieldConstraint(strings));
        }

        public static IFieldValidatorBuilder BeOneOf(this IFieldValidatorBuilder self, IEnumerable<string> strings, StringComparer comparer)
        {
            return self.Append(new OneOfFieldConstraint(strings, comparer));
        }
        
        public static IFieldValidatorBuilder BeOneOf(this IFieldValidatorBuilder self, params string [] strings)
        {
            return self.Append(new OneOfFieldConstraint(strings));
        }
        public static IFieldValidatorBuilder BeOneOf(this IFieldValidatorBuilder self, StringComparer comparer, params string[] strings)
        {
            return self.Append(new OneOfFieldConstraint(strings, comparer));
        }
    }
}