using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Client command line application to interact with CustomerPortal DevOps Center");

            rootCommand.AddCommand(new CheckAgentConnectionCommand());
            rootCommand.AddCommand(new CreateInfrastructureCommand());
            rootCommand.AddCommand(new DeployAgentCommand());
            rootCommand.AddCommand(new DeployCommand());
            rootCommand.AddCommand(new DownloadArtifactsCommand());
            rootCommand.AddCommand(new InstallAppCommand());
            rootCommand.AddCommand(new LoginCommand());
            rootCommand.AddCommand(new PublishCommand());
            rootCommand.AddCommand(new PublishPackageCommand());
            rootCommand.AddCommand(new UndeployCommand());

            return await rootCommand.InvokeAsync(args);
        }
    }
}
