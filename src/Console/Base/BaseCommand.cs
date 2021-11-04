using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.Collections.Generic;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class BaseCommand : Command
    {
        protected IServiceLocator ServiceLocator
        {
            get; private set;
        }

        protected BaseCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<bool>(new[] { "--verbose", "-v" }, Resources.VERBOSE_HELP));

            UseExtension(ExtendWith());
            UseExtensions(ExtendWithRange());
        }

        private void UseExtensions(IEnumerable<IOptionExtension> extensions)
        {
            if (extensions != null)
            {
                foreach (IOptionExtension optionExtension in extensions)
                {
                    UseExtension(optionExtension);
                }
            }
        }

        private void UseExtension(IOptionExtension optionExtension)
        {
            if (optionExtension != null)
            {
                optionExtension.Use(this);
            }
        }

        /// <summary>
        /// TODO: Rename this and make it void
        /// </summary>
        /// <param name="verbose"></param>
        /// <returns></returns>
        protected void CreateSession(bool verbose)
        {
            Session session = new Session(verbose);
            ServiceLocator = new ServiceLocator(session);
        }

        protected virtual IOptionExtension ExtendWith()
        {
            return null;
        }

        protected virtual IEnumerable<IOptionExtension> ExtendWithRange()
        {
            return null;
        }
    }
}
