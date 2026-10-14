namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// The logs of a Customer Environment version to get.
/// </summary>
public enum EnvironmentLogType
{
    /// <summary>
    /// The installation (deployment) logs and the termination logs.
    /// </summary>
    All,

    /// <summary>
    /// The installation (deployment) logs.
    /// </summary>
    Installation,

    /// <summary>
    /// The termination logs.
    /// </summary>
    Termination
}
