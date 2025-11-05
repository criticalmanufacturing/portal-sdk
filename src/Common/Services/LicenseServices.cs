using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common;
using Cmf.Foundation.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

internal class LicenseServices : ILicenseServices
{
    /// <inheritdoc/>
    public async Task<IEnumerable<CPSoftwareLicense>> GetLicensesByUniqueName(string[] licensesUniqueNames)
    {
        FilterCollection fcCollection =
            [
                new Filter()
                {
                    Name = "LicenseUniqueName",
                    LogicalOperator = LogicalOperator.AND,
                    Operator = FieldOperator.In,
                    Value = licensesUniqueNames
                },
                new Filter() // exclude Definition or Revision results
                {
                    Name = "Version",
                    LogicalOperator= LogicalOperator.AND,
                    Operator= FieldOperator.GreaterThan,
                    Value = 0
                },
                 new Filter()
                {
                    Name = "UniversalState",
                    LogicalOperator= LogicalOperator.AND,
                    Operator= FieldOperator.NotIn,
                    Value = new [] { (int)UniversalState.Terminated, (int)UniversalState.Frozen }
                }
            ];

        GetObjectsByFilterInput gobfiInput = new()
        {
            Filter = fcCollection,
            Type = new CPSoftwareLicense()
        };

        var objects = (await gobfiInput.GetObjectsByFilterAsync(true)).Instance;
        if (objects.Count == 0 || objects.Any(l => (l as CPSoftwareLicense) == null))
        {
            throw new Exception("No Licenses found");
        }

        var licenses = objects.Cast<CPSoftwareLicense>();

        // compute missing licenses
        var missingLicenses = licensesUniqueNames
        .Except(licenses.Select(x => x.LicenseUniqueName))
        .ToArray();

        if (missingLicenses.Length > 0)
        {
            // show the missing licenses
            throw new Exception($"The following Licenses were not found: {string.Join(", ", missingLicenses)}");
        }

        return licenses;
    }
}
