using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Deploy
{
    /// <summary>
    /// Options and action shared by the commands that install an App in a Customer Environment.
    /// Subclasses only decide how the App name is supplied.
    /// </summary>
    abstract class DeployAppCommandBase : BaseCommand
    {
        private readonly ReplaceTokensExtension replaceTokens;

        private readonly Option<string> appVersionOption = new("--app-version", "-av") { Description = Resources.AppVersionHelp, Required = true };

        private readonly Option<string> customerEnvironmentOption = new("--customer-environment", "-ce") { Description = Resources.AppCustomerEnvironmentHelp, Required = true };

        private readonly Option<string> licenseOption = new("--license", "-lic") { Description = Resources.AppLicenseHelp, Required = true };

        private readonly Option<FileInfo> parametersOption = new Option<FileInfo>("--parameters", "-params") { Description = Resources.AppParametersPathHelp }.AcceptExistingOnly();

        private readonly Option<DirectoryInfo> outputOption = new("--output", "-o") { Description = Resources.DeploymentOutputDirHelp };

        private readonly Option<double?> timeoutOption = new("--timeout", "-to") { Description = Resources.AppInstallationTimeoutHelp };

        private readonly Option<double?> timeoutToGetSomeMBMsgOption = new("--timeoutToGetSomeMBMsg", "-tombm") { Description = Resources.DeploymentTimeoutMinutesToGetSomeMBMessageHelp, Required = false };

        protected DeployAppCommandBase(string name, string description) : base(name, description)
        {
            replaceTokens = Use(new ReplaceTokensExtension());

            Options.Add(appVersionOption);
            Options.Add(customerEnvironmentOption);
            Options.Add(licenseOption);
            Options.Add(parametersOption);
            Options.Add(outputOption);
            Options.Add(timeoutOption);
            Options.Add(timeoutToGetSomeMBMsgOption);

            SetAction(InstallHandler);
        }

        /// <summary>
        /// Gets the name of the App to install.
        /// </summary>
        protected abstract string GetAppName(ParseResult parseResult);

        private async Task InstallHandler(ParseResult parseResult)
        {
            // get install app handler and run it
            CreateSession(parseResult);

            InstallAppHandler installAppHandler = ServiceLocator.Get<InstallAppHandler>();
            await installAppHandler.Run(
                GetAppName(parseResult),
                parseResult.GetValue(appVersionOption),
                parseResult.GetValue(customerEnvironmentOption),
                parseResult.GetValue(licenseOption),
                parseResult.GetValue(parametersOption),
                parseResult.GetValue(replaceTokens.ReplaceTokens),
                parseResult.GetValue(outputOption),
                parseResult.GetValue(timeoutOption),
                parseResult.GetValue(timeoutToGetSomeMBMsgOption));
        }
    }
}
