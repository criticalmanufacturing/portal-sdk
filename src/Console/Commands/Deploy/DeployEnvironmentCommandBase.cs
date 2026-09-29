using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Deploy
{
    /// <summary>
    /// Options and action shared by the commands that deploy a Customer Environment or an Infrastructure Agent.
    /// Subclasses only decide how the name is supplied.
    /// </summary>
    abstract class DeployEnvironmentCommandBase : BaseCommand
    {
        private readonly bool isInfrastructureAgent;

        protected ReplaceTokensExtension ReplaceTokens { get; }

        protected CommonParametersExtension CommonParameters { get; }

        /// <summary>
        /// Site, package and license options. <c>null</c> for Infrastructure Agents, which don't take them.
        /// </summary>
        protected EnvironmentPackageOptionsExtension PackageOptions { get; }

        protected DeploymentOptionsExtension DeploymentOptions { get; }

        /// <param name="name">Command name.</param>
        /// <param name="description">Command description.</param>
        /// <param name="isInfrastructureAgent">Whether the command deploys an Infrastructure Agent.</param>
        /// <param name="includeNameOption">Whether to add the <c>--name</c> option.</param>
        protected DeployEnvironmentCommandBase(string name, string description, bool isInfrastructureAgent, bool includeNameOption) : base(name, description)
        {
            this.isInfrastructureAgent = isInfrastructureAgent;

            ReplaceTokens = Use(new ReplaceTokensExtension());
            CommonParameters = Use(new CommonParametersExtension(includeNameOption));
            if (!isInfrastructureAgent)
            {
                PackageOptions = Use(new EnvironmentPackageOptionsExtension());
            }
            DeploymentOptions = Use(new DeploymentOptionsExtension());

            SetAction(DeployHandler);
        }

        /// <summary>
        /// Gets the name of the Customer Environment or Infrastructure Agent, or <c>null</c> to generate one.
        /// </summary>
        protected abstract string GetName(ParseResult parseResult);

        private async Task DeployHandler(ParseResult parseResult)
        {
            // get new environment handler and run it
            CreateSession(parseResult);
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();

            await newEnvironmentHandler.Run(
                GetName(parseResult),
                parseResult.GetValue(CommonParameters.Parameters),
                CommonParameters.GetEnvironmentType(parseResult),
                PackageOptions == null ? null : parseResult.GetValue(PackageOptions.Site),
                PackageOptions == null ? null : parseResult.GetValue(PackageOptions.License),
                PackageOptions == null ? null : parseResult.GetValue(PackageOptions.Package),
                CommonParameters.GetDeploymentTarget(parseResult),
                parseResult.GetValue(CommonParameters.Output),
                parseResult.GetValue(ReplaceTokens.ReplaceTokens),
                parseResult.GetValue(CommonParameters.Interactive),
                parseResult.GetValue(CommonParameters.CustomerInfrastructureName),
                parseResult.GetValue(CommonParameters.Description),
                parseResult.GetValue(DeploymentOptions.TerminateOtherVersions),
                isInfrastructureAgent,
                parseResult.GetValue(DeploymentOptions.DeploymentTimeoutMinutes),
                parseResult.GetValue(DeploymentOptions.DeploymentTimeoutMinutesToGetSomeMBMsg),
                parseResult.GetValue(DeploymentOptions.TerminateOtherVersionsRemove),
                parseResult.GetValue(DeploymentOptions.TerminateOtherVersionsRemoveVolumes));
        }
    }
}
