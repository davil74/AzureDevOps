using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureDevOpsMigrationClient;

public static class MigrationConfigurationProvider
{
    public static async Task<MigrationConfiguration?> LoadAsync()
    {
        await using var openStream = File.OpenRead("config.json");
        return
            await JsonSerializer.DeserializeAsync<MigrationConfiguration>(openStream);
    }
}

public class MigrationConfiguration
{
    [JsonConstructor]
    public MigrationConfiguration(AzureConnection sourceConnection, AzureConnection targetConnection)
    {
        SourceConnection = sourceConnection;
        TargetConnection = targetConnection;
    }

    public AzureConnection SourceConnection { get; set; }

    public AzureConnection TargetConnection { get; set; }

    public List<WorkItemType> WorkItems { get; set; } = new();
}

public class AzureConnection
{
    [JsonConstructor]
    public AzureConnection(string baseUrl, string orgName, string personalAccessToken, string defaultProjectName)
    {
        BaseUrl = baseUrl;
        OrgName = orgName;
        PersonalAccessToken = personalAccessToken;
        DefaultProjectName = defaultProjectName;
    }

    public string BaseUrl { get; set; }

    public string OrgName { get; set; }

    public string PersonalAccessToken { get; set; }

    public string DefaultProjectName { get; set; }
}

public class WorkItemType
{
    public bool RootLevel { get; set; }

    public string Name { get; set; }

    public string[] FieldList { get; set; }
}