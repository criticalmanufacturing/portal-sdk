using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Common.CustomerInfrastructure;
using Cmf.CustomerPortal.Common.Deployment;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.OutputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessObjects.QueryObject.Enums;
using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.InputObjects;
using Cmf.Foundation.Common;
using Cmf.Foundation.Common.Base;
using Cmf.Foundation.Security;
using Cmf.Services.GenericServiceManagement;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public partial class CustomerPortalClient(ISession session, IFileSystem fileSystem) : ICustomerPortalClient
    {

        #region Private Methods

        private static DataSet NgpDataSetToDataSet(NgpDataSet ngpDataSet)
        {
            DataSet ds = new DataSet();

            if (ngpDataSet == null || (string.IsNullOrWhiteSpace(ngpDataSet.XMLSchema) && string.IsNullOrWhiteSpace(ngpDataSet.DataXML)))
            {
                return ds;
            }

            //Insert schema
            using (TextReader a = new StringReader(ngpDataSet.XMLSchema))
            using (XmlReader readerS = new XmlTextReader(a))
            {
                ds.ReadXmlSchema(readerS);
            }

            //Insert data
            byte[] byteArray = Encoding.UTF8.GetBytes(ngpDataSet.DataXML);
            using (MemoryStream stream = new MemoryStream(byteArray))
            using (XmlReader reader = new XmlTextReader(stream))
            {
                try
                {
                    ds.ReadXml(reader);
                }
                catch (ConstraintException ex)
                {
                    throw new Exception("Error while parsing results from getting other Customer Environments to terminate", ex);
                }
            }
            XDocument xd = XDocument.Parse(ngpDataSet.DataXML);

            foreach (DataTable dt in ds.Tables)
            {
                var rs = xd.Descendants(dt.TableName).ToArray();

                for (int i = 0; i < rs.Length; i++)
                {
                    var r = rs[i];
                    DataRowState state = DataRowState.Added;
                    if (r.Attribute("RowState") != null)
                    {
                        state = (DataRowState)Enum.Parse(typeof(DataRowState), r.Attribute("RowState").Value);
                    }

                    DataRow dr = dt.Rows[i];
                    dr.AcceptChanges();

                    switch (state)
                    {
                        case DataRowState.Added:
                            dr.SetAdded();
                            break;
                        case DataRowState.Deleted:
                            dr.Delete();
                            break;
                        case DataRowState.Modified:
                            dr.SetModified();
                            break;
                        case DataRowState.Detached:
                        case DataRowState.Unchanged:
                        default:
                            break;
                    }
                }
            }

            return ds;
        }

        #endregion

        /// <summary>
        /// Gets object by Name
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="name">The object name</param>
        /// <param name="levelsToLoad">Levels to Load</param>
        /// <returns></returns>
        public async Task<T> GetObjectByName<T>(string name, int levelsToLoad = 0) where T : CoreBase, new()
        {
            var output = await new GetObjectByNameInput
            {
                Name = name,
                Type = typeof(T).BaseType.Name == typeof(CoreBase).Name ? new T() : (object)new T().GetType().Name,
                LevelsToLoad = levelsToLoad
            }.GetObjectByNameAsync(true);

            return output.Instance as T;
        }

        public async Task<T> LoadObjectRelations<T>(T obj, Collection<string> relationsNames) where T : CoreBase, new()
        {
            return (await new LoadObjectRelationsInput
            {
                Object = obj,
                RelationNames = relationsNames
            }.LoadObjectRelationsAsync(true)).Object as T;
        }

        public async Task<DataSet> ExecuteQuery(QueryObject queryObject)
        {
            return NgpDataSetToDataSet((await new ExecuteQueryInput
            {
                QueryObject = queryObject
            }.ExecuteQueryAsync(true)).NgpDataSet);
        }

        public async Task<T> TerminateObjects<T, U>(T obj, OperationAttributeCollection operationAttributes = null, bool isToTerminateAllVersions = false) where T : List<U>, new() where U : new()
        {
            return (await new TerminateObjectsInput
            {
                Objects = new Collection<object>(obj.ConvertAll(x => x as object)),
                OperationAttributes = operationAttributes,
                IgnoreLastServiceId = true,
                OperationTarget = isToTerminateAllVersions ? EntityTypeSource.Revision : EntityTypeSource.Version
            }.TerminateObjectsAsync(true)).Objects as T;
        }

        public async Task<CustomerEnvironmentCollection> GetCustomerEnvironmentsById(long[] ids)
        {
            QueryObject query = new QueryObject
            {
                EntityTypeName = "CustomerEnvironment",
                Name = "GetEnvironmentsById",
                Query = new Query()
            };
            query.Query.Distinct = false;
            query.Query.HasParameters = true;
            query.Query.Filters = new FilterCollection() {
                new Filter()
                {
                    Name = "Id",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Operator = FieldOperator.In,
                    Value = ids,
                    LogicalOperator = LogicalOperator.Nothing,
                    FilterType = FilterType.Normal,
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
                    Sort = FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "DefinitionId",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "DefinitionId",
                    Position = 1,
                    Sort = FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "Name",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "Name",
                    Position = 2,
                    Sort = FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "Status",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "Status",
                    Position = 3,
                    Sort = FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "UniversalState",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "UniversalState",
                    Position = 4,
                    Sort = FieldSort.NoSort
                }
            };
            query.Query.Relations = new RelationCollection();

            // execute query
            var result = await ExecuteQuery(query);

            var customerEnvironments = new CustomerEnvironmentCollection();
            foreach (DataRow row in result?.Tables?[0]?.Rows)
            {
                customerEnvironments.Add(new CustomerEnvironment
                {
                    Id = (long)row["Id"],
                    DefinitionId = (long)row["DefinitionId"],
                    Name = (string)row["Name"],
                    Status = (DeploymentStatus)row["Status"],
                    UniversalState = (UniversalState)row["UniversalState"]
                });
            }

            return customerEnvironments;
        }


        /// Gets object by Id
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="name">The object name</param>
        /// <param name="levelsToLoad">Levels to Load</param>
        /// <returns></returns>
        public async Task<T> GetObjectById<T>(long id, int levelsToLoad = 0) where T : CoreBase, new()
        {
            var output = await new GetObjectByIdInput
            {
                Id = id,
                Type = typeof(T).BaseType.Name == typeof(CoreBase).Name ? new T() : (object)new T().GetType().Name,
                LevelsToLoad = levelsToLoad
            }.GetObjectByIdAsync(true);

            return output.Instance as T;
        }

        /// <summary>
        /// Get current user authenticated
        /// </summary>
        /// <returns>Current user</returns>
        public async Task<User> GetCurrentUser()
        {
            var result = await new GetApplicationBootInformationInput().GetApplicationBootInformationAsync(true);
            return result.User;
        }

        /// <summary>
        /// Check if Customer Environment is connected
        /// </summary>
        /// <param name="definitionId">definition id</param>
        /// <returns></returns>

        public async Task<bool> CheckCustomerEnvironmentConnectionStatus(long? definitionId)
        {
            CheckCustomerEnvironmentConnectionStatusOutput output = await new CheckCustomerEnvironmentConnectionStatusInput() { DefinitionId = definitionId }
                    .CheckCustomerEnvironmentConnectionStatusAsync(true);
            return output.CustomerEnvironmentConnectionStatus == InfrastructureConnectionStatus.Connected;
        }

        /// <inheritdoc/>
        public async Task<CustomerEnvironmentApplicationPackage> CreateOrUpdateAppInstallation(long customerEnvironmentId, string appName, string appVersion, string parameters, string softwareLicenseName)
        {
            return (await new CreateOrUpdateAppInstallationInput()
            {
                CustomerEnvironmentId = customerEnvironmentId,
                ApplicationPackageName = appName,
                AppVersion = appVersion,
                Parameters = parameters,
                SoftwareLicenseName = softwareLicenseName
            }.CreateOrUpdateAppInstallationAsync(true)).CustomerEnvironmentApplicationPackage;
        }

        /// <inheritdoc/>
        public async Task<bool> CheckStartDeploymentConnection(CustomerEnvironment customerEnvironment, CustomerInfrastructure customerInfrastructure)
        {
            return (await new CheckStartDeploymentConnectionInput()
            {
                CustomerEnvironment = customerEnvironment,
                CustomerInfrastructure = customerInfrastructure
            }.CheckStartDeploymentConnectionAsync(true)).CanStartDeploymentConnection;
        }

        /// <inheritdoc/>
        public async Task<EntityDocumentationCollection> GetAttachmentsForEntity(EntityBase entityBase)
        {
            return (await new GetAttachmentsForEntityInput()
            {
                Entity = entityBase
            }.GetAttachmentsForEntityAsync(true)).Attachments;
        }

        /// <inheritdoc/>
        public async Task<string> DownloadAttachmentStreaming(long attachmentId)
        {
            using var downloadAttachmentOutput = await new DownloadAttachmentStreamingInput
            {
                attachmentId = attachmentId
            }.DownloadAttachmentAsync(true);

            string outputFile = fileSystem.Path.Combine(
                fileSystem.Path.GetTempPath(),
                downloadAttachmentOutput.FileName.Replace(" ", "").Replace("\"", "")
            );
            session.LogDebug($"Downloading to {outputFile}");

            await using var file = fileSystem.File.OpenWrite(outputFile);
            await downloadAttachmentOutput.Stream.CopyToAsync(file);

            return outputFile;
        }

        /// <inheritdoc/>
        public async Task<string> GetCustomerEnvironmentTerminationLogs(long ceId)
        {
            return (await new GetCustomerEnvironmentTerminationLogsInput()
            {
                CustomerEnvironmentId = ceId
            }.GetCustomerEnvironmentTerminationLogsAsync(true)).Logs;
        }

        /// <inheritdoc/>
        public async Task<EntityType> GetEntityTypeByName(string name)
        {
            return (await new GetEntityTypeByNameInput { Name = name }.GetEntityTypeByNameAsync(true)).EntityType;
        }

        /// <summary>
        /// Update a customer environment.
        /// </summary>
        /// <param name="customerEnvironment">customer environment</param>
        /// <returns></returns>
        public async Task<CustomerEnvironment> UpdateEnvironment(CustomerEnvironment customerEnvironment)
        {
            customerEnvironment.ChangeSet = null;
            return (await new UpdateCustomerEnvironmentInput
            {
                CustomerEnvironment = customerEnvironment,
                DeploymentParametersMergeMode = DeploymentParametersMergeMode.Merge
            }.UpdateCustomerEnvironmentAsync(true)).CustomerEnvironment;
        }

        /// <summary>
        /// Get Customer Environment By Id
        /// </summary>
        /// <param name="customerEnvironmentId">customer Environment id</param>
        /// <returns></returns>
        public async Task<CustomerEnvironment> GetCustomerEnvironmentById(long customerEnvironmentId, int levelsToLoad = 0)
        {
            return (await new GetCustomerEnvironmentByIdInput()
            {
                CustomerEnvironmentId = customerEnvironmentId,
                IsToLoadParameters = true,
                LevelsToLoad = levelsToLoad
            }.GetCustomerEnvironmentByIdAsync(true)).CustomerEnvironment;
        }

        /// <summary>
        /// Starts the uninstallation of an application, given its id and options <see cref="StartAppUninstallInput"/>.
        /// </summary>
        public async Task StartAppUninstall(long appId, bool removeDeployments, bool removeVolumes, bool undeploy)
        {
            await new StartAppUninstallInput
            {
                CustomerEnvironmentApplicationPackageId = appId,
                RemoveDeployments = removeDeployments,
                RemoveVolumes = removeVolumes,
                Undeploy = undeploy,
            }.StartAppUninstallAsync(true);
        }

        /// <summary>
        /// Gets the infrastructure agent related to a customer environment, given the environment's name.
        /// </summary>
        /// <param name="customerEnvironmentName">Name of the customer environment.</param>
        /// <returns>The infrastructure agent associated with the specified customer environment.</returns>
        public async Task<CustomerEnvironment> GetCustomerInfrastructureAgentByCustomerEnvironment(string customerEnvironmentName)
        {

            QueryObject query = new QueryObject();
            query.Description = "Gets the infrastructure agent related to a customer environment, given the environment's name.";
            query.EntityTypeName = "CustomerEnvironment";
            query.Name = "GetCustomerInfrastructureAgentByCustomerEnvironment";
            query.Query = new Query();
            query.Query.Distinct = false;
            query.Query.Filters = new FilterCollection() {
                new Filter()
                {
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Name = "Name",
                    Value = customerEnvironmentName,
                    Operator = FieldOperator.IsEqualTo
                },
                new Filter()
                {
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Name = "UniversalState",
                    Value = UniversalState.Created,
                    Operator = FieldOperator.IsEqualTo,
                    LogicalOperator = LogicalOperator.AND
                 },
                new Filter()
                {
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    Name = "Version",
                    Value = 1,
                    Operator = FieldOperator.GreaterThan,
                    LogicalOperator = LogicalOperator.AND
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
                    Alias = "Revision",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "Revision",
                    Position = 2,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "Name",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "Name",
                    Position = 3,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "CustomerInfrastructureInfrastructureAgent_Id",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_CustomerInfrastructure_InfrastructureAgent",
                    IsUserAttribute = false,
                    Name = "Id",
                    Position = 4,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "CustomerInfrastructureInfrastructureAgent_Name",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_CustomerInfrastructure_InfrastructureAgent",
                    IsUserAttribute = false,
                    Name = "Name",
                    Position = 6,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "CustomerInfrastructureInfrastructureAgent_Revision",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_CustomerInfrastructure_InfrastructureAgent",
                    IsUserAttribute = false,
                    Name = "Revision",
                    Position = 7,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "CustomerInfrastructureInfrastructureAgent_DefinitionId",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_CustomerInfrastructure_InfrastructureAgent",
                    IsUserAttribute = false,
                    Name = "DefinitionId",
                    Position = 8,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "CustomerInfrastructure_Id",
                    ObjectName = "CustomerInfrastructure",
                    ObjectAlias = "CustomerEnvironment_CustomerInfrastructure_2",
                    IsUserAttribute = false,
                    Name = "InfrastructureAgentId",
                    Position = 9,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                }
            };
            query.Query.Relations = new RelationCollection() {
                new Relation()
                {
                    Alias = "",
                    IsRelation = false,
                    Name = "",
                    SourceEntity = "CustomerEnvironment",
                    SourceEntityAlias = "CustomerEnvironment_1",
                    SourceJoinType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.JoinType.LeftJoin,
                    SourceProperty = "CustomerInfrastructureId",
                    TargetEntity = "CustomerInfrastructure",
                    TargetEntityAlias = "CustomerEnvironment_CustomerInfrastructure_2",
                    TargetJoinType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.JoinType.LeftJoin,
                    TargetProperty = "Id"
                }
                ,
                new Relation()
                {
                    Alias = "",
                    IsRelation = false,
                    Name = "",
                    SourceEntity = "CustomerInfrastructure",
                    SourceEntityAlias = "CustomerEnvironment_CustomerInfrastructure_2",
                    SourceJoinType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.JoinType.InnerJoin,
                    SourceProperty = "InfrastructureAgentId",
                    TargetEntity = "CustomerEnvironment",
                    TargetEntityAlias = "CustomerEnvironment_CustomerInfrastructure_InfrastructureAgent",
                    TargetJoinType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.JoinType.InnerJoin,
                    TargetProperty = "Id"
                }
            };


            DataSet dataSet = await ExecuteQuery(query);

            if (dataSet?.Tables?.Count > 0 && dataSet?.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                CustomerEnvironment customerEnvironment = new()
                {
                    Id = (long)row["Id"],
                    Name = (string)row["Name"],
                    DefinitionId = (long)row["DefinitionId"],
                    Revision = (string)row["Revision"]
                };
                
                if(!row.IsNull("CustomerInfrastructure_Id"))
                {
                    customerEnvironment.CustomerInfrastructure = new CustomerInfrastructure
                    {
                        Id = (long)row["CustomerInfrastructure_Id"],
                    };
                    
                    if(!row.IsNull("CustomerInfrastructureInfrastructureAgent_Id"))
                    {
                        customerEnvironment.CustomerInfrastructure.InfrastructureAgent = new CustomerEnvironment
                        {
                            Id = (long)row["CustomerInfrastructureInfrastructureAgent_Id"],
                            Name = (string)row["CustomerInfrastructureInfrastructureAgent_Name"],
                            DefinitionId = (long)row["CustomerInfrastructureInfrastructureAgent_DefinitionId"],
                            Revision = (string)row["CustomerInfrastructureInfrastructureAgent_Revision"],
                        };
                    }
                }

                if(customerEnvironment.CustomerInfrastructure == null || customerEnvironment.CustomerInfrastructure.InfrastructureAgent == null)
                {
                    return null!; // cases where environment exists but has no agent or infra associated
                }

                return customerEnvironment.CustomerInfrastructure.InfrastructureAgent;
            }
            else
            {
                throw new NotFoundException($"No customer environment found for name {customerEnvironmentName}");
            }
        }
    }
}
