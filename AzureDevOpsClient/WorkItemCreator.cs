using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace AzureDevOpsClient;

public interface IWorkItemCreator
{
    Task<WorkItem> CreateFromWorkItemAsync(WorkItem sourceWorkItem, IEnumerable<string> fieldsKey, string? parentUrl);
}

internal class WorkItemCreator : IWorkItemCreator
{
    private readonly VssCredentials _credentials;
    private readonly string _defaultProjectName;
    private readonly Uri _uri;

    public WorkItemCreator(Uri uri, VssCredentials credentials, string defaultProjectName)
    {
        _uri = uri;
        _credentials = credentials;
        _defaultProjectName = defaultProjectName;
    }

    public async Task<WorkItem> CreateFromWorkItemAsync(WorkItem sourceWorkItem, IEnumerable<string> fieldsKey,
        string? parentUrl)
    {
        JsonPatchDocument patchDocument = new();

        // add fields
        foreach (var key in fieldsKey)
            if (sourceWorkItem.Fields.TryGetValue(key, out var value))
                patchDocument.Add(
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = $"/fields/{key}",
                        Value = value.ToString()
                    }
                );

        // MigrateAsync
        if (parentUrl != null)
            patchDocument.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = "/relations/-",
                Value = new
                {
                    rel = "System.LinkTypes.Hierarchy-Reverse",
                    url = parentUrl
                }
            });

        // type
        var workItemType = sourceWorkItem.GetWorkItemType();
        var connection = new VssConnection(_uri, _credentials);
        var workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();

        var result =
            await workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, _defaultProjectName, workItemType);

        return result;
    }
}