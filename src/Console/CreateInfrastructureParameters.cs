using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class CreateInfrastructureParameters
    {
        public bool Verbose { get; set; }
        public string Name { get; set; }
        public string Site { get; set; }
        public string Customer { get; set; }
        public bool IgnoreIfExists { get; set; }
    }
}
