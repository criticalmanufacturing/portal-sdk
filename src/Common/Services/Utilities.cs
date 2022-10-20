using Cmf.Foundation.Common.Base;
using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class Utilities
    {
        /// <summary>
        /// Makes the request GetObjectByName of type T, but return a controlled exception and log error message. Beside this, it's possible to log some message before the request.
        /// </summary>
        /// <typeparam name="T">Type of object to get by name</typeparam>
        /// <param name="session">Session</param>
        /// <param name="customerPortalClient">Customer Portal Client to make requests to API</param>
        /// <param name="objectName">Name of object</param>
        /// <param name="msgForError">Error Message to use on exception and log</param>
        /// <param name="levelsToLoad">Levels to load</param>
        /// <param name="msgInfoBeforeCall">Message to use on log before the request</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Task<T> GetObjectByNameWithDefaultErrorMessage<T>(ISession session, ICustomerPortalClient customerPortalClient, string objectName, string msgForError, int levelsToLoad = 0, string msgInfoBeforeCall = null) where T : CoreBase, new()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(msgInfoBeforeCall))
                {
                    session.LogInformation(msgInfoBeforeCall);
                }

                return customerPortalClient.GetObjectByName<T>(objectName, levelsToLoad);
            }
            catch(Exception e)
            {
                if(string.IsNullOrWhiteSpace(msgForError))
                {
                    msgForError = e.Message;
                }

                session.LogError(msgForError);
                throw new Exception(msgForError);
            }
        }
    }
}



