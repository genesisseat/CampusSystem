namespace CampusSystem.Sql;

public sealed class CampusSystemDbConnector
{
    public const string ConnectionStringName = "CampusSystemDb";

    public const string DefaultLocalConnectionString =
        "Server=localhost,1433;Database=CampusSystemDb;User Id=sa;Password=MakeItStrong!2026;TrustServerCertificate=True;Encrypt=False";

    public CampusSystemDbConnector(string? connectionString = null)
    {
        ConnectionString = string.IsNullOrWhiteSpace(connectionString)
            ? DefaultLocalConnectionString
            : connectionString;
    }

    public string ConnectionString { get; }

    public static string Resolve(string? configuredConnectionString)
    {
        return string.IsNullOrWhiteSpace(configuredConnectionString)
            ? DefaultLocalConnectionString
            : configuredConnectionString;
    }
}
