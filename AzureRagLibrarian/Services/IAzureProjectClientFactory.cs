using Azure.AI.Projects;

namespace AzureRagLibrarian.Services;

public interface IAzureProjectClientFactory
{
    AIProjectClient Create();
}
