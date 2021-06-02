using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.OutputObjects;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.ChangeSetManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.OutputObjects;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Client;
using Cmf.MessageBus.Messages;
using Cmf.Services.GenericServiceManagement;
using Newtonsoft.Json;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DeployCommand : BaseCommand
    {
        public DeployCommand() : this("deploy", "Creates and deploys a new Customer Environment")
        {
        }

        public DeployCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n", }, "Name of the environment to create."));

            Add(new Option(new string[] { "--parameters", "-params" }, "Path to parameters file that describes the environment.")
            {
                Argument = new Argument<FileInfo>().ExistingOnly(),
                IsRequired = true
            });

            var typeargument = new Argument<string>().FromAmong("Development", "Production", "Staging", "Testing");
            typeargument.SetDefaultValue("Development");

            Add(new Option<string>(new[] { "--type", "-type", }, "Type of the environment to create.") { 
                Argument = typeargument
            });
            Add(new Option<string>(new[] { "--site", "-s", }, "Name of the Site assotiated with the environment.")
            {
                IsRequired = true
            });
            Add(new Option<string>(new[] { "--license", "-lic", }, "Name of the license to use for the environment")
            {
                IsRequired = true
            });
            Add(new Option<string>(new[] { "--package", "-pck", }, "Name of the package to use to create the environment")
            {
                IsRequired = true
            });

            var targetArgument = new Argument<string>().FromAmong("portainer", "dockerswarm");
            targetArgument.SetDefaultValue("dockerswarm");
            Add(new Option<string>(new[] { "--target", "-trg", }, "Target for the environment.")
            {
                Argument = targetArgument,
                IsRequired = true
            });

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, "Path to Deployment Package Manifest file, or folder to a folder containing multiple manifest files"));

            Handler = CommandHandler.Create(typeof(Deploy).GetMethod(nameof(Deploy.DeployHandler))!);
        }
    }

    class Deploy : BaseHandler
    {
        private Transport messageBus = null;
        private bool deploymentFinished = false;
        private bool deploymentFailed = false;

        async public Task<int> DeployHandler(bool verbose, string token, string name, FileInfo parameters, string type, string site, string license, string package,
            string target, DirectoryInfo output, string[] replaceTokens)
        {
            Configure(token, verbose, replaceTokens);

            string customerEnvironmentName = string.IsNullOrWhiteSpace(name) ? $"Deployment-{Guid.NewGuid()}" : name;
            string rawParameters = File.ReadAllText(parameters.FullName);

            if (Tokens?.Count > 0)
            {
                LogVerbose($"Replacing tokens in parameters file");
                rawParameters = ReplaceTokens(rawParameters, true);
            }

            LogVerbose($"Creating customer environment {customerEnvironmentName}..");

            // let's see if it exists
            CustomerEnvironment environment = null;
            try
            {
                environment = (await new GetObjectByNameInput { Name = name, Type = "CustomerEnvironment" }.GetObjectByNameAsync()).Instance as CustomerEnvironment;
            }
            catch (Exception) { }

            // if it exists, maintain everything that is definition (name, type, site), change everything else and create new version
            if (environment != null)
            {
                environment.DeploymentPackage = Utilities.GetObjectByName<DeploymentPackage>(package);
                environment.CustomerLicense = Utilities.GetObjectByName<CustomerLicense>(license);
                environment.DeploymentTarget = GetTargetValue(target);
                environment.Parameters = rawParameters;

                environment = (await new CreateObjectVersionInput { Object = environment }.CreateObjectVersionAsync()).Object as CustomerEnvironment;
            }
            // if not, just build a new complete object and create it
            else
            {
                environment = new CustomerEnvironment
                {
                    EnvironmentType = type,
                    Site = Utilities.GetObjectByName<ProductSite>(site),
                    Name = customerEnvironmentName,
                    DeploymentPackage = Utilities.GetObjectByName<DeploymentPackage>(package),
                    CustomerLicense = Utilities.GetObjectByName<CustomerLicense>(license),
                    DeploymentTarget = GetTargetValue(target),
                    Parameters = rawParameters
                };

                environment = (await new CreateObjectInput { Object = environment }.CreateObjectAsync()).Object as CustomerEnvironment;
            }

            var startDeploymentInput = new StartDeploymentInput();
            startDeploymentInput.CustomerEnvironment = environment;

            var messageBusConfigured = await SetupMessageBus();
            var subject = $"CE_DEPLOYMENT_{environment.Id}";

            LogVerbose($"Subscribing messagebus subject {subject}");
            messageBus.Subscribe(subject, ProcessDeploymentMessage);

            Log($"Starting deployment of environment {environment.Name}...");
            var result = await startDeploymentInput.StartDeploymentAsync();


            if (messageBusConfigured)
            {
                while (!this.deploymentFinished)
                {
                    if (!messageBus.IsConnected)
                    {
                        Log("Messagebus connection lost, attempting to reconnect");
                        if (!await WaitForMessageBusConnection())
                        {
                            this.deploymentFinished = true;
                            Log("It was not possible to reconnect to customer portal. Please check status of the environment deployment in the customer portal");
                            break;

                        } else
                        {
                            Log("Reconnected to messagebus");
                        }
                    }   

                    await Task.Delay(100);
                }
            } else
            {
                Log("The deployment was started, but it was not possible to fetch deployment logs and status");
            }

            Log($"Customer Environment available at {GetCustomerEnvironmentAddress(environment)}");

            if (!deploymentFailed)
            {
                return await ProcessEnvironmentDeployment(environment, target, output);
            }

            return deploymentFailed ? 1 : 0;
        }

        private async Task<int> ProcessEnvironmentDeployment(CustomerEnvironment environment, string target, DirectoryInfo outputPath)
        {

            switch (target)
            {
                case "dockerswarm":

                    // download the attachment
                    GetAttachmentsForEntityInput input = new GetAttachmentsForEntityInput()
                    {
                        Entity = environment
                    };

                    await Task.Delay(1000);
                    var output = await input.GetAttachmentsForEntityAsync();
                    EntityDocumentation attachmentToDownload = null;
                    if (output?.Attachments.Count > 0)
                    {
                        output.Attachments.Sort((a, b) => DateTime.Compare(b.CreatedOn, a.CreatedOn));
                        attachmentToDownload = output.Attachments.Where(x => x.Filename.Contains(environment.Name)).FirstOrDefault();
                    }

                    if (attachmentToDownload == null)
                    {
                        LogVerbose("No attachment was found to download.");
                        return 1;
                    } else
                    {
                        //Download Attachment
                        LogVerbose($"Downloading attachment {attachmentToDownload.Filename}");

                        string outputFile = "";
                        using (DownloadAttachmentStreamingOutput downloadAttachmentOutput = new DownloadAttachmentStreamingInput() { attachmentId = attachmentToDownload.Id }.DownloadAttachmentSync())
                        {
                            int bytesToRead = 10000;
                            byte[] buffer = new Byte[bytesToRead];

                            outputFile = Path.Combine(Path.GetTempPath(), downloadAttachmentOutput.FileName);
                            LogVerbose($"Downloading to {outputFile}");

                            using (BinaryWriter streamWriter = new BinaryWriter(File.Open(outputFile, FileMode.Create, FileAccess.Write)))
                            {
                                int length;
                                do
                                {
                                    length = downloadAttachmentOutput.Stream.Read(buffer, 0, bytesToRead);
                                    streamWriter.Write(buffer, 0, length);
                                    buffer = new Byte[bytesToRead];

                                } while (length > 0);
                            }
                        }


                        string extractionTarget = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractionTarget);

                        LogVerbose($"Extracting environment contents to {extractionTarget}");

                        ZipFile.ExtractToDirectory(outputFile, extractionTarget);


                        var realTarget = outputPath?.FullName;

                        if (string.IsNullOrEmpty(realTarget))
                        {
                            realTarget = Path.Combine(Directory.GetCurrentDirectory(), "out", environment.Name);
                        } else
                        {
                            realTarget = Path.GetFullPath(realTarget);
                        }

                        Directory.CreateDirectory(realTarget);

                        LogVerbose($"Moving environment contents from {extractionTarget} to {realTarget}");

                        //Now Create all of the directories
                        foreach (string dirPath in Directory.GetDirectories(extractionTarget, "*",
                            SearchOption.AllDirectories))
                            Directory.CreateDirectory(dirPath.Replace(extractionTarget, realTarget));

                        //Copy all the files & Replaces any files with the same name
                        foreach (string newPath in Directory.GetFiles(extractionTarget, "*.*",
                            SearchOption.AllDirectories))
                            File.Copy(newPath, newPath.Replace(extractionTarget, realTarget), true);



                        Log($"Customer environment created at {realTarget}");
                    }

                    break;
                default:
                    break;
            }

            return 0;
        }

        private string GetCustomerEnvironmentAddress(CustomerEnvironment environment)
        {
            return $"{(ClientConfigurationProvider.ClientConfiguration.UseSsl ? "https" : "http")}://{ClientConfigurationProvider.ClientConfiguration.HostAddress}/Entity/CustomerEnvironment/{environment?.Id}/View/Installation";
        }

        private string GetTargetValue(string targetInput)
        {
           switch (targetInput)
            {
                case "portainer":
                    return "PortainerV2Target";
                case "dockerswarm":
                    return "DockerSwarmOnPremisesTarget";
                default:
                    throw new Exception($"Target parameter '{targetInput}' not recognized");
            }
        }

        private void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            if (message != null && !string.IsNullOrWhiteSpace(message.Data)) {
                
                var messageContentFormat = new { Data = "", DeploymentStatus = (DeploymentStatus?)DeploymentStatus.NotDeployed, StepId = "" };
                var content = JsonConvert.DeserializeAnonymousType(message.Data, messageContentFormat);

                Log(content.Data);

                if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed || content.DeploymentStatus == DeploymentStatus.DeploymentPartiallySucceeded || content.DeploymentStatus == DeploymentStatus.DeploymentSucceeded)
                {
                    if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed) {
                        this.deploymentFailed = true;
                    }
                    this.deploymentFinished = true;
                }
            } else
            {
                LogVerbose("Unkown message received");
            }
        }

        private async Task<bool> SetupMessageBus()
        {
            LogVerbose($"Configuring message bus");

            var transportConfigString = new GetApplicationBootInformationInput().GetApplicationBootInformationSync().TransportConfig;
            var transportConfig = JsonConvert.DeserializeObject<TransportConfig>(transportConfigString);

            transportConfig.ApplicationName = "Customer Portal Client";
            transportConfig.TenantName = ClientConfigurationProvider.ClientConfiguration.ClientTenantName;

            this.messageBus = new Transport(transportConfig);

            // Register events
            messageBus.Connected += MessageBusConnected;
            messageBus.Disconnected += MessageBusDisconnected;
            messageBus.InformationMessage += MessageBusMessage;
            messageBus.Exception += MessageBusException;

            messageBus.Start();

            bool failedConnection = await WaitForMessageBusConnection();

            if (failedConnection)
            {
                Log("Timed out waiting for client to connect to MessageBus");
            }

            if (messageBus.IsConnected)
            {
                LogVerbose($"Messagebus configured with success");
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> WaitForMessageBusConnection()
        {
            var timeout = 2000;
            int totalWaitedTime = 0;
            var failedConnection = false;

            while (!messageBus.IsConnected && totalWaitedTime < timeout)
            {
                await Task.Delay(100);
                totalWaitedTime += 100;
            }

            if (totalWaitedTime > 0 && totalWaitedTime > timeout)
            {
                failedConnection = true;
            }

            return failedConnection;
        }

        private void MessageBusMessage(string message)
        {
            LogVerbose(message);
        }

        private void MessageBusException(string message)
        {
            Log(message);
        }

        private void MessageBusConnected()
        {
            LogVerbose("Messagebus connected");
        }

        private void MessageBusDisconnected()
        {
            Log("Messagebus disconnected");
        }
    }
}
