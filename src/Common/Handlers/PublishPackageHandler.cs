using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.OutputObjects;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class PublishPackageHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        public PublishPackageHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
        }

        public async Task Run(FileSystemInfo path, string datagroup)
        {
            await EnsureLogin();

            Session.LogDebug("-------------------");

            if (path.Attributes.HasFlag(FileAttributes.Directory)) {
                foreach (string file in Directory.GetFiles(path.FullName))
                {
                    await UploadPackage(file, datagroup);
                }
            }
            else
            {
                await UploadPackage(path.FullName, datagroup);
            }
        }


        private async Task UploadPackage(string filePath, string datagroup)
        {
            string fileName = Path.GetFileName(filePath);

            if (!ValidateFile(filePath))
            {
                return;
            }

            Session.LogDebug($"Starting to publish package {fileName}...");

            if (!await PackageExists(fileName))
            {
                try
                {
                    // publish
                    Session.LogDebug("Uploading package...");
                    var publishNewNewStreamingOutput = await new PackageManagement.PublishApplicationPackageBaseStreamingInput
                    {
                        FilePath = filePath,
                        DatagroupName = datagroup,
                    }.PublishApplicationPackageBaseAsync(true);

                    Session.LogInformation($"Package {fileName} successfully uploaded");
                }
                catch (Exception exception)
                {
                    Session.LogError($"Package {fileName} failed to publish");
                    Session.LogError(exception);
                }
            } else
            {
                Session.LogInformation($"Package {fileName} skipped");
            }

            Session.LogDebug("-------------------");
        }

        private bool ValidateFile(string filePath)
        {
            // verify if is zip
            try
            {
                var zipFile = System.IO.Compression.ZipFile.OpenRead(filePath);
                if (zipFile.Entries.Count == 0)
                {
                    throw Exception("No files in package zip");
                }
                zipFile.Dispose();
            }
            catch
            {
                Session.LogInformation($"Invalid file to publish ignored: {filePath}");
                Session.LogDebug("-------------------");
                return false;
            }
            return true;
        }

        private Exception Exception(string v)
        {
            throw new NotImplementedException();
        }

        private async Task<bool> PackageExists(string fileName)
        {

            bool packageExists = false;

            string pattern = @"^(.+)\.(\d+)\.(\d+)\.(\d+)\.zip$";
            RegexOptions options = RegexOptions.IgnoreCase;
            MatchCollection matches = Regex.Matches(fileName, pattern, options);

            if (matches.Count > 0 && matches[0].Groups != null && matches[0].Groups.Count == 5)
            {
                string packageName = matches[0].Groups[1].Value;
                string packageVersion = $"{matches[0].Groups[2].Value}.{matches[0].Groups[3].Value}.{matches[0].Groups[4].Value}";

                // check if this package already exists
                Session.LogDebug($"Verifying if package with name {packageName} and version {packageVersion} exists...");

                packageExists = await PackageExistsInPortal(packageName, packageVersion);

                if (!packageExists)
                {
                    Session.LogDebug("Package does not exist");
                }
            } else
            {
                Session.LogDebug($"Could not get data from file name ({fileName}) to validate if package exists.");
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

            ExecuteQueryOutput result = null;
            try
            {
                result = await new ExecuteQueryInput()
                {
                    QueryObject = query,
                    PageNumber = 1,
                    PageSize = 1
                }.ExecuteQueryAsync(true);
            } catch (Exception e)
            {
                Session.LogDebug($"Failed to verify if package exists: {e.Message}");
            }


            return result?.TotalRows > 0;
        }
    }
}
