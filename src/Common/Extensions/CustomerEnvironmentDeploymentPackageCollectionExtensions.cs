using Cmf.CustomerPortal.BusinessObjects;

namespace Cmf.CustomerPortal.Sdk.Common.Extensions;

public static class CustomerEnvironmentDeploymentPackageCollectionExtensions
{
    public static bool SoftEquals(this CustomerEnvironmentDeploymentPackageCollection cedpCollection, CustomerEnvironmentDeploymentPackageCollection targetcedpCollection)
    {
        if (cedpCollection.Count != targetcedpCollection.Count)
        {
            return false;
        }

        for (int i = 0; i < cedpCollection.Count; i++)
        {
            var cedp1 = cedpCollection[i];
            var cedp2 = targetcedpCollection[i];

            if (cedp1.SourceEntity != cedp2.SourceEntity ||
                cedp1.TargetEntity != cedp2.TargetEntity ||
                cedp1.SoftwareLicense != cedp2.SoftwareLicense)
            {
                return false;
            }
        }

        return true;
    }
}
