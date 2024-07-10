using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;

namespace AzureDevOpsClient;

public interface IWorkItemsSearch
{
    Task<IList<WorkItem>> SearchWorkItemsAsync(SearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<WorkItem?> GetFromUrlAsync(string url, CancellationToken cancellationToken);
}

public class SearchCriteria
{
    public string? ProjectName { get; set; }

    public string? WorkItemType { get; set; }

    public string? Tags { get; set; }
}

internal class WorkItemsSearch : IWorkItemsSearch
{
    private readonly string _defaultProjectName;
    private readonly string _pat;
    private readonly Uri _uri;

    public WorkItemsSearch(Uri uri, string pat, string defaultProjectName)
    {
        _uri = uri;
        _pat = pat;
        _defaultProjectName = defaultProjectName;
    }

    public async Task<IList<WorkItem>> SearchWorkItemsAsync(SearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        // create query,
        // NOTE: Even if other columns are specified, only the ID & URL are available in the WorkItemReference
        var query = new StringBuilder("SELECT [Id] FROM WorkItems WHERE ");

        // project name
        query.Append($"[System.TeamProject] = '{criteria.ProjectName ?? _defaultProjectName}' ");

        // type
        if (criteria.WorkItemType != null)
            query.Append($" AND [Work Item Type] = '{criteria.WorkItemType}'");

        // Tags
        if (criteria.Tags != null)
            query.Append($" AND [System.Tags] CONTAINS '{criteria.Tags}'");


        /*
         AND [System.State] <> 'Closed'
           ORDER BY [State] ASC, [Changed Date] DESC
              $query =' "

         */

        // create a wiql object and build our query
        var wiql = new Wiql
        {
            Query = query.ToString()
        };
        // create instance of work item tracking http client
        using var httpClient = new WorkItemTrackingHttpClient(_uri, new VssBasicCredential(string.Empty, _pat));
        // execute the query to get the list of work items in the results
        var result = await httpClient.QueryByWiqlAsync(wiql, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var ids = result.WorkItems.Select(item => item.Id).ToArray();

        // some error handling
        if (ids.Length == 0) return Array.Empty<WorkItem>();

        // get work items for the ids found in query
        return await httpClient
            .GetWorkItemsAsync(ids, expand: WorkItemExpand.Relations, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkItem?> GetFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", _pat)));

        using var client = new HttpClient();
        client.BaseAddress = new Uri(url); //url of your organization
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return await client.GetFromJsonAsync<WorkItem>(url, cancellationToken);
    }
}