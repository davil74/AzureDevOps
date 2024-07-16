using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsClient;

public static class WorkItemExtensions
{
    public static string? GetWorkItemType(this WorkItem wi)
    {
        return wi.Fields["System.WorkItemType"].ToString();
    }
}