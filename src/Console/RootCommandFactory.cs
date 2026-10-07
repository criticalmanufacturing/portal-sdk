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
using System.CommandLine.Parsing;
using System.Linq;
using Legacy = Cmf.CustomerPortal.Sdk.Console.Commands.Legacy;

namespace Cmf.CustomerPortal.Sdk.Console
{
    /// <summary>
    /// Builds the <c>cmf-portal</c> command tree: the <c>verb noun [name] [options]</c> commands, plus the
    /// deprecated legacy flat commands, which keep working and are listed in help with their replacement.
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

            // deprecated legacy commands without a matching verb, listed after the verbs so their help points to the replacement
            rootCommand.Subcommands.Add(new Legacy.CheckAgentConnectionCommand());
            rootCommand.Subcommands.Add(new Legacy.CreateInfrastructureCommand());
            rootCommand.Subcommands.Add(new Legacy.DeployAgentCommand());
            rootCommand.Subcommands.Add(new Legacy.DownloadArtifactsCommand());
            rootCommand.Subcommands.Add(new Legacy.InstallAppCommand());
            rootCommand.Subcommands.Add(new Legacy.PublishPackageCommand());
            rootCommand.Subcommands.Add(new Legacy.UninstallAppCommand());

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
        /// The legacy options still parse without a noun, so existing invocations keep working, but are rejected
        /// before a noun, so the verb behaves like the ones without a legacy command.
        /// </summary>
        private static Command AsVerb(BaseCommand legacyCommand, string description, params Command[] nouns)
        {
            legacyCommand.Description = description;
            foreach (Option option in legacyCommand.Options)
            {
                option.Hidden = true;
            }

            foreach (Command noun in nouns)
            {
                noun.Validators.Add(RejectVerbOptions);
                legacyCommand.Subcommands.Add(noun);
            }

            return legacyCommand;
        }

        private static void RejectVerbOptions(CommandResult nounResult)
        {
            // options given before the noun were parsed by the verb (the legacy command) and would be ignored
            if (nounResult.Parent is CommandResult verbResult)
            {
                foreach (OptionResult optionResult in verbResult.Children.OfType<OptionResult>().Where(o => !o.Implicit))
                {
                    nounResult.AddError($"Unrecognized command or argument '{optionResult.IdentifierToken.Value}'.");
                }
            }
        }
    }
}
