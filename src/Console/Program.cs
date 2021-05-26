using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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

            return await rootCommand.InvokeAsync(args);
        }
    }
}
