using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.OutputObjects;
using Cmf.Foundation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

internal class LicenseServices : ILicenseServices
{
    /// <summary>
    /// Name of SoftwareLicense is not unique, since it is a versioned entity
    /// To load a license by name, we need to get it by the LicenseUniqueName
    /// </summary>
    /// <param name="licenseUniqueName"></param>
    /// <returns></returns>
    public async Task<CPSoftwareLicense> GetLicenseByUniqueName(string licenseUniqueName)
    {
        FilterCollection fcCollection = new FilterCollection()
            {
                new Filter()
                {
                    Name = "LicenseUniqueName",
                    LogicalOperator = LogicalOperator.AND,
                    Operator = FieldOperator.IsEqualTo,
                    Value = licenseUniqueName
                },
                new Filter() // exclude Definition or Revision results
                {
                    Name = "Version",
                    LogicalOperator= LogicalOperator.AND,
                    Operator= FieldOperator.GreaterThan,
                    Value = 0
                }
            };

        GetObjectsByFilterInput gobfiInput = new GetObjectsByFilterInput
        {
            Filter = fcCollection,
            Type = Activator.CreateInstance<CPSoftwareLicense>()
        };

        GetObjectsByFilterOutput gobfOutput = await gobfiInput.GetObjectsByFilterAsync(true);

        if (gobfOutput.Instance.Count == 0)
        {
            throw new Exception($"License with name {licenseUniqueName} does not exist");
        }

        if (gobfOutput.Instance.Count > 1)
        {
            throw new Exception($"Too many matches for license {licenseUniqueName}");
        }

        return (CPSoftwareLicense)gobfOutput.Instance[0];
    }
}
