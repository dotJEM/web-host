namespace DotJEM.Web.Host.Validation2.Factories
{
    public interface IConstraintFactory
    {
    }
    public interface IHaveConstraintFactory : IConstraintFactory
    {
    }

    public interface IBeConstraintFactory : IConstraintFactory
    {
    }

    public interface IGuardConstraintFactory : IBeConstraintFactory, IHaveConstraintFactory
    {
    }


    public interface IValidatorConstraintFactory
    {
        IBeConstraintFactory Be { get; }

        IHaveConstraintFactory Have { get; }
    }

    public class ValidatorConstraintFactory : IValidatorConstraintFactory
    {
        public IBeConstraintFactory Be { get; set; }
        public IHaveConstraintFactory Have { get; set; }
    }

    //public static class GuardConstraintFactoryCommonExtensions
    //{
    //    public static JsonConstraint Defined(this IGuardConstraintFactory self)
    //    {
    //        return new IsDefinedJsonConstraint();
    //    }
    //}

}