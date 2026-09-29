using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    /// <summary>
    /// Timeout and version termination options shared by the Customer Environment and Infrastructure Agent deployments.
    /// </summary>
    class DeploymentOptionsExtension : IOptionExtension
    {
        public Option<bool> TerminateOtherVersions { get; } = new("--terminateOtherVersions", "-tov") { Description = Resources.DeploymentTerminateOtherVersionsHelp };

        public Option<double?> DeploymentTimeoutMinutes { get; } = new("--deploymentTimeoutMinutes", "-to") { Description = Resources.DeploymentTimeoutMinutesHelp };

        public Option<double?> DeploymentTimeoutMinutesToGetSomeMBMsg { get; } = new("--deploymentTimeoutMinutesToGetSomeMBMsg", "-tombm") { Description = Resources.DeploymentTimeoutMinutesToGetSomeMBMessageHelp };

        public Option<bool> TerminateOtherVersionsRemove { get; } = new("--terminateOtherVersionsRemove", "-tovr") { Description = Resources.DeploymentTerminateOtherVersionsRemoveHelp };

        public Option<bool> TerminateOtherVersionsRemoveVolumes { get; } = new("--terminateOtherVersionsRemoveVolumes", "-tovrv") { Description = Resources.DeploymentTerminateOtherVersionsRemoveVolumesHelp };

        public void Use(Command command)
        {
            command.Options.Add(TerminateOtherVersions);
            command.Options.Add(DeploymentTimeoutMinutes);
            command.Options.Add(DeploymentTimeoutMinutesToGetSomeMBMsg);
            command.Options.Add(TerminateOtherVersionsRemove);
            command.Options.Add(TerminateOtherVersionsRemoveVolumes);
        }
    }
}
