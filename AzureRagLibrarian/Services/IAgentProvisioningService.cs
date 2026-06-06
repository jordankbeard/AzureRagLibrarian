using AzureRagLibrarian.Configuration;

namespace AzureRagLibrarian.Services;

public interface IAgentProvisioningService
{
    Task EnsureAgentAsync(RagOptions options, string vectorStoreId);
}
