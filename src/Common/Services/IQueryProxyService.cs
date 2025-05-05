using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.OutputObjects;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    /// <summary>
    /// Interface to be used as proxy in lbos request for queries
    /// </summary>
    public interface IQueryProxyService
    {
        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        Task<ExecuteQueryOutput> ExecuteQuery(QueryObject query, int pageNumber, int pageSize, ISession session);
    }
}
