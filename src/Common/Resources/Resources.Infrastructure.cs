namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// Help texts for Customer Infrastructures and Infrastructure Agent health checks.
/// </summary>
public static partial class Resources
{
    public const string InfrastructureNameHelp = "The name of the Customer Infrastructure to be created";
    public const string InfrastructureSiteHelp = "(deprecated) Name of a Site used to match a Customer with the Customer Infrastructure";
    public const string InfrastructureCustomerHelp = "Name of the Customer associated with the Customer Infrastructure";
    public const string InfrastructureExistingNameHelp = "Name of the existing Customer Infrastructure";
    public const string InfrastructureIgnoreIfExistsHelp = "Flag that ignores a throw if an error of type 'Customer Infrastructure already exist' occurs";
    public const string InfrastructureParametersPathHelp = "Path to deployment parameters json file that includes parameters for the Customer Infrastructure";

    public const string GetAgentConnectionNameHelp = "The name of the Infrastructure Agent";
    public const string GetAgentConnectionCustomerEnvironmentHelp = "The name of the Customer Environment associated with the Infrastructure Agent";
}
