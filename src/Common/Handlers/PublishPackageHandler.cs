using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects.QueryObject;
using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class PublishPackageHandler(
        ISession session,
        IFileSystem fileSystem,
        IQueryProxyService queryProxyService)
        : AbstractHandler(session, true)
    {
        public async Task Run(string path, string datagroup)
        {
            await EnsureLogin();

            Session.LogDebug("-------------------");

            if (fileSystem.File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                foreach (string file in fileSystem.Directory.GetFiles(path))
                {
                    await UploadPackage(file, datagroup);
                }
            }
            else
            {
                await UploadPackage(path, datagroup);
            }
        }

        private async Task UploadPackage(string filePath, string datagroup)
        {
            string fileName = fileSystem.Path.GetFileName(filePath);

            if (!ValidateFile(filePath))
            {
                return;
            }

            Session.LogDebug($"Starting to publish package {fileName}...");

            var result = await PackageExists(fileName);
            if (result.HasValue && !result.Value)
            {
                try
                {
                    // publish
                    Session.LogDebug("Uploading package...");
                    await new PackageManagement.PublishApplicationPackageBaseStreamingInput
                    {
                        FilePath = filePath, // boundary for IO.Abstractions
                        DatagroupName = datagroup,
                    }.PublishApplicationPackageBaseAsync(true);

                    Session.LogInformation($"Package {fileName} successfully uploaded");
                }
                catch (Exception exception)
                {
                    Session.LogError($"Package {fileName} failed to publish");
                    Session.LogError(exception);
                }
            }
            else
            {
                Session.LogInformation($"Package {fileName} skipped");
            }

            Session.LogDebug("-------------------");
        }

        private bool ValidateFile(string filePath)
        {
            // verify if is a zip file and it's not empty
            try
            {
                using var zipFile = new ZipArchive(fileSystem.File.OpenRead(filePath));
                if (zipFile.Entries.Count == 0)
                {
                    throw new Exception("No files in package zip");
                }
            }
            catch
            {
                Session.LogInformation($"Invalid file to publish ignored: {filePath}");
                Session.LogDebug("-------------------");
                return false;
            }
            return true;
        }

        private async Task<bool?> PackageExists(string fileName)
        {
            bool packageExists = false;

            const string pattern = @"^(?<packagename>.+?)\.(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)(?:-(?<prerelease>[0-9A-Za-z\-\.]+))?\.zip$";
            MatchCollection matches = Regex.Matches(fileName, pattern, RegexOptions.IgnoreCase);

            if (matches.Count > 0 && matches[0].Groups != null && (matches[0].Groups.Count == 5 || matches[0].Groups.Count == 6))
            {
                var match = matches[0];
                string packageName = match.Groups["packagename"].Value;
                string major = match.Groups["major"].Value;
                string minor = match.Groups["minor"].Value;
                string patch = match.Groups["patch"].Value;
                string prerelease = match.Groups["prerelease"].Value;
                string packageVersion = $"{major}.{minor}.{patch}";
                packageVersion += string.IsNullOrWhiteSpace(prerelease) ? "" : $"-{prerelease}";

                // check if this package already exists
                Session.LogDebug($"Verifying if package with name {packageName} and version {packageVersion} exists...");

                packageExists = await PackageExistsInPortal(packageName, packageVersion);

                if (!packageExists)
                {
                    Session.LogDebug("Package does not exist");
                }
            }
            else
            {
                Session.LogDebug($"Could not get data from file name ({fileName}) to validate if package exists.");
                return null;
            }

            return packageExists;
        }

        private async Task<bool> PackageExistsInPortal(string name, string version)
        {
            QueryObject query = new QueryObject();
            query.EntityTypeName = "CPInstallationPackage";
            query.Query = new Query();
            query.Query.Distinct = false;
            query.Query.Filters = new FilterCollection() {
                new Filter()
                {
                    Name = "Name",
                    ObjectName = "CPInstallationPackage",
                    ObjectAlias = "CPInstallationPackage_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.IsEqualTo,
                    Value = name,
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.AND,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                },
                new Filter()
                {
                    Name = "PackageVersion",
                    ObjectName = "CPInstallationPackage",
                    ObjectAlias = "CPInstallationPackage_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.IsEqualTo,

                    Value = version,
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.AND,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                },
                new Filter()
                {
                    Name = "UniversalState",
                    ObjectName = "CPInstallationPackage",
                    ObjectAlias = "CPInstallationPackage_1",
                    Operator = Cmf.Foundation.Common.FieldOperator.IsNotEqualTo,
                    Value = 4,
                    LogicalOperator = Cmf.Foundation.Common.LogicalOperator.Nothing,
                    FilterType = Cmf.Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
                }
            };
            query.Query.Fields = new FieldCollection() {
                new Field()
                {
                    Alias = "Id",
                    ObjectName = "CPInstallationPackage",
                    ObjectAlias = "CPInstallationPackage_1",
                    IsUserAttribute = false,
                    Name = "Id",
                    Position = 0,
                    Sort = Cmf.Foundation.Common.FieldSort.NoSort
                }
            };
            query.Query.Relations = new RelationCollection();

            var result = await queryProxyService.ExecuteQuery(query, 1, 1, Session);

            return result?.TotalRows > 0;
        }
    }
}
