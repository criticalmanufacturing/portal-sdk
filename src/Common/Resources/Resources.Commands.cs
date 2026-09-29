namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// Help texts for the Console <c>verb noun [name]</c> command tree: the root command, verbs, nouns and positional arguments.
/// </summary>
public static partial class Resources
{
    public const string RootHelp = "Client command line application to interact with CustomerPortal DevOps Center";

    public const string VerbDeployHelp = "Deploys a Customer Environment, an Infrastructure Agent or an App";
    public const string VerbUndeployHelp = "Undeploys a Customer Environment or an App";
    public const string VerbCreateHelp = "Creates a resource in the Customer Portal";
    public const string VerbHealthcheckHelp = "Checks the health of a resource";
    public const string VerbPublishHelp = "Publishes Deployment Packages or Installation Packages into the Customer Portal";
    public const string VerbDownloadHelp = "Downloads resources from the Customer Portal";

    public const string NounDeployEnvironmentHelp = "Creates and deploys a new Customer Environment, or a new version of an existing one";
    public const string NounDeployAgentHelp = "Creates and deploys a new Infrastructure Agent";
    public const string NounDeployAppHelp = "Installs an App in a previously deployed Convergence Customer Environment";
    public const string NounUndeployEnvironmentHelp = "Creates a new Customer Environment version and terminates the other versions, removing deployments";
    public const string NounUndeployAppHelp = "Uninstalls an App from a Customer Environment";
    public const string NounCreateInfrastructureHelp = "Creates a Customer Infrastructure";
    public const string NounHealthcheckAgentHelp = "Checks if an Infrastructure Agent is connected. Identify the agent by name or by --customer-environment";
    public const string NounPublishDeploymentPackageHelp = "Publishes one or more Deployment Package manifests into the Customer Portal";
    public const string NounPublishInstallationPackageHelp = "Publishes one or more Installation Packages (zip) into the Customer Portal";
    public const string NounDownloadArtifactsHelp = "Downloads all artifacts of a Customer Environment";

    public const string ArgEnvironmentNameHelp = "Name of the Customer Environment. A name is generated when omitted";
    public const string ArgAgentNameHelp = "Name of the Infrastructure Agent. A name is generated when omitted";
    public const string ArgExistingEnvironmentNameHelp = "Name of the Customer Environment";
}
