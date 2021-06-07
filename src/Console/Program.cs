using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Client command line application to interact with CustomerPortal DevOps Center");
            rootCommand.AddCommand(new PublishCommand());
            rootCommand.AddCommand(new DeployCommand());
            rootCommand.AddCommand(new LoginCommand());
            rootCommand.AddCommand(new CreateInfrastructureFromTemplateCommand());

            return await rootCommand.InvokeAsync(args);
        }
    }
}
