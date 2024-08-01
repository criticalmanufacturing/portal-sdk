using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.OutputObjects;
using Cmf.Foundation.Common;

namespace Cmf.CustomerPortal.Sdk.Common
{
    static class Utils
    {
        private static readonly Regex REPLACE_TOKENS_REGEX = new Regex(@"\#{(.+?)}#", RegexOptions.Compiled);

        public static async Task<string> ReplaceTokens(ISession _session, string content, string[] replaceTokens, bool isJson = false)
        {
            return await Task.Run(async () =>
            {
                StringBuilder stringBuilder = new StringBuilder(content);

                if (replaceTokens?.Length > 0)
                {
                    _session.LogDebug($"Replacing tokens");

                    Dictionary<string, string> tokens = await ConfigureTokens(_session, replaceTokens);

                    MatchCollection m = REPLACE_TOKENS_REGEX.Matches(content);
                    int indexChanges = 0;
                    for (int i = 0; i < m.Count; i++)
                    {
                        // get env var name from match
                        string matchedString = m[i].Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(matchedString))
                        {
                            // get env var value
                            tokens.TryGetValue(matchedString, out string envVar);
                            string value = envVar ?? string.Empty;
                            if (isJson)
                            {
                                // escape backslashes
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    value = value.ToString().Replace("\\", "\\\\");
                                }
                                // if not inside double quotes and value is null or whitespace, be sure to add double quotes as the value or else it will result in invalid json
                                else if (!m[i].Groups[0].Value.StartsWith("\"") && !m[i].Groups[0].Value.EndsWith("\""))
                                {
                                    value = "\"\"";
                                }
                            }

                            // replace env var with ${} for the env var value
                            string envToSubstitute = m[i].Groups[0].Value;
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                _session.LogInformation($"Found no match for token '{envToSubstitute}'. Replacing with '{value}'");
                            }
                            else
                            {
                                _session.LogDebug($"Replacing '{envToSubstitute}' with '{value}'");
                            }
                            stringBuilder.Replace(envToSubstitute, value, m[i].Groups[0].Index - indexChanges, m[i].Groups[0].Length);

                            indexChanges += envToSubstitute.Length - value.Length;
                        }
                    }
                }

                return stringBuilder.ToString();
            });
        }

        private static async Task<Dictionary<string, string>> ConfigureTokens(ISession _session, string[] replaceTokens)
        {
            return await Task.Run(() =>
            {
                Dictionary<string, string> tokens = new Dictionary<string, string>();
                foreach (string tokenToSet in replaceTokens)
                {
                    int splitCharIndex = tokenToSet.IndexOf('=');
                    if (splitCharIndex > 0 && splitCharIndex < tokenToSet.Length - 1)
                    {
                        string[] tokenNameAndValue = tokenToSet.Split(new char[] { '=' }, 2);
                        _session.LogDebug($"Registering token {tokenNameAndValue[0]} with value {tokenNameAndValue[1]}");
                        tokens.Add(tokenNameAndValue[0], tokenNameAndValue[1]);
                    }
                }
                return tokens;
            });
        }

        /// <summary>
        /// Name of SoftwareLicense is not unique, since it is a versioned entity
        /// To load a license by name, we need to get it by the LicenseUniqueName
        /// </summary>
        /// <param name="licenseUniqueName"></param>
        /// <returns></returns>
        public static async Task<CPSoftwareLicense> GetLicenseByUniqueName(string licenseUniqueName)
        {
            FilterCollection fcCollection = new FilterCollection()
            {
                new Filter()
                {
                    Name = "LicenseUniqueName",
                    LogicalOperator = LogicalOperator.AND,
                    Operator = FieldOperator.IsEqualTo,
                    Value = licenseUniqueName
                }
            };

            GetObjectsByFilterInput gobfiInput = new GetObjectsByFilterInput
            {
                Filter = fcCollection,
                Type = Activator.CreateInstance<CPSoftwareLicense>()
            };

            GetObjectsByFilterOutput gobfOutput = gobfiInput.GetObjectsByFilterAsync(true).Result;

            if (gobfOutput.Instance.Count == 0)
            {
                throw new Exception($"License with name {licenseUniqueName} does not exist");
            }

            return (CPSoftwareLicense)gobfOutput.Instance[0];
        }
    }
}
