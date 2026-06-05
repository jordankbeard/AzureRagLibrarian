using AzureRagLibrarian.Configuration;
using AzureRagLibrarian.Services;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = AppConfiguration.Build(args);
RagOptionsResult optionsResult = RagOptions.Load(configuration);

if (!optionsResult.IsValid)
{
    Console.WriteLine("Azure RAG Librarian is not configured yet.");
    Console.WriteLine();
    Console.WriteLine("Please fix the following configuration issue(s):");

    foreach (string error in optionsResult.Errors)
    {
        Console.WriteLine($"- {error}");
    }

    Console.WriteLine();
    Console.WriteLine("Copy appsettings.example.json to appsettings.json or use user secrets/environment variables.");
    Console.WriteLine("Required key: AzureAI:ProjectEndpoint");

    return 1;
}

RagOptions options = optionsResult.Options;

Console.WriteLine("Azure RAG Librarian");
Console.WriteLine($"Document: {options.DocumentPath}");
Console.WriteLine($"Agent: {options.AgentName}");
Console.WriteLine();

try
{
    AzureProjectClientFactory clientFactory = new();
    var projectClient = clientFactory.Create(options);

    DocumentIndexService documentIndexService = new(projectClient);
    var vectorStore = await documentIndexService.EnsureVectorStoreAsync(options);

    AgentProvisioningService agentProvisioningService = new(projectClient);
    await agentProvisioningService.EnsureAgentAsync(options, vectorStore.Id);

    RagChatSession chatSession = new(projectClient, options.AgentName);
    await chatSession.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"[Error] {ex.Message}");
    return 1;
}
