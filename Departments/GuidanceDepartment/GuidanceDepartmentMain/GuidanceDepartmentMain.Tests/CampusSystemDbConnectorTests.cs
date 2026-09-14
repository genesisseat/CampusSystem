using CampusSystem.Sql;
using Xunit;

namespace GuidanceDepartmentMain.Tests;

public sealed class CampusSystemDbConnectorTests
{
    [Fact]
    public void Resolve_uses_default_connection_when_none_configured()
    {
        var result = CampusSystemDbConnector.Resolve(null);

        Assert.Equal(CampusSystemDbConnector.DefaultLocalConnectionString, result);
        Assert.Contains("CampusSystemDb", result);
    }

    [Fact]
    public void Resolve_uses_configured_connection_when_present()
    {
        const string configured = "Server=campus-db.internal;Database=CampusSystemDb;User Id=campus_app;Password=SecurePass!;TrustServerCertificate=True;Encrypt=False";

        var result = CampusSystemDbConnector.Resolve(configured);

        Assert.Equal(configured, result);
    }

    [Fact]
    public void Connection_string_name_is_campus_system_db()
    {
        Assert.Equal("CampusSystemDb", CampusSystemDbConnector.ConnectionStringName);
    }
}
