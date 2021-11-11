using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.InputObjects;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class NewEnvironmentUtilities : INewEnvironmentUtilities
    {
		private DataSet NgpDataSetToDataSet(NgpDataSet ngpDataSet)
        {
			DataSet ds = new DataSet();

			if (ngpDataSet == null || (string.IsNullOrWhiteSpace(ngpDataSet.XMLSchema) && string.IsNullOrWhiteSpace(ngpDataSet.DataXML)))
			{
				return ds;
			}

			//Insert schema
			TextReader a = new StringReader(ngpDataSet.XMLSchema);
			XmlReader readerS = new XmlTextReader(a);
			ds.ReadXmlSchema(readerS);
			XDocument xdS = XDocument.Parse(ngpDataSet.XMLSchema);

			//Insert data
			UTF8Encoding encoding = new UTF8Encoding();
			Byte[] byteArray = encoding.GetBytes(ngpDataSet.DataXML);
			MemoryStream stream = new MemoryStream(byteArray);

			XmlReader reader = new XmlTextReader(stream);
			try
			{
				ds.ReadXml(reader);
			}
			catch (ConstraintException ex)
			{
				throw new Exception("Error while parsing results from getting other Customer Environments to terminate", ex);
			}
			XDocument xd = XDocument.Parse(ngpDataSet.DataXML);

			foreach (DataTable dt in ds.Tables)
			{
				var rs = from row in xd.Descendants(dt.TableName)
						 select row;

				int i = 0;
				foreach (var r in rs)
				{
					DataRowState state = DataRowState.Added;
					if (r.Attribute("RowState") != null)
					{
						state = (DataRowState)Enum.Parse(typeof(DataRowState), r.Attribute("RowState").Value);
					}

					DataRow dr = dt.Rows[i];
					dr.AcceptChanges();

					if (state == DataRowState.Deleted)
					{
						dr.Delete();
					}
					else if (state == DataRowState.Added)
					{
						dr.SetAdded();
					}
					else if (state == DataRowState.Modified)
					{
						dr.SetModified();
					}

					i++;
				}
			}

			return ds;
		}

		private readonly ISession _session;
		private readonly ICustomerPortalClient _customerPortalClient;

		public NewEnvironmentUtilities(ISession session, ICustomerPortalClient customerPortalClient)
		{
			_session = session;
			_customerPortalClient = customerPortalClient;
		}

		public string GetDeploymentTargetValue(string abbreviatedDeploymentTarget)
        {
            switch (abbreviatedDeploymentTarget)
            {
                case "portainer":
                    return "PortainerV2Target";
                case "dockerswarm":
                    return "DockerSwarmOnPremisesTarget";
                default:
                    throw new Exception($"Target parameter '{abbreviatedDeploymentTarget}' not recognized");
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
			if (dataSet != null && dataSet.Tables.Count == 1 && dataSet.Tables[0].Rows.Count > 0)
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
