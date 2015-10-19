using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public interface IFieldGuardValidatorBuilder
    {
        void Field(JPath field, IFieldValidatorBuilder builder);
    }

    public class FieldGuardValidatorBuilder : IFieldGuardValidatorBuilder
    {
        private readonly Action<string, IFieldValidator> callback;
        private readonly IFieldGuard guard;

        public FieldGuardValidatorBuilder(Action<string, IFieldValidator> callback, IFieldGuard guard)
        {
            this.callback = callback;
            this.guard = guard;
        }

        public void Field(JPath field, IFieldValidatorBuilder builder)
        {
            callback(field.ToString(), new GuardedFieldValidator(builder.BuildValidator(field), guard));
        }
    }

    public interface IFieldGuard
    {
        bool Matches(JObject entity);
    }

    public class FieldGuard : IFieldGuard
    {
        private readonly IFieldConstraint constraint;
        private readonly JPath field;

        public FieldGuard(JPath field, IFieldConstraint constraint)
        {
            this.constraint = constraint;
            this.field = field;
        }

        public bool Matches(JObject entity)
        {
            return constraint.Matches(entity.SelectToken(field.Path));
        }
    }

    public class GuardedFieldValidator : IFieldValidator
    {
        private readonly IFieldGuard guard;
        private readonly IFieldValidator inner;

        public GuardedFieldValidator(IFieldValidator inner, IFieldGuard guard)
        {
            this.inner = inner;
            this.guard = guard;
        }

        public IEnumerable<FieldValidationResults> Validate(JObject entity, IValidationContext context)
        {
            return guard.Matches(entity) 
                ? inner.Validate(entity, context)
                : Enumerable.Empty<FieldValidationResults>();
        }
    }

    public abstract class Validator : IValidator
    {
        private readonly Lazy<string> contentType;
        private readonly Dictionary<string, IFieldValidator> fieldValidators = new Dictionary<string, IFieldValidator>();
        private IEnumerable<IFieldValidator> Validators { get { return fieldValidators.Values; } }

        public string ContentType { get { return contentType.Value; } }

        //NOTE: These are just for a more natural feel (fluent)...
        protected IFieldValidatorBuilder Is { get { return new FieldValidatorBuilder(); } }
        protected IFieldValidatorBuilder Must { get { return new FieldValidatorBuilder(); } }
        
        protected Validator()
        {
            contentType = new Lazy<string>(() => GetValidatorName(this));
        }

        protected void Field(JPath field, IFieldValidatorBuilder builder)
        {
            fieldValidators.Add(field.ToString(), builder.BuildValidator(field));
        }

        protected IFieldGuardValidatorBuilder When(JPath field, IFieldValidatorBuilder builder)
        {
            return new FieldGuardValidatorBuilder((key, validator) => fieldValidators.Add(key, validator), builder.BuildGuard(field));
        }

        public ValidationResult Validate(JObject entity, IValidationContext context)
        {
            //TODO: Need a better concept for this, specifically a "Dependant field" concept, that can also be used in cross validation.
            return RequiresValidation(entity, context.PreviousEntity)
                ? new ValidationResult(ContentType, entity, Validators.SelectMany(v => v.Validate(entity, context)).ToList())
                : new ValidationResult(ContentType, entity, Enumerable.Empty<FieldValidationResults>().ToList());
        }

        protected virtual bool RequiresValidation(JObject entity)
        {
            return true;
        }

        protected virtual bool RequiresValidation(JObject update, JObject original)
        {
            return RequiresValidation(update);
        }

        public static string GetValidatorName(object obj)
        {
            Type type = obj.GetType();
            ValidatorAttribute definition = type
                .GetCustomAttributes(typeof(ValidatorAttribute), false)
                .OfType<ValidatorAttribute>()
                .SingleOrDefault();
            return definition == null ? type.Name.Replace("Validator", "") : definition.ContentType;
        }
    }
}
