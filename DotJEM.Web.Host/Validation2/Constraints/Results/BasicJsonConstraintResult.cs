using System;

namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public sealed class BasicJsonConstraintResult : JsonConstraintResult
    {
        private readonly bool value;
        private readonly string message;

        public override bool Value
        {
            get { return value; }
        }

        public override string Message
        {
            get { return message; }
        }

        public Type ConstraintType { get; private set; }

        public BasicJsonConstraintResult(bool value, string message, Type constraintType)
        {
            this.value = value;
            this.message = message;
            ConstraintType = constraintType;
        }
    }
}