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
    public static RagOptionsResult Load(IConfiguration configuration)
    {
        return RagOptionsLoader.Load(configuration);
    }
}

public sealed record RagOptionsResult(RagOptions Options, IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class RagOptionsLoader
{
    public const string DefaultModelDeploymentName = "gpt-4o-mini";
    public const string DefaultDocumentPath = "samples/quiet-hours.txt";
    public const string DefaultVectorStoreName = "quiet-hours-vector-store";
    public const string DefaultAgentName = "quiet-hours-librarian";

    public static RagOptionsResult Load(IConfiguration configuration)
    {
        List<string> errors = [];

        string? endpointValue = Read(configuration, "AzureAI:ProjectEndpoint");
        Uri endpoint = new("https://example.invalid");

        if (string.IsNullOrWhiteSpace(endpointValue))
        {
            errors.Add("AzureAI:ProjectEndpoint is required.");
        }
        else if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? parsedEndpoint))
        {
            errors.Add("AzureAI:ProjectEndpoint must be an absolute URI.");
        }
        else
        {
            endpoint = parsedEndpoint;
        }

        string documentPath = ResolveDocumentPath(Read(configuration, "Rag:DocumentPath") ?? DefaultDocumentPath);

        if (!File.Exists(documentPath))
        {
            errors.Add($"Rag:DocumentPath does not exist: {documentPath}");
        }

        RagOptions options = new(
            endpoint,
            EmptyToNull(Read(configuration, "AzureAI:TenantId")),
            Read(configuration, "AzureAI:ModelDeploymentName") ?? DefaultModelDeploymentName,
            documentPath,
            Read(configuration, "Rag:VectorStoreName") ?? DefaultVectorStoreName,
            Read(configuration, "Rag:AgentName") ?? DefaultAgentName);

        ValidateRequired(options.ModelDeploymentName, "AzureAI:ModelDeploymentName", errors);
        ValidateRequired(options.VectorStoreName, "Rag:VectorStoreName", errors);
        ValidateRequired(options.AgentName, "Rag:AgentName", errors);

        return new RagOptionsResult(options, errors);
    }

    private static string? Read(IConfiguration configuration, string key)
    {
        string? value = configuration[key];

        if (value is not null)
        {
            return value.Trim();
        }

        string environmentKey = key.Replace(':', '_');
        value = Environment.GetEnvironmentVariable(environmentKey)
            ?? Environment.GetEnvironmentVariable(key.Replace(":", "__"));

        return value?.Trim();
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

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void ValidateRequired(string value, string key, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{key} cannot be empty.");
        }
    }
}
