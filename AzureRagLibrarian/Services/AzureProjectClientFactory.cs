using Azure.AI.Projects;
using Azure.Identity;
using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Options;

namespace AzureRagLibrarian.Services;

public sealed class AzureProjectClientFactory(IOptions<RagOptions> options)
{
    public AIProjectClient Create()
    {
        Console.WriteLine($"Connecting to Azure AI Foundry project.");

        DefaultAzureCredentialOptions credentialOptions = new();

        if (!string.IsNullOrWhiteSpace(options.Value.TenantId))
        {
            credentialOptions.TenantId = options.Value.TenantId;
        }

        return new AIProjectClient(options.Value.ProjectEndpoint, new DefaultAzureCredential(credentialOptions));
    }
}
