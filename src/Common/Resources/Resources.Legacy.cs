namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// Help texts for the deprecated flat Console commands, which will be removed in a future major version.
/// </summary>
public static partial class Resources
{
    public const string LegacyCheckAgentConnectionHelp = "[Deprecated] Check if an Infrastructure Agent is connected. Use 'healthcheck agent' instead.";
    public const string LegacyCreateInfrastructureHelp = "[Deprecated] Creates a customer Infrastructure. Use 'create infrastructure' instead.";
    public const string LegacyDeployAgentHelp = "[Deprecated] Creates and deploys a new Infrastructure Agent. Use 'deploy agent' instead.";
    public const string LegacyDeployHelp = "[Deprecated] Creates and deploys a new Customer Environment. Use 'deploy env' instead.";
    public const string LegacyDownloadArtifactsHelp = "[Deprecated] Downloads all artifacts of a specific Customer Environment. Use 'download artifacts' instead.";
    public const string LegacyInstallAppHelp = "[Deprecated] Install an App in a previously deployed Convergence environment. Use 'deploy app' instead.";
    public const string LegacyPublishHelp = "[Deprecated] Publishes one or more Deployment Package(s) into Customer Portal. Use 'publish deploymentpackage' instead.";
    public const string LegacyPublishPackageHelp = "[Deprecated] Publishes a Deployment Package into Customer Portal. Use 'publish installationpackage' instead.";
    public const string LegacyUndeployHelp = "[Deprecated] Creates a new CustomerEnvironment's version and terminates the other versions, removing deployments. Use 'undeploy env' instead.";
    public const string LegacyUninstallAppHelp = "[Deprecated] Uninstalls an app from a customer environment version. Use 'undeploy app' instead.";

    // messages printed when a deprecated command runs
    public const string LegacyCheckAgentConnectionDeprecated = "[Deprecated] 'checkagentconnection' will be removed in a future major version. Use 'healthcheck agent' instead.";
    public const string LegacyCreateInfrastructureDeprecated = "[Deprecated] 'createinfrastructure' will be removed in a future major version. Use 'create infrastructure' instead.";
    public const string LegacyDeployAgentDeprecated = "[Deprecated] 'deployagent' will be removed in a future major version. Use 'deploy agent' instead.";
    public const string LegacyDeployDeprecated = "[Deprecated] 'deploy' without a noun will be removed in a future major version. Use 'deploy env' instead.";
    public const string LegacyDownloadArtifactsDeprecated = "[Deprecated] 'download-artifacts' will be removed in a future major version. Use 'download artifacts' instead.";
    public const string LegacyInstallAppDeprecated = "[Deprecated] 'install-app' will be removed in a future major version. Use 'deploy app' instead.";
    public const string LegacyPublishDeprecated = "[Deprecated] 'publish' without a noun will be removed in a future major version. Use 'publish deploymentpackage' instead.";
    public const string LegacyPublishPackageDeprecated = "[Deprecated] 'publish-package' will be removed in a future major version. Use 'publish installationpackage' instead.";
    public const string LegacyUndeployDeprecated = "[Deprecated] 'undeploy' without a noun will be removed in a future major version. Use 'undeploy env' instead.";
    public const string LegacyUninstallAppDeprecated = "[Deprecated] 'uninstall-app' will be removed in a future major version. Use 'undeploy app' instead.";
}
