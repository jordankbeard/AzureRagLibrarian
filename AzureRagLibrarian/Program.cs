using AzureRagLibrarian.Configuration;
using AzureRagLibrarian.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

ServiceCollection services = new();

services.AddLogging(b => b.AddConsole());
services.AddSingleton<AzureProjectClientFactory>();
services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<AzureProjectClientFactory>();
    return factory.Create(options);
});
services.AddSingleton<DocumentIndexService>();
services.AddSingleton<AgentProvisioningService>();
services.AddSingleton(sp => new RagChatSession(
    sp.GetRequiredService<Azure.AI.Projects.AIProjectClient>(),
    options.AgentName,
    sp.GetRequiredService<ILogger<RagChatSession>>()));

await using ServiceProvider provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Azure RAG Librarian starting");
logger.LogInformation("Document: {DocumentPath}", options.DocumentPath);
logger.LogInformation("Agent: {AgentName}", options.AgentName);

try
{
    var documentIndexService = provider.GetRequiredService<DocumentIndexService>();
    var vectorStore = await documentIndexService.EnsureVectorStoreAsync(options);

    var agentProvisioningService = provider.GetRequiredService<AgentProvisioningService>();
    await agentProvisioningService.EnsureAgentAsync(options, vectorStore.Id);

    var chatSession = provider.GetRequiredService<RagChatSession>();
    await chatSession.RunAsync();

    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Unhandled error");
    return 1;
}
