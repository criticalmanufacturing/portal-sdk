using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.InputObjects;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public class CustomerPortalClient : ICustomerPortalClient
    {
        private static readonly SemaphoreSlim _transportLock = new SemaphoreSlim(1, 1);

        private readonly ISession _session;
        private Transport _transport;

        public CustomerPortalClient(ISession session)
        {
            _session = session;
        }

        #region Private Methods

        private static DataSet NgpDataSetToDataSet(NgpDataSet ngpDataSet)
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

        #endregion

        /// <summary>
        /// Gets object by Id
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

        public async Task<T> TerminateObjects<T, U>(T obj, OperationAttributeCollection operationAttributes = null) where T : List<U>, new() where U : new()
        {
            return (await new TerminateObjectsInput
            {
                Objects = new Collection<object>(obj.ConvertAll(x => x as object)),
                OperationAttributes = operationAttributes,
                IgnoreLastServiceId = true
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
                    Operator = Cmf.Foundation.Common.FieldOperator.In,
                    Value = ids,
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
                },
                new Field()
                {
                    Alias = "Status",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "Status",
                    Position = 3,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                },
                new Field()
                {
                    Alias = "UniversalState",
                    ObjectName = "CustomerEnvironment",
                    ObjectAlias = "CustomerEnvironment_1",
                    IsUserAttribute = false,
                    Name = "UniversalState",
                    Position = 4,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                }
            };
            query.Query.Relations = new RelationCollection();

            // execute query
            var result = await ExecuteQuery(query);

            var customerEnvironments = new CustomerEnvironmentCollection();
            if (result?.Tables?.Count > 0)
            {
                foreach (DataRow row in result.Tables[0].Rows)
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
            }

            return customerEnvironments;
        }

        /// <summary>
        /// Setup Message Bus Transport
        /// </summary>
        /// <returns>An instance of Message Bus transport</returns>
        public async Task<Transport> GetMessageBusTransport()
        {
            if (_transport != null)
            {
                return _transport;
            }

            await _transportLock.WaitAsync(TimeSpan.FromSeconds(30));

            try
            {
                if (_transport != null)
                {
                    return _transport;
                }

                _session.LogDebug($"Configuring message bus...");

                // create new transport using the config
                TransportConfig transportConfig = JsonConvert.DeserializeObject<TransportConfig>((await new GetApplicationBootInformationInput().GetApplicationBootInformationAsync(true)).TransportConfig);
                transportConfig.ApplicationName = "Customer Portal Client";
                transportConfig.TenantName = ClientConfigurationProvider.ClientConfiguration.ClientTenantName;
                transportConfig.SecurityToken = transportConfig.SecurityToken == null ? _session.AccessToken : transportConfig.SecurityToken;
                Transport messageBus = new Transport(transportConfig);

                // Register events
                messageBus.Connected += () =>
                {
                    _session.LogDebug("Message Bus Connect!");  
                };

                messageBus.Disconnected += () =>
                {
                    _session.LogDebug("Message Bus Disconnected!");
                };

                messageBus.InformationMessage += (string message) =>
                {
                    _session.LogDebug(message);
                };

                messageBus.Exception += (string message) =>
                {
                    _session.LogError(new Exception(message));
                };

                // start the message bus and ensure that it is connected
                messageBus.Start();

                bool failedConnection = false;
                TimeSpan timeout = TimeSpan.FromSeconds(2);
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    failedConnection = await Task.Run(async () =>
                    {
                        while (!messageBus.IsConnected)
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(0.1), cancellationTokenSource.Token);
                            }
                            catch (TaskCanceledException)
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                }

                // if we failed to setup the message bus, throw error
                if (failedConnection)
                {
                    Exception error = new Exception("Timed out waiting for client to connect to MessageBus");
                    _session.LogError(error);
                    throw error;
                }

                _session.LogDebug("Message Bus connected with success!");

                _transport = messageBus;
            }
            finally
            {
                _transportLock.Release();
            }

            return _transport;
        }
    }
}
