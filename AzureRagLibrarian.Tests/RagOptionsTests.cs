using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AzureRagLibrarian.Tests;

public sealed class RagOptionsTests
{
    [Fact]
    public void Load_WhenRequiredEndpointIsMissing_ReturnsConfigurationError()
    {
        using TemporaryDocument document = TemporaryDocument.Create();
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Rag:DocumentPath"] = document.Path
        });

        RagOptionsResult result = RagOptions.Load(configuration);

        Assert.False(result.IsValid);
        Assert.Contains("AzureAI:ProjectEndpoint is required.", result.Errors);
    }

    [Fact]
    public void Load_WithMinimalValidConfiguration_UsesPortfolioFriendlyDefaults()
    {
        using TemporaryDocument document = TemporaryDocument.Create();
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AzureAI:ProjectEndpoint"] = "https://example.services.ai.azure.com/api/projects/demo",
            ["Rag:DocumentPath"] = document.Path
        });

        RagOptionsResult result = RagOptions.Load(configuration);

        Assert.True(result.IsValid);
        Assert.Equal("gpt-4o-mini", result.Options.ModelDeploymentName);
        Assert.Equal("quiet-hours-vector-store", result.Options.VectorStoreName);
        Assert.Equal("quiet-hours-librarian", result.Options.AgentName);
        Assert.Equal(System.IO.Path.GetFullPath(document.Path), result.Options.DocumentPath);
    }

    [Fact]
    public void Load_WhenDocumentDoesNotExist_ReturnsDocumentPathError()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AzureAI:ProjectEndpoint"] = "https://example.services.ai.azure.com/api/projects/demo",
            ["Rag:DocumentPath"] = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.txt")
        });

        RagOptionsResult result = RagOptions.Load(configuration);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.StartsWith("Rag:DocumentPath does not exist:", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_WhenNamesAreEmpty_ReturnsValidationErrors()
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

        RagOptionsResult result = RagOptions.Load(configuration);

        Assert.False(result.IsValid);
        Assert.Contains("AzureAI:ModelDeploymentName cannot be empty.", result.Errors);
        Assert.Contains("Rag:VectorStoreName cannot be empty.", result.Errors);
        Assert.Contains("Rag:AgentName cannot be empty.", result.Errors);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class TemporaryDocument : IDisposable
    {
        private TemporaryDocument(string path)
        {
            Path = path;
        }

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
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
