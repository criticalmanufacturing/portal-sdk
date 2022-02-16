﻿using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DeployAgentCommand : BaseCommand
    {
        public DeployAgentCommand() : this("deployagent", "Creates and deploys a new Infrastructure Agent")
        {
        }

        public DeployAgentCommand(string name, string description = null) : base(name, description)
        {
            Handler = CommandHandler.Create(typeof(DeployAgentCommand).GetMethod(nameof(DeployAgentCommand.DeployHandler)), this);
        }

        protected override IEnumerable<IOptionExtension> ExtendWithRange()
        {
            List<IOptionExtension> extensions = new List<IOptionExtension>();
            extensions.Add(new ReplaceTokensExtension());
            extensions.Add(new CommonParametersExtension());
            return extensions;
        }

        public async Task DeployHandler(bool verbose, string customerInfrastructureName, string name, string description, FileInfo parameters, string type, string site, string license,
            string package, string target, string templateName, DirectoryInfo output, string[] replaceTokens, bool interactive)
        {
            // get new environment handler and run it
            CreateSession(verbose);
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run(name, parameters, (EnvironmentType)Enum.Parse(typeof(EnvironmentType), type), site, license, null,
                (DeploymentTarget)Enum.Parse(typeof(DeploymentTarget), target), output,
                replaceTokens, interactive, customerInfrastructureName, description, templateName, false, true);
        }
    }
}
