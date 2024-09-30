using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.OutputObjects;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using Cmf.Services.GenericServiceManagement;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class ManifestsDownloaderHandler : IManifestsDownloaderHandler
    {
        private readonly ISession _session;

        private readonly ICustomerPortalClient _customerPortalClient;

        public ManifestsDownloaderHandler(ISession session, ICustomerPortalClient customerPortalClient)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
        }

        public async Task Handle(string name, DirectoryInfo outputDir)
        {
            _session.LogInformation($"Checking if customer environment {name} exists...");
            // let's see if the environment already exists
            CustomerEnvironment environment = null;
            try
            {
                environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(name);

                _session.LogInformation($"Customer environment {name} actually exists...");
            }
            catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
            {
                // when was not found
                string errorMessage = $"Customer environment {name} doesn't exist...";
                _session.LogInformation(errorMessage);

                throw new NotFoundException(errorMessage);
            }

            // get the attachments of the current customer environment
            GetAttachmentsForEntityInput input = new GetAttachmentsForEntityInput()
            {
                Entity = environment
            };

            await Task.Delay(TimeSpan.FromSeconds(1));
            GetAttachmentsForEntityOutput output = await input.GetAttachmentsForEntityAsync(true);
            EntityDocumentation attachmentToDownload = null;
            if (output?.Attachments.Count > 0)
            {
                output.Attachments.Sort((a, b) => DateTime.Compare(b.CreatedOn, a.CreatedOn));
                attachmentToDownload = output.Attachments.Where(x => x.Filename.Contains(environment.Name)).FirstOrDefault();
            }

            if (attachmentToDownload == null)
            {
                _session.LogError("No attachment was found to download.");
            }
            else
            {
                // Download the attachment
                _session.LogDebug($"Downloading attachment {attachmentToDownload.Filename}");

                string outputFile = "";
                using (DownloadAttachmentStreamingOutput downloadAttachmentOutput = await new DownloadAttachmentStreamingInput() { attachmentId = attachmentToDownload.Id }.DownloadAttachmentAsync(true))
                {
                    int bytesToRead = 10000;
                    byte[] buffer = new byte[bytesToRead];

                    outputFile = Path.Combine(Path.GetTempPath(), downloadAttachmentOutput.FileName);
                    outputFile = outputFile.Replace(" ", "").Replace("\"", "");
                    _session.LogDebug($"Downloading to {outputFile}");

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

                _session.LogDebug($"Extracting environment contents to {extractionTarget}");

                // extract the zip to the previously created dir
                ZipFile.ExtractToDirectory(outputFile, extractionTarget);

                // get target full dir
                string outputPathFullName = outputDir?.FullName;
                if (string.IsNullOrEmpty(outputPathFullName))
                {
                    outputPathFullName = Path.Combine(Directory.GetCurrentDirectory(), "out", environment.Name);
                }
                else
                {
                    outputPathFullName = Path.GetFullPath(outputPathFullName);
                }

                // ensure the output path exists
                Directory.CreateDirectory(outputPathFullName);

                _session.LogDebug($"Moving environment contents from {extractionTarget} to {outputPathFullName}");

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

                _session.LogInformation($"Customer environment created at {outputPathFullName}");
            }
        }
    }
}
