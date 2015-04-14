namespace DotJEM.Web.Host.Validation.Results
{
    public interface IValidationCollector
    {
        IValidationCollector AddError(string format, params object[] args);
    }
}