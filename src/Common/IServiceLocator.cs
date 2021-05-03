namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface IServiceLocator
    {
        TService Get<TService>() where TService : class;
    }
}
