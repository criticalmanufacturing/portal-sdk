using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.OutputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.ChangeSetManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.OutputObjects;
using Cmf.LightBusinessObjects.Infrastructure;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class BaseCommand : Command
    {
        public BaseCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new string[] { "--destination", "--dest" }, "Target Customer Portal environment")
            {
                Argument = new Argument<string>().FromAmong("qa", "dev", "local", "prod"),
                AllowMultipleArgumentsPerToken = false
            });

            Add(new Option<string>(new[] { "--token","--pat", "-t", }, "Use the provided personal access token to publish in customer portal"));

            var replaceTokensOption = new Option<string[]>(new[] { "--replace-tokens" }, "Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values.")
            {
                AllowMultipleArgumentsPerToken = true
            };
            replaceTokensOption.AddSuggestions(new string[] { "MyToken=value MyToken2=value2" });

            Add(replaceTokensOption);

            Add(new Option(new[] { "--verbose", "-v" }, "Show detailed logging"));
        }

    }

    class BaseHandler {

        private static readonly Regex REPLACE_TOKENS_REGEX = new Regex(@"\#{([^}]+)}#", RegexOptions.Compiled);
        protected bool verbose = false;
        protected Dictionary<string, string> Tokens = new Dictionary<string, string>();

        protected void Configure(string destination, string token, bool verbose, string[] replaceTokens)
        {
            this.verbose = verbose;
            ConfigureLBOs(destination, token);
            if (replaceTokens != null) {
                ConfigureTokens(replaceTokens);
            }
        }

        private void ConfigureTokens(string[] replaceTokens)
        {
            foreach (string tokenToSet in replaceTokens)
            {
                int splitCharIndex = tokenToSet.IndexOf('=');
                if (splitCharIndex > 0 && splitCharIndex < tokenToSet.Length - 1)
                {
                    string[] tokenNameAndValue = tokenToSet.Split('=', 2);
                    LogVerbose($"Registering token {tokenNameAndValue[0]} with value {tokenNameAndValue[1]}");
                    Tokens.Add(tokenNameAndValue[0], tokenNameAndValue[1]);
                }
            }
        }

        private void ConfigureLBOs(string destination, string token)
        {
            if (!string.IsNullOrWhiteSpace(destination))
            {
                switch (destination)
                {
                    case "prod":
                        CreateConfiguration("production", "portal.criticalmanufacturing.com:443", "CustomerPortal", "https://security.criticalmanufacturing.com:443/", "Applications", token);
                        break;
                    case "dev":
                        CreateConfiguration("development", "portaldev.criticalmanufacturing.dev:443", "CustomerPortalDEV", "https://securitydev.criticalmanufacturing.dev:443/", "MES", token);
                        break;
                    case "qa":
                        CreateConfiguration("QA", "portalqa.criticalmanufacturing.dev:443", "CustomerPortalQA", "https://securityqa.criticalmanufacturing.dev:443/", "Applications", token);
                        break;
                    case "local":
                        CreateConfiguration("local", "localhost:8073", "CustomerPortalDEV", "https://securitydev.criticalmanufacturing.dev:443/", "MES", token, false);
                        break;

                    default:
                        throw new Exception($"Destination parameter '{destination}' not recognized");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    LogVerbose("Overriding default configuration with personal access token");
                   

                    CreateConfiguration("default", ConfigurationManager.AppSettings["HostAddress"], ConfigurationManager.AppSettings["ClientTenantName"], ConfigurationManager.AppSettings["SecurityPortalBaseAddress"], ConfigurationManager.AppSettings["ClientId"], token, bool.Parse(ConfigurationManager.AppSettings["UseSsl"]), bool.Parse(ConfigurationManager.AppSettings["IsUsingLoadBalancer"]));
                }
                else
                {
                    Log("Using default configuration");
                }
            }
        }

        private void CreateConfiguration(string environmentName, string hostAddress, string tenant, string securityPortalUrl, string clientId, string token, bool useSSL = true, bool useLoadBalancer = false)
        {
            Log($"Using {environmentName} configuration");
            ClientConfigurationProvider.ConfigurationFactory = () =>
            {
                return new ClientConfiguration()
                {
                    HostAddress = hostAddress,
                    ClientTenantName = tenant,
                    IsUsingLoadBalancer = useLoadBalancer,
                    ClientId = clientId,
                    UseSsl = useSSL,
                    SecurityAccessToken = !string.IsNullOrWhiteSpace(token) ? token : null,
                    SecurityPortalBaseAddress = new Uri(securityPortalUrl)
                };
            };
        }

        protected string ReplaceTokens(string content, bool isJson = false)
        {
            StringBuilder stringBuilder = new StringBuilder(content);

            MatchCollection m = REPLACE_TOKENS_REGEX.Matches(content);
            int indexChanges = 0;
            for (int i = 0; i < m.Count; i++)
            {
                // get env var name from match
                string matchedString = m[i].Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(matchedString))
                {
                    // get env var value
                    Tokens.TryGetValue(matchedString, out string envVar);
                    string value = envVar ?? string.Empty;
                    if (isJson)
                    {
                        // espace backslashes
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            value = value.ToString().Replace("\\", "\\\\");
                        }
                        // if not inside double quotes and value is null or whitespace, be sure to add double quotes as the value or else it will result in invalid json
                        else if (!m[i].Groups[0].Value.StartsWith('"') && !m[i].Groups[0].Value.EndsWith('"'))
                        {
                            value = "\"\"";
                        }
                    }

                    // replace env var with ${} for the env var value
                    string envToSubstitute = m[i].Groups[0].Value;
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        Log($"Found no match for token '{envToSubstitute}'. Replacing with '{value}'");
                    }
                    else
                    {
                        LogVerbose($"Replacing '{envToSubstitute}' with '{value}'");
                    }
                    stringBuilder.Replace(envToSubstitute, value, m[i].Groups[0].Index - indexChanges, m[i].Groups[0].Length);

                    indexChanges += envToSubstitute.Length - value.Length;
                }
            }

            return stringBuilder.ToString();
        }

        protected void Log(string message = "")
        {
            System.Console.WriteLine(message);
        }

        protected void LogVerbose(string message = "")
        {
            if (this.verbose)
            {
                Log(message);
            }
        }


    }
}
