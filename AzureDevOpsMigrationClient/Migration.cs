using AzureDevOpsClient;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsMigrationClient;

internal class Migration
{
    private readonly MigrationConfiguration _configuration;
    private readonly AzureDevOpsConnection _sourceConnection;
    private readonly AzureDevOpsConnection _targetConnection;

    public Migration(MigrationConfiguration configuration)
    {
        _configuration = configuration;
        _sourceConnection = new AzureDevOpsConnection(configuration.SourceConnection.BaseUrl,
            configuration.SourceConnection.OrgName,
            configuration.SourceConnection.PersonalAccessToken, configuration.SourceConnection.DefaultProjectName);
        _targetConnection = new AzureDevOpsConnection(configuration.TargetConnection.BaseUrl,
            configuration.TargetConnection.OrgName,
            configuration.TargetConnection.PersonalAccessToken, configuration.TargetConnection.DefaultProjectName);
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        var roots = _configuration.WorkItems.Where(w => w.RootLevel).ToArray();
        foreach (var root in roots)
        {
            var rootItems = await _sourceConnection.WorkItemsSearch()
                .SearchWorkItemsAsync(new SearchCriteria { WorkItemType = root.Name }, CancellationToken.None);

            foreach (var workItem in rootItems)
            {
                Console.WriteLine(
                    "Import {0}\t{1}",
                    workItem.Fields["System.WorkItemType"],
                    workItem.Fields["System.Title"]);
                await MigrateAsync(workItem, null, cancellationToken);
                /* var newWorkItem = await _targetConnection.GetWorkItemCreator().CreateFromWorkItemAsync(workItem, root.FieldList);

                 // child
                 foreach (var relation in workItem.Relations)
                 {
                     switch (relation.Rel)
                     {
                         // child
                         case "System.LinkTypes.Hierarchy-Forward":
                             // import next level
                             var item = await _sourceConnection.WorkItemsSearch().GetFromUrlAsync(relation.Url, cancellationToken);
                             if (item == null)
                                 throw new Exception($"Item not found {relation.Url}");

                             await MigrateAsync(item, cancellationToken);
                             continue;
                         // parent
                         case "System.LinkTypes.Hierarchy-Reverse":
                             // ignore
                             continue;
                         default:
                             Console.WriteLine($"ERROR: Relation is not a child : {relation.Rel} {relation.Url}");
                             break;
                     }
                 }*/
            }
        }
        //
// level 1


// "Microsoft.VSTS.TCM.ReproSteps",
//        "Microsoft.VSTS.Common.Priority",
//         "Microsoft.VSTS.Common.Severity",
// "System.State"
    }

    private async Task MigrateAsync(WorkItem workItem, string? parentUrl, CancellationToken cancellationToken)
    {
        // Load configuration
        var cfg = _configuration.WorkItems.FirstOrDefault(t =>
            t.Name.Equals(workItem.GetWorkItemType(), StringComparison.InvariantCultureIgnoreCase));
        if (cfg == null)
        {
            Console.WriteLine("Configuration not found: {0}", workItem.GetWorkItemType());
            return;
        }

        // create work item
        var newWorkItem = await _targetConnection.GetWorkItemCreator()
            .CreateFromWorkItemAsync(workItem, cfg.FieldList, parentUrl);

        // child
        if (workItem.Relations == null)
            // Console.WriteLine($"No Child for {workItem.Id}" );
            return;

        foreach (var relation in workItem.Relations)
            switch (relation.Rel)
            {
                // child
                case "System.LinkTypes.Hierarchy-Forward":
                    // import next level
                    var item = await _sourceConnection.WorkItemsSearch()
                        .GetFromUrlAsync(relation.Url, cancellationToken);
                    if (item == null)
                        throw new Exception($"Item not found {relation.Url}");

                    await MigrateAsync(item, newWorkItem.Url, cancellationToken);
                    continue;
                // parent
                case "System.LinkTypes.Hierarchy-Reverse":
                    // ignore
                    continue;
                default:
                    Console.WriteLine($"ERROR: Relation is not a child : {relation.Rel} {relation.Url}");
                    break;
            }
    }
}