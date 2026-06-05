using Azure.AI.Projects;
using Azure.Identity;
using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureRagLibrarian.Services;

public sealed class AzureProjectClientFactory(ILogger<AzureProjectClientFactory> logger)
{
    public AIProjectClient Create(RagOptions options)
    {
        logger.LogInformation("Connecting to Azure AI Foundry project: {Endpoint}", options.ProjectEndpoint);

        DefaultAzureCredentialOptions credentialOptions = new();

        if (!string.IsNullOrWhiteSpace(options.TenantId))
        {
            credentialOptions.TenantId = options.TenantId;
        }

        return new AIProjectClient(options.ProjectEndpoint, new DefaultAzureCredential(credentialOptions));
    }
}
