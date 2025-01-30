using Cmf.CustomerPortal.BusinessObjects;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

public interface ILicenseServices
{
    /// <summary>
    /// Name of SoftwareLicense is not unique, since it is a versioned entity
    /// To load a license by name, we need to get it by the LicenseUniqueName
    /// </summary>
    /// <param name="licenseUniqueName"></param>
    /// <returns></returns>
    Task<CPSoftwareLicense> GetLicenseByUniqueName(string licenseUniqueName);
}
