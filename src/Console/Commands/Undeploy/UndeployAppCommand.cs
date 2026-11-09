using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;
using System.Globalization;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy
{
    /// <summary>
    /// <c>undeploy app &lt;name&gt;</c>: uninstalls an App from a Customer Environment. <c>--purge</c> replaces the
    /// deprecated <c>--removeVolumes</c> and <c>--undeploy</c>, which the legacy <c>uninstall-app</c> keeps as they were.
    /// </summary>
    class UndeployAppCommand : UndeployAppCommandBase
    {
        private const string PurgeOptionName = "--purge";

        private readonly Argument<string> nameArgument = new("name") { Description = Resources.AppUninstallNameHelp };

        private readonly Option<bool> purgeOption = new(PurgeOptionName) { Description = Resources.AppUninstallPurgeHelp };

        public UndeployAppCommand() : base("app", Resources.NounUndeployAppHelp)
        {
            Aliases.Add("application");
            Arguments.Add(nameArgument);
            Options.Add(purgeOption);

            RemoveVolumesOption.Description = Resources.AppUninstallRemoveVolumesDeprecatedHelp;
            UndeployOption.Description = Resources.AppUndeployDeprecatedHelp;
            DeprecateOption(RemoveVolumesOption, alias => string.Format(CultureInfo.InvariantCulture, Resources.AppUninstallRemoveVolumesDeprecated, alias, PurgeOptionName));
            DeprecateOption(UndeployOption, alias => string.Format(CultureInfo.InvariantCulture, Resources.AppUndeployDeprecated, alias, PurgeOptionName));
        }

        protected override string GetAppName(ParseResult parseResult) => parseResult.GetValue(nameArgument);

        protected override bool GetPurge(ParseResult parseResult) => parseResult.GetValue(purgeOption);
    }
}
