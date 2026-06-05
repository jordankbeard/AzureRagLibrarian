using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace AzureRagLibrarian.Tests;

public sealed class RagOptionsTests
{
    [Fact]
    public void Validate_WhenRequiredEndpointIsMissing_ReturnsConfigurationError()
    {
        using TemporaryDocument document = TemporaryDocument.Create();
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Rag:DocumentPath"] = document.Path
        });

        RagOptions options = Bind(configuration);
        RagOptionsValidator validator = new();

        ValidateOptionsResult result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("AzureAI:ProjectEndpoint is required.", result.Failures);
    }

    [Fact]
    public void Validate_WithMinimalValidConfiguration_UsesDefaults()
    {
        using TemporaryDocument document = TemporaryDocument.Create();
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AzureAI:ProjectEndpoint"] = "https://example.services.ai.azure.com/api/projects/demo",
            ["Rag:DocumentPath"] = document.Path
        });

        RagOptions options = Bind(configuration);
        RagOptionsValidator validator = new();

        ValidateOptionsResult result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
        Assert.Equal(RagOptions.DefaultModelDeploymentName, options.ModelDeploymentName);
        Assert.Equal(RagOptions.DefaultVectorStoreName, options.VectorStoreName);
        Assert.Equal(RagOptions.DefaultAgentName, options.AgentName);
        Assert.Equal(Path.GetFullPath(document.Path), options.DocumentPath);
    }

    [Fact]
    public void Validate_WhenDocumentDoesNotExist_ReturnsDocumentPathError()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AzureAI:ProjectEndpoint"] = "https://example.services.ai.azure.com/api/projects/demo",
            ["Rag:DocumentPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.txt")
        });

        RagOptions options = Bind(configuration);
        RagOptionsValidator validator = new();

        ValidateOptionsResult result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, e => e.StartsWith("Rag:DocumentPath does not exist:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenNamesAreEmpty_ReturnsValidationErrors()
    {
        using TemporaryDocument document = TemporaryDocument.Create();
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AzureAI:ProjectEndpoint"] = "https://example.services.ai.azure.com/api/projects/demo",
            ["AzureAI:ModelDeploymentName"] = " ",
            ["Rag:DocumentPath"] = document.Path,
            ["Rag:VectorStoreName"] = " ",
            ["Rag:AgentName"] = " "
        });

        RagOptions options = Bind(configuration);
        RagOptionsValidator validator = new();

        ValidateOptionsResult result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("AzureAI:ModelDeploymentName cannot be empty.", result.Failures);
        Assert.Contains("Rag:VectorStoreName cannot be empty.", result.Failures);
        Assert.Contains("Rag:AgentName cannot be empty.", result.Failures);
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private static RagOptions Bind(IConfiguration config)
    {
        var options = new RagOptions();
        config.GetSection("Rag").Bind(options);

        // AzureAI values are applied via PostConfigure in the real app
        options.ProjectEndpoint = config.GetValue<Uri>("AzureAI:ProjectEndpoint");
        options.TenantId = config["AzureAI:TenantId"];
        options.ModelDeploymentName =
            config["AzureAI:ModelDeploymentName"] ?? RagOptions.DefaultModelDeploymentName;

        return options;
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class TemporaryDocument : IDisposable
    {
        private TemporaryDocument(string path) => Path = path;

        public string Path { get; }

        public static TemporaryDocument Create()
        {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            string path = System.IO.Path.Combine(directory, "sample.txt");
            File.WriteAllText(path, "A small RAG sample document.");

            return new TemporaryDocument(path);
        }

        public void Dispose()
        {
            string? directory = System.IO.Path.GetDirectoryName(Path);
            if (directory is not null && Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
