using Azure.AI.Projects;
using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Files;
using OpenAI.VectorStores;

namespace AzureRagLibrarian.Services;

public sealed class DocumentIndexService(AIProjectClient projectClient, ILogger<DocumentIndexService> logger) : IDocumentIndexService
{
    public async Task<VectorStore> EnsureVectorStoreAsync(RagOptions options)
    {
        OpenAIFileClient fileClient = projectClient.ProjectOpenAIClient.GetOpenAIFileClient();
        VectorStoreClient vectorClient = projectClient.ProjectOpenAIClient.GetVectorStoreClient();

        string targetFileName = Path.GetFileName(options.DocumentPath);

        OpenAIFile uploadedFile = await FindUploadedFileAsync(fileClient, targetFileName)
            ?? await UploadFileAsync(fileClient, options.DocumentPath, targetFileName);

        return await FindVectorStoreAsync(vectorClient, options.VectorStoreName)
            ?? await CreateVectorStoreAsync(vectorClient, options.VectorStoreName, uploadedFile.Id);
    }

    private async Task<OpenAIFile?> FindUploadedFileAsync(OpenAIFileClient fileClient, string targetFileName)
    {
        logger.LogDebug("Checking for existing file in Azure AI Foundry storage");

        var filesResult = await fileClient.GetFilesAsync();

        foreach (OpenAIFile file in filesResult.Value)
        {
            if (file.Filename == targetFileName && file.Purpose == FilePurpose.Assistants)
            {
                logger.LogDebug("Reusing file: {FileId}", file.Id);
                return file;
            }
        }

        return null;
    }

    private async Task<OpenAIFile> UploadFileAsync(OpenAIFileClient fileClient, string documentPath, string targetFileName)
    {
        logger.LogDebug("Uploading document");

        await using Stream uploadStream = File.OpenRead(documentPath);
        OpenAIFile uploadedFile = await fileClient.UploadFileAsync(uploadStream, targetFileName, FileUploadPurpose.Assistants);

        logger.LogDebug("Upload complete: {FileId}", uploadedFile.Id);
        return uploadedFile;
    }

    private async Task<VectorStore?> FindVectorStoreAsync(VectorStoreClient vectorClient, string vectorStoreName)
    {
        logger.LogDebug("Checking for existing vector store");

        await foreach (VectorStore store in vectorClient.GetVectorStoresAsync())
        {
            if (store.Name == vectorStoreName)
            {
                logger.LogDebug("Reusing vector store: {StoreId}", store.Id);
                return store;
            }
        }

        return null;
    }

    private async Task<VectorStore> CreateVectorStoreAsync(
        VectorStoreClient vectorClient,
        string vectorStoreName,
        string fileId)
    {
        logger.LogDebug("Creating vector store and indexing document");

        var vectorStoreResult = await vectorClient.CreateVectorStoreAsync(new VectorStoreCreationOptions
        {
            Name = vectorStoreName,
            FileIds = { fileId }
        });

        logger.LogDebug("Vector store ready: {StoreId}", vectorStoreResult.Value.Id);
        return vectorStoreResult.Value;
    }
}
