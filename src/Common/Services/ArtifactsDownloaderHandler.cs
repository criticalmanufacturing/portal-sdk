using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.OutputObjects;
using Cmf.Services.GenericServiceManagement;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class ArtifactsDownloaderHandler : IArtifactsDownloaderHandler
    {
        private readonly ISession _session;

        public ArtifactsDownloaderHandler(ISession session)
        {
            _session = session;
        }

        public async Task<bool> Handle(EntityBase deployEntity, string outputPath)
        {
            string entityType = deployEntity.GetType() == typeof(CustomerEnvironment) ? "Customer Environment" : "App";

            // get the attachments of the current customer environment or app
            GetAttachmentsForEntityInput input = new GetAttachmentsForEntityInput()
            {
                Entity = deployEntity
            };

            await Task.Delay(TimeSpan.FromSeconds(1));
            GetAttachmentsForEntityOutput output = await input.GetAttachmentsForEntityAsync(true);
            EntityDocumentation attachmentToDownload = null;
            if (output?.Attachments.Count > 0)
            {
                string prefix = entityType.Replace(" ", "");
                output.Attachments.Sort((a, b) => DateTime.Compare(b.CreatedOn, a.CreatedOn));
                attachmentToDownload = output.Attachments.Where(x => x.Filename.StartsWith($"{prefix}_{deployEntity.Name}")).FirstOrDefault();
            }
            
            if (attachmentToDownload == null)
            {
                _session.LogError("No attachment was found to download.");
                return false;
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

                _session.LogDebug($"Extracting attachment contents to {extractionTarget}");

                // extract the zip to the previously created dir
                ZipFile.ExtractToDirectory(outputFile, extractionTarget);

                // get target full dir
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(Directory.GetCurrentDirectory(), "out");
                }

                outputPath = Path.Combine(outputPath, deployEntity.Name);

                // ensure the output path exists
                Directory.CreateDirectory(outputPath);

                _session.LogDebug($"Moving attachment contents from {extractionTarget} to {outputPath}");

                // create all of the directories
                foreach (string dirPath in Directory.GetDirectories(extractionTarget, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(extractionTarget, outputPath));
                }

                // copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(extractionTarget, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(extractionTarget, outputPath), true);
                }

                _session.LogDebug($"Attachment successfully downloaded to {outputPath}");
            }
            return true;
        }
    }
}
