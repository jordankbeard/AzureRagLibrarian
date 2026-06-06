using Azure.AI.Projects;
using Azure.Identity;
using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureRagLibrarian.Services;

public sealed class AzureProjectClientFactory(IOptions<RagOptions> options, ILogger<AzureProjectClientFactory> logger) : IAzureProjectClientFactory
{
    public AIProjectClient Create()
    {
        logger.LogDebug("Connecting to Azure AI Foundry project");

        DefaultAzureCredentialOptions credentialOptions = new();

        if (!string.IsNullOrWhiteSpace(options.Value.TenantId))
        {
            credentialOptions.TenantId = options.Value.TenantId;
        }

        return new AIProjectClient(options.Value.ProjectEndpoint, new DefaultAzureCredential(credentialOptions));
    }
}
