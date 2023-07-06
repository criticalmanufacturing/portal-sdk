using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using System;
using System.Data;
using System.Threading.Tasks;

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
						(int)DeploymentStatus.DeploymentSucceeded,
						(int)DeploymentStatus.TerminationFailed
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
