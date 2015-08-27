using System;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;

namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public sealed class BasicJsonConstraintResult : JsonConstraintResult
    {
        public JsonConstraintDescription Description { get; private set; }
        public Type ConstraintType { get; private set; }

        public BasicJsonConstraintResult(bool value, JsonConstraintDescription description, Type constraintType)
            : base(value)
        {
            Description = description;
            ConstraintType = constraintType;
        }
    }
}