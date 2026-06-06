using AzureRagLibrarian.Configuration;
using OpenAI.VectorStores;

namespace AzureRagLibrarian.Services;

public interface IDocumentIndexService
{
    Task<VectorStore> EnsureVectorStoreAsync(RagOptions options);
}
