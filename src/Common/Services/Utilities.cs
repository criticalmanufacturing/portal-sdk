using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.OutputObjects;
using Cmf.Foundation.Common;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using Cmf.Services.GenericServiceManagement;

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
            catch (CmfFaultException e)
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

        /// <summary>
        /// Downloads the attachment and extract the contents to the outputDir
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="attachmentToDownload">Attachment to download</param>
        /// <param name="outputDir">Output directory to extract the contents</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task DownloadAttachment(ISession session, EntityDocumentation attachmentToDownload, DirectoryInfo outputDir)
        {
            if (attachmentToDownload == null)
            {
                session.LogError("No attachment was found to download.");
            }
            else
            {
                // Download the attachment
                session.LogDebug($"Downloading attachment {attachmentToDownload.Filename}");

                string outputFile = "";
                using (DownloadAttachmentStreamingOutput downloadAttachmentOutput = await new DownloadAttachmentStreamingInput() { attachmentId = attachmentToDownload.Id }.DownloadAttachmentAsync(true))
                {
                    int bytesToRead = 10000;
                    byte[] buffer = new byte[bytesToRead];

                    outputFile = Path.Combine(Path.GetTempPath(), downloadAttachmentOutput.FileName);
                    outputFile = outputFile.Replace(" ", "").Replace("\"", "");
                    session.LogDebug($"Downloading to {outputFile}");

                    using (BinaryWriter streamWriter = new BinaryWriter(File.Open(outputFile, FileMode.Create, FileAccess.Write)))
                    {
                        int length;
                        do
                        {
                            length = downloadAttachmentOutput.Stream.Read(buffer, 0, bytesToRead);
                            streamWriter.Write(buffer, 0, length);
                            buffer = new byte[bytesToRead];

                        } while (length > 0);
                    }
                }

                // create the dir to extract to
                string extractionTarget = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(extractionTarget);

                session.LogDebug($"Extracting attachment contents to {extractionTarget}");

                // extract the zip to the previously created dir
                ZipFile.ExtractToDirectory(outputFile, extractionTarget);

                // get target full dir
                string outputPathFullName = outputDir?.FullName;
                if (string.IsNullOrEmpty(outputPathFullName))
                {
                    outputPathFullName = Path.Combine(Directory.GetCurrentDirectory(), "out", outputFile.Replace(".zip", ""));
                }
                else
                {
                    outputPathFullName = Path.GetFullPath(outputPathFullName);
                }

                // ensure the output path exists
                Directory.CreateDirectory(outputPathFullName);

                session.LogDebug($"Moving attachment contents from {extractionTarget} to {outputPathFullName}");

                // create all of the directories
                foreach (string dirPath in Directory.GetDirectories(extractionTarget, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(extractionTarget, outputPathFullName));
                }

                // copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(extractionTarget, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(extractionTarget, outputPathFullName), true);
                }

                session.LogDebug($"Attachment successfully downloaded to {outputPathFullName}");
            }
        }
    }
}



