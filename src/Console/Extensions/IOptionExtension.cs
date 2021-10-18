using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    internal interface IOptionExtension
    {
        void Use(Command command);
    }
}
