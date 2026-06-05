using Microsoft.Extensions.Configuration;

namespace AzureRagLibrarian.Configuration;

public sealed record RagOptions(
    Uri ProjectEndpoint,
    string? TenantId,
    string ModelDeploymentName,
    string DocumentPath,
    string VectorStoreName,
    string AgentName)
{
    public const string DefaultModelDeploymentName = "gpt-4o-mini";
    public const string DefaultDocumentPath = "samples/quiet-hours.txt";
    public const string DefaultVectorStoreName = "quiet-hours-vector-store";
    public const string DefaultAgentName = "quiet-hours-librarian";

    public static RagOptionsResult Load(IConfiguration configuration)
    {
        List<string> errors = [];

        string? endpointValue = configuration["AzureAI:ProjectEndpoint"]?.Trim();
        Uri? endpoint = null;

        if (string.IsNullOrWhiteSpace(endpointValue))
        {
            errors.Add("AzureAI:ProjectEndpoint is required.");
        }
        else if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out endpoint))
        {
            errors.Add("AzureAI:ProjectEndpoint must be an absolute URI.");
        }

        string documentPath = ResolveDocumentPath(configuration["Rag:DocumentPath"]?.Trim() ?? DefaultDocumentPath);

        if (!File.Exists(documentPath))
        {
            errors.Add($"Rag:DocumentPath does not exist: {documentPath}");
        }

        string? tenantId = configuration["AzureAI:TenantId"]?.Trim();

        RagOptions options = new(
            endpoint ?? new Uri("https://example.invalid"),
            string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
            configuration["AzureAI:ModelDeploymentName"]?.Trim() ?? DefaultModelDeploymentName,
            documentPath,
            configuration["Rag:VectorStoreName"]?.Trim() ?? DefaultVectorStoreName,
            configuration["Rag:AgentName"]?.Trim() ?? DefaultAgentName);

        return new RagOptionsResult(options, errors);
    }

    private static string ResolveDocumentPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        string workingDirectoryPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        if (File.Exists(workingDirectoryPath))
        {
            return workingDirectoryPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}

public sealed record RagOptionsResult(RagOptions Options, IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
