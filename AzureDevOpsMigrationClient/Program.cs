using AzureDevOpsMigrationClient;

Console.WriteLine("Welcome to Azure DevOps Migration Client");

Console.WriteLine("Load configuration ...");
var configuration = await MigrationConfigurationProvider.LoadAsync();
if (configuration == null)
{
    Console.WriteLine("Configuration file not found");
    return;
}

// Connect
Console.WriteLine("Lancement de la migration ...");
var migration = new Migration(configuration);
await migration.ImportAsync(CancellationToken.None);