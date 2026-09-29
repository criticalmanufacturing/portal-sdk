using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await RootCommandFactory.Create().Parse(args).InvokeAsync();
        }
    }
}
