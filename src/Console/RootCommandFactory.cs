using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Commands;
using Cmf.CustomerPortal.Sdk.Console.Commands.Create;
using Cmf.CustomerPortal.Sdk.Console.Commands.Deploy;
using Cmf.CustomerPortal.Sdk.Console.Commands.Download;
using Cmf.CustomerPortal.Sdk.Console.Commands.Healthcheck;
using Cmf.CustomerPortal.Sdk.Console.Commands.Publish;
using Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Linq;
using Legacy = Cmf.CustomerPortal.Sdk.Console.Commands.Legacy;

namespace Cmf.CustomerPortal.Sdk.Console
{
    /// <summary>
    /// Builds the <c>cmf-portal</c> command tree: the <c>verb noun [name] [options]</c> commands, plus the
    /// legacy flat commands, which keep working but are hidden from help.
    /// </summary>
    static class RootCommandFactory
    {
        /// <summary>
        /// Creates the root command with every verb, noun and legacy command.
        /// </summary>
        public static RootCommand Create()
        {
            var rootCommand = new RootCommand(Resources.RootHelp);

            rootCommand.Subcommands.Add(new LoginCommand());

            // verbs that share their name with a legacy command: the legacy command is the verb, so its options keep working
            rootCommand.Subcommands.Add(AsVerb(new Legacy.DeployCommand(), Resources.VerbDeployHelp,
                new DeployEnvironmentCommand(), new DeployAgentCommand(), new DeployAppCommand()));
            rootCommand.Subcommands.Add(AsVerb(new Legacy.UndeployCommand(), Resources.VerbUndeployHelp,
                new UndeployEnvironmentCommand(), new UndeployAppCommand()));
            rootCommand.Subcommands.Add(AsVerb(new Legacy.PublishCommand(), Resources.VerbPublishHelp,
                new PublishDeploymentPackageCommand(), new PublishInstallationPackageCommand()));

            rootCommand.Subcommands.Add(Verb("create", Resources.VerbCreateHelp, new CreateInfrastructureCommand()));
            rootCommand.Subcommands.Add(Verb("healthcheck", Resources.VerbHealthcheckHelp, new HealthcheckAgentCommand()));
            rootCommand.Subcommands.Add(Verb("download", Resources.VerbDownloadHelp, new DownloadArtifactsCommand()));

            // legacy commands without a matching verb
            Command[] legacyCommands =
            [
                new Legacy.CheckAgentConnectionCommand(),
                new Legacy.CreateInfrastructureCommand(),
                new Legacy.DeployAgentCommand(),
                new Legacy.DownloadArtifactsCommand(),
                new Legacy.InstallAppCommand(),
                new Legacy.PublishPackageCommand(),
                new Legacy.UninstallAppCommand(),
            ];
            foreach (Command legacyCommand in legacyCommands)
            {
                legacyCommand.Hidden = true;
                rootCommand.Subcommands.Add(legacyCommand);
            }

            // hidden legacy commands must still print their own help
            HelpOption helpOption = rootCommand.Options.OfType<HelpOption>().Single();
            helpOption.Action = new HiddenCommandHelpAction((SynchronousCommandLineAction)helpOption.Action);

            return rootCommand;
        }

        private static Command Verb(string name, string description, params Command[] nouns)
        {
            var verb = new Command(name, description);
            foreach (Command noun in nouns)
            {
                verb.Subcommands.Add(noun);
            }

            return verb;
        }

        /// <summary>
        /// Turns a legacy command into a verb: adds the nouns and hides the legacy options from help.
        /// The legacy options still parse, so existing invocations keep working.
        /// </summary>
        private static Command AsVerb(BaseCommand legacyCommand, string description, params Command[] nouns)
        {
            legacyCommand.Description = description;
            foreach (Option option in legacyCommand.Options)
            {
                if (option != legacyCommand.VerboseOption)
                {
                    option.Hidden = true;
                }
            }

            foreach (Command noun in nouns)
            {
                legacyCommand.Subcommands.Add(noun);
            }

            return legacyCommand;
        }
    }
}
