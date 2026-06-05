using Azure.AI.Projects;
using Azure.Identity;
using AzureRagLibrarian.Configuration;

namespace AzureRagLibrarian.Services;

public sealed class AzureProjectClientFactory
{
    public AIProjectClient Create(RagOptions options)
    {
        DefaultAzureCredentialOptions credentialOptions = new();

        if (!string.IsNullOrWhiteSpace(options.TenantId))
        {
            credentialOptions.TenantId = options.TenantId;
        }

        return new AIProjectClient(options.ProjectEndpoint, new DefaultAzureCredential(credentialOptions));
    }
}
