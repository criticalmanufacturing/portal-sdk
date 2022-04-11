using System;
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
            rootCommand.AddCommand(new CreateInfrastructureFromTemplateCommand());
            rootCommand.AddCommand(new CreateInfrastructureCommand());
            rootCommand.AddCommand(new DeployAgentCommand());
            rootCommand.AddCommand(new DeployCommand());
            rootCommand.AddCommand(new LoginCommand());
            rootCommand.AddCommand(new PublishCommand());
            rootCommand.AddCommand(new PublishPackageCommand());

            // cannot have an argument associated with --name with spaces. This condition is required to continue allowing users to have --name as a parameter
            if (Array.IndexOf(args, "--name") > -1)
            {
                args[Array.IndexOf(args, "--name")] = "--id";
            }
           
            
            return await rootCommand.InvokeAsync(args);

           
        }
    }
}
