using Cmf.CustomerPortal.BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

public interface ILicenseServices
{
    /// <summary>
    /// Gets a collection of Software Licenses, searching by their LicenseUniqueName propery.
    /// </summary>
    /// <param name="licensesUniqueNames">The LicenseUniqueName values of all Software Licenses to get.</param>
    Task<IEnumerable<CPSoftwareLicense>> GetLicensesByUniqueName(string[] licensesUniqueNames);
}
