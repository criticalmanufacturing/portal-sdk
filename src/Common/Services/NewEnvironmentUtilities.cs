using System;
using System.Data;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.LightBusinessObjects.Infrastructure.Errors;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class NewEnvironmentUtilities : INewEnvironmentUtilities
    {
        private readonly ISession _session;
        private readonly ICustomerPortalClient _customerPortalClient;

        public NewEnvironmentUtilities(ISession session, ICustomerPortalClient customerPortalClient)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
        }
        private static DeploymentTarget GetAbbreviatedDeploymentTargetFromDeploymentTargetValue(string deploymentTargetValue)
        {
            return deploymentTargetValue switch
            {
                "DockerSwarmOnPremisesTarget" => DeploymentTarget.dockerswarm,
                "PortainerV2Target" => DeploymentTarget.portainer,
                "KubernetesOnPremisesTarget" => DeploymentTarget.KubernetesOnPremisesTarget,
                "KubernetesRemoteTarget" => DeploymentTarget.KubernetesRemoteTarget,
                "OpenShiftOnPremisesTarget" => DeploymentTarget.OpenShiftOnPremisesTarget,
                "OpenShiftRemoteTarget" => DeploymentTarget.OpenShiftRemoteTarget,
                "AzureKubernetesServiceTarget" => DeploymentTarget.AzureKubernetesServiceTarget,
                _ => throw new Exception($"Target parameter '{deploymentTargetValue}' not supported"),
            };
        }

        public string GetDeploymentTargetValue(DeploymentTarget abbreviatedDeploymentTarget)
        {
            switch (abbreviatedDeploymentTarget)
            {
                case DeploymentTarget.dockerswarm:
                    return "DockerSwarmOnPremisesTarget";
                case DeploymentTarget.portainer:
                    return "PortainerV2Target";
                case DeploymentTarget.KubernetesOnPremisesTarget:
                case DeploymentTarget.KubernetesRemoteTarget:
                case DeploymentTarget.OpenShiftOnPremisesTarget:
                case DeploymentTarget.OpenShiftRemoteTarget:
                case DeploymentTarget.AzureKubernetesServiceTarget:
                    return abbreviatedDeploymentTarget.ToString();
                default:
                    throw new Exception($"Target parameter '{abbreviatedDeploymentTarget}' not supported");
            }
        }


        /// <summary>
        /// Informs if the deployment is a remote target
        /// </summary>
        /// <param name="depoymentTargetValue">depoymentTargetValue</param>
        /// <returns></returns>
        public static bool IsRemoteDeploymentTarget(string depoymentTargetValue)
        {
            DeploymentTarget deploymentTarget = GetAbbreviatedDeploymentTargetFromDeploymentTargetValue(depoymentTargetValue);

            return deploymentTarget switch
            {
                DeploymentTarget.portainer or DeploymentTarget.KubernetesRemoteTarget or DeploymentTarget.OpenShiftRemoteTarget or DeploymentTarget.AzureKubernetesServiceTarget => true,
                _ => false,
            };
        }

        /// <inheritdoc/>
        public async Task CheckEnvironmentConnection(CustomerEnvironment environment)
        {
            _session.LogInformation($"Checking the environment connection of the Customer environment {environment?.Name}...");

            if (environment != null && IsRemoteDeploymentTarget(environment.DeploymentTarget))
            {
                var checkConnectionResult = await _customerPortalClient.CheckStartDeploymentConnection(environment, environment.CustomerInfrastructure);

                if (!checkConnectionResult)
                {
                    throw new CmfFaultException("The deployment can't be started because the connection can't be established with the agent!");
                }
            }
        }

        /// <summary>
        /// Returns true if the Deployment Target is remote, otherwise return false
        /// </summary>
        /// <param name="abbreviatedDeploymentTarget"></param>
        /// <returns></returns>
        public static bool IsRemoteDeploymentTarget(DeploymentTarget abbreviatedDeploymentTarget)
        {
            return abbreviatedDeploymentTarget switch
            {
                DeploymentTarget.portainer or DeploymentTarget.KubernetesRemoteTarget or DeploymentTarget.OpenShiftRemoteTarget or DeploymentTarget.AzureKubernetesServiceTarget => true,
                _ => false,
            };
        }

        /// <inheritdoc/>
        public async Task<CustomerEnvironmentCollection> GetOtherVersionToTerminate(CustomerEnvironment customerEnvironment)
        {
            QueryObject query = new QueryObject
            {
                EntityTypeName = "CustomerEnvironment",
                Name = "GetOtherEnvironmentsToTerminate",
                Query = new Query()
            };
            query.Query.Distinct = false;
            query.Query.Filters = new FilterCollection() {
                new Filter()
                {
                    Name = "Version",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.GreaterThan,
                    Value = "0",
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.AND,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                },
                new Filter()
                {
                    Name = "Version",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.IsNotEqualTo,
                    Value = customerEnvironment.Version,
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.AND,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                },
                new Filter()
                {
                    Name = "UniversalState",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.IsNotEqualTo,
                    Value = 4,
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.AND,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                },
                new Filter()
                {
                    Name = "Status",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.In,
                    Value = new int[] {
                        (int)DeploymentStatus.NotDeployed,
                        (int)DeploymentStatus.DeploymentFailed,
                        (int)DeploymentStatus.DeploymentPartiallySucceeded,
                        (int)DeploymentStatus.DeploymentSucceeded
                    },
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.AND,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                },
                new Filter()
                {
                    Name = "Name",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.IsEqualTo,
                    Value = customerEnvironment.Name,
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.Nothing,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                }
            };
            query.Query.Fields = new FieldCollection() {
                new Field()
                {
                    Alias = "Id",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "Id",
                    Position = 0,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "DefinitionId",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "DefinitionId",
                    Position = 1,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "Name",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "Name",
                    Position = 2,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                }
            };
            query.Query.Relations = new RelationCollection();

            DataSet dataSet = await _customerPortalClient.ExecuteQuery(query);

            var result = new CustomerEnvironmentCollection();
            if (dataSet?.Tables?.Count > 0)
            {
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    result.Add(new CustomerEnvironment
                    {
                        Id = (long)row["Id"],
                        Name = (string)row["Name"],
                        DefinitionId = (long)row["DefinitionId"]
                    });
                }
            }

            return result;
        }
    }
}
