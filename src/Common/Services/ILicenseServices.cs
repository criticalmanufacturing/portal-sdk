using Cmf.CustomerPortal.BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

public interface ILicenseServices
{
    /// <summary>
    /// Gets the applications requiring licenses in a deployment package and its dependencies, as reported by Portal.
    /// </summary>
    /// <param name="deploymentPackageId">Id of the target deployment package.</param>
    /// <returns>The required application names, or an empty array when no licenses are required.</returns>
    Task<string[]> GetRequiredApplicationNames(long deploymentPackageId);

    /// <summary>
    /// Gets a collection of Software Licenses, searching by their LicenseUniqueName propery.
    /// </summary>
    /// <param name="licensesUniqueNames">The LicenseUniqueName values of all Software Licenses to get.</param>
    Task<IEnumerable<CPSoftwareLicense>> GetLicensesByUniqueName(string[] licensesUniqueNames);
}
