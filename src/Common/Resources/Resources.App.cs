namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// Help texts for installing and uninstalling Apps.
/// </summary>
public static partial class Resources
{
    public const string AppNameHelp = "The name of the App to install.";
    public const string AppUninstallNameHelp = "The name of the App to uninstall.";
    public const string AppVersionHelp = "The version of the App to install.";
    public const string AppCustomerEnvironmentHelp = "The name of a Convergence Customer Environment to install the App on.";
    public const string AppLicenseHelp = "Name of the License to use for the App.";
    public const string AppInstallationTimeoutHelp = "Timeout, in minutes, to wait for an App to install. The default is 360 minutes (6 hours).";
    public const string AppUninstallationTimeoutHelp = "Timeout, in minutes, to wait for an App to uninstall. The default is 360 minutes (6 hours).";
    public const string AppParametersPathHelp = "Path to parameters file that describes the App in a Convergence Customer Environment.";
    public const string AppUninstallRemoveHelp = "Deprecated, always enabled. Flag that controls if the deployments of the App that will be uninstalled should be removed.";
    public const string AppUninstallRemoveVolumesHelp = "Flag that controls if the volumes of the App that will be uninstalled should be removed.";
    public const string AppUndeployHelp = "Flag that controls if the app's undeploy procedures will be executed. Implies volume removal.";
    public const string AppUninstallPurgeHelp = "Flag that wipes out the App: removes its workloads and volumes and runs its undeploy procedures. When no cleanup option is given, only the App's workloads are removed.";

    // cleanup options deprecated in favour of purge in 'undeploy app' and Uninstall-App
    public const string AppUninstallRemoveVolumesDeprecatedHelp = "Deprecated, use --purge instead. Removes the App's volumes along with its workloads, without running its undeploy procedures.";
    public const string AppUndeployDeprecatedHelp = "Deprecated, use --purge instead, which does the same. Runs the App's undeploy procedures and removes its volumes.";
    public const string AppUninstallRemoveVolumesDeprecatedPowershellHelp = "Deprecated, use -Purge instead. Removes the App's volumes along with its workloads, without running its undeploy procedures.";
    public const string AppUndeployDeprecatedPowershellHelp = "Deprecated, use -Purge instead, which does the same. Runs the App's undeploy procedures and removes its volumes.";

    // messages printed when a deprecated cleanup option is given: {0} is the option as typed, {1} the purge option
    public const string AppUninstallRemoveVolumesDeprecated = "[Deprecated] '{0}' will be removed in a future major version. Use '{1}' instead, which also runs the App's undeploy procedures, or neither to keep the App's volumes.";
    public const string AppUndeployDeprecated = "[Deprecated] '{0}' will be removed in a future major version. Use '{1}' instead, which does the same.";
}
