using Cmf.Foundation.Common;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Collections.Generic;
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
        /// <param name="exceptionTypeAndErrorMsg">Dictionary with mapping the exception type and the respective error message to be presented</param>
        /// <param name="levelsToLoad">Levels to load</param>
        /// <param name="msgInfoBeforeCall">Message to use on log before the request</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<T> GetObjectByNameWithDefaultErrorMessage<T>(ISession session, ICustomerPortalClient customerPortalClient, string objectName, Dictionary<CmfExceptionType, string> exceptionTypeAndErrorMsg, int levelsToLoad = 0, string msgInfoBeforeCall = null) where T : CoreBase, new()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(msgInfoBeforeCall))
                {
                    session.LogInformation(msgInfoBeforeCall);
                }

                return await customerPortalClient.GetObjectByName<T>(objectName, levelsToLoad);
            }
            catch(CmfFaultException e)
            {
                if (Enum.TryParse(e.Code?.Name, out CmfExceptionType exceptionType) && exceptionTypeAndErrorMsg != null && exceptionTypeAndErrorMsg.ContainsKey(exceptionType))
                {
                    string msgForError = exceptionTypeAndErrorMsg[exceptionType];
                    if (string.IsNullOrWhiteSpace(msgForError))
                    {
                        msgForError = e.Message;
                    }
                    session.LogError(msgForError);
                    throw new Exception(msgForError);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}



