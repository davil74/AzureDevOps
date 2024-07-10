using Microsoft.VisualStudio.Services.Common;

namespace AzureDevOpsClient;

public class AzureDevOpsConnection
{
    private readonly string _defaultProjectName;
    private readonly string _personalAccessToken;
    private readonly Uri _uri;

    public AzureDevOpsConnection(string baseUrl, string orgName, string personalAccessToken, string defaultProjectName)
    {
        _uri = new Uri(new Uri(baseUrl), orgName);
        _personalAccessToken = personalAccessToken;
        _defaultProjectName = defaultProjectName;
    }

    public IWorkItemsSearch WorkItemsSearch()
    {
        return new WorkItemsSearch(_uri, _personalAccessToken, _defaultProjectName);
    }

    public IWorkItemCreator GetWorkItemCreator()
    {
        return new WorkItemCreator(_uri, new VssBasicCredential(string.Empty, _personalAccessToken),
            _defaultProjectName);
    }
}