﻿using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DeployParameters
    {

        public bool Verbose { get; set; }
        public string CustomerInfrastructureName { get; set; }
        public string Name { get; set; }
        public string AgentName { get; set; }
        public string Description { get; set; }
        public FileInfo Parameters { get; set; }
        public string Type { get; set; }
        public string Site { get; set; }
        public string[] License { get; set; }
        public string Package { get; set; }
        public string Target { get; set; }
        public DirectoryInfo Output { get; set; }
        public string[] ReplaceTokens { get; set; }
        public bool Interactive { get; set; }
        public bool TerminateOtherVersions { get; set; }
        public double? DeploymentTimeoutMinutes { get; set; }
        public double? DeploymentTimeoutMinutesToGetSomeMBMessage { get; set; }
        public bool TerminateOtherVersionsRemove { get; set; }
        public bool TerminateOtherVersionsRemoveVolumes { get; set; }
    }
}
