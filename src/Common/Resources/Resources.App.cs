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
}
