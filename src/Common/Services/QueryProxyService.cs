using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.OutputObjects;
using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    /// <summary>
    /// Class to be used as proxy in lbos request for queries
    /// </summary>
    public class QueryProxyService
    {
        /// <inheritdoc/>
        public async Task<ExecuteQueryOutput> ExecuteQuery(QueryObject query, int pageNumber, int pageSize, ISession session)
        {
            ExecuteQueryOutput result = null;
            try
            {
                result = await new ExecuteQueryInput()
                {
                    QueryObject = query,
                    PageNumber = 1,
                    PageSize = 1
                }.ExecuteQueryAsync(true);
            }
            catch (Exception e)
            {
                session.LogDebug($"Failed to verify if package exists: {e.Message}");
            }

            return result;
        }
    }
}
