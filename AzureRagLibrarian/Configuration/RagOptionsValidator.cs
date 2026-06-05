using Microsoft.Extensions.Options;

namespace AzureRagLibrarian.Configuration;

public sealed class RagOptionsValidator : IValidateOptions<RagOptions>
{
    public ValidateOptionsResult Validate(string? name, RagOptions options)
    {
        List<string> errors = new();

        if (options.ProjectEndpoint is null)
        {
            errors.Add("AzureAI:ProjectEndpoint is required.");
        }
        else if (!options.ProjectEndpoint.IsAbsoluteUri)
        {
            errors.Add("AzureAI:ProjectEndpoint must be an absolute URI.");
        }

        string resolvedPath = ResolveDocumentPath(options.DocumentPath);
        if (!File.Exists(resolvedPath))
        {
            errors.Add($"Rag:DocumentPath does not exist: {resolvedPath}");
        }
        else
        {
            options.DocumentPath = resolvedPath;
        }

        if (string.IsNullOrWhiteSpace(options.ModelDeploymentName))
            errors.Add("AzureAI:ModelDeploymentName cannot be empty.");

        if (string.IsNullOrWhiteSpace(options.VectorStoreName))
            errors.Add("Rag:VectorStoreName cannot be empty.");

        if (string.IsNullOrWhiteSpace(options.AgentName))
            errors.Add("Rag:AgentName cannot be empty.");

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static string ResolveDocumentPath(string path)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        string workingDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        if (File.Exists(workingDir))
            return workingDir;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}