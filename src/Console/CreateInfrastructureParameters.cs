using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console
{
    /// <summary>
    /// Parameters for the create infrastructure command.
    /// NOTE: Property names must exactly match the option names defined in CreateInfrastructureCommand
    /// for System.CommandLine to properly bind CLI arguments to these properties.
    /// </summary>
    class CreateInfrastructureParameters
    {
        public bool Verbose { get; set; }
        public string Name { get; set; }
        public string Site { get; set; }
        public string Customer { get; set; }
        public bool IgnoreIfExists { get; set; }
        public FileInfo Parameters { get; set; }
    }
}
