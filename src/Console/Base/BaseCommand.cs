using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

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

        public void AddParameters()
        {
            Add(new Option<string>(new[] { "--customer-infrastructure-name", "-ci", }, Resources.INFRASTRUCTURE_EXISTING_NAME_HELP));

            Add(new Option<string>(new[] { "--name", "-n", }, Resources.DEPLOYMENT_NAME_HELP));

            Add(new Option<string>(new[] { "--description", "-d", }, Resources.DEPLOYMENT_NAME_HELP));

            Add(new Option<FileInfo>(new string[] { "--parameters", "-params" }, Resources.DEPLOYMENT_PARAMETERSPATH_HELP)
            {
                Argument = new Argument<FileInfo>().ExistingOnly()
            });

            var typeargument = new Argument<string>().FromAmong(Enum.GetNames(typeof(EnvironmentType)));
            typeargument.SetDefaultValue(EnvironmentType.Development.ToString());
            Add(new Option<string>(new[] { "--type", "-type", }, Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP)
            {
                Argument = typeargument
            });

            Add(new Option<string>(new[] { "--license", "-lic", }, Resources.DEPLOYMENT_LICENSE_HELP)
            {
                IsRequired = true
            });
            var targetArgument = new Argument<string>().FromAmong("portainer", "dockerswarm");
            targetArgument.SetDefaultValue("dockerswarm");
            Add(new Option<string>(new[] { "--target", "-trg", }, Resources.DEPLOYMENT_TARGET_HELP)
            {
                Argument = targetArgument,
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--template-name", "-template", }, Resources.INFRASTRUCTURE_EXISTING_ENVIRONMENT_TEMPLATE_NAME_HELP));

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            Add(new Option<bool>(new[] { "--interactive", "-i" }, Resources.DEPLOYMENT_INTERACTIVE_HELP));

        }


    }
}
