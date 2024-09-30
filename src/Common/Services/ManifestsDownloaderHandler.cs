using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.OutputObjects;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.IO;
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

            await Utilities.DownloadAttachment(_session, attachmentToDownload, outputDir);

            _session.LogInformation($"Customer environment created at {outputDir.FullName}");
        }
    }
}
