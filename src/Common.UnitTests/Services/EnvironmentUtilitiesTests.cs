using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Moq;
using System.Data;

namespace Common.UnitTests.Services;

public class EnvironmentUtilitiesTests
{
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();

    private readonly EnvironmentUtilities _environmentUtilities;

    public EnvironmentUtilitiesTests()
    {
        _environmentUtilities = new EnvironmentUtilities(Mock.Of<ISession>(), _customerPortalClientMock.Object);
    }

    [Fact]
    public async Task GetOtherVersionToTerminate_ReturnsTheOtherVersionsWithTheirVersionNumbers()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(long));
        table.Columns.Add("DefinitionId", typeof(long));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Version", typeof(int));
        table.Rows.Add(10L, 1L, "env", 1);
        table.Rows.Add(20L, 1L, "env", 2);
        var dataSet = new DataSet();
        dataSet.Tables.Add(table);

        QueryObject? query = null;
        _customerPortalClientMock.Setup(c => c.ExecuteQuery(It.IsAny<QueryObject>()))
            .Callback<QueryObject>(q => query = q)
            .ReturnsAsync(dataSet);

        // Act
        CustomerEnvironmentCollection result = await _environmentUtilities.GetOtherVersionToTerminate(new CustomerEnvironment { Name = "env", Version = 3 });

        // Assert
        Assert.NotNull(query);
        Assert.Single(query.Query.Fields, f => f.Name == "Version");
        Assert.Equal(new[] { (10L, 1), (20L, 2) }, result.Select(ce => (ce.Id, ce.Version)));
    }
}
